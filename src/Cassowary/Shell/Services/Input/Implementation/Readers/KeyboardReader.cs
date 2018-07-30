using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Cassowary.Services.Input.Implementation.Readers
{
    [Export(typeof(IKeyboardReader))]
    internal class KeyboardReader : IKeyboardReader
    {
        #region Private Fields

        private List<Key> activeKeys;

        #endregion

        #region Constructor

        internal KeyboardReader()
        {
            this.activeKeys = new List<Key>();
        }

        #endregion

        #region IKeyboardReader Implementation

        bool IKeyboardReader.GetKeyState(Key key)
        {
            return activeKeys.Contains(key);
        }

        void IKeyboardReader.NotifyKeyDown(Key key)
        {
            lock (this.activeKeys)
            {
                if (!this.activeKeys.Contains(key))
                {
                    this.activeKeys.Add(key);
                }
            }
        }

        void IKeyboardReader.NotifyKeyUp(Key key)
        {
            lock (this.activeKeys)
            {
                this.activeKeys.Remove(key);
            }
        }

        #endregion
    }
}
