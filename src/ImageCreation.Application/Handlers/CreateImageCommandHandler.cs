// ImageCreation.Application.Handlers/CreateImageCommandHandler.cs
using ImageCreation.Application.Commands;
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.ValueObjects; // Necesario para Platform

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

         // Usa PlatformRequested
         string platformName = command.PlatformRequested?.ToLowerInvariant() ?? "public";

         IOpenAiService openAiService = _openAiServiceFactory.GetService(platformName);

         var base64 = await openAiService.GenerateImageAsync(command.Description)
             ?? throw new InvalidOperationException("Image generation failed: base64 data is null.");

         var id = Guid.NewGuid();
         var created = DateTime.UtcNow;

         // Crea el Value Object Platform
         var platformUsedVo = new Platform(platformName); // ¡NUEVO!

         var record = new ImageRecord(
             id,
             new ImageDescription(command.Description),
             new Base64Data(base64),
             platformUsedVo, // ¡NUEVO PARÁMETRO!
             created
         );

         await _eventStore.PublishAsync(record.ToDomainEvent());
         _logger.LogInformation("ImageCreatedEvent published to EventStoreDB for ID: {Id}", id);

         var imageDto = new ImageDto
         {
            Id = id,
            Description = command.Description,
            Base64Data = base64,
            PlatformUsed = platformUsedVo.Value, // ¡NUEVO!
            CreatedAt = created
         };

         return imageDto;
      }
   }
}