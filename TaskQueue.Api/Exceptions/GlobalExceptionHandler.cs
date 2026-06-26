using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace TaskQueue.Api.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var errorResponse = new
            {
                error = "Ocorreu um erro inesperado.",
                detail = exception.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse), cancellationToken);

            return true; // true = exceção foi tratada, não propaga mais
        }
    }
}