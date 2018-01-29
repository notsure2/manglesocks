using System;
using System.Collections.Generic;

namespace MangleSocks.Core.Util.Directory
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class ImplDescriptorAttribute : Attribute
    {
        readonly Type _settingsType;
        readonly IReadOnlyList<string> _aliases;

        public SettingsDescriptor SettingsDescriptor { get; }

        public ImplDescriptorAttribute(params string[] aliases) : this(null, aliases) { }

        public ImplDescriptorAttribute(Type settingsType, params string[] aliases)
        {
            this._settingsType = settingsType;
            this._aliases = aliases;

            if (settingsType != null)
            {
                this.SettingsDescriptor = new SettingsDescriptor(settingsType);
            }
        }

        public ImplDescriptor CreateDescriptor(Type decoratedType)
        {
            return new ImplDescriptor(decoratedType, this._aliases, this.SettingsDescriptor);
        }
    }
}