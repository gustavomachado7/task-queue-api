using TaskQueue.Core.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskQueue.Core.Models
{
    // representa uma tarefa salva no banco de dados
    public class JobRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid TrackingId { get; set; } = Guid.NewGuid();

        public string Category { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public JobStatus CurrentStatus { get; set; } = JobStatus.Created;

        public int AttemptCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdatedAt { get; set; }

        public string? FailureReason { get; set; }
    }
}