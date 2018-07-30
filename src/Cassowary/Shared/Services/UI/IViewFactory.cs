using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cassowary.Shared.Services.UI
{
    public interface IViewFactory
    {
        FrameworkElement CreateInstance();
    }

    public interface IViewFactoryMetadata
    {
        Type ViewModelType { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ViewModelTypeAttribute : Attribute
    {
        public ViewModelTypeAttribute(Type viewModelType)
        {
            this.ViewModelType = viewModelType;
        }

        public Type ViewModelType { get; private set; }
    }
}
