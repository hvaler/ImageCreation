using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Interfaces
{
    public interface IOpenAiService
    {
        Task<string?> GenerateImageAsync(string description);
    }
}