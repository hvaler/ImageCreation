using EventStore.Client;
using ImageCreation.Domain.Events;
using ImageCreation.Application.Projections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;

namespace ImageCreation.Infrastructure.Services.EventSourcing
{
   public class EventStoreSubscriptionService : IHostedService
   {
      private readonly EventStoreClient _client;
      private readonly ILogger<EventStoreSubscriptionService> _logger;
      private readonly IServiceScopeFactory _scopeFactory;

      // Opciones de deserialización de Newtonsoft.Json
      private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
      {
         ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
         DateParseHandling = DateParseHandling.DateTimeOffset,
     
      };

      public EventStoreSubscriptionService(EventStoreClient client, ILogger<EventStoreSubscriptionService> logger, IServiceScopeFactory scopeFactory)
      {
         _client = client ?? throw new ArgumentNullException(nameof(client));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
         _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
      }

      public async Task StartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Starting EventStoreDB Subscription Service...");

         await _client.SubscribeToAllAsync(
             FromAll.Start,
             EventAppeared,
             subscriptionDropped: SubscriptionDropped,
             filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
             cancellationToken: cancellationToken
         );

         _logger.LogInformation("Subscribed to EventStoreDB $all stream.");
      }

      private async Task EventAppeared(StreamSubscription subscription, ResolvedEvent resolvedEvent, CancellationToken cancellationToken)
      {
         // Omitir eventos de sistema
         if (resolvedEvent.Event.EventType.StartsWith('$')) return;

         _logger.LogInformation("Event '{EventType}' appeared. StreamId: {StreamId}, EventId: {EventId}, Position: {Position}",
             resolvedEvent.Event.EventType, resolvedEvent.Event.EventStreamId, resolvedEvent.Event.EventId, resolvedEvent.Event.Position);

         using (var scope = _scopeFactory.CreateScope())
         {
            var imageProjector = scope.ServiceProvider.GetRequiredService<ImageRecordProjector>();
            var classifiedImageProjector = scope.ServiceProvider.GetRequiredService<ClassifiedImageRecordProjector>();

            try
            {
               Type? eventType = AppDomain.CurrentDomain.GetAssemblies()
                                          .Select(a => a.GetType($"ImageCreation.Domain.Events.{resolvedEvent.Event.EventType}"))
                                          .FirstOrDefault(t => t != null);

               if (eventType == null)
               {
                  _logger.LogWarning("Unknown event type: {EventType}. Skipping. Could not find type in any loaded assembly.",
                      resolvedEvent.Event.EventType);
                  return;
               }

               // ¡Usar Newtonsoft.Json para deserializar!
               string json = System.Text.Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
               IDomainEvent? domainEvent = JsonConvert.DeserializeObject(json, eventType, _jsonSerializerSettings) as IDomainEvent;


               if (domainEvent == null)
               {
                  _logger.LogError("Failed to deserialize event data to IDomainEvent for type: {EventType}. Raw data: {RawData}",
                      resolvedEvent.Event.EventType, json); // Usar 'json' aquí
                  return;
               }

               switch (domainEvent)
               {
                  case ImageCreatedEvent imageCreatedEvent:
                     await imageProjector.ProjectAsync(imageCreatedEvent);
                     break;
                  case ImageClassifiedEvent imageClassifiedEvent:
                     await classifiedImageProjector.ProjectAsync(imageClassifiedEvent);
                     break;
                  default:
                     _logger.LogWarning("No handler found for event type: {EventType}", domainEvent.GetType().Name);
                     break;
               }
            }
            catch (JsonException ex) // Newtonsoft.Json.JsonException
            {
               _logger.LogError(ex, "Error deserializing event data for '{EventType}'. Raw data: {RawData}",
                   resolvedEvent.Event.EventType, System.Text.Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray()));
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Error processing event '{EventType}': {ErrorMessage}. EventData: {EventData}",
                   resolvedEvent.Event.EventType, ex.Message, System.Text.Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray()));
            }
         }
      }

      private void SubscriptionDropped(StreamSubscription subscription, SubscriptionDroppedReason reason, Exception? exception)
      {
         _logger.LogError(exception, "EventStoreDB Subscription Dropped. Reason: {Reason}", reason);
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Stopping EventStoreDB Subscription Service...");
         return Task.CompletedTask;
      }
   }
}