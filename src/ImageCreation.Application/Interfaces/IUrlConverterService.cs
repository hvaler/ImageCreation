using System.Threading.Tasks;

namespace ImageCreation.Application.Interfaces
{
   public interface IUrlConverterService
   {
      Task<string?> ConvertUrlToBase64Async(string imageUrl);
   }
}