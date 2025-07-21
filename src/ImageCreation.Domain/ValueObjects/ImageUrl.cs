using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCreation.Domain.ValueObjects
{
   public class ImageUrl
   {
      public string Value { get; }

      public ImageUrl(string value)
      {
         if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("La URL de la imagen no puede estar vacía.", nameof(value));

         // Podemos añadir una validación de formato de URL más robusta aquí si es necesario
         if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            throw new ArgumentException("La URL proporcionada no es un formato válido.", nameof(value));

         Value = value;
      }

      public override string ToString() => Value;

      // Opcional: Implementar Equals y GetHashCode para igualdad por valor
      public override bool Equals(object? obj)
      {
         return obj is ImageUrl other &&
                Value == other.Value;
      }

      public override int GetHashCode()
      {
         return HashCode.Combine(Value);
      }
   }
}
