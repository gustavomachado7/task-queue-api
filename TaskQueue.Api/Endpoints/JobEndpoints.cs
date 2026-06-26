using TaskQueue.Api.Contracts;
using TaskQueue.Core.Enums;
using TaskQueue.Core.Interfaces;

namespace TaskQueue.Api.Endpoints
{
    public static class JobEndpoints
    {
        public static void MapJobEndpoints(this WebApplication app)
        {
            app.MapPost("/jobs", async (CreateJobRequest request, IJobRepository repository, IJobPublisher publisher) =>
                await JobEndpointsHelper.CriarTarefa(request, repository, publisher))
                .WithTags("Jobs")
                .WithOpenApi(operation =>
                {
                    operation.Summary = "Criar nova tarefa";
                    operation.Description = "Cria uma nova tarefa e a envia para processamento em background via fila.";
                    return operation;
                })
                .Produces(201)
                .Produces(400);

            app.MapGet("/jobs/{trackingId}", async (string trackingId, IJobRepository repository) =>
            {
                if (!Guid.TryParse(trackingId, out var parsedId))
                    return Results.BadRequest(new ErrorResponse($"O id '{trackingId}' não é um formato válido de GUID."));

                var foundJob = await repository.FindByIdAsync(parsedId);

                if (foundJob is null)
                    return Results.NotFound(new ErrorResponse($"Nenhuma tarefa encontrada com o id '{parsedId}'."));

                return Results.Ok(new
                {
                    foundJob.TrackingId,
                    foundJob.Category,
                    foundJob.CurrentStatus,
                    foundJob.AttemptCount,
                    foundJob.CreatedAt,
                    foundJob.LastUpdatedAt,
                    foundJob.FailureReason
                });
            })
            .WithTags("Jobs")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Consultar status da tarefa";
                operation.Description = "Informe o trackingId (GUID) retornado na criação da tarefa.";
                operation.Parameters[0].Description = "GUID de rastreamento retornado na criação da tarefa.";
                return operation;
            })
            .Produces(200)
            .Produces(400)
            .Produces(404);

            app.MapGet("/jobs", async (string? status, IJobRepository repository) =>
                await JobEndpointsHelper.ListarTarefas(status, repository))
            .WithTags("Jobs")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Listar tarefas";
                operation.Description = "Retorna todas as tarefas. Filtre por status com o parâmetro opcional: Created, Pending, Processing, Completed, Failed.";
                return operation;
            })
            .Produces(200)
            .Produces(400);
        }
    }
}