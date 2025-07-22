// ImageCreation.Application.Handlers/GetImageByIdQueryHandler.cs
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Application.Queries;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.ValueObjects; // Necesario para Platform

using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace ImageCreation.Application.Handlers
{
   public class GetImageByIdQueryHandler : IQueryHandler<GetImageByIdQuery, ImageDto?>
   {
      private readonly IDapperRepository _dapperRepository;
      private readonly ICacheService _cacheService;
      private readonly ILogger<GetImageByIdQueryHandler> _logger;

      public GetImageByIdQueryHandler(IDapperRepository dapperRepository, ICacheService cacheService, ILogger<GetImageByIdQueryHandler> logger)
      {
         _dapperRepository = dapperRepository;
         _cacheService = cacheService;
         _logger = logger;
      }

      public async Task<ImageDto?> HandleAsync(GetImageByIdQuery query)
      {
         string imageId = query.Id.ToString();
         _logger.LogInformation("Handling GetImageByIdQuery for ID: {Id}", imageId);

         string? imageDtoJsonFromCache = await _cacheService.GetAsync(imageId);

         if (!string.IsNullOrEmpty(imageDtoJsonFromCache))
         {
            try
            {
               ImageDto? imageDto = JsonSerializer.Deserialize<ImageDto>(imageDtoJsonFromCache);
               if (imageDto != null)
               {
                  _logger.LogInformation("Image ID: {Id} retrieved from cache.", imageId);
                  return imageDto;
               }
               _logger.LogWarning("Cache data for ID: {Id} was null or invalid after deserialization. Searching DB.", imageId);
            }
            catch (JsonException ex)
            {
               _logger.LogError(ex, "JSON deserialization error from cache for ID: {Id}. Searching DB.", imageId);
            }
         }

         ImageRecord? record = await _dapperRepository.GetByIdAsync(imageId);
         if (record == null)
         {
            _logger.LogWarning("Image ID: {Id} not found in cache or DB.", imageId);
            return null;
         }

         var imageDtoFromDb = new ImageDto
         {
            Id = record.Id,
            Description = record.Description.Value,
            Base64Data = record.Base64Data.Value,
            PlatformUsed = record.PlatformUsed.Value, // ¡NUEVO!
            CreatedAt = record.CreatedAt
         };

         var imageDtoJsonToCache = JsonSerializer.Serialize(imageDtoFromDb);
         await _cacheService.SetAsync(imageId, imageDtoJsonToCache);
         _logger.LogInformation("Image ID: {Id} retrieved from DB and saved to cache.", imageId);

         return imageDtoFromDb;
      }
   }
}