using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace MangleSocks.Core.Util.Directory
{
    public class ImplDescriptor
    {
        public const string Default = "default";

        public Type Type { get; }
        public string Identifier { get; }
        public IReadOnlyList<string> Aliases { get; }
        public SettingsDescriptor SettingsDescriptor { get; }

        public ImplDescriptor(Type type, IReadOnlyList<string> aliases, SettingsDescriptor settingsDescriptor)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.Aliases = (aliases ?? new string[0]).Union(new[] { type.Name }).ToList();
            this.Identifier = this.Aliases.First();
            this.SettingsDescriptor = settingsDescriptor;
        }

        public T CreateInstance<T>(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            if (!typeof(T).IsAssignableFrom(this.Type))
            {
                throw new ArgumentException(
                    $"'{typeof(T).Name}' is not assignable to '{this.Type.Name}'",
                    nameof(T));
            }

            return (T)ActivatorUtilities.CreateInstance(serviceProvider, this.Type);
        }

        public static ImplDescriptor GetDefault<T>()
        {
            return ImplDirectory<T>.GetDescriptorByNameOrNull(Default)
                   ?? throw new InvalidOperationException($"No default impl has been defined for {typeof(T).Name}");
        }

        public static ImplDescriptor GetByNameOrNull<T>(string name)
        {
            return ImplDirectory<T>.GetDescriptorByNameOrNull(name);
        }

        public static IEnumerable<ImplDescriptor> GetAll<T>()
        {
            return ImplDirectory<T>.Descriptors;
        }
    }
}