using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

namespace DockDAPnext.UserControls.projectManagersUCs
{
    /// <summary>
    /// Interaction logic for UCprojectSectionManager.xaml
    /// </summary>
    public partial class UCprojectSectionManager : UserControl
    {
        public UCprojectSectionManager()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            if (DockDAPnextPackage.appHost != null)
            {
                var service = DockDAPnextPackage.appHost.Services;

                var vm = service.GetRequiredService<MainViewModel>();

                this.DataContext = vm;
            }
        }
    }
}
