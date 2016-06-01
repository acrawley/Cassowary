using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using EmulatorCore.Components;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.Memory;
using NesEmulator.ROM.Mappers;
using NesEmulator.ROM.Readers;
using System.Globalization;
using EmulatorCore.Components.CPU;

namespace NesEmulator.ROM
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
