using System.Threading.Tasks;

namespace ImageCreation.Application.Interfaces
{
    public interface ICacheService
    {
        Task SetAsync(string key, string value);
        Task<string?> GetAsync(string key);
    }
}