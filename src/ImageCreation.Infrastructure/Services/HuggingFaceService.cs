// ImageCreation.Infrastructure.Services/HuggingFaceService.cs
using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Services
{
   public class HuggingFaceService : IOpenAiService
   {
      private readonly HttpClient _httpClient;
      private readonly string _apiKey;
      private readonly string _modelEndpoint;
      private readonly ILogger<HuggingFaceService> _logger;

      public HuggingFaceService(HttpClient httpClient, IConfiguration config, ILogger<HuggingFaceService> logger)
      {
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
         _apiKey = config["HuggingFace:ApiKey"] ?? throw new InvalidOperationException("HuggingFace:ApiKey not configured.");
         _modelEndpoint = config["HuggingFace:Endpoint"] ?? throw new InvalidOperationException("HuggingFace:Endpoint for model not configured.");
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
         _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png")); // O image/jpeg, etc.
      }

      public async Task<string?> GenerateImageAsync(string description)
      {
         _logger.LogInformation("Generating image with Hugging Face for description: '{Description}' using endpoint: {Endpoint}", description, _modelEndpoint);
         try
         {
            var requestBody = new { inputs = description };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_modelEndpoint, content);

            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            if (imageBytes == null || imageBytes.Length == 0)
            {
               _logger.LogWarning("Hugging Face did not return valid image bytes for description: '{Description}'", description);
               return null;
            }

            _logger.LogInformation("Image generated successfully with Hugging Face.");
            return Convert.ToBase64String(imageBytes);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error generating image with Hugging Face for description: '{Description}'", description);
            throw;
         }
      }
   }
}