// ImageCreation.Application.DTOs/ImageDto.cs
using System;

namespace ImageCreation.Application.DTOs
{
   public class ImageDto
   {
      public Guid Id { get; set; }
      public required string Description { get; set; }
      public required string Base64Data { get; set; }
      public required string PlatformUsed { get; set; } // ¡NUEVO!
      public DateTime CreatedAt { get; set; }
   }
}