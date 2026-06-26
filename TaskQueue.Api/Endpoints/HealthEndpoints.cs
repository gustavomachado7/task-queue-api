namespace TaskQueue.Api.Endpoints
{
    public static class HealthEndpoints
    {
        public static void MapHealthEndpoints(this WebApplication app)
        {
            app.MapGet("/health", () =>
            {
                return Results.Ok(new
                {
                    status = "Healthy",
                    service = "TaskQueue.Api",
                    timestamp = DateTime.UtcNow
                });
            })
            .WithTags("Health")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Health check";
                operation.Description = "Verifica se a API está disponível.";
                return operation;
            })
            .Produces(200);
        }
    }
}