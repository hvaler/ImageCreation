using ImageCreation.Application.Interfaces; // Para IOpenAiService y IOpenAiServiceFactory

using Microsoft.Extensions.Logging; // Para los loggers de los servicios

namespace ImageCreation.Infrastructure.Services
{
   public class OpenAiServiceFactory : IOpenAiServiceFactory
   {
      // Inyectamos las implementaciones concretas aquí.
      // Podríamos inyectar IServiceProvider y resolverlas lazily,
      // pero para dos servicios, inyectarlos directamente es más simple.
      private readonly PublicOpenAiService _publicOpenAiService;
      private readonly AzureOpenAiService _azureOpenAiService;

      public OpenAiServiceFactory(
          PublicOpenAiService publicOpenAiService,
          AzureOpenAiService azureOpenAiService)
      {
         _publicOpenAiService = publicOpenAiService;
         _azureOpenAiService = azureOpenAiService;
      }

      public IOpenAiService GetService(string platform)
      {
         return platform.ToLowerInvariant() switch
         {
            "azure" => _azureOpenAiService,
            "public" => _publicOpenAiService, 
            _ => _publicOpenAiService, // Fallback si la plataforma no es reconocida
         };
      }
   }
}