using Azure; // Para AzureKeyCredential (parte del SDK de Azure Core)
using Azure.AI.OpenAI; // Para OpenAIClient, ImageClient, ImageGenerationOptions, ImageSize, ImageQuality, ImageResponseFormat, GeneratedImageCollection

using ImageCreation.Application.Interfaces;
using ImageCreation.Infrastructure.Interfaces;

using Microsoft.Extensions.Configuration; 
using Microsoft.Extensions.Logging;

using OpenAI.Images;

using System; 
using System.Threading.Tasks; 

namespace ImageCreation.Infrastructure.Services
{
   
   public class AzureOpenAiService : IOpenAiService 
   {
     
      private readonly AzureOpenAIClient _openAIClient;
      private readonly string _dalleModelName;
      private readonly ILogger<AzureOpenAiService> _logger; 

      /// <summary>
      /// Constructor para inicializar el servicio.
      /// Recibe IConfiguration y ILogger a través de inyección de dependencias.
      /// </summary>
      /// <param name="configuration">Instancia de IConfiguration para acceder a los ajustes de la aplicación.</param>
      /// <param name="logger">Instancia de ILogger para registrar mensajes.</param> // ¡NUEVO! Parámetro del constructor
      public AzureOpenAiService(IConfiguration configuration, ILogger<AzureOpenAiService> logger)
      {
         _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Asignar el logger

         // Obtener las credenciales y el nombre del despliegue desde la configuración.
         string? endpoint = configuration["AzureOpenAI:Endpoint"];
         string? apiKey = configuration["AzureOpenAI:ApiKey"];
         _dalleModelName = configuration["AzureOpenAI:DeploymentName"] ??
                           throw new ArgumentNullException(
                               "AzureOpenAI:DeploymentName no encontrado en la configuración. " +
                               "Asegúrate de que 'DeploymentName' (el nombre de tu despliegue DALL-E en Azure) " +
                               "esté correctamente configurado en appsettings.json o variables de entorno."
                           );

         // Validaciones básicas para asegurar que las credenciales no son nulas o vacías
         if (string.IsNullOrWhiteSpace(endpoint))
         {
            _logger.LogError("AzureOpenAI:Endpoint no encontrado o vacío en la configuración.");
            throw new ArgumentNullException(
                "AzureOpenAI:Endpoint no encontrado o vacío en la configuración. " +
                "Asegúrate de que 'Endpoint' de tu recurso Azure OpenAI esté configurado."
            );
         }
         if (string.IsNullOrWhiteSpace(apiKey))
         {
            _logger.LogError("AzureOpenAI:ApiKey no encontrado o vacío en la configuración.");
            throw new ArgumentNullException(
                "AzureOpenAI:ApiKey no encontrado o vacío en la configuración. " +
                "Asegúrate de que 'ApiKey' de tu recurso Azure OpenAI esté configurada."
            );
         }

         // Inicializar la instancia de AzureOpenAIClient.
         _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
         _logger.LogInformation("AzureOpenAiService inicializado con éxito.");
      }

      /// <summary>
      /// Genera una imagen a partir de una descripción textual utilizando Azure OpenAI (DALL-E 3).
      /// </summary>
      /// <param name="description">La descripción detallada de la imagen que se desea generar.</param>
      /// <returns>La URL de la imagen generada como una cadena, o null si la generación falla.</returns>
      public async Task<string?> GenerateImageAsync(string description)
      {
         if (string.IsNullOrWhiteSpace(description))
         {
            _logger.LogWarning("GenerateImageAsync llamado con una descripción vacía o nula.");
            return null; // O lanza una ArgumentException si prefieres un error más explícito.
         }

         _logger.LogInformation("Intentando generar imagen para el prompt: '{Description}'...", description);

         try
         {
            
            ImageClient imageClient = _openAIClient.GetImageClient(_dalleModelName);
            
            ImageGenerationOptions imageGenerationOptions = new ImageGenerationOptions()
            {
               Size = GeneratedImageSize.W1024xH1024,
               Quality = GeneratedImageQuality.Standard,
               ResponseFormat = GeneratedImageFormat.Bytes
            };
            
            var imageGenerations = await imageClient.GenerateImageAsync(description, imageGenerationOptions);
            
            BinaryData? bytes = imageGenerations.Value?.ImageBytes;

            if (bytes == null || bytes.ToArray().Length == 0)
            {
               _logger.LogWarning("La generación de imagen no devolvió bytes válidos para la descripción: '{Description}'", description);
               return null;
            }

            // Convertir los bytes a Base64
            string base64Image = Convert.ToBase64String(bytes.ToArray());
            _logger.LogInformation("¡Imagen generada exitosamente y convertida a Base64 para el prompt: '{Description}'!", description);
            return base64Image;
         
         }
         catch (RequestFailedException ex) 
         {
            _logger.LogError(ex, "ERROR DE LA API: Falló la generación de imagen para '{Description}'. " +
                                "Código de Estado HTTP: {HttpStatus}, Mensaje de Error: {ErrorMessage}, Código de Error de Servicio: {ServiceErrorCode}",
                                description, ex.Status, ex.Message, ex.ErrorCode);
            return null; 
         }
         catch (Exception ex) 
         {
            _logger.LogError(ex, "ERROR INESPERADO: Ocurrió un error al generar la imagen para '{Description}'. Mensaje: {ErrorMessage}",
                                description, ex.Message);
            return null; 
         }
      }
   }
}