using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Emulator.UI.Helpers
{
    internal class KeyEventBinding
    {
        #region Attached Properties

        #region KeyDownCommand Property

        public static ICommand GetKeyDownCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(KeyDownCommandProperty);
        }

        public static void SetKeyDownCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(KeyDownCommandProperty, value);
        }

        public static readonly DependencyProperty KeyDownCommandProperty =
            DependencyProperty.RegisterAttached("KeyDownCommand", typeof(ICommand), typeof(KeyEventBinding), new PropertyMetadata(null, KeyEventBinding.KeyDownCommandPropertyChanged));

        #endregion

        #region KeyUpCommand Property

        public static ICommand GetKeyUpCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(KeyUpCommandProperty);
        }

        public static void SetKeyUpCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(KeyUpCommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for KeyUpCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeyUpCommandProperty =
            DependencyProperty.RegisterAttached("KeyUpCommand", typeof(ICommand), typeof(KeyEventBinding), new PropertyMetadata(null, KeyEventBinding.KeyUpCommandPropertyChanged));

        #endregion

        #endregion

        #region Event Handlers

        public static void KeyDownCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uiElement = d as UIElement;

            if (uiElement != null)
            {
                if (e.OldValue != null)
                {
                    uiElement.KeyDown -= KeyEventBinding.OnUIElementKeyDown;
                }

                if (e.NewValue != null)
                {
                    uiElement.KeyDown += KeyEventBinding.OnUIElementKeyDown;
                }
            }
        }

        private static void OnUIElementKeyDown(object sender, KeyEventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;

            if (obj != null)
            {
                ICommand keyDownCommand = KeyEventBinding.GetKeyDownCommand(obj);
                if (keyDownCommand != null && keyDownCommand.CanExecute(e))
                {
                    keyDownCommand.Execute(e);
                }
            }
        }

        private static void KeyUpCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uiElement = d as UIElement;

            if (uiElement != null)
            {
                if (e.OldValue != null)
                {
                    uiElement.KeyUp -= KeyEventBinding.OnUIElementKeyUp;
                }

                if (e.NewValue != null)
                {
                    uiElement.KeyUp += KeyEventBinding.OnUIElementKeyUp;
                }
            }
        }

        private static void OnUIElementKeyUp(object sender, KeyEventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;

            if (obj != null)
            {
                ICommand keyUpCommand = KeyEventBinding.GetKeyUpCommand(obj);
                if (keyUpCommand != null && keyUpCommand.CanExecute(e))
                {
                    keyUpCommand.Execute(e);
                }
            }
        }

        #endregion
    }
}
