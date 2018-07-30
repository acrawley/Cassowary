using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Core.Nes.ROM.Readers
{
    internal interface IImageReaderFactory
    {
        IImageReader CreateImageReader(Stream imageStream);
    }
}
