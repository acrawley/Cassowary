using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Services.UI
{
    public interface IViewModel: INotifyPropertyChanged, IPartImportsSatisfiedNotification
    {
    }
}
