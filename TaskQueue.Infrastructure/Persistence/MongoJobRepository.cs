using MongoDB.Driver;
using TaskQueue.Core.Enums;
using TaskQueue.Core.Interfaces;
using TaskQueue.Core.Models;

namespace TaskQueue.Infrastructure.Persistence
{
    public class MongoJobRepository : IJobRepository
    {
        private readonly IMongoCollection<JobRecord> _collection;

        public MongoJobRepository(IMongoClient client, string databaseName)
        {
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<JobRecord>("jobs");
        }

        public async Task<JobRecord> InsertAsync(JobRecord job)
        {
            await _collection.InsertOneAsync(job);
            return job;
        }

        public async Task<JobRecord?> FindByIdAsync(Guid trackingId)
        {
            return await _collection
                .Find(x => x.TrackingId == trackingId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<JobRecord>> ListAllAsync(string? status = null)
        {
            if (string.IsNullOrWhiteSpace(status))
                return await _collection.Find(_ => true).ToListAsync();

            if (!Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsedStatus))
                return new List<JobRecord>();

            return await _collection
                .Find(x => x.CurrentStatus == parsedStatus)
                .ToListAsync();
        }
        public async Task<JobRecord?> ClaimNextCreatedAsync()
        {
            var filter = Builders<JobRecord>.Filter.Eq(x => x.CurrentStatus, JobStatus.Created);
            var update = Builders<JobRecord>.Update
                .Set(x => x.CurrentStatus, JobStatus.Pending)
                .Set(x => x.LastUpdatedAt, DateTime.UtcNow);

            // FindOneAndUpdate garante que apenas um worker pega essa tarefa por vez
            return await _collection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<JobRecord> { ReturnDocument = ReturnDocument.After }
            );
        }

        public async Task<JobRecord?> ClaimNextPendingAsync()
        {
            var filter = Builders<JobRecord>.Filter.Eq(x => x.CurrentStatus, JobStatus.Pending);
            var update = Builders<JobRecord>.Update
                .Set(x => x.CurrentStatus, JobStatus.Processing)
                .Set(x => x.LastUpdatedAt, DateTime.UtcNow);


            // Busca e atualiza o documento em uma operação atômica no MongoDB, ou seja,
            // garante que 2 workers nunca peguem a mesma tarefa ao mesmo tempo.
            // É o controle de concorrência do sistema.
            return await _collection.FindOneAndUpdateAsync(
                filter,
                update,
                new FindOneAndUpdateOptions<JobRecord> { ReturnDocument = ReturnDocument.After }
            );
        }

        public async Task UpdateStatusAsync(Guid trackingId, JobStatus status, string? failureReason = null)
        {
            var update = Builders<JobRecord>.Update
                .Set(x => x.CurrentStatus, status)
                .Set(x => x.LastUpdatedAt, DateTime.UtcNow)
                .Set(x => x.FailureReason, failureReason);

            await _collection.UpdateOneAsync(x => x.TrackingId == trackingId, update);
        }

        public async Task IncrementAttemptAsync(Guid trackingId)
        {
            var update = Builders<JobRecord>.Update
                .Inc(x => x.AttemptCount, 1)
                .Set(x => x.LastUpdatedAt, DateTime.UtcNow);

            await _collection.UpdateOneAsync(x => x.TrackingId == trackingId, update);
        }
    }
}