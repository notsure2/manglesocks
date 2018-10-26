using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Humanizer;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Conventions;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace MangleSocks.Cli.CommandLine.Conventions
{
    class EnrichOptionsConvention : IConvention
    {
        static readonly FieldInfo s_ShortOptionsField = typeof(CommandLineApplication).GetField(
            "_shortOptions",
            BindingFlags.NonPublic | BindingFlags.Instance);

        static readonly FieldInfo s_LongOptionsField = typeof(CommandLineApplication).GetField(
            "_longOptions",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public void Apply(ConventionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (s_ShortOptionsField == null || s_LongOptionsField == null)
            {
                throw new InvalidOperationException(
                    "BUG: Could not find CommandLineApplication._shortOptions or CommandLineApplication._longOptions fields.");
            }

            if (context.ModelAccessor == null)
            {
                return;
            }

            var shortOptions = s_ShortOptionsField.GetValue(context.Application) as Dictionary<string, PropertyInfo>;
            var longOptions = s_LongOptionsField.GetValue(context.Application) as Dictionary<string, PropertyInfo>;

            if (shortOptions == null || longOptions == null)
            {
                throw new InvalidOperationException(
                    "BUG: CommandLineApplication._shortOptions or CommandLineApplication._longOptions fields are null.");
            }

            var propertiesByOption = shortOptions.Concat(longOptions)
                .Select(
                    x => new
                    {
                        Option = context.Application.Options.FirstOrDefault(
                            o => o.ShortName == x.Key
                                 || o.LongName == x.Key),
                        Property = x.Value
                    })
                .Where(x => x.Option != null)
                .Distinct();

            var defaultModelInstance = context.ModelAccessor.GetModel();
            foreach (var item in propertiesByOption)
            {
                var property = item.Property;
                var option = item.Option;

                EnrichDescription(property, defaultModelInstance, option);
                EnrichValidation(property, option);
            }
        }

        static void EnrichDescription(PropertyInfo property, object defaultModelInstance, CommandOption option)
        {
            var description = property.GetCustomAttribute<OptionAttribute>()?.Description
                              ?? property.GetCustomAttribute<DisplayAttribute>()?.GetDescription()
                              ?? property.GetCustomAttribute<DescriptionAttribute>()?.Description
                              ?? property.Name.Humanize();

            if (property.PropertyType.IsEnum)
            {
                description += Environment.NewLine
                               + "(one of: "
                               + string.Join(
                                   ", ",
                                   Enum.GetNames(property.PropertyType))
                               + ")";
            }

            var defaultValue = property.GetValue(defaultModelInstance);
            if (defaultValue != null
                && !property.PropertyType.IsValueType
                || defaultValue != Activator.CreateInstance(property.PropertyType))
            {
                description += Environment.NewLine + "(default: " + defaultValue + ")";
            }

            option.Description = description;
        }

        static void EnrichValidation(MemberInfo property, CommandOption option)
        {
            var validationAttributes = property.GetCustomAttributes<ValidationAttribute>();

            foreach (var attribute in validationAttributes)
            {
                option.Accepts().Use(new AttributeValidator(attribute));
            }
        }
    }
}