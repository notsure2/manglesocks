using System;
using System.ComponentModel.DataAnnotations;

namespace MangleSocks.Core.DataAnnotations
{
    public class GreaterThanOrEqualToAttribute : ValidationAttribute
    {
        readonly string _otherPropertyName;

        public GreaterThanOrEqualToAttribute(string otherPropertyName) : base("{0} must be greater than {1}")
        {
            if (string.IsNullOrWhiteSpace(otherPropertyName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(otherPropertyName));
            }

            this._otherPropertyName = otherPropertyName;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(this.ErrorMessageString, name, this._otherPropertyName);
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null) throw new ArgumentNullException(nameof(validationContext));

            var firstComparable = value as IComparable;

            var secondProperty = validationContext
                .ObjectType
                .GetProperty(this._otherPropertyName);

            if (secondProperty == null)
            {
                throw new InvalidOperationException($"Property '{this._otherPropertyName}' could not be found.");
            }

            var secondComparable = secondProperty.GetValue(validationContext.ObjectInstance) as IComparable;

            if (firstComparable != null && secondComparable != null && firstComparable.CompareTo(secondComparable) < 0)
            {
                return new ValidationResult(this.FormatErrorMessage(validationContext.MemberName));
            }

            return ValidationResult.Success;
        }
    }
}