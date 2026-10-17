using DockDAPnext.AppConfiguration;
using DockDAPnext.Service;
using DockDAPnext.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using System;
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

        // The five action buttons grow slightly and tint with the current VS accent
        // while hovered. AnimationManagerAp copies the brushes locally first, so the
        // shared (frozen) theme brushes from the styles are never animated directly.
        private void AttachHoverAnimations(object sender, RoutedEventArgs e)
        {
            if (_hoverAnimationsAttached)
            {
                return;
            }

            _hoverAnimationsAttached = true;

            var duration = TimeSpan.FromMilliseconds(160);
            var hoverColor = ResolveAccentColor();

            foreach (var button in new[] { BtnAiPropose, BtnAiApply, BtnAiCreateConfig, BtnAiOpenConfig, BtnAiEditTemplate })
            {
                AnimationManagerAp.AddHoverAnimation(button, button.ActualWidth + 8, hoverColor, Colors.White, duration);
            }
        }

        private Color ResolveAccentColor()
        {
            try
            {
                if (Application.Current?.TryFindResource(VsBrushes.AccentMediumKey) is SolidColorBrush brush)
                {
                    return brush.Color;
                }
            }
            catch (Exception)
            {
                // no shell/theme service available: fall through to the default accent
            }

            return Color.FromRgb(0x00, 0x78, 0xD4);
        }
    }
}
