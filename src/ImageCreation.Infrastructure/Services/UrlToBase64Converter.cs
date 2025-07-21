using ImageCreation.Application.Interfaces;

using Microsoft.Extensions.Logging; 

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImageCreation.Infrastructure.Services
{
   public class UrlToBase64Converter : IUrlConverterService
   {
      private readonly HttpClient _httpClient;
      private readonly ILogger<UrlToBase64Converter> _logger;

      public UrlToBase64Converter(HttpClient httpClient, ILogger<UrlToBase64Converter> logger)
      {
         _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      }

      /// <summary>
      /// Descarga una imagen de una URL y la convierte a cadena Base64.
      /// </summary>
      /// <param name="imageUrl">La URL de la imagen.</param>
      /// <returns>La imagen como cadena Base64, o null si falla.</returns>
      public async Task<string?> ConvertUrlToBase64Async(string imageUrl)
      {
         try
         {
            _logger.LogInformation("Descargando imagen de URL: {ImageUrl}", imageUrl);
            byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
            string base64String = Convert.ToBase64String(imageBytes);
            _logger.LogInformation("Imagen descargada y convertida a Base64 con éxito desde URL: {ImageUrl}", imageUrl);
            return base64String;
         }
         catch (HttpRequestException ex)
         {
            _logger.LogError(ex, "Error HTTP al descargar imagen de URL: {ImageUrl}. Mensaje: {ErrorMessage}", imageUrl, ex.Message);
            return null;
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error inesperado al convertir URL a Base64 para URL: {ImageUrl}. Mensaje: {ErrorMessage}", imageUrl, ex.Message);
            return null;
         }
      }
   }
}