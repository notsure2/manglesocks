using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace MangleSocks.Core.Util.Directory
{
    public class SettingsDescriptor
    {
        readonly Type _type;
        public IReadOnlyDictionary<string, PropertyDescriptor> PropertyDescriptorsByName { get; }

        public SettingsDescriptor(Type type)
        {
            this._type = type ?? throw new ArgumentNullException(nameof(type));

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetMethod.IsPublic && x.SetMethod.IsPublic)
                .ToList();

            var defaultInstance = Activator.CreateInstance(type);
            this.PropertyDescriptorsByName = properties.ToDictionary(
                x => x.Name,
                x => new PropertyDescriptor(x, defaultInstance));
        }

        public object CreateInstance()
        {
            return Activator.CreateInstance(this._type);
        }

        public void SetProperties(object instance, IEnumerable<KeyValuePair<string, string>> propertyValuesByName)
        {
            foreach (var propertyValueByName in propertyValuesByName)
            {
                if (!this.PropertyDescriptorsByName.TryGetValue(propertyValueByName.Key, out var descriptor))
                {
                    throw new ArgumentException("Invalid property name", propertyValueByName.Key);
                }

                descriptor.SetValue(instance, StringToType(propertyValueByName.Value, descriptor.PropertyType));
            }
            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }

        static object StringToType(string value, Type propertyType)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType);
            return underlyingType == null
                ? Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture)
                : (string.IsNullOrEmpty(value)
                    ? null
                    : Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture));
        }
    }
}