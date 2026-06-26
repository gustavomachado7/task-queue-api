using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskQueue.Core.Enums;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Worker
{
    public class JobProcessor : BackgroundService
    {
        private readonly IJobRepository _repository;
        private readonly ILogger<JobProcessor> _logger;
        private readonly string _rabbitHost;

        private const int MaxAttempts = 3;
        private const string QueueName = "taskqueue.jobs";
        internal const int MaxConcurrentMessages = 10; // quantidade de instâncias paralelas — usado no Program.cs
        private const int MaxMessagesPerFetch = 10;    // quantas mensagens cada worker pode pegar da fila por vez

        private IConnection? _connection;
        private IChannel? _channel;

        public JobProcessor(IJobRepository repository, ILogger<JobProcessor> logger, IConfiguration config)
        {
            _repository = repository;
            _logger = logger;
            _rabbitHost = config["Rabbit:Host"]!;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory { HostName = _rabbitHost };
            var connected = false;
            var attempts = 0;

            while (!connected && attempts < 10)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                    connected = true;
                }
                catch (Exception)
                {
                    attempts++;
                    _logger.LogWarning("RabbitMQ não disponível. Tentativa {Tentativa}/10. Aguardando...", attempts);
                    await Task.Delay(3000, cancellationToken);
                }
            }

            if (!connected)
                throw new Exception("Não foi possível conectar ao RabbitMQ após 10 tentativas.");

            await _channel!.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken
            );

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: MaxMessagesPerFetch, global: false, cancellationToken: cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, delivery) =>
            {
                var rawMessage = Encoding.UTF8.GetString(delivery.Body.ToArray());
                var job = JsonSerializer.Deserialize<JobRecord>(rawMessage);

                if (job is null)
                {
                    await _channel!.BasicAckAsync(delivery.DeliveryTag, false);
                    return;
                }

                // Task.Run dispara o processamento em uma thread separada
                // sem isso o consumer ficaria bloqueado esperando cada tarefa terminar
                _ = Task.Run(() => HandleJobAsync(job, delivery.DeliveryTag));
            };

            await _channel!.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleJobAsync(JobRecord job, ulong deliveryTag)
        {
            try
            {
                await Task.Delay(10000); // fica 10 segundos em Created

                await _repository.UpdateStatusAsync(job.TrackingId, JobStatus.Pending);
                _logger.LogInformation("Tarefa {Id} aguardando processamento.", job.TrackingId);

                await Task.Delay(10000); // fica 10 segundos em Pending

                await _repository.IncrementAttemptAsync(job.TrackingId);
                await _repository.UpdateStatusAsync(job.TrackingId, JobStatus.Processing);
                _logger.LogInformation("Tarefa {Id} em processamento | Categoria: {Category}", job.TrackingId, job.Category);

                await SimulateProcessingAsync(job);

                await _repository.UpdateStatusAsync(job.TrackingId, JobStatus.Completed);
                _logger.LogInformation("Tarefa {Id} finalizada com sucesso.", job.TrackingId);

                await _channel!.BasicAckAsync(deliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar tarefa {Id}.", job.TrackingId);

                var currentJob = await _repository.FindByIdAsync(job.TrackingId);
                var totalAttempts = currentJob?.AttemptCount ?? 0;

                if (totalAttempts >= MaxAttempts)
                {
                    await _repository.UpdateStatusAsync(job.TrackingId, JobStatus.Failed, ex.Message);
                    _logger.LogWarning("Tarefa {Id} excedeu o limite de tentativas. Marcada como Failed.", job.TrackingId);
                    await _channel!.BasicAckAsync(deliveryTag, false);
                }
                else
                {
                    await _repository.UpdateStatusAsync(job.TrackingId, JobStatus.Pending);
                    await _channel!.BasicNackAsync(deliveryTag, false, requeue: true);
                }
            }
        }

        private async Task SimulateProcessingAsync(JobRecord job)
        {
            await Task.Delay(10000);

            if (job.Category == "ForceError")
                throw new InvalidOperationException("Erro simulado para testes.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel is not null)
                await _channel.CloseAsync();

            if (_connection is not null)
                await _connection.CloseAsync();

            await base.StopAsync(cancellationToken);
        }
    }
}