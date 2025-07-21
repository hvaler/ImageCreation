using System;
using System.Text.Json;
using System.Threading.Tasks;

using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.Events;
using ImageCreation.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace ImageCreation.Application.Projections
{
   public class ClassifiedImageRecordProjector
   {
      private readonly IDapperRepository _repository;
      private readonly ICacheService _cache;
      private readonly ILogger<ClassifiedImageRecordProjector> _logger;

      public ClassifiedImageRecordProjector(
          IDapperRepository repository,
          ICacheService cache,
          ILogger<ClassifiedImageRecordProjector> logger)
      {
         _repository = repository ?? throw new ArgumentNullException(nameof(repository));
         _cache = cache ?? throw new ArgumentNullException(nameof(cache));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      public async Task ProjectAsync(ImageClassifiedEvent imageClassifiedEvent)
      {
         _logger.LogInformation("Processing ImageClassifiedEvent for ID: {Id}", imageClassifiedEvent.Id);

         try
         {
            // Reconstruir el ClassifiedImageRecord desde el evento
            var classifiedRecord = new ClassifiedImageRecord(
                imageClassifiedEvent.Id,
                new ImageUrl(imageClassifiedEvent.OriginalUrl),
                new Base64Data(imageClassifiedEvent.ClassifiedImageBase64),
                new ClassificationResult(imageClassifiedEvent.ClassificationResult),
                imageClassifiedEvent.Timestamp
            );

            // Persistir en SQL (¡Aquí es donde necesitamos idempotencia!)
            await _repository.InsertClassifiedImageAsync(classifiedRecord); // O Update/Upsert si ya existe
            _logger.LogInformation("ClassifiedImageRecord guardado/actualizado en SQL con ID: {Id}", imageClassifiedEvent.Id);

            // Crear DTO para caché
            var classifiedImageDto = new ClassifiedImageDto
            {
               Id = classifiedRecord.Id,
               OriginalUrl = classifiedRecord.OriginalUrl.Value,
               ClassifiedImageBase64 = classifiedRecord.ClassifiedImageBase64.Value,
               ClassificationResult = classifiedRecord.ClassificationResult.Value,
               ClassifiedAt = classifiedRecord.ClassifiedAt
            };

            // Persistir en Redis
            var classifiedImageDtoJson = JsonSerializer.Serialize(classifiedImageDto);
            await _cache.SetAsync(classifiedImageDto.Id.ToString(), classifiedImageDtoJson);
            _logger.LogInformation("ClassifiedImageDto guardado/actualizado en Redis con ID: {Id}", classifiedImageDto.Id);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error processing ImageClassifiedEvent for ID: {Id}. URL: {Url}",
                imageClassifiedEvent.Id, imageClassifiedEvent.OriginalUrl);
            // Aquí podrías implementar una lógica de reintento o enviar a una cola de errores (DLQ)
         }
      }
   }
}