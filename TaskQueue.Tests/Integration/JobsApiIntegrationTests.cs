using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TaskQueue.Api.Contracts;
using TaskQueue.Core.Enums;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Tests.Integration
{
    public class JobsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public JobsApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            var repositoryMock = new Mock<IJobRepository>();
            var publisherMock = new Mock<IJobPublisher>();

            // job fixo que o mock retorna nas consultas
            var job = new JobRecord
            {
                Category = "EnviarEmail",
                Payload = "{\"destinatario\":\"teste@email.com\"}",
                CurrentStatus = JobStatus.Created
            };

            repositoryMock.Setup(r => r.InsertAsync(It.IsAny<JobRecord>()))
                .ReturnsAsync((JobRecord j) => j);

            repositoryMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(job);

            publisherMock.Setup(p => p.PublishAsync(It.IsAny<JobRecord>()))
                .Returns(Task.CompletedTask);

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {

                    var toRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(IJobRepository) ||
                            d.ServiceType == typeof(IJobPublisher) ||
                            d.ServiceType == typeof(IMongoClient))
                        .ToList();

                    foreach (var descriptor in toRemove)
                        services.Remove(descriptor);

                    // substitui as dependências reais pelas versões mockadas
                    services.AddSingleton(repositoryMock.Object);
                    services.AddSingleton(publisherMock.Object);
                });
            });
        }

        [Fact]
        public async Task Post_CriarTarefa_DeveRetornar201()
        {
            var client = _factory.CreateClient();

            var request = new CreateJobRequest
            {
                Category = "EnviarEmail",
                Payload = "{\"destinatario\":\"teste@email.com\"}"
            };

            var response = await client.PostAsJsonAsync("/jobs", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Get_ConsultarTarefa_DeveRetornar200()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/jobs/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}