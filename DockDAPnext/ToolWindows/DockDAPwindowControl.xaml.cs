using DockDAPnext.AppConfiguration;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace DockDAPnext
{
    public partial class DockDAPwindowControl : UserControl
    {
        public DockDAPwindowControl()
        {
            InitializeComponent();

            if (ServiceLocator.Provider != null)
            {
                var vm = ServiceLocator.Provider.GetService<MainViewModel>();

                this.DataContext = vm;
            }


        }

    }
}
