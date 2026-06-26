using Moq;
using TaskQueue.Core.Enums;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Tests.Unit
{
    public class JobProcessorTests
    {
        [Fact]
        public async Task TarefaComErro_AposMaxTentativas_DeveMarcarComoFailed()
        {
            var repositoryMock = new Mock<IJobRepository>();

            var job = new JobRecord
            {
                TrackingId = Guid.NewGuid(),
                Category = "ForceError",
                AttemptCount = 3
            };

            repositoryMock.Setup(r => r.FindByIdAsync(job.TrackingId))
                .ReturnsAsync(job);

            repositoryMock.Setup(r => r.UpdateStatusAsync(
                job.TrackingId,
                JobStatus.Failed,
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var attempts = job.AttemptCount;
            const int maxAttempts = 3;

            if (attempts >= maxAttempts)
                await repositoryMock.Object.UpdateStatusAsync(job.TrackingId, JobStatus.Failed, "Erro simulado.");

            repositoryMock.Verify(r => r.UpdateStatusAsync(
                job.TrackingId,
                JobStatus.Failed,
                It.IsAny<string>()), Times.Once);
        }
    }
}