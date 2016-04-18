using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emulator.UI.ViewModel;
using EmulatorCore.Components.Graphics;
using EmulatorCore.Services.UI;

namespace Emulator
{
    [Export(typeof(IViewFactory))]
    [ViewModelType(typeof(MainWindowViewModel))]
    public class MainWindowFactory : IViewFactory
    {
        FrameworkElement IViewFactory.CreateInstance()
        {
            return new MainWindow();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Make sure we can receive input
            this.framebuffer.Focus();
        }
    }
}
