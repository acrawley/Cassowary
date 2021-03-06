﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cassowary.Shared.Services.UI
{
    public interface IViewModelService
    {
        T CreateViewForViewModel<T>(object viewModel) where T : FrameworkElement;
    }
}
