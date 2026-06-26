using TaskQueue.Api.Contracts;
using TaskQueue.Core.Enums;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Api.Endpoints
{
    // classe estática com a lógica dos endpoints extraída para facilitar os testes unitários
    // endpoints Minimal API não são testáveis diretamente, então a lógica fica aqui
    public static class JobEndpointsHelper
    {
        public static async Task<IResult> CriarTarefa(
            CreateJobRequest request,
            IJobRepository repository,
            IJobPublisher publisher)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
                return Results.BadRequest(new ErrorResponse("O campo 'category' é obrigatório."));

            if (request.Category.Length < 5 || request.Category.Length > 100)
                return Results.BadRequest(new ErrorResponse("Category deve ter entre 5 e 100 caracteres."));

            if (string.IsNullOrWhiteSpace(request.Payload))
                return Results.BadRequest(new ErrorResponse("O campo 'payload' é obrigatório."));

            if (request.Payload.Length < 7 || request.Payload.Length > 2000)
                return Results.BadRequest(new ErrorResponse("Payload deve ter entre 7 e 2000 caracteres."));

            var newJob = new JobRecord
            {
                Category = request.Category.Trim(),
                Payload = request.Payload.Trim()
            };

            await repository.InsertAsync(newJob);
            await publisher.PublishAsync(newJob);

            return Results.Created($"/jobs/{newJob.TrackingId}", new
            {
                newJob.TrackingId,
                newJob.Category,
                newJob.CurrentStatus,
                newJob.CreatedAt
            });
        }

        public static async Task<IResult> ListarTarefas(string? status, IJobRepository repository)
        {
            if (status is not null && !Enum.TryParse<JobStatus>(status, ignoreCase: true, out _))
                return Results.BadRequest(new ErrorResponse($"Status '{status}' inválido. Use: Created, Pending, Processing, Completed, Failed."));

            var jobs = await repository.ListAllAsync(status);

            if (!jobs.Any())
                return Results.Ok(new { message = "Nenhuma tarefa encontrada.", total = 0 });

            return Results.Ok(new
            {
                total = jobs.Count,
                items = jobs.Select(j => new
                {
                    j.TrackingId,
                    j.Category,
                    j.CurrentStatus,
                    j.AttemptCount,
                    j.CreatedAt,
                    j.LastUpdatedAt,
                    j.FailureReason
                })
            });
        }
    }
}