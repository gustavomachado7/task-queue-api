using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using TaskQueue.Api.Contracts;
using TaskQueue.Api.Endpoints;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Tests.Unit
{
    public class JobEndpointsTests
    {
        private readonly Mock<IJobRepository> _repositoryMock = new();
        private readonly Mock<IJobPublisher> _publisherMock = new();

        [Fact]
        public async Task CriarTarefa_CategoryVazia_DeveRetornar400()
        {
            var request = new CreateJobRequest { Category = "", Payload = "{\"key\":\"value\"}" };

            var result = await JobEndpointsHelper.CriarTarefa(request, _repositoryMock.Object, _publisherMock.Object);

            Assert.IsType<BadRequest<ErrorResponse>>(result);
        }

        [Fact]
        public async Task CriarTarefa_PayloadVazio_DeveRetornar400()
        {
            var request = new CreateJobRequest { Category = "EnviarEmail", Payload = "" };

            var result = await JobEndpointsHelper.CriarTarefa(request, _repositoryMock.Object, _publisherMock.Object);

            Assert.IsType<BadRequest<ErrorResponse>>(result);
        }

        [Fact]
        public async Task CriarTarefa_DadosValidos_DeveRetornar201()
        {
            var request = new CreateJobRequest { Category = "EnviarEmail", Payload = "{\"key\":\"value\"}" };

            _repositoryMock.Setup(r => r.InsertAsync(It.IsAny<JobRecord>()))
                .ReturnsAsync((JobRecord j) => j);

            _publisherMock.Setup(p => p.PublishAsync(It.IsAny<JobRecord>()))
                .Returns(Task.CompletedTask);

            var result = await JobEndpointsHelper.CriarTarefa(request, _repositoryMock.Object, _publisherMock.Object);

            var statusCode = (result as IStatusCodeHttpResult)?.StatusCode;
            Assert.Equal(201, statusCode);
        }

        [Fact]
        public async Task ListarTarefas_StatusInvalido_DeveRetornar400()
        {
            var result = await JobEndpointsHelper.ListarTarefas("StatusInexistente", _repositoryMock.Object);

            Assert.IsType<BadRequest<ErrorResponse>>(result);
        }

        [Fact]
        public async Task ListarTarefas_SemFiltro_DeveRetornar200()
        {
            _repositoryMock.Setup(r => r.ListAllAsync(null))
                .ReturnsAsync(new List<JobRecord>());

            var result = await JobEndpointsHelper.ListarTarefas(null, _repositoryMock.Object);

            var statusCode = (result as IStatusCodeHttpResult)?.StatusCode;
            Assert.Equal(200, statusCode);
        }
    }
}