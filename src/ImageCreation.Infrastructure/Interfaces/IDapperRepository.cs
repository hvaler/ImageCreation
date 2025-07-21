using System.Threading.Tasks;
using ImageCreation.Domain.Entities;

namespace ImageCreation.Infrastructure.Interfaces
{
    public interface IDapperRepository
    {
        Task InsertAsync(ImageRecord record);
        Task<ImageRecord?> GetByIdAsync(string id);

      Task InsertClassifiedImageAsync(ClassifiedImageRecord record);
      Task<ClassifiedImageRecord?> GetClassifiedImageByIdAsync(string id);
   }
}