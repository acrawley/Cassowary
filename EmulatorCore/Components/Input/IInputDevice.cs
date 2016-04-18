﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Core;

namespace EmulatorCore.Components.Input
{
    public interface IInputDevice : IEmulatorComponent
    {
        IEnumerable<IInputElement> InputElements { get; }
    }
}
