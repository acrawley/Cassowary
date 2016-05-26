using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using EmulatorCore.Components;
using EmulatorCore.Components.Core;
using EmulatorCore.Components.Debugging;
using EmulatorCore.Components.Memory;
using EmulatorCore.Extensions;

namespace EmulatorCore.Memory
{
    public class MemoryBus : IMemoryBus
    {
        #region Private Fields

        private List<MemoryMapping> mappings;
        private MemoryMapping[] mappingsArray;

        private List<MemoryMirroring> mirrorings;
        private MemoryMirroring[] mirroringsArray;

        private List<MemoryBreakpoint> disabledBreakpoints;

        private List<MemoryBreakpoint> readBreakpoints;
        private MemoryBreakpoint[] readBreakpointsArray;

        private List<MemoryBreakpoint> writeBreakpoints;
        private MemoryBreakpoint[] writeBreakpointsArray;

        private int width;
        private int size;

        #endregion

        #region Constructor

        public MemoryBus(int width, string name)
        {
            this.width = width;
            this.Name = name;
            this.size = (int)Math.Pow(2, width);

            this.mappingsArray = new MemoryMapping[0];
            this.mappings = new List<MemoryMapping>();

            this.mirroringsArray = new MemoryMirroring[0];
            this.mirrorings = new List<MemoryMirroring>();

            this.disabledBreakpoints = new List<MemoryBreakpoint>();

            this.readBreakpoints = new List<MemoryBreakpoint>();
            this.readBreakpointsArray = new MemoryBreakpoint[0];

            this.writeBreakpoints = new List<MemoryBreakpoint>();
            this.writeBreakpointsArray = new MemoryBreakpoint[0];
        }

        #endregion

        #region IEmulatorComponent Implementation

        public string Name { get; private set; }

        #endregion

        #region Helpers

        private IMemoryMappedDevice GetDeviceAtAddress(ref int address)
        {
            // This method is on a very hot path - use arrays instead of List<T>, indexed loops instead
            //  of foreach or LINQ, and inline as much as possible.  Improves perf by ~40%.
            MemoryMapping activeMapping = null;
            for (int i = 0; i < this.mappingsArray.Length; i++)
            {
                MemoryMapping mapping = this.mappingsArray[i];
                if (mapping.StartAddress <= address && mapping.EndAddress >= address)
                {
                    activeMapping = mapping;
                    break;
                }
            }

            if (activeMapping == null)
            {
                // If we weren't able to find a mapping for the address, check to see if the address is in a mirrored range
                MemoryMirroring activeMirroring = null;
                for (int i = 0; i < this.mirroringsArray.Length; i++)
                {
                    MemoryMirroring mirroring = this.mirroringsArray[i];
                    if (mirroring.MirrorStartAddress <= address && mirroring.MirrorEndAddress >= address)
                    {
                        activeMirroring = mirroring;
                        break;
                    }
                }

                if (activeMirroring != null)
                {
                    // Calculate the base address and try to find a mapping again
                    address = activeMirroring.SourceStartAddress + ((address - activeMirroring.MirrorStartAddress) % activeMirroring.SourceSize);

                    for (int i = 0; i < this.mappingsArray.Length; i++)
                    {
                        MemoryMapping mapping = this.mappingsArray[i];
                        if (mapping.StartAddress <= address && mapping.EndAddress >= address)
                        {
                            activeMapping = mapping;
                            break;
                        }
                    }
                }
            }

            return activeMapping?.Device;
        }

        #endregion

        #region IComponentWithBreakpoints Implementation

        IEnumerable<string> IComponentWithBreakpoints.SupportedBreakpointTypes
        {
            get
            {
                return new string[] { MemoryBreakpoint.BreakpointType };
            }
        }

        IBreakpoint IComponentWithBreakpoints.CreateBreakpoint(string breakpointType)
        {
            if (String.Equals(breakpointType, MemoryBreakpoint.BreakpointType, StringComparison.Ordinal))
            {
                MemoryBreakpoint bp = new MemoryBreakpoint(this);
                bp.PropertyChanged += this.OnBreakpointPropertyChanged;
                this.disabledBreakpoints.Add(bp);

                return bp;
            }

            throw new NotSupportedException("'{0}' is not a supported breakpoint type!".FormatCurrentCulture(breakpointType));
        }

        private void OnBreakpointPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MemoryBreakpoint bp = sender as MemoryBreakpoint;
            if (bp != null)
            {
                switch (e.PropertyName)
                {
                    case nameof(IMemoryBreakpoint.AccessType):
                    case nameof(IMemoryBreakpoint.Enabled):
                        this.disabledBreakpoints.Remove(bp);
                        this.readBreakpoints.Remove(bp);
                        this.writeBreakpoints.Remove(bp);

                        if (bp.Enabled)
                        {
                            if ((bp.AccessType & AccessType.Read) == AccessType.Read)
                            {
                                this.readBreakpoints.Add(bp);
                            }

                            if ((bp.AccessType & AccessType.Write) == AccessType.Write)
                            {
                                this.writeBreakpoints.Add(bp);
                            }
                        }
                        else
                        {
                            this.disabledBreakpoints.Add(bp);
                        }

                        this.readBreakpointsArray = this.readBreakpoints.ToArray();
                        this.writeBreakpointsArray = this.writeBreakpoints.ToArray();

                        break;
                }
            }
        }

