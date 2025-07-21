using System;
using ImageCreation.Domain.Events;
using ImageCreation.Domain.ValueObjects;

namespace ImageCreation.Domain.Entities
{
    public class ImageRecord
    {
        public Guid Id { get; private set; }
        public ImageDescription Description { get; private set; }
        public Base64Data Base64Data { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private ImageRecord() { }

        public ImageRecord(Guid id, ImageDescription description, Base64Data base64Data, DateTime createdAt)
        {
            Id = id;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Base64Data = base64Data ?? throw new ArgumentNullException(nameof(base64Data));
            CreatedAt = createdAt;
        }

        public ImageCreatedEvent ToDomainEvent()
        {
            return new ImageCreatedEvent(
                Id,
                Description.Value,
                Base64Data.Value,
                CreatedAt
            );
        }
    }
}