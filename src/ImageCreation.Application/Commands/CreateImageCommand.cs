// ImageCreation.Application.Commands/CreateImageCommand.cs
namespace ImageCreation.Application.Commands
{
   public class CreateImageCommand
   {
      public string Description { get; }
      public string? PlatformRequested { get; }  // Renombrado a PlatformRequested

      public CreateImageCommand(string description, string? platformRequested = null) // Renombrado
      {
         Description = description;
         PlatformRequested = platformRequested; // Renombrado
      }
   }
}