using System.Text.Json.Serialization;
using TaskQueue.Api.Endpoints;
using TaskQueue.Api.Exceptions;
using TaskQueue.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "TaskQueue API",
        Version = "v1",
        Description = "Serviço de processamento de tarefas em background."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddInfrastructure(
    mongoConnectionString: builder.Configuration["Mongo:ConnectionString"]!,
    mongoDatabaseName: builder.Configuration["Mongo:Database"]!,
    rabbitHostName: builder.Configuration["Rabbit:Host"]!
);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    application = "TaskQueue API",
    version = "v1",
    description = "Serviço de processamento de tarefas em background.",
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        jobs = "/jobs"
    }
}))
.WithTags("Info")
.WithOpenApi(operation =>
{
    operation.Summary = "Informações da API";
    operation.Description = "Retorna informações gerais sobre a aplicação e os endpoints disponíveis.";
    return operation;
});

app.MapHealthChecks("/health");
app.MapJobEndpoints();

app.Run();

// expõe a classe Program pro WebApplicationFactory usar nos testes de integração
public partial class Program { }