using System.Threading.Tasks;

namespace ImageCreation.Application.Interfaces
{
   public interface IQueryHandler<TQuery, TResult>
       where TQuery : IQuery
   {
      Task<TResult> HandleAsync(TQuery query);
   }

   // Interfaz de marcador para todas las queries
   public interface IQuery { }
}