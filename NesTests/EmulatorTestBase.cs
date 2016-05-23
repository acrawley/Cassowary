using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using EmulatorCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NesTests
{
    public class EmulatorTestBase
    {
        #region Test Setup

        [TestInitialize]
        public void ComposeTestClass()
        {
            this.Container.SatisfyImportsOnce(this);
        }

        private CompositionContainer _container;
        private CompositionContainer Container
        {
            get
            {
                if (this._container == null)
                {
                    this._container = InitializeComponentModel();
                }

                return this._container;
            }
        }

        private CompositionContainer InitializeComponentModel()
        {
            AggregateCatalog catalog = new AggregateCatalog();

            //catalog.Catalogs.Add(new AssemblyCatalog(this.GetType().Assembly));
            catalog.Catalogs.Add(new DirectoryCatalog(Path.GetDirectoryName(this.GetType().Assembly.Location)));

            CompositionContainer container = new CompositionContainer(catalog);

            // Add the container to itself so other components can do additional composition
            container.ComposeExportedValue(container);

            return container;
        }

        #endregion

        #region MEF Imports

        [ImportMany(typeof(IEmulatorFactory))]
        protected IEnumerable<Lazy<IEmulatorFactory, IEmulatorFactoryMetadata>> EmulatorFactories { get; private set; }

        #endregion

        protected IEmulator GetInstance()
        {
            IEmulatorFactory factory = this.EmulatorFactories.SingleOrDefault(e => e.Metadata.EmulatorName == "NES").Value;
            return factory.CreateInstance();
        }
    }
}
