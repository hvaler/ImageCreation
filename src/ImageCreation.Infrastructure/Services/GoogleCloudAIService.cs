// ImageCreation.Infrastructure.Services/GoogleCloudAIService.cs
// ¡Considera renombrar esta clase a GoogleImagenService.cs para mayor claridad!

using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic; // Necesario para List
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json; // Para serialización/deserialización JSON
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Services
{
   public class GoogleCloudAIService : IOpenAiService
   {
      private readonly HttpClient _httpClient;
      private readonly string _apiKey;
      // El MODEL_ID del script curl
      private readonly string _modelId = "models/imagen-4.0-generate-preview-06-06"; // ¡Actualiza esto si usas otro modelo!
      private readonly ILogger<GoogleCloudAIService> _logger;

      public GoogleCloudAIService(HttpClient httpClient, IConfiguration config, ILogger<GoogleCloudAIService> logger)
      {
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         _apiKey = config["GoogleCloudAI:ApiKey"] ?? // Asumimos que la API Key está aquí ahora
                   throw new InvalidOperationException("GoogleCloudAI:ApiKey not configured for Google Imagen.");

         // La URL base para la Generative Language API
         _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
         // No se necesita el encabezado X-goog-api-key aquí, va en la URL.
         // No se necesita configuración de autenticación compleja con credenciales de cuenta de servicio aquí.

         _logger.LogInformation("GoogleCloudAIService initialized for model '{ModelId}'.", _modelId);
      }

      public async Task<string?> GenerateImageAsync(string description)
      {
         _logger.LogInformation("Generating image with Google Imagen for description: '{Description}' using model '{ModelId}'", description, _modelId);
         try
         {
            // Endpoint conforme al script curl: MODEL_ID en la ruta, API Key como query param
            var endpoint = $"v1beta/{_modelId}:predict?key={_apiKey}";

            // Construir el cuerpo de la solicitud JSON según el script curl
            var requestBody = new
            {
               instances = new[]
                {
                        new { prompt = description }
                    },
               parameters = new
               {
                  outputMimeType = "image/jpeg",
                  sampleCount = 1,
                  personGeneration = "ALLOW_ADULT", // ¡Cuidado con las políticas de contenido aquí!
                  aspectRatio = "1:1",
               }
            };

            // Serializar el cuerpo a JSON
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json" // Content-Type correcto
            );

            var response = await _httpClient.PostAsync(endpoint, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
               string errorContent = await response.Content.ReadAsStringAsync();
               _logger.LogError("Google Imagen API returned an error. Status: {StatusCode}, Body: {ErrorBody}", response.StatusCode, errorContent);
               // Puedes intentar deserializar el errorContent a un objeto de error si la API de Google proporciona uno estructurado
               throw new HttpRequestException($"Google Imagen API error: {response.StatusCode} - {errorContent}");
            }

            // Deserializar la respuesta para obtener el Base64 de la imagen
            var apiResponse = await response.Content.ReadFromJsonAsync<GoogleImagenApiResponse>();

            string? base64Image = apiResponse?.Predictions?.FirstOrDefault()?.BytesBase64Encoded;

            if (string.IsNullOrEmpty(base64Image))
            {
               _logger.LogWarning("Google Imagen API did not return valid image Base64 for description: '{Description}'", description);
               return null;
            }

            _logger.LogInformation("Image generated successfully with Google Imagen for description: '{Description}'.", description);
            return base64Image;
         }
         catch (HttpRequestException ex)
         {
            _logger.LogError(ex, "HTTP error when calling Google Imagen API for description: '{Description}'", description);
            throw;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An unexpected error occurred when calling Google Imagen API for description: '{Description}'.", description);
            throw;
         }
      }

      // --- Clases DTO para la respuesta de Google Imagen API ---
      private class GoogleImagenApiResponse
      {
         [JsonPropertyName("predictions")]
         public List<PredictionItem>? Predictions { get; set; }
      }

      private class PredictionItem
      {
         // Nota: JSON PropertyName suele ser camelCase por defecto para System.Text.Json,
         // pero si la API usa snake_case, se debe especificar. El curl lo muestra como bytesBase64Encoded.
         [JsonPropertyName("bytesBase64Encoded")]
         public string? BytesBase64Encoded { get; set; }
         // Otros campos de predicción si los hubiera
      }
   }
}