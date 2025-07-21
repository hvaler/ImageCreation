using ImageCreation.Application.Commands;
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Domain.Entities;
using ImageCreation.Domain.ValueObjects;
using ImageCreation.Infrastructure.Interfaces;

using Microsoft.Extensions.Logging;

namespace ImageCreation.Application.Handlers
{
   public class ClassifyImageCommandHandler : ICommandHandler<ClassifyImageCommand, ClassifiedImageDto>
   {
      private readonly IUrlConverterService _urlToBase64Converter;
      private readonly IImageClassifierService _imageClassifierService;
      
      private readonly IEventStore _eventStore;
     
      private readonly ILogger<ClassifyImageCommandHandler> _logger;

      public ClassifyImageCommandHandler(
          IUrlConverterService urlToBase64Converter,
          IImageClassifierService imageClassifierService,
          
          IEventStore eventStore,
        
          ILogger<ClassifyImageCommandHandler> logger)
      {
         _urlToBase64Converter = urlToBase64Converter ?? throw new ArgumentNullException(nameof(urlToBase64Converter));
         _imageClassifierService = imageClassifierService ?? throw new ArgumentNullException(nameof(imageClassifierService));
        
         _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      public async Task<ClassifiedImageDto> HandleAsync(ClassifyImageCommand command)
      {
         _logger.LogInformation("Iniciando clasificación de imagen para URL: {ImageUrl}", command.ImageUrl);

         ImageUrl imageUrlVo;
         try
         {
            imageUrlVo = new ImageUrl(command.ImageUrl);
         }
         catch (ArgumentException ex)
         {
            _logger.LogError(ex, "URL de imagen inválida: {ImageUrl}", command.ImageUrl);
            throw new InvalidOperationException($"La URL de la imagen proporcionada no es válida: {ex.Message}", ex);
         }

         string? base64Data = await _urlToBase64Converter.ConvertUrlToBase64Async(imageUrlVo.Value);

         if (string.IsNullOrEmpty(base64Data))
         {
            _logger.LogError("No se pudieron obtener los datos Base64 de la imagen para URL: {ImageUrl}", command.ImageUrl);
            throw new InvalidOperationException($"No se pudo descargar o convertir la imagen de la URL: {command.ImageUrl}.");
         }

         string classificationResult = (await _imageClassifierService.ClassifyImageAsync(imageUrlVo)).classification;

         Base64Data classifiedImageBase64Vo;
         ClassificationResult classificationResultVo;
         try
         {
            classifiedImageBase64Vo = new Base64Data(base64Data);
            classificationResultVo = new ClassificationResult(classificationResult);
         }
         catch (ArgumentException ex)
         {
            _logger.LogError(ex, "Datos de imagen o resultado de clasificación inválidos después de procesar URL: {ImageUrl}. Mensaje: {ErrorMessage}", command.ImageUrl, ex.Message);
            throw new InvalidOperationException($"Error en los datos procesados: {ex.Message}", ex);
         }

         var id = Guid.NewGuid();
         var classifiedAt = DateTime.UtcNow;

         var classifiedRecord = new ClassifiedImageRecord(
             id,
             imageUrlVo,
             classifiedImageBase64Vo,
             classificationResultVo,
             classifiedAt
         );

         // ¡ÚNICA PERSISTENCIA DEL COMMAND HANDLER! Publicar el evento de dominio en EventStoreDB
         await _eventStore.PublishAsync(classifiedRecord.ToDomainEvent());
         _logger.LogInformation("Evento 'ImageClassifiedEvent' publicado para ID: {Id}", id);

         // ¡ELIMINAR ESTAS LÍNEAS! La persistencia en SQL/Redis la harán los proyectores.
         // await _repository.InsertClassifiedImageAsync(classifiedRecord);
         // _logger.LogInformation("Imagen clasificada guardada en la base de datos con ID: {Id}", id);
         // var classifiedImageDtoJson = JsonSerializer.Serialize(classifiedImageDto);
         // await _cache.SetAsync(id.ToString(), classifiedImageDtoJson);
         // _logger.LogInformation("Imagen clasificada DTO guardada en caché Redis con ID: {Id}", id);

         // Crear el DTO para la respuesta (para el cliente inmediato)
         var classifiedImageDto = new ClassifiedImageDto
         {
            Id = id,
            OriginalUrl = command.ImageUrl,
            ClassifiedImageBase64 = base64Data,
            ClassificationResult = classificationResult,
            ClassifiedAt = classifiedAt
         };

         _logger.LogInformation("Clasificación de imagen completada con éxito para URL: {ImageUrl}. Resultado: {Classification}. Evento publicado.", command.ImageUrl, classificationResult);
         return classifiedImageDto;
      }
   }
}