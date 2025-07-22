using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OpenAI.Images;

namespace ImageCreation.Infrastructure.Services.AI.OpenAI
{
   public class PublicOpenAiService : IOpenAiService
   {
      private readonly string _apiKey;
      private readonly ImageClient _imageClient;
      private readonly ImageGenerationOptions _defaultImageOptions;
      private readonly ILogger<PublicOpenAiService> _logger;

      public PublicOpenAiService(IConfiguration config, ILogger<PublicOpenAiService> logger)
      {
         _apiKey = config["OpenAI:ApiKey"]
             ?? throw new InvalidOperationException("OpenAI:ApiKey not configured.");
         _imageClient = new ImageClient("dall-e-3", _apiKey);
         _defaultImageOptions = new ImageGenerationOptions
         {
            Size = GeneratedImageSize.W1024xH1024,
            Quality = GeneratedImageQuality.High,
            ResponseFormat = GeneratedImageFormat.Bytes
         };
         _logger = logger;
      }

      public async Task<string?> GenerateImageAsync(string description)
      {
         try
         {
            var res = await _imageClient.GenerateImageAsync(description, _defaultImageOptions);

            var img = res.Value;
            return Convert.ToBase64String(img.ImageBytes.ToArray());

         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error generating image with OpenAI for description: {Description}", description);
            // You might want to re-throw a more specific exception or return a default value/error indicator
            throw; // Re-throw the exception after logging
         }
      }
   }
}
