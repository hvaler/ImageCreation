using System;
using System.Threading.Tasks;
using ImageCreation.Infrastructure.Interfaces;
using ImageCreation.Domain.ValueObjects; // Necesario para ImageUrl y ClassificationResult
using Microsoft.Extensions.Logging; // Añadir para logging

namespace ImageCreation.Infrastructure.Services
{
   public class MockImageClassifierService : IImageClassifierService
   {
      private readonly UrlToBase64Converter _urlToBase64Converter;
      private readonly ILogger<MockImageClassifierService> _logger;

      public MockImageClassifierService(UrlToBase64Converter urlToBase64Converter, ILogger<MockImageClassifierService> logger)
      {
         _urlToBase64Converter = urlToBase64Converter ?? throw new ArgumentNullException(nameof(urlToBase64Converter));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      public async Task<(string base64Data, string classification)> ClassifyImageAsync(ImageUrl imageUrl)
      {
         _logger.LogInformation("Iniciando clasificación simulada para URL: {ImageUrl}", imageUrl.Value);

         // Descargar la imagen y convertirla a Base64
         string? base64Data = await _urlToBase64Converter.ConvertUrlToBase64Async(imageUrl.Value);

         if (string.IsNullOrEmpty(base64Data))
         {
            _logger.LogWarning("No se pudo obtener datos Base64 de la URL para clasificación: {ImageUrl}", imageUrl.Value);
            return (null, "None"); // O lanzar una excepción si prefieres
         }

         // Lógica de clasificación SIMULADA
         string classification = "None";
         var urlLower = imageUrl.Value.ToLowerInvariant();

         if (urlLower.Contains("food") || urlLower.Contains("pizza") || urlLower.Contains("burger"))
         {
            classification = "Food";
         }
         else if (urlLower.Contains("person") || urlLower.Contains("human") || urlLower.Contains("face"))
         {
            classification = "Person";
         }
         // Puedes añadir más reglas de simulación aquí

         _logger.LogInformation("Clasificación simulada para URL: {ImageUrl} -> {Classification}", imageUrl.Value, classification);

         return (base64Data, classification);
      }
   }
}