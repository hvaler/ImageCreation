using System.Threading.Tasks;
using ImageCreation.Domain.Events;

namespace ImageCreation.Application.Interfaces
{
    public interface IEventStore
    {
        Task PublishAsync(IDomainEvent domainEvent);
    }
}