using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Interfaces
{
    public interface ICacheService
    {
        Task SetAsync(string key, string value);
        Task<string?> GetAsync(string key);
    }
}