using System;
using System.Collections.Generic;

namespace MangleSocks.Core.Util.Directory
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class DirectoryDescriptorAttribute : Attribute
    {
        readonly Type _settingsType;
        readonly IReadOnlyList<string> _aliases;

        public SettingsDescriptor SettingsDescriptor { get; }

        public DirectoryDescriptorAttribute(params string[] aliases) : this(null, aliases) { }

        public DirectoryDescriptorAttribute(Type settingsType, params string[] aliases)
        {
            this._settingsType = settingsType;
            this._aliases = aliases;

            if (settingsType != null)
            {
                this.SettingsDescriptor = new SettingsDescriptor(settingsType);
            }
        }

        public DirectoryDescriptor CreateDescriptor(Type decoratedType)
        {
            return new DirectoryDescriptor(decoratedType, this._aliases, this.SettingsDescriptor);
        }
    }
}