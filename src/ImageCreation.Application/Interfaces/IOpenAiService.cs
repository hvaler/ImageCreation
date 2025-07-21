using System.Threading.Tasks;

namespace ImageCreation.Application.Interfaces
{
    public interface IOpenAiService
    {
        Task<string?> GenerateImageAsync(string description);
    }
}