using DockDAPnext.AppConfiguration;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace DockDAPnext.UserControls
{
    /// <summary>
    /// Interaction logic for UCaiAssistant.xaml
    /// </summary>
    public partial class UCaiAssistant : UserControl
    {
        public UCaiAssistant()
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
