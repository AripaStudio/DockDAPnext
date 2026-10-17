using DockDAPnext.AppConfiguration;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace DockDAPnext
{
    public partial class DockDaPwindowControl : UserControl
    {
        public DockDaPwindowControl()
        {
            InitializeComponent();

            if (ServiceLocator.Provider != null)
            {
                this.DataContext = ServiceLocator.Provider.GetService<MainViewModel>();
            }
        }
    }
}
