using System;

namespace Cassowary.Services.Configuration
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class ValidTypeAttribute : Attribute
    {
        public ValidTypeAttribute(Type validType)
        {
            this.ValidType = validType;
        }

        public Type ValidType { get; private set; }
    }
}
