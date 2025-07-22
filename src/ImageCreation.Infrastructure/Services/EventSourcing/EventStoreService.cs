using EventStore.Client;
using ImageCreation.Domain.Events;
using ImageCreation.Application.Interfaces; // Para IEventStore
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
// using System.Text.Json; // <-- ELIMINAR ESTE USING
using Newtonsoft.Json;

namespace ImageCreation.Infrastructure.Services.EventSourcing
{
   public class EventStoreService : IEventStore
   {
      private readonly EventStoreClient _client;
      private readonly ILogger<EventStoreService> _logger;

      // Las opciones de serialización de Newtonsoft.Json
      private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
      {
         ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(), // Serializa a camelCase
         Formatting = Formatting.None, // No usar formato para ahorrar espacio
         DateParseHandling = DateParseHandling.DateTimeOffset, // Para un manejo robusto de fechas
         // Añadir esto si los atributos [JsonPropertyName] no son suficientes
         // DefaultValueHandling = DefaultValueHandling.Ignore, // Ignora propiedades con valores por defecto (Guid.Empty, null)
         // NullValueHandling = NullValueHandling.Ignore // Ignora propiedades null
      };


      public EventStoreService(IConfiguration config, ILogger<EventStoreService> logger)
      {
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));

         string? connectionString = config.GetConnectionString("EventStoreConnection");
         if (string.IsNullOrWhiteSpace(connectionString))
         {
            _logger.LogError("EventStoreConnection no encontrado o vacío en la configuración.");
            throw new InvalidOperationException("EventStoreConnection no está configurado en appsettings.json.");
         }

         try
         {
            var settings = EventStoreClientSettings.Create(connectionString);
            _client = new EventStoreClient(settings);
            _logger.LogInformation("EventStoreService inicializado con éxito para el endpoint: {Endpoint}", settings.ConnectivitySettings.Address);
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error al inicializar EventStoreClient con la cadena de conexión: {ConnectionString}", connectionString);
            throw;
         }
      }

      public async Task PublishAsync(IDomainEvent domainEvent)
      {
         if (domainEvent == null)
         {
            _logger.LogWarning("PublishAsync llamado con un domainEvent nulo.");
            throw new ArgumentNullException(nameof(domainEvent));
         }

         byte[] eventDataBytes;
         try
         {
            // ¡Usar Newtonsoft.Json para serializar!
            string json = JsonConvert.SerializeObject(domainEvent, _jsonSerializerSettings);
            eventDataBytes = System.Text.Encoding.UTF8.GetBytes(json);
         }
         catch (Exception ex) 
         {
            _logger.LogError(ex, "Error al serializar el evento '{EventType}' a JSON con Newtonsoft.Json.", domainEvent.GetType().Name);
            throw new InvalidOperationException($"Error al serializar el evento {domainEvent.GetType().Name}.", ex);
         }

         var eventData = new EventData(
             Uuid.NewUuid(),
             domainEvent.GetType().Name, 
             eventDataBytes,
             contentType: "application/json"
         );

         string streamName = "$et-ImageCreated"; 

         _logger.LogInformation("Publicando evento '{EventType}' en el stream '{StreamName}'. EventId: {EventId}",
             domainEvent.GetType().Name, streamName, eventData.EventId);

         try
         {
            await _client.AppendToStreamAsync(
                streamName,
                StreamState.Any,
                new[] { eventData }
            );
            _logger.LogInformation("Evento '{EventType}' publicado exitosamente en el stream '{StreamName}'.",
                domainEvent.GetType().Name, streamName);
         }
         catch (WrongExpectedVersionException ex)
         {
            _logger.LogError(ex, "Error de versión esperada al publicar el evento '{EventType}' en el stream '{StreamName}'. Mensaje: {ErrorMessage}",
                domainEvent.GetType().Name, streamName, ex.Message);
            throw;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error inesperado al publicar el evento '{EventType}' en el stream '{StreamName}'. Mensaje: {ErrorMessage}",
                domainEvent.GetType().Name, streamName, ex.Message);
            throw;
         }
      }
   }
}