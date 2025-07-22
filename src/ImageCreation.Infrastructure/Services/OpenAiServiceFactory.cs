// ImageCreation.Infrastructure.Services/OpenAiServiceFactory.cs
using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.DependencyInjection; // ¡NUEVO! Para IServiceProvider

using System; // Para ArgumentNullException

namespace ImageCreation.Infrastructure.Services
{
   public class OpenAiServiceFactory : IOpenAiServiceFactory
   {
      // Quitamos las inyecciones directas de los servicios específicos.
      private readonly IServiceProvider _serviceProvider; // ¡CAMBIO CLAVE!

      public OpenAiServiceFactory(IServiceProvider serviceProvider) // ¡CAMBIO CLAVE!
      {
         _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
      }

      public IOpenAiService GetService(string platform)
      {
         // Resolvemos el servicio de forma perezosa, solo cuando se solicita.
         return platform.ToLowerInvariant() switch
         {
            "azure" => _serviceProvider.GetRequiredService<AzureOpenAiService>(),
            "public" => _serviceProvider.GetRequiredService<PublicOpenAiService>(),
            "stability" => _serviceProvider.GetRequiredService<StabilityAIService>(),
            "google" => _serviceProvider.GetRequiredService<GoogleGenerativeAIService>(),
            "huggingface" => _serviceProvider.GetRequiredService<HuggingFaceService>(),
            _ => _serviceProvider.GetRequiredService<PublicOpenAiService>(), // Fallback si la plataforma no es reconocida
         };
      }
   }
}