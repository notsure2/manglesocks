using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MangleSocks.Core.Util.Directory
{
    static class TypeDirectory<TInterface>
    {
        static readonly IReadOnlyDictionary<string, DirectoryDescriptor> s_DescriptorsByName = typeof(TInterface)
            .Assembly
            .GetTypes()
            .Where(x => !x.IsInterface && !x.IsAbstract && typeof(TInterface).IsAssignableFrom(x))
            .Select(
                x => new
                {
                    Type = x,
                    Descriptor = (x.GetCustomAttribute<DirectoryDescriptorAttribute>()
                                  ?? new DirectoryDescriptorAttribute(null, new string[0])).CreateDescriptor(x)
                })
            .SelectMany(
                x => new[] { x.Type.Name.Replace(typeof(TInterface).Name, "") }.Union(x.Descriptor.Aliases),
                (x, n) => new { Name = n, x.Descriptor })
            .ToDictionary(x => x.Name, x => x.Descriptor);

        public static readonly IEnumerable<DirectoryDescriptor> Descriptors = s_DescriptorsByName.Values.Distinct();

        public static DirectoryDescriptor GetDescriptorByNameOrNull(string name)
        {
            s_DescriptorsByName.TryGetValue(name, out var descriptor);
            return descriptor;
        }
    }
}