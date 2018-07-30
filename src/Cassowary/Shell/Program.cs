using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;

namespace Cassowary
{
    internal class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            EmulatorApp emulator = new EmulatorApp();

            Container.SatisfyImportsOnce(emulator);

            return emulator.Run();
        }

        private static CompositionContainer _container;
        private static CompositionContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = InitializeComponentModel();
                }

                return _container;
            }
        }

        private static CompositionContainer InitializeComponentModel()
        {
            AggregateCatalog catalog = new AggregateCatalog();

            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Program).Assembly));
            catalog.Catalogs.Add(new DirectoryCatalog(Path.GetDirectoryName(typeof(Program).Assembly.Location)));

            CompositionContainer container = new CompositionContainer(catalog);

            // Add the container to itself so other components can do additional composition
            container.ComposeExportedValue(container);

            return container;
        }
    }
}
