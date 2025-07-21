using System;

using ImageCreation.Domain.Events; // Necesario para el evento de dominio
using ImageCreation.Domain.ValueObjects; // Necesario para ImageUrl, Base64Data, ClassificationResult

namespace ImageCreation.Domain.Entities
{
   public class ClassifiedImageRecord
   {
      public Guid Id { get; private set; }
      public ImageUrl OriginalUrl { get; private set; }
      public Base64Data ClassifiedImageBase64 { get; private set; } // Podría ser ImageBase64
      public ClassificationResult ClassificationResult { get; private set; }
      public DateTime ClassifiedAt { get; private set; }

      // Constructor privado para ORMs como Dapper
      private ClassifiedImageRecord() { }

      public ClassifiedImageRecord(Guid id, ImageUrl originalUrl, Base64Data classifiedImageBase64, ClassificationResult classificationResult, DateTime classifiedAt)
      {
         Id = id;
         OriginalUrl = originalUrl ?? throw new ArgumentNullException(nameof(originalUrl));
         ClassifiedImageBase64 = classifiedImageBase64 ?? throw new ArgumentNullException(nameof(classifiedImageBase64));
         ClassificationResult = classificationResult ?? throw new ArgumentNullException(nameof(classificationResult));
         ClassifiedAt = classifiedAt;
      }

      // Método para crear un evento de dominio cuando una imagen es clasificada
      public ImageClassifiedEvent ToDomainEvent()
      {
         return new ImageClassifiedEvent(
             Id,
             OriginalUrl.Value,
             ClassifiedImageBase64.Value,
             ClassificationResult.Value,
             ClassifiedAt
         );
      }
   }
}