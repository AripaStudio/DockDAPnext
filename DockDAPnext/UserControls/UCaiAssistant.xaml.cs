using DockDAPnext.AppConfiguration;
using DockDAPnext.Service;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DockDAPnext.UserControls
{
    /// <summary>
    /// Interaction logic for UCaiAssistant.xaml
    /// </summary>
    public partial class UCaiAssistant : UserControl
    {
        private bool _hoverAnimationsAttached;

        public UCaiAssistant()
        {
            InitializeComponent();

            Debug.WriteLine("[DOCKDAP] UCaiAssistant loaded");

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            if (ServiceLocator.Provider != null)
            {
                var vm = ServiceLocator.Provider.GetService<MainViewModel>();

                this.DataContext = vm;
            }

            // After the first layout pass the buttons know their resting width,
            // which the hover animation restores on MouseLeave.
            this.Loaded += AttachHoverAnimations;
        }

        private void AttachHoverAnimations(object sender, RoutedEventArgs e)
        {
            if (_hoverAnimationsAttached)
            {
                return;
            }

            _hoverAnimationsAttached = true;

            var duration = TimeSpan.FromMilliseconds(500);
            foreach (var button in new[]
                         { BtnAiPropose, BtnAiApply, BtnAiCreateConfig, BtnAiOpenConfig, BtnAiEditTemplate })
            {
                AnimationManagerAp.AddHoverScale(button, 1.05, duration);
            }
        }
    }
}
