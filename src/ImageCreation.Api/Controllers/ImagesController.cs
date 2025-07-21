using ImageCreation.Application.Commands;
using ImageCreation.Application.DTOs;
using ImageCreation.Application.Interfaces;
using ImageCreation.Application.Queries;

using Microsoft.AspNetCore.Mvc;

namespace ImageCreation.Api.Controllers
{
   [ApiController]
   [Route("api/[controller]")] // Ruta base para el controlador: /api/Images
   public class ImagesController : ControllerBase
   {
      // Handlers para las operaciones de escritura/creación
      private readonly ICommandHandler<CreateImageCommand, ImageDto> _createImageCommandHandler;
      private readonly ICommandHandler<ClassifyImageCommand, ClassifiedImageDto> _classifyImageCommandHandler;

      // ¡NUEVO! Handlers para las operaciones de lectura
      private readonly IQueryHandler<GetImageByIdQuery, ImageDto?> _getImageByIdQueryHandler;
      private readonly IQueryHandler<GetImageBase64Query, string?> _getImageBase64QueryHandler;
      private readonly IQueryHandler<GetClassifiedImageByIdQuery, ClassifiedImageDto?> _getClassifiedImageByIdQueryHandler;

      private readonly ILogger<ImagesController> _logger;

      public ImagesController(
          ICommandHandler<CreateImageCommand, ImageDto> createImageCommandHandler,
          ICommandHandler<ClassifyImageCommand, ClassifiedImageDto> classifyImageCommandHandler,
          IQueryHandler<GetImageByIdQuery, ImageDto?> getImageByIdQueryHandler, // Inyectar nuevo
          IQueryHandler<GetImageBase64Query, string?> getImageBase64QueryHandler, // Inyectar nuevo
          IQueryHandler<GetClassifiedImageByIdQuery, ClassifiedImageDto?> getClassifiedImageByIdQueryHandler, // Inyectar nuevo
          ILogger<ImagesController> logger)
      {
         _createImageCommandHandler = createImageCommandHandler ?? throw new ArgumentNullException(nameof(createImageCommandHandler));
         _classifyImageCommandHandler = classifyImageCommandHandler ?? throw new ArgumentNullException(nameof(classifyImageCommandHandler));
         _getImageByIdQueryHandler = getImageByIdQueryHandler ?? throw new ArgumentNullException(nameof(getImageByIdQueryHandler));
         _getImageBase64QueryHandler = getImageBase64QueryHandler ?? throw new ArgumentNullException(nameof(getImageBase64QueryHandler));
         _getClassifiedImageByIdQueryHandler = getClassifiedImageByIdQueryHandler ?? throw new ArgumentNullException(nameof(getClassifiedImageByIdQueryHandler));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      // Endpoint para generar una imagen (POST /api/Images/generate)
      [HttpPost("generate")] 
      public async Task<IActionResult> GenerateImage([FromBody] CreateImageCommand command)
      {
         if (string.IsNullOrWhiteSpace(command.Description))
         {
            _logger.LogWarning("Solicitud de generación de imagen inválida: Descripción vacía.");
            return BadRequest("La descripción no puede estar vacía.");
         }

         _logger.LogInformation("Recibida solicitud para generar imagen con descripción: '{Description}'", command.Description);
         try
         {
            var result = await _createImageCommandHandler.HandleAsync(command);
            _logger.LogInformation("Imagen generada con éxito. ID: {Id}", result.Id);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error al generar imagen para descripción: '{Description}'", command.Description);
            return StatusCode(500, new { Message = "Ocurrió un error interno al generar la imagen.", Details = ex.Message });
         }
      }      

      // Endpoint para clasificar una imagen (POST /api/Images/classify)
      [HttpPost("classify")]
      public async Task<IActionResult> ClassifyImage([FromBody] ClassifyImageCommand command)
      {
         if (string.IsNullOrWhiteSpace(command.ImageUrl))
         {
            _logger.LogWarning("Solicitud de clasificación de imagen inválida: URL vacía.");
            return BadRequest("La URL de la imagen no puede estar vacía.");
         }

         _logger.LogInformation("Recibida solicitud para clasificar imagen de URL: '{ImageUrl}'", command.ImageUrl);
         try
         {
            var result = await _classifyImageCommandHandler.HandleAsync(command);
            _logger.LogInformation("Imagen clasificada con éxito. ID: {Id}, Clasificación: {Classification}", result.Id, result.ClassificationResult);
          
            return CreatedAtAction(nameof(GetClassifiedById), new { id = result.Id }, result);
         }
         catch (ArgumentException ex) // Captura errores de validación de Value Objects (ej. URL inválida)
         {
            _logger.LogError(ex, "Error de validación al clasificar imagen desde URL: '{ImageUrl}'. Mensaje: {ErrorMessage}", command.ImageUrl, ex.Message);
            return BadRequest(new { Message = ex.Message });
         }
         catch (InvalidOperationException ex) // Captura errores de lógica de negocio o servicios externos
         {
            _logger.LogError(ex, "Error de operación al clasificar imagen desde URL: '{ImageUrl}'. Mensaje: {ErrorMessage}", command.ImageUrl, ex.Message);
            return StatusCode(500, new { Message = ex.Message });
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error inesperado al clasificar imagen desde URL: '{ImageUrl}'. Mensaje: {ErrorMessage}", command.ImageUrl, ex.Message);
            return StatusCode(500, new { Message = "Ocurrió un error inesperado al clasificar la imagen." });
         }
      }

      // Endpoint para obtener una imagen generada por ID (GET /api/Images/{id})
      [HttpGet("{id}")]
      public async Task<IActionResult> GetById(Guid id)
      {
         _logger.LogInformation("Recibida solicitud para obtener imagen generada por ID: {Id}", id);
         var query = new GetImageByIdQuery(id);
         var result = await _getImageByIdQueryHandler.HandleAsync(query);

         if (result == null)
         {
            _logger.LogWarning("Imagen generada ID: {Id} no encontrada.", id);
            return NotFound();
         }
         _logger.LogInformation("Imagen generada ID: {Id} devuelta con éxito.", id);
         return Ok(result);
      }

      [HttpGet("{id}/base64")]
      public async Task<IActionResult> GetImageBase64(Guid id)
      {
         _logger.LogInformation("Recibida solicitud para obtener Base64 de imagen generada por ID: {Id}", id);
         var query = new GetImageBase64Query(id);
         var base64Data = await _getImageBase64QueryHandler.HandleAsync(query);

         if (string.IsNullOrEmpty(base64Data))
         {
            _logger.LogWarning("Base64 de imagen generada ID: {Id} no encontrada.", id);
            return NotFound();
         }
         _logger.LogInformation("Base64 de imagen generada ID: {Id} devuelto.", id);
         return Content(base64Data, "text/plain");
      }


      [HttpGet("{id}/classified")]
      public async Task<IActionResult> GetClassifiedById(Guid id)
      {
         _logger.LogInformation("Recibida solicitud para obtener imagen clasificada por ID: {Id}", id);
         var query = new GetClassifiedImageByIdQuery(id);
         var result = await _getClassifiedImageByIdQueryHandler.HandleAsync(query);

         if (result == null)
         {
            _logger.LogWarning("Imagen clasificada ID: {Id} no encontrada.", id);
            return NotFound();
         }
         _logger.LogInformation("Imagen clasificada ID: {Id} devuelta con éxito.", id);
         return Ok(result);
      }
   }
}