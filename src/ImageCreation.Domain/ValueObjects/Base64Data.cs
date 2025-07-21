using System;

namespace ImageCreation.Domain.ValueObjects
{
    public class Base64Data
    {
        public string Value { get; }

        public Base64Data(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Los datos Base64 no pueden estar vacíos.", nameof(value));

            try
            {
                Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                throw new ArgumentException("El valor proporcionado no es un Base64 válido.", nameof(value));
            }

            Value = value;
        }

        public override string ToString() => Value;
    }
}