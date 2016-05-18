using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Memory;
using NesEmulator.ROM.Readers;

namespace NesEmulator.ROM.Mappers
{
    internal interface IMapperFactory
    {
        IMapper CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus);
    }

    public interface IMapperFactoryMetadata
    {
        int MapperId { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class MapperIdAttribute : Attribute
    {
        public MapperIdAttribute(int mapperId)
        {
            this.MapperId = mapperId;
        }

        public int MapperId { get; private set; }
    }
}
