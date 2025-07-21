using ImageCreation.Application.Commands;
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

namespace ImageCreation.Application.Handlers
{
   public class CreateImageCommandHandler : ICommandHandler<CreateImageCommand, ImageDto>
   {
      private readonly IOpenAiServiceFactory _openAiServiceFactory;
      private readonly IEventStore _eventStore;
 
      private readonly ILogger<CreateImageCommandHandler> _logger;

      public CreateImageCommandHandler(
          IOpenAiServiceFactory openAiServiceFactory,
          IEventStore eventStore,
     
          ILogger<CreateImageCommandHandler> logger)
      {
         _openAiServiceFactory = openAiServiceFactory ?? throw new ArgumentNullException(nameof(openAiServiceFactory));
         _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore)); 
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      public async Task<ImageDto> HandleAsync(CreateImageCommand command)
      {
         _logger.LogInformation("Handling CreateImageCommand for description: '{Description}'", command.Description);

         string platform = command.Platform?.ToLowerInvariant() ?? "public";

         IOpenAiService openAiService = _openAiServiceFactory.GetService(platform);

         var base64 = await openAiService.GenerateImageAsync(command.Description)
             ?? throw new InvalidOperationException("Image generation failed: base64 data is null.");

         var id = Guid.NewGuid();
         var created = DateTime.UtcNow;

         var record = new ImageRecord(
             id,
             new ImageDescription(command.Description),
             new Base64Data(base64),
             created
         );

         // ¡ÚNICA PERSISTENCIA DEL COMMAND HANDLER! Publicar el evento de dominio en EventStoreDB
         await _eventStore.PublishAsync(record.ToDomainEvent());
         _logger.LogInformation("ImageCreatedEvent published to EventStoreDB for ID: {Id}", id);

         // ¡ELIMINAR ESTAS LÍNEAS! La persistencia en SQL/Redis la harán los proyectores.
         // await _repository.InsertAsync(record);
         // _logger.LogInformation("Imagen generada guardada en la base de datos con ID: {Id}", id);
         // var imageDtoJson = JsonSerializer.Serialize(imageDto);
         // await _cache.SetAsync(id.ToString(), imageDtoJson);
         // _logger.LogInformation("Imagen generada DTO guardada en caché Redis con ID: {Id}", id);

         // Devolver el DTO inmediatamente para la respuesta de la API
         var imageDto = new ImageDto
         {
            Id = id,
            Description = command.Description,
            Base64Data = base64,
            CreatedAt = created
         };

         return imageDto;
      }
   }
}