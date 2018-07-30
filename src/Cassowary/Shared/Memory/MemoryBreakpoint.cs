using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Debugging;
using Cassowary.Shared.Components.Memory;
using Cassowary.Shared.Debugging;

namespace Cassowary.Shared.Memory
{
    internal class MemoryBreakpoint : BreakpointBase, IMemoryBreakpoint
    {
        #region Constants

        internal const string BreakpointType = "MemoryBreakpoint";

        #endregion

        internal MemoryBreakpoint(IMemoryBus bus)
        {
            this.Bus = bus;
        }

        private AccessType _accessType;
        public AccessType AccessType
        {
            get { return this._accessType; }
            set
            {
                if (this._accessType != value)
                {
                    this._accessType = value;
                    this.OnPropertyChanged(nameof(AccessType));
                }
            }
        }

        public IMemoryBus Bus { get; private set; }

        private int _targetAddress;
        public int TargetAddress
        {
            get { return this._targetAddress; }
            set
            {
                if (this._targetAddress != value)
                {
                    this._targetAddress = value;
                    this.OnPropertyChanged(nameof(TargetAddress));
                }
            }
        }
    }
}
