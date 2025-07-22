// ImageCreation.Infrastructure.Services/GoogleGenerativeAIService.cs

using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization; // Para JsonPropertyName

namespace ImageCreation.Infrastructure.Services.AI.Google
{
   public class GoogleGenerativeAIService : IOpenAiService
   {
      private readonly HttpClient _httpClient;
      private readonly string _apiKey;
      private readonly string _defaultImageModel;
      private readonly string _defaultTextModel; // Para uso futuro si la API de Gemini también generara texto para tu app.
      private readonly string _apiVersion;
      private readonly ILogger<GoogleGenerativeAIService> _logger;

      public GoogleGenerativeAIService(HttpClient httpClient, IConfiguration config, ILogger<GoogleGenerativeAIService> logger)
      {
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         _apiKey = config["GoogleGenerativeAI:ApiKey"] ??
                   throw new InvalidOperationException("GoogleGenerativeAI:ApiKey not configured.");
         _defaultImageModel = config["GoogleGenerativeAI:DefaultImageModel"] ??
                              throw new InvalidOperationException("GoogleGenerativeAI:DefaultImageModel not configured.");
         _defaultTextModel = config["GoogleGenerativeAI:DefaultTextModel"] ??
                             throw new InvalidOperationException("GoogleGenerativeAI:DefaultTextModel not configured.");
         _apiVersion = config["GoogleGenerativeAI:ApiVersion"] ?? "v1beta"; // Default a v1beta

         _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");

         _logger.LogInformation("GoogleGenerativeAIService initialized. Image Model: '{ImageModel}', Text Model: '{TextModel}', API Version: '{ApiVersion}'.",
             _defaultImageModel, _defaultTextModel, _apiVersion);
      }

      public async Task<string?> GenerateImageAsync(string description)
      {
         _logger.LogInformation("Generating image with Google Generative AI (Imagen Model) for description: '{Description}'", description);
         try
         {
            // Usamos el modelo de imagen configurado
            var modelEndpoint = $"models/{_defaultImageModel}:predict"; // El endpoint para modelos de imagen (predict)
            var apiUrlWithKey = $"{_apiVersion}/{modelEndpoint}?key={_apiKey}";

            // Construir el cuerpo de la solicitud JSON para el modelo Imagen
            var requestBody = new
            {
               instances = new[]
                {
                        new { prompt = description }
                    },
               parameters = new
               {
                  outputMimeType = "image/jpeg", // O "image/webp" o "image/png"
                  sampleCount = 1,
                  personGeneration = "ALLOW_ADULT", // ¡Cuidado con las políticas de contenido aquí!
                  aspectRatio = "1:1",
               }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(apiUrlWithKey, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
               string errorContent = await response.Content.ReadAsStringAsync();
               _logger.LogError("Google Generative AI (Imagen) API returned an error. Status: {StatusCode}, Body: {ErrorBody}", response.StatusCode, errorContent);
               throw new HttpRequestException($"Google Generative AI (Imagen) API error: {response.StatusCode} - {errorContent}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<GoogleImagenApiResponse>();

            string? base64Image = apiResponse?.Predictions?.FirstOrDefault()?.BytesBase64Encoded;

            if (string.IsNullOrEmpty(base64Image))
            {
               _logger.LogWarning("Google Generative AI (Imagen) API did not return valid image Base64 for description: '{Description}'. Response: {ResponseJson}", description, JsonSerializer.Serialize(apiResponse));
               return null;
            }

            _logger.LogInformation("Image generated successfully with Google Generative AI (Imagen) for description: '{Description}'.", description);
            return base64Image;

         }
         catch (HttpRequestException ex)
         {
            _logger.LogError(ex, "HTTP error when calling Google Generative AI (Imagen) for description: '{Description}'", description);
            throw;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "An unexpected error occurred when calling Google Generative AI (Imagen) for description: '{Description}'.", description);
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
         [JsonPropertyName("bytesBase64Encoded")]
         public string? BytesBase64Encoded { get; set; }
         // Otros campos de predicción si los hubiera
      }
   }
}