using System;

namespace ImageCreation.Application.DTOs
{
    public class ImageDto
    {
        public Guid Id { get; set; }
        public required string Description { get; set; }
        public required string Base64Data { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
