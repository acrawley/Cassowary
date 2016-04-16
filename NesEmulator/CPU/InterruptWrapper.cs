using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.CPU;

namespace NesEmulator.CPU
{
    [DebuggerDisplay("{Name,nq} (Priority {Priority})")]
    internal class InterruptWrapper : IProcessorInterrupt
    {
        #region Private Fields

        Action<bool> assertFunc;

        #endregion

        #region Constructor

        internal InterruptWrapper(string name, int priority, Action<bool> assertFunc)
        {
            this.Name = name;
            this.Priority = priority;
            this.assertFunc = assertFunc;
        }

        #endregion

        private class InterruptAssertion : IDisposable
        {
            #region Private Fields

            private Action<bool> assertFunc;

            #endregion

            #region Constructor

            internal InterruptAssertion(Action<bool> assertFunc)
            {
                this.assertFunc = assertFunc;
            }

            #endregion

            #region IDisposable Implementation

            private bool isDisposed = false;

            public void Dispose()
            {
                if (!this.isDisposed)
                {
                    this.assertFunc(false);
                    this.isDisposed = true;
                }
            }

            #endregion
        }

        #region IProcessorInterrupt Implementation

        public string Name { get; private set; }

        public int Priority { get; private set; }

        public IDisposable Assert()
        {
            this.assertFunc(true);
            return new InterruptAssertion(this.assertFunc);
        }

        #endregion
    }
}
