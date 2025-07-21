using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Application.Queries;
using ImageCreation.Domain.Entities;

using Microsoft.Extensions.Logging;

using System.Text.Json; 

namespace ImageCreation.Application.Handlers
{
   public class GetClassifiedImageByIdQueryHandler : IQueryHandler<GetClassifiedImageByIdQuery, ClassifiedImageDto?>
   {
      private readonly IDapperRepository _dapperRepository;
      private readonly ICacheService _cacheService;
      private readonly ILogger<GetClassifiedImageByIdQueryHandler> _logger;

      public GetClassifiedImageByIdQueryHandler(IDapperRepository dapperRepository, ICacheService cacheService, ILogger<GetClassifiedImageByIdQueryHandler> logger)
      {
         _dapperRepository = dapperRepository;
         _cacheService = cacheService;
         _logger = logger;
      }

      public async Task<ClassifiedImageDto?> HandleAsync(GetClassifiedImageByIdQuery query)
      {
         string classifiedImageId = query.Id.ToString();
         _logger.LogInformation("Handling GetClassifiedImageByIdQuery for ID: {Id}", classifiedImageId);

         // 1. Intentar obtener el JSON completo del DTO de la caché (Redis)
         string? classifiedImageDtoJsonFromCache = await _cacheService.GetAsync("classified_" + classifiedImageId);

         if (!string.IsNullOrEmpty(classifiedImageDtoJsonFromCache))
         {
            try
            {
               ClassifiedImageDto? classifiedImageDto = JsonSerializer.Deserialize<ClassifiedImageDto>(classifiedImageDtoJsonFromCache);
               if (classifiedImageDto != null)
               {
                  _logger.LogInformation("Classified image ID: {Id} retrieved from cache.", classifiedImageId);
                  return classifiedImageDto;
               }
               _logger.LogWarning("Cache data for classified image ID: {Id} was null or invalid after deserialization. Searching DB.", classifiedImageId);
            }
            catch (JsonException ex)
            {
               _logger.LogError(ex, "JSON deserialization error from cache for classified image ID: {Id}. Searching DB.", classifiedImageId);
            }
         }

         // 2. Si no está en caché (o el JSON en caché era inválido), obtener de la base de datos
         ClassifiedImageRecord? record = await _dapperRepository.GetClassifiedImageByIdAsync(classifiedImageId);
         if (record == null)
         {
            _logger.LogWarning("Classified image ID: {Id} not found in cache or DB.", classifiedImageId);
            return null;
         }

         // 3. Si se encuentra en la base de datos, construir ClassifiedImageDto
         var classifiedImageDtoFromDb = new ClassifiedImageDto
         {
            Id = record.Id,
            OriginalUrl = record.OriginalUrl.Value,
            ClassifiedImageBase64 = record.ClassifiedImageBase64.Value,
            ClassificationResult = record.ClassificationResult.Value,
            ClassifiedAt = record.ClassifiedAt
         };

         // 4. Almacenarlo en caché como JSON para futuras solicitudes
         var classifiedImageDtoJsonToCache = JsonSerializer.Serialize(classifiedImageDtoFromDb);
         await _cacheService.SetAsync("classified_" + classifiedImageId, classifiedImageDtoJsonToCache);
         _logger.LogInformation("Classified image ID: {Id} retrieved from DB and saved to cache.", classifiedImageId);

         return classifiedImageDtoFromDb;
      }
   }
}