        void IComponentWithBreakpoints.DeleteBreakpoint(IBreakpoint breakpoint)
        {
            MemoryBreakpoint bp = breakpoint as MemoryBreakpoint;
            if (bp != null)
            {
                bp.PropertyChanged -= this.OnBreakpointPropertyChanged;

                this.disabledBreakpoints.Remove(bp);
                this.readBreakpoints.Remove(bp);
                this.writeBreakpoints.Remove(bp);

                this.readBreakpointsArray = this.readBreakpoints.ToArray();
                this.writeBreakpointsArray = this.writeBreakpoints.ToArray();
            }
        }

        #endregion

        #region IMemoryBus Implementation

        byte IMemoryBus.Read(int address)
        {
            if (this.readBreakpointsArray.Length != 0)
            {
                for (int i = 0; i < this.readBreakpointsArray.Length; i++)
                {
                    if (this.readBreakpointsArray[i].TargetAddress == address)
                    {
                        this.readBreakpointsArray[i].OnBreakpointHit();
                    }
                }
            }

            IMemoryMappedDevice device = this.GetDeviceAtAddress(ref address);
            if (device == null)
            {
                Debug.WriteLine("Read of unmapped address 0x{0,8:X8} on bus '{1}'!", address, this.Name);
                return 4; // Uninitialized memory contents, generated by fair dice roll;
            }

            return device.Read(address);
        }

        void IMemoryBus.Write(int address, byte value)
        {
            if (this.writeBreakpointsArray.Length != 0)
            {
                for (int i = 0; i < this.writeBreakpointsArray.Length; i++)
                {
                    if (this.writeBreakpointsArray[i].TargetAddress == address)
                    {
                        this.writeBreakpointsArray[i].OnBreakpointHit();
                    }
                }
            }

            IMemoryMappedDevice device = this.GetDeviceAtAddress(ref address);
            if (device == null)
            {
                Debug.WriteLine("Write of unmapped address 0x{0:X8} = 0x{1:X2} on bus '{2}'!", address, value, this.Name);
                return;
            }

            device.Write(address, value);
        }

        IEnumerable<IMemoryMapping> IMemoryBus.Mappings
        {
            get
            {
                return new ReadOnlyCollection<IMemoryMapping>(this.mappings.Cast<IMemoryMapping>().ToList());
            }
        }

        IMemoryMapping IMemoryBus.RegisterMappedDevice(IMemoryMappedDevice device, int startAddress, int endAddress)
        {
            MemoryMapping mapping = new MemoryMapping(device, startAddress, endAddress);
            this.mappings.Add(mapping);
            this.mappingsArray = this.mappings.ToArray();

            return mapping;
        }

        void IMemoryBus.RemoveMapping(IMemoryMapping mapping)
        {
            MemoryMapping mappingToRemove = this.mappings.FirstOrDefault(m =>
                m.Device == mapping.Device &&
                m.StartAddress == mapping.StartAddress &&
                m.EndAddress == mapping.EndAddress);

            if (mappingToRemove != null)
            {
                this.mappings.Remove(mappingToRemove);
                this.mappingsArray = this.mappings.ToArray();
            }
        }

        IEnumerable<IMemoryMirroring> IMemoryBus.Mirrorings
        {
            get
            {
                return new ReadOnlyCollection<IMemoryMirroring>(this.mirrorings.Cast<IMemoryMirroring>().ToList());
            }
        }

        IMemoryMirroring IMemoryBus.SetMirroringRange(int sourceStartAddress, int sourceEndAddress, int mirrorStartAddress, int mirrorEndAddress)
        {
            int sourceSize = (sourceEndAddress - sourceStartAddress) + 1;
            int mirrorSize = (mirrorEndAddress - mirrorStartAddress) + 1;
            if (mirrorSize % sourceSize != 0)
            {
                throw new InvalidOperationException("Size of mirrored range must be a multiple of size of source range!");
            }

            MemoryMirroring mirroring = new MemoryMirroring(sourceStartAddress, sourceSize, mirrorStartAddress, mirrorEndAddress);
            this.mirrorings.Add(mirroring);
            this.mirroringsArray = this.mirrorings.ToArray();

            return mirroring;
        }

        void IMemoryBus.RemoveMirroring(IMemoryMirroring mirroring)
        {
            MemoryMirroring mirroringToRemove = this.mirroringsArray.FirstOrDefault(m =>
                m.SourceStartAddress == mirroring.SourceStartAddress &&
                m.SourceSize == mirroring.SourceSize &&
                m.MirrorStartAddress == mirroring.MirrorStartAddress &&
                m.MirrorEndAddress == mirroring.MirrorEndAddress);

            if (mirroringToRemove != null)
            {
                this.mirrorings.Remove(mirroringToRemove);
                this.mirroringsArray = this.mirrorings.ToArray();
            }
        }

        #endregion
    }
}
