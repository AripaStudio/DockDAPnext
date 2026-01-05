using DockDAPnext.AppConfiguration;
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

namespace DockDAPnext.UserControls
{
   
    public partial class UCabouteMe : UserControl
    {
        public UCabouteMe()
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
