using System;
using System.Linq; // Necesario para .Any() y .All()
using System.Collections.Generic; // Necesario para HashSet

namespace ImageCreation.Domain.ValueObjects
{
   public class ClassificationResult
   {
      public string Value { get; }

      public ClassificationResult(string value)
      {
         if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El resultado de la clasificación no puede estar vacío.", nameof(value));

         // Convertimos la cadena de entrada a un conjunto de categorías individuales en minúsculas
         var individualCategories = value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim().ToLowerInvariant())
                                       .ToHashSet();

         // Definimos las categorías individuales válidas que esperamos
         var allowedIndividualCategories = new HashSet<string> { "food", "person", "none" };

         // --- VALIDACIÓN MEJORADA ---
         // 1. Verificar que no esté vacío después de dividir
         if (!individualCategories.Any())
         {
            throw new ArgumentException("El resultado de la clasificación no puede estar vacío o contener solo espacios en blanco.", nameof(value));
         }

         // 2. Verificar que todas las categorías individuales sean válidas (food, person, none)
         if (!individualCategories.All(c => allowedIndividualCategories.Contains(c)))
         {
            throw new ArgumentException($"El resultado de la clasificación contiene categorías no reconocidas: '{value}'. Las categorías permitidas son 'Food', 'Person' o 'None', y pueden ser combinadas con ', '. Por ejemplo: 'Food, Person'.", nameof(value));
         }

         // 3. Verificar que 'none' no esté presente con otras categorías
         if (individualCategories.Contains("none") && individualCategories.Count > 1)
         {
            throw new ArgumentException("El resultado de la clasificación no puede contener 'None' junto con otras categorías.", nameof(value));
         }

         // --- NORMALIZACIÓN DEL VALOR ---
         // Construimos la cadena Value final de forma consistente
         if (individualCategories.Count > 1)
         {
            // Si hay múltiples categorías (ej. "food", "person"), las ordenamos alfabéticamente
            // y las unimos con ", " para un formato consistente (ej. "Food, Person")
            var orderedCategories = individualCategories.OrderBy(c => c).ToList();
            Value = string.Join(", ", orderedCategories.Select(c => char.ToUpper(c[0]) + c.Substring(1)));
         }
         else // Si es una sola categoría (ej. "food", "person", o "none")
         {
            // Capitalizamos la primera letra de la única categoría (ej. "food" -> "Food")
            // y nos aseguramos de que el resto de la palabra sea minúscula.
            // Usamos First() porque sabemos que hay al menos una categoría en individualCategories.
            var singleCategory = individualCategories.First();
            Value = char.ToUpper(singleCategory[0]) + singleCategory.Substring(1).ToLowerInvariant();
         }
      }

      public override string ToString() => Value;

      // Opcional: Implementar Equals y GetHashCode para igualdad por valor
      public override bool Equals(object? obj)
      {
         return obj is ClassificationResult other &&
                Value == other.Value;
      }

      public override int GetHashCode()
      {
         return HashCode.Combine(Value);
      }
   }
}