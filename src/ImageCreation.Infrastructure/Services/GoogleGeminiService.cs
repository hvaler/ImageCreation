// ImageCreation.Infrastructure.Services/GeminiProImageService.cs
// (Sugerencia: Renombrar esta clase a GoogleImagenService.cs para mayor claridad en tu proyecto,
// ya que esta implementación es para el modelo Imagen de Google, no para Gemini Pro de texto.)

using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Services
{
   public class GeminiProImageService : IOpenAiService // Considera renombrar a GoogleImagenService
   {
      private readonly HttpClient _httpClient;
      private readonly string _apiKey;
      // ¡Este es el modelo real de generación de imágenes de Google Imagen!
      private readonly string _imageGenerationModel = "models/imagen-3.0-generate-002"; // Confirma este ID si es necesario
      private readonly ILogger<GeminiProImageService> _logger;

      public GeminiProImageService(HttpClient httpClient, IConfiguration config, ILogger<GeminiProImageService> logger)
      {
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         _apiKey = config["GoogleAI:ApiKey"] ?? // Asumimos que la API Key de Google está aquí
                   throw new InvalidOperationException("GoogleAI:ApiKey no encontrada o vacía en la configuración para Google Imagen.");

         _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
         // La API Key va en la URL como parámetro de consulta.

         _logger.LogInformation("GeminiProImageService initialized with image generation model '{ImageGenerationModel}'.", _imageGenerationModel);
      }

      public async Task<string?> GenerateImageAsync(string description)
      {
         _logger.LogInformation("Attempting to generate image with Google Imagen for description: '{Description}'", description);
         try
         {
            // Endpoint conforme al script curl: MODEL_ID en la ruta, API Key como query param
            var apiUrlWithKey = $"v1beta/{_imageGenerationModel}:predict?key={_apiKey}";

            // Construir el cuerpo de la solicitud JSON según el script curl
            var requestBody = new // Asegúrate de que los nombres de las propiedades coincidan con snake_case si la API lo espera
            {
               instances = new[]
                {
                        new { prompt = description }
                    },
               parameters = new // ¡CORRECCIÓN CLAVE! Ahora es "parameters"
               {
                  outputMimeType = "image/jpeg", // ¡CORRECCIÓN CLAVE! Ahora es "outputMimeType"
                  sampleCount = 1,
                  personGeneration = "ALLOW_ADULT", // ¡Importante: revisa las políticas de contenido!
                  aspectRatio = "1:1",
                  // Puedes añadir otros parámetros del script curl si los necesitas, ej:
                  // seed = 123,
               }
            };

            // Serializar el cuerpo a JSON
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(apiUrlWithKey, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
               string errorContent = await response.Content.ReadAsStringAsync();
               _logger.LogError("Google Imagen API returned an error. Status: {StatusCode}, Body: {ErrorBody}", response.StatusCode, errorContent);
               throw new HttpRequestException($"Google Imagen API error: {response.StatusCode} - {errorContent}");
            }

            // Deserializar la respuesta para encontrar la imagen Base64
            // La estructura de respuesta es la misma que ya teníamos de la última vez para Google Imagen.
            var googleImagenApiResponse = await response.Content.ReadFromJsonAsync<GoogleImagenApiResponse>();

            string? base64Image = googleImagenApiResponse?.Predictions?
                                                        .FirstOrDefault()?
                                                        .BytesBase64Encoded;

            if (string.IsNullOrEmpty(base64Image))
            {
               _logger.LogWarning("Google Imagen API did not return valid image Base64 for description: '{Description}'. Response: {ResponseJson}", description, JsonSerializer.Serialize(googleImagenApiResponse));
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

      // --- Clases DTO para la respuesta de Google Imagen API (sin cambios) ---
      private class GoogleImagenApiResponse
      {
         [JsonPropertyName("predictions")]
         public List<PredictionItem>? Predictions { get; set; }
      }

      private class PredictionItem
      {
         [JsonPropertyName("bytesBase64Encoded")]
         public string? BytesBase64Encoded { get; set; }
      }
   }
}