using System;

namespace ImageCreation.Application.DTOs
{
   public class ClassifiedImageDto
   {
      public Guid Id { get; set; }
      public required string OriginalUrl { get; set; }
      public required string ClassifiedImageBase64 { get; set; }
      public required string ClassificationResult { get; set; }
      public DateTime ClassifiedAt { get; set; }
   }
}