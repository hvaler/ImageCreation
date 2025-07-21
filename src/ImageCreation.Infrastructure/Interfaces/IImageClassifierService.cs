using System.Threading.Tasks;

using ImageCreation.Domain.ValueObjects; // Necesario para ImageUrl y ClassificationResult

namespace ImageCreation.Infrastructure.Interfaces
{
   public interface IImageClassifierService
   {
      /// <summary>
      /// Descarga una imagen de la URL y la clasifica (simulado) como comida o persona.
      /// </summary>
      /// <param name="imageUrl">La URL de la imagen a clasificar.</param>
      /// <returns>Una tupla con los datos Base64 de la imagen y el resultado de la clasificación.</returns>
      Task<(string base64Data, string classification)> ClassifyImageAsync(ImageUrl imageUrl);
   }
}