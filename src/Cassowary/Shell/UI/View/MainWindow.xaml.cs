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
using Cassowary.UI.ViewModel;
using Cassowary.Shared.Components.Graphics;
using Cassowary.Shared.Services.UI;

namespace Cassowary
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

        private void OnFramebufferMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.framebuffer.Focus();
        }
    }
}
