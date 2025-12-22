using System.Windows;
using System.Windows.Controls;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DockDAPnext
{
    public partial class DockDAPwindowControl : UserControl
    {
        public DockDAPwindowControl()
        {
            InitializeComponent();

            var service = DockDAPnextPackage.appHost.Services;

            var vm = service.GetRequiredService<MainViewModel>();

            this.DataContext = vm;


        }

    }
}
