using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Cassowary.Shared.Components;
using Cassowary.Shared.Components.Core;
using Cassowary.Shared.Components.Memory;
using Cassowary.Core.Nes.ROM.Mappers;
using Cassowary.Core.Nes.ROM.Readers;
using System.Globalization;
using Cassowary.Shared.Components.CPU;
using System.Diagnostics;
using Cassowary.Shared.Extensions;

namespace Cassowary.Core.Nes.ROM
{
    internal class NesRomLoader : IEmulatorComponent
    {
        #region MEF Imports

        [ImportMany(typeof(IImageReaderFactory))]
        private IEnumerable<IImageReaderFactory> ImageReaderFactories { get; set; }

        [ImportMany(typeof(IMapperFactory))]
        private IEnumerable<Lazy<IMapperFactory, IMapperFactoryMetadata>> MapperFactories { get; set; }

        #endregion

        #region Private Fields

        private IMemoryBus cpuBus;
        private IMemoryBus ppuBus;
        private IProcessorInterrupt irq;

        private IMapper currentMapper;

        private Stream imageStream;
        private IImageReader reader;

        #endregion

        #region Constructor

        internal NesRomLoader(IMemoryBus cpuBus, IMemoryBus ppuBus, IProcessorInterrupt irq)
        {
            this.cpuBus = cpuBus;
            this.ppuBus = ppuBus;
            this.irq = irq;
        }

        #endregion

        #region Public API

        public void LoadRom(string fileName)
        {
            if (this.imageStream != null)
            {
                this.imageStream.Close();
                this.imageStream.Dispose();
            }

            this.imageStream = File.OpenRead(fileName);

            this.reader = this.GetReader();
            if (reader == null)
            {
                throw new InvalidOperationException("Not a valid ROM!");
            }

            Trace.WriteLine("Loaded ROM: {0}".FormatCurrentCulture(fileName));
            Trace.WriteLine("  PRG ROM: {0} bytes".FormatCurrentCulture(this.reader.PrgRomSize));
            Trace.WriteLine("  PRG RAM: {0} bytes".FormatCurrentCulture(this.reader.PrgRamSize));
            Trace.WriteLine("  CHR ROM: {0} bytes".FormatCurrentCulture(this.reader.ChrRomSize));
            Trace.WriteLine("  Mapper ID: {0}".FormatCurrentCulture(this.reader.Mapper));

            IMapperFactory mapperFactory = this.MapperFactories.FirstOrDefault(f => f.Metadata.MapperId == this.reader.Mapper)?.Value;
            if (mapperFactory == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "No mapper available with ID '{0}'!", this.reader.Mapper));
            }

            if (this.currentMapper != null)
            {
                // Dispose the old mapper to unhook it from the memory buses
                this.currentMapper.Dispose();
                this.currentMapper = null;
            }

            this.currentMapper = mapperFactory.CreateInstance(this.reader, this.cpuBus, this.ppuBus, this.irq);

            Trace.WriteLine("  Mapper name: {0}".FormatCurrentCulture(this.currentMapper.Name));
        }

        #endregion

        private IImageReader GetReader()
        {
            IImageReader reader;

            foreach (IImageReaderFactory factory in this.ImageReaderFactories)
            {
                reader = factory.CreateImageReader(this.imageStream);
                if (reader.IsValidImage)
                {
                    return reader;
                }
            }

            return null;
        }

        #region IEmulatorComponent Implementation

        public string Name
        {
            get { return "NES ROM Loader"; }
        }

        #endregion
    }
}
