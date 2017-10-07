using System;
using System.ComponentModel;
using System.Reflection;

namespace MangleSocks.Core.Util.Directory
{
    public class PropertyDescriptor
    {
        public string PropertyName { get; }
        public Type PropertyType { get; }
        public string Description { get; }
        public Func<object, object> GetValue { get; }
        public Action<object, object> SetValue { get; }
        public object DefaultValue { get; }

        public PropertyDescriptor(PropertyInfo property, object containerInstance)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (containerInstance == null) throw new ArgumentNullException(nameof(containerInstance));

            this.PropertyName = property.Name;
            this.Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            this.PropertyType = property.PropertyType;
            this.GetValue = property.GetValue;
            this.SetValue = property.SetValue;
            this.DefaultValue = this.GetValue(containerInstance);
        }
    }
}