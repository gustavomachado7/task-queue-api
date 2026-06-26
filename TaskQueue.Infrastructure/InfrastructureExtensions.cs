using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TaskQueue.Core.Interfaces;
using TaskQueue.Infrastructure.Messaging;
using TaskQueue.Infrastructure.Persistence;

namespace TaskQueue.Infrastructure
{

    // Esse arquivo centraliza o registro de todas as dependências da Infrastructure.
    // Tanto a Api quanto o Worker vão chamar esse método (evita repetição de código).
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            string mongoConnectionString,
            string mongoDatabaseName,
            string rabbitHostName)
        {
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));

            services.AddSingleton<IJobRepository>(provider =>
            {
                var mongoClient = provider.GetRequiredService<IMongoClient>();
                return new MongoJobRepository(mongoClient, mongoDatabaseName);
            });

            services.AddSingleton<IJobPublisher>(_ => new RabbitJobPublisher(rabbitHostName));

            return services;
        }
    }
}