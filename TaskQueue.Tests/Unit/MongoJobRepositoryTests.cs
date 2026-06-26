using Moq;
using MongoDB.Driver;
using TaskQueue.Core.Models;
using TaskQueue.Infrastructure.Persistence;

namespace TaskQueue.Tests.Unit
{
    public class MongoJobRepositoryTests
    {
        [Fact]
        public async Task FindByIdAsync_IdInexistente_DeveRetornarNull()
        {
            var collectionMock = new Mock<IMongoCollection<JobRecord>>();
            var cursorMock = new Mock<IAsyncCursor<JobRecord>>();

            cursorMock.Setup(c => c.Current).Returns(new List<JobRecord>());
            cursorMock.SetupSequence(c => c.MoveNextAsync(default)).ReturnsAsync(false);

            collectionMock.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<JobRecord>>(),
                It.IsAny<FindOptions<JobRecord, JobRecord>>(),
                default))
                .ReturnsAsync(cursorMock.Object);

            var clientMock = new Mock<IMongoClient>();
            var dbMock = new Mock<IMongoDatabase>();

            dbMock.Setup(d => d.GetCollection<JobRecord>("jobs", null)).Returns(collectionMock.Object);
            clientMock.Setup(c => c.GetDatabase("taskqueue", null)).Returns(dbMock.Object);

            var repository = new MongoJobRepository(clientMock.Object, "taskqueue");
            var result = await repository.FindByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }
    }
}