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
   public class ImageRecordProjector
   {
      private readonly IDapperRepository _repository;
      private readonly ICacheService _cache;
      private readonly ILogger<ImageRecordProjector> _logger;

      public ImageRecordProjector(
          IDapperRepository repository,
          ICacheService cache,
          ILogger<ImageRecordProjector> logger)
      {
         _repository = repository ?? throw new ArgumentNullException(nameof(repository));
         _cache = cache ?? throw new ArgumentNullException(nameof(cache));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      public async Task ProjectAsync(ImageCreatedEvent imageCreatedEvent)
      {
         _logger.LogInformation("Processing ImageCreatedEvent for ID: {Id}", imageCreatedEvent.Id);

         try
         {
            // Reconstruir el ImageRecord desde el evento
            var imageRecord = new ImageRecord(
                imageCreatedEvent.Id,
                new ImageDescription(imageCreatedEvent.Description),
                new Base64Data(imageCreatedEvent.Base64Data),
                imageCreatedEvent.Timestamp
            );

            // Persistir en SQL (¡Aquí es donde necesitamos idempotencia!)
            await _repository.InsertAsync(imageRecord); // O Update/Upsert si ya existe
            _logger.LogInformation("ImageRecord guardado/actualizado en SQL con ID: {Id}", imageCreatedEvent.Id);

            // Crear DTO para caché
            var imageDto = new ImageDto
            {
               Id = imageRecord.Id,
               Description = imageRecord.Description.Value,
               Base64Data = imageRecord.Base64Data.Value,
               CreatedAt = imageRecord.CreatedAt
            };

            // Persistir en Redis
            var imageDtoJson = JsonSerializer.Serialize(imageDto);
            await _cache.SetAsync(imageDto.Id.ToString(), imageDtoJson);
            _logger.LogInformation("ImageDto guardado/actualizado en Redis con ID: {Id}", imageDto.Id);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error processing ImageCreatedEvent for ID: {Id}. Description: {Description}",
                imageCreatedEvent.Id, imageCreatedEvent.Description);
            // Aquí podrías implementar una lógica de reintento o enviar a una cola de errores (DLQ)
         }
      }
   }
}