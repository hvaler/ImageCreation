using System;

namespace ImageCreation.Domain.ValueObjects
{
    public class ImageDescription
    {
        public string Value { get; }

        public ImageDescription(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("La descripción no puede estar vacía.", nameof(value));
            if (value.Length > 500)
                throw new ArgumentException("La descripción excede el límite de 500 caracteres.", nameof(value));

            Value = value;
        }

        public override string ToString() => Value;

        // Implementar Equals y GetHashCode si es necesario
    }
}