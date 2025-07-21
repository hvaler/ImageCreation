using System.Threading.Tasks;

namespace ImageCreation.Application.Interfaces
{
    public interface ICommandHandler<TCommand, TResult>
    {
        Task<TResult> HandleAsync(TCommand command);
    }
}