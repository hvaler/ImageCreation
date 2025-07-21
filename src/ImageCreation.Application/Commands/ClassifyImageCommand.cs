using ImageCreation.Domain.ValueObjects; // Necesario para ImageUrl

namespace ImageCreation.Application.Commands
{
   public class ClassifyImageCommand
   {
      public string ImageUrl { get; }

      public ClassifyImageCommand(string imageUrl)
      {
         ImageUrl = imageUrl;
      }
   }
}