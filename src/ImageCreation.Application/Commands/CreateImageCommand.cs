
namespace ImageCreation.Application.Commands
{
   public class CreateImageCommand
   {
      public string Description { get; }
      public string? Platform { get; }  // "Public" (default) or "Azure"

      public CreateImageCommand(string description, string? platform = null)
      {
         Description = description;
         Platform = platform;
      }
   }
}
