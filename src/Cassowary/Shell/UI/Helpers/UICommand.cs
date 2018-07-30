using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Cassowary.UI.Helpers
{
    internal class UICommand : ICommand
    {
        #region Private Fields

        private Func<bool> canExecuteFunc;
        private Action executeFunc;

        #endregion

        #region Constructors

        internal UICommand(Action executeFunc)
            : this(null, executeFunc)
        {
        }

        internal UICommand(Func<bool> canExecuteFunc, Action executeFunc)
        {
            this.canExecuteFunc = canExecuteFunc;
            this.executeFunc = executeFunc;
        }

        #endregion

        #region ICommand Implementation

        public event EventHandler CanExecuteChanged;

        internal void NotifyCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            if (this.canExecuteFunc != null)
            {
                return this.canExecuteFunc();
            }

            return true;
        }

        public void Execute(object parameter)
        {
            this.executeFunc();
        }

        #endregion
    }

    internal class UICommand<TParameter> : ICommand
    {
        #region Private Fields

        private Func<TParameter, bool> canExecuteFunc;
        private Action<TParameter> executeFunc;

        #endregion

        #region Constructors

        internal UICommand(Action<TParameter> executeFunc)
            : this(null, executeFunc)
        {
        }

        internal UICommand(Func<TParameter, bool> canExecuteFunc, Action<TParameter> executeFunc)
        {
            this.canExecuteFunc = canExecuteFunc;
            this.executeFunc = executeFunc;
        }

        #endregion

        #region ICommand Implementation

        public event EventHandler CanExecuteChanged;

        internal void NotifyCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            if (this.canExecuteFunc != null)
            {
                return this.canExecuteFunc((TParameter)parameter);
            }

            return true;
        }

        public void Execute(object parameter)
        {
            this.executeFunc((TParameter)parameter);
        }

        #endregion
    }
}
