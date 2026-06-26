using TaskQueue.Core.Models;

namespace TaskQueue.Core.Interfaces
{
    public interface IJobPublisher
    {
        Task PublishAsync(JobRecord job);
    }
}