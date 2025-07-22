// ImageCreation.Application.Handlers/GetImageBase64QueryHandler.cs
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Application.Queries;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.ValueObjects; // Necesario para Platform

using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace ImageCreation.Application.Handlers
{
   public class GetImageBase64QueryHandler : IQueryHandler<GetImageBase64Query, string?>
   {
      private readonly IDapperRepository _dapperRepository;
      private readonly ICacheService _cacheService;
      private readonly ILogger<GetImageBase64QueryHandler> _logger;

      public GetImageBase64QueryHandler(IDapperRepository dapperRepository, ICacheService cacheService, ILogger<GetImageBase64QueryHandler> logger)
      {
         _dapperRepository = dapperRepository;
         _cacheService = cacheService;
         _logger = logger;
      }

      public async Task<string?> HandleAsync(GetImageBase64Query query)
      {
         string imageId = query.Id.ToString();
         _logger.LogInformation("Handling GetImageBase64Query for ID: {Id}", imageId);

         string? imageDtoJsonFromCache = await _cacheService.GetAsync(imageId);
         ImageDto? imageDto = null;

         if (!string.IsNullOrEmpty(imageDtoJsonFromCache))
         {
            try
            {
               imageDto = JsonSerializer.Deserialize<ImageDto>(imageDtoJsonFromCache);
            }
            catch (JsonException ex)
            {
               _logger.LogError(ex, "JSON deserialization error from cache when getting Base64 for ID: {Id}. Searching DB.", imageId);
            }
         }

         if (imageDto == null)
         {
            ImageRecord? record = await _dapperRepository.GetByIdAsync(imageId);
            if (record == null)
            {
               _logger.LogWarning("Image ID: {Id} not found in cache or DB for Base64.", imageId);
               return null;
            }

            imageDto = new ImageDto
            {
               Id = record.Id,
               Description = record.Description.Value,
               Base64Data = record.Base64Data.Value,
               PlatformUsed = record.PlatformUsed.Value, // ¡NUEVO!
               CreatedAt = record.CreatedAt
            };

            var imageDtoJsonToCache = JsonSerializer.Serialize(imageDto);
            await _cacheService.SetAsync(imageId, imageDtoJsonToCache);
            _logger.LogInformation("Image ID: {Id} retrieved from DB and full DTO saved to cache.", imageId);
         }

         _logger.LogInformation("Base64 data for image ID: {Id} returned.", imageId);
         return imageDto.Base64Data;
      }
   }
}