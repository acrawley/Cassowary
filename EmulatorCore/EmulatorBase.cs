using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using EmulatorCore.Components.Core;

namespace EmulatorCore
{
    public abstract class EmulatorBase
    {
        private CompositionContainer emulatorContainer;

        public EmulatorBase(CompositionContainer container)
        {
            this.emulatorContainer = container;
        }

        public abstract IEnumerable<IEmulatorComponent> Components { get; }

        protected void Compose()
        {
            foreach (IEmulatorComponent component in this.Components)
            {
                this.emulatorContainer.SatisfyImportsOnce(component);
            }
        }
    }
}
