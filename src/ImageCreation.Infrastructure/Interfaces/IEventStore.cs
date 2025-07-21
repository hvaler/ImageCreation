using System.Threading.Tasks;
using ImageCreation.Domain.Events;

namespace ImageCreation.Infrastructure.Interfaces
{
    public interface IEventStore
    {
        Task PublishAsync(IDomainEvent domainEvent);
    }
}