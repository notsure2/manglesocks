using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MangleSocks.Core.Util.Directory
{
    static class ImplDirectory<TInterface>
    {
        static readonly IReadOnlyDictionary<string, ImplDescriptor> s_DescriptorsByName = typeof(TInterface)
            .Assembly
            .GetTypes()
            .Where(x => !x.IsInterface && !x.IsAbstract && typeof(TInterface).IsAssignableFrom(x))
            .Select(
                x => new
                {
                    Type = x,
                    Descriptor = (x.GetCustomAttribute<ImplDescriptorAttribute>()
                                  ?? new ImplDescriptorAttribute(null, new string[0])).CreateDescriptor(x)
                })
            .SelectMany(
                x => new[] { x.Type.Name.Replace(typeof(TInterface).Name, "") }.Union(x.Descriptor.Aliases),
                (x, n) => new { Name = n, x.Descriptor })
            .ToDictionary(x => x.Name, x => x.Descriptor);

        public static readonly IEnumerable<ImplDescriptor> Descriptors = s_DescriptorsByName.Values.Distinct();

        public static ImplDescriptor GetDescriptorByNameOrNull(string name)
        {
            s_DescriptorsByName.TryGetValue(name, out var descriptor);
            return descriptor;
        }
    }
}