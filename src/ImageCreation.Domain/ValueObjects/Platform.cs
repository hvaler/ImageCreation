// ImageCreation.Domain.ValueObjects/Platform.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageCreation.Domain.ValueObjects
{
   public class Platform
   {
      public string Value { get; }

      private static readonly HashSet<string> AllowedPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "public", "azure", "stability", "google", "huggingface","gemini"
        };

      public Platform(string value)
      {
         if (string.IsNullOrWhiteSpace(value))
         {
            throw new ArgumentException("La plataforma no puede estar vacía.", nameof(value));
         }

         // Normaliza el valor a minúsculas para la validación interna
         var normalizedValue = value.ToLowerInvariant();

         if (!AllowedPlatforms.Contains(normalizedValue))
         {
            throw new ArgumentException($"Plataforma no reconocida: '{value}'. Plataformas permitidas: {string.Join(", ", AllowedPlatforms)}", nameof(value));
         }

         // Guarda el valor normalizado (primera letra mayúscula para consistencia si lo deseas)
         // O simplemente guarda el valor normalizado si prefieres todo en minúsculas.
         Value = char.ToUpper(normalizedValue[0]) + normalizedValue.Substring(1);
      }

      public override string ToString() => Value;

      public override bool Equals(object? obj)
      {
         return obj is Platform other && Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
      }

      public override int GetHashCode()
      {
         return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
      }
   }
}