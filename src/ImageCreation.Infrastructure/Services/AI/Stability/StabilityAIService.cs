// ImageCreation.Infrastructure.Services/StabilityAIService.cs

using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Net.Http;
using System.Net.Http.Headers; // Necesario para MediaTypeWithQualityHeaderValue y ContentDispositionHeaderValue
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Services.AI.Stability
{
   public class StabilityAIService : IOpenAiService
   {
      private readonly HttpClient _httpClient;
      private readonly string _apiKey;
      private readonly string _defaultModel;
      private readonly ILogger<StabilityAIService> _logger;

      public StabilityAIService(HttpClient httpClient, IConfiguration config, ILogger<StabilityAIService> logger)
      {
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
         _apiKey = config["StabilityAI:ApiKey"] ?? throw new InvalidOperationException("StabilityAI:ApiKey not configured.");
         _defaultModel = config["StabilityAI:DefaultModel"]?.ToLowerInvariant() ?? "core";
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
         _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
         _httpClient.BaseAddress = new Uri("https://api.stability.ai/");
      }

      public async Task<string?> GenerateImageAsync(string description)
      {
         string modelSegment = _defaultModel;

         _logger.LogInformation("Generating image with Stability AI using model '{Model}' for description: '{Description}'", modelSegment, description);
         try
         {
            using (var formData = new MultipartFormDataContent())
            {
               // Función auxiliar para crear y añadir StringContent con Content-Disposition explícito
               void AddFormField(string name, string value)
               {
                  var content = new StringContent(value);
                  content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                  {
                     Name = $"\"{name}\"" // El nombre del campo DEBE estar entre comillas dobles
                  };
                  formData.Add(content);
               }

               // Añade los campos del formulario usando la función auxiliar
               AddFormField("prompt", description);
               AddFormField("output_format", "webp"); // Usamos "webp" como en el ejemplo curl
               AddFormField("width", "1024");
               AddFormField("height", "1024");
               // Puedes añadir más si son requeridos o deseados:
               // AddFormField("cfg_scale", "7");
               // AddFormField("steps", "50");

               var endpoint = $"v2beta/stable-image/generate/{modelSegment}";

               _logger.LogDebug("Stability AI API Endpoint: {Endpoint}", endpoint);
               _logger.LogDebug("Stability AI Request Content-Type: {ContentType}", formData.Headers.ContentType?.ToString());

               var response = await _httpClient.PostAsync(endpoint, formData);

               if (!response.IsSuccessStatusCode)
               {
                  string errorContent = await response.Content.ReadAsStringAsync();
                  _logger.LogError("Stability AI API returned an error. Status: {StatusCode}, Body: {ErrorBody}", response.StatusCode, errorContent);
                  throw new HttpRequestException($"Stability AI API error: {response.StatusCode} - {errorContent}");
               }

               byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

               if (imageBytes == null || imageBytes.Length == 0)
               {
                  _logger.LogWarning("Stability AI did not return valid image bytes for description: '{Description}' using model '{Model}'", description, modelSegment);
                  return null;
               }

               _logger.LogInformation("Image generated successfully with Stability AI using model '{Model}'.", modelSegment);
               return Convert.ToBase64String(imageBytes);
            }
         }
         catch (HttpRequestException ex)
         {
            _logger.LogError(ex, "Error HTTP when calling Stability AI for description: '{Description}'", description);
            throw;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An unexpected error occurred when calling Stability AI for description: '{Description}'", description);
            throw;
         }
      }
   }
}