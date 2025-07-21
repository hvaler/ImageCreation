using Azure;
using Azure.AI.Vision.ImageAnalysis;

using ImageCreation.Application.Interfaces; 
using ImageCreation.Domain.ValueObjects;
using ImageCreation.Infrastructure.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Services
{
   
   public class AzureVisionClassifierService : IImageClassifierService
   {
    
      private readonly IUrlConverterService _urlConverterService;
      private readonly ILogger<AzureVisionClassifierService> _logger;
      private readonly ImageAnalysisClient _imageAnalysisClient;

      public AzureVisionClassifierService(        
          IUrlConverterService urlConverterService,
          ILogger<AzureVisionClassifierService> logger,
          IConfiguration config)
      {
         _urlConverterService = urlConverterService ?? throw new ArgumentNullException(nameof(urlConverterService));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         string? endpoint = config["AzureVision:Endpoint"];
         string? apiKey = config["AzureVision:ApiKey"];

         if (string.IsNullOrWhiteSpace(endpoint))
         {
            _logger.LogError("AzureVision:Endpoint no encontrado o vacío en la configuración.");
            throw new ArgumentNullException("AzureVision:Endpoint no está configurado.");
         }
         if (string.IsNullOrWhiteSpace(apiKey))
         {
            _logger.LogError("AzureVision:ApiKey no encontrado o vacío en la configuración.");
            throw new ArgumentNullException("AzureVision:ApiKey no está configurada.");
         }

         _imageAnalysisClient = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
         _logger.LogInformation("AzureVisionClassifierService inicializado con éxito.");
      }

      public async Task<(string base64Data, string classification)> ClassifyImageAsync(ImageUrl imageUrl)
      {
         _logger.LogInformation("Iniciando clasificación con Azure AI Vision para URL: {ImageUrl}", imageUrl.Value);

   
         string? base64Data = await _urlConverterService.ConvertUrlToBase64Async(imageUrl.Value);

         if (string.IsNullOrEmpty(base64Data))
         {
            _logger.LogWarning("No se pudo obtener datos Base64 de la URL para clasificación: {ImageUrl}", imageUrl.Value);
            return (null, "None"); // Devolvemos "None" si la descarga falla
         }

         var foundCategories = new HashSet<string>();

         try
         {
            BinaryData imageData = new BinaryData(Convert.FromBase64String(base64Data));

            VisualFeatures features = VisualFeatures.Tags | VisualFeatures.Objects;

            ImageAnalysisResult result = await _imageAnalysisClient.AnalyzeAsync(imageData, features);

            if (result.Objects != null && result.Objects.Values.Any())
            {
               foreach (DetectedObject detectedObject in result.Objects.Values)
               {
                  var objectNameLower = detectedObject.Tags.FirstOrDefault()?.Name?.ToLowerInvariant();
                  if (objectNameLower != null)
                  {
                     if (objectNameLower.Contains("food") || objectNameLower.Contains("pizza") || objectNameLower.Contains("burger") || objectNameLower.Contains("dish"))
                     {
                        foundCategories.Add("Food"); // Las categorías se agregan con la primera letra en mayúscula
                     }
                     else if (objectNameLower.Contains("person") || objectNameLower.Contains("human") || objectNameLower.Contains("face") || objectNameLower.Contains("people"))
                     {
                        foundCategories.Add("Person"); // Las categorías se agregan con la primera letra en mayúscula
                     }
                  }
               }
            }

            if (result.Tags != null && result.Tags.Values.Any())
            {
               foreach (DetectedTag tag in result.Tags.Values)
               {
                  var lowerName = tag.Name.ToLowerInvariant();
                  if (lowerName.Contains("food") || lowerName.Contains("dish") || lowerName.Contains("meal") || lowerName.Contains("cuisine"))
                  {
                     foundCategories.Add("Food");
                  }
                  else if (lowerName.Contains("person") || lowerName.Contains("human") || lowerName.Contains("face") || lowerName.Contains("people"))
                  {
                     foundCategories.Add("Person"); 
                  }
               }
            }

            string finalClassificationResult;
            if (foundCategories.Any())
            {
               // Unir las categorías, ordenadas alfabéticamente.
               // Esto genera "Food, Person" o "Food" o "Person"
               finalClassificationResult = string.Join(", ", foundCategories.OrderBy(c => c));
            }
            else
            {
               finalClassificationResult = "None";
            }

            _logger.LogInformation("Clasificación de Azure AI Vision para URL: {ImageUrl} -> {Classification}", imageUrl.Value, finalClassificationResult);
            return (base64Data, finalClassificationResult);
         }
         catch (RequestFailedException ex)
         {
            _logger.LogError(ex, "ERROR DE AZURE VISION: Falló la clasificación para URL: {ImageUrl}. Código de Estado HTTP: {HttpStatus}, Mensaje: {ErrorMessage}",
                imageUrl.Value, ex.Status, ex.Message);
            return (base64Data, "None");
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "ERROR INESPERADO: Ocurrió un error al clasificar imagen con Azure AI Vision para URL: {ImageUrl}. Mensaje: {ErrorMessage}",
                imageUrl.Value, ex.Message);
            return (base64Data, "None");
         }
      }
   }
}