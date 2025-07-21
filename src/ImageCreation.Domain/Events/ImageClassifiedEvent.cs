using System;
using System.Text.Json.Serialization;

namespace ImageCreation.Domain.Events
{
   public class ImageClassifiedEvent : IDomainEvent
   {
      [JsonPropertyName("id")]
      public Guid Id { get; set; } 
      [JsonPropertyName("originalUrl")]
      public string OriginalUrl { get; set; } 
      [JsonPropertyName("classifiedImageBase64")]
      public string ClassifiedImageBase64 { get; set; } 
      [JsonPropertyName("classificationResult")]
      public string ClassificationResult { get; set; } 
      [JsonPropertyName("timestamp")]
      public DateTime Timestamp { get; set; } 

      [JsonConstructor] 
      public ImageClassifiedEvent(Guid id, string originalUrl, string classifiedImageBase64, string classificationResult, DateTime timestamp)
      {
         Id = id;
         OriginalUrl = originalUrl;
         ClassifiedImageBase64 = classifiedImageBase64;
         ClassificationResult = classificationResult;
         Timestamp = timestamp;
      }
   }
}