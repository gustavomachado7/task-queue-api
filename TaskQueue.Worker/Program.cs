using TaskQueue.Infrastructure;
using TaskQueue.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(
    mongoConnectionString: builder.Configuration["Mongo:ConnectionString"]!,
    mongoDatabaseName: builder.Configuration["Mongo:Database"]!,
    rabbitHostName: builder.Configuration["Rabbit:Host"]!
);

for (int i = 0; i < JobProcessor.MaxConcurrentMessages; i++)
    builder.Services.AddHostedService<JobProcessor>();

var host = builder.Build();
await host.RunAsync();