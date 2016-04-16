﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EmulatorCore.Services.UI
{
    [Export(typeof(IViewModelService))]
    internal class ViewModelService : IViewModelService
    {
        [ImportMany(typeof(IViewFactory))]
        private IEnumerable<Lazy<IViewFactory, IViewFactoryMetadata>> UIElementFactories { get; set; }

        [Import(typeof(CompositionContainer))]
        private CompositionContainer Container { get; set; }

        T IViewModelService.CreateViewForViewModel<T>(object viewModel)
        {
            Type viewModelType = viewModel.GetType();

            IViewFactory factory = this.UIElementFactories.FirstOrDefault(f => f.Metadata.ViewModelType == viewModelType)?.Value;
            if (factory != null)
            {
                T view = (T)factory.CreateInstance();

                this.Container.SatisfyImportsOnce(view);
                this.Container.SatisfyImportsOnce(viewModel);

                this.Container.ComposeExportedValue<Components.Graphics.IPaletteFramebuffer>((Components.Graphics.IPaletteFramebuffer)viewModel);

                view.DataContext = viewModel;
                return view;
            }

            Debug.Fail("No view registered for view model of type '{0}'!", viewModelType.Name);
            return null;
        }
    }
}
