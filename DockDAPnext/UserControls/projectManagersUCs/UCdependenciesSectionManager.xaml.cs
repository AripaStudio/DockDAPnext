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
using DockDAPnext.AppConfiguration;

namespace DockDAPnext.UserControls.projectManagersUCs
{
    /// <summary>
    /// Interaction logic for UCdependenciesSectionManager.xaml
    /// </summary>
    public partial class UCdependenciesSectionManager : UserControl
    {
        public UCdependenciesSectionManager()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            if (ServiceLocator.Provider != null)
            {
                var vm = ServiceLocator.Provider.GetService<MainViewModel>();

                this.DataContext = vm;
            }
        }
    }
}
