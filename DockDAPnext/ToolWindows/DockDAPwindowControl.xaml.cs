using DockDAPnext.AppConfiguration;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
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

            this.Loaded += OnControlLoaded;
        }

        // Fill the selector only. Loading waits for the user to pick a project and press Refresh.
        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnControlLoaded;

            if (this.DataContext is MainViewModel viewModel)
            {
                _ = viewModel.RefreshSolutionProjectsAsync();
            }
        }
    }
}
