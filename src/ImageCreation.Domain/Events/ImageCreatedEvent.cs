using System;
using System.Text.Json.Serialization;

namespace ImageCreation.Domain.Events
{
   public class ImageCreatedEvent : IDomainEvent
   {
      [JsonPropertyName("id")]
      public Guid Id { get; set; } 
      [JsonPropertyName("description")]
      public string Description { get; set; } 
      [JsonPropertyName("base64Data")]
      public string Base64Data { get; set; } 
      [JsonPropertyName("timestamp")]
      public DateTime Timestamp { get; set; } 

      [JsonConstructor] 
      public ImageCreatedEvent(Guid id, string description, string base64Data, DateTime timestamp)
      {
         Id = id;
         Description = description;
         Base64Data = base64Data;
         Timestamp = timestamp;
      }
   }
}