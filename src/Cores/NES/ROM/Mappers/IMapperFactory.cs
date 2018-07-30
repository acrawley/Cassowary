using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.CPU;
using Cassowary.Shared.Components.Memory;
using Cassowary.Core.Nes.ROM.Readers;

namespace Cassowary.Core.Nes.ROM.Mappers
{
    internal interface IMapperFactory
    {
        IMapper CreateInstance(IImageReader reader, IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq);
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
