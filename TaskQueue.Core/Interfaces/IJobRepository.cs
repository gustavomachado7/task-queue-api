using TaskQueue.Core.Enums;
using TaskQueue.Core.Models;

namespace TaskQueue.Core.Interfaces
{
    public interface IJobRepository
    {
        Task<JobRecord> InsertAsync(JobRecord job);
        Task<JobRecord?> FindByIdAsync(Guid trackingId);
        Task<List<JobRecord>> ListAllAsync(string? status = null);
        Task<JobRecord?> ClaimNextCreatedAsync();
        Task<JobRecord?> ClaimNextPendingAsync();
        Task UpdateStatusAsync(Guid trackingId, JobStatus status, string? failureReason = null);
        Task IncrementAttemptAsync(Guid trackingId);
    }
}