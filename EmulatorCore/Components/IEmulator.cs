using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using EmulatorCore.Components.Core;

namespace EmulatorCore.Components
{
    public interface IEmulatorFactory
    {
        IEmulator CreateInstance();
    }

    public interface IEmulator
    {
        void LoadFile(string fileName);
        void Run();
        void Stop();
        IEnumerable<IEmulatorComponent> Components { get; }
    }

    public interface IEmulatorFactoryMetadata
    {
        string EmulatorName { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EmulatorNameAttribute : Attribute
    {
        public EmulatorNameAttribute(string emulatorName)
        {
            this.EmulatorName = emulatorName;
        }

        public string EmulatorName { get; private set; }
    }
}
