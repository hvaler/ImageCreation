namespace ImageCreation.Application.Interfaces
{
   public interface IOpenAiServiceFactory
   {
      // Este método devolverá la implementación correcta de IOpenAiService
      // basándose en la plataforma solicitada.
      IOpenAiService GetService(string platform);
   }
}