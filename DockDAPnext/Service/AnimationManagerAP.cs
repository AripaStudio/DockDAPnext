using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DockDAPnext.Service
{
    // Small hover-effect helper for the tool window's buttons. A WPF storyboard can
    // only animate a local, unfrozen SolidColorBrush, and the button styles hand out
    // shared theme brushes (usually frozen), so every animation first copies the
    // element's current colors into its own local brushes. The clone is visually
    // identical; the only trade-off is that a theme switch mid-session does not
    // repaint an already-animated button.
    public static class AnimationManagerAp
    {
        // Animates width plus background/foreground color of one element. Colors are
        // applied through local brush copies, so it never throws on frozen brushes.
        public static void AnimateWidth(FrameworkElement targetElement,
            double toWidth,
            Color toBackgroundColor,
            Color toForegroundColor,
            TimeSpan duration)
        {
            if (targetElement == null)
            {
                return;
            }

            var storyBoard = new Storyboard();

            var widthAnimation = new DoubleAnimation
            {
                To = toWidth,
                Duration = new Duration(duration)
            };
            Storyboard.SetTarget(widthAnimation, targetElement);
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(FrameworkElement.WidthProperty));
            storyBoard.Children.Add(widthAnimation);

            AddColorAnimation(storyBoard, targetElement, "Background", toBackgroundColor, duration);
            AddColorAnimation(storyBoard, targetElement, "Foreground", toForegroundColor, duration);

            storyBoard.Begin();
        }

        // Grows the element and tints it on hover; leaving animates everything back to
        // the look captured when this was attached (no hardcoded widths or colors).
        public static void AddHoverAnimation(FrameworkElement element,
            double hoverWidth,
            Color hoverBackgroundColor,
            Color hoverForegroundColor,
            TimeSpan duration)
        {
            if (element == null)
            {
                return;
            }

            var backgroundProperty = FindColorProperty(element, "Background");
            var foregroundProperty = FindColorProperty(element, "Foreground");

            // The resting look, captured once - by attach time (Loaded) the first
            // layout pass has run, so ActualWidth is the real resting width.
            var normalWidth = double.IsNaN(element.Width) ? element.ActualWidth : element.Width;
            var normalBackgroundColor = ReadColor(element, backgroundProperty) ?? Colors.Transparent;
            var normalForegroundColor = ReadColor(element, foregroundProperty) ?? Colors.Black;

            element.MouseEnter += (sender, args) =>
                AnimateWidth(element, hoverWidth, hoverBackgroundColor, hoverForegroundColor, duration);

            element.MouseLeave += (sender, args) =>
                AnimateWidth(element, normalWidth, normalBackgroundColor, normalForegroundColor, duration);
        }

        // Adds one ColorAnimation for the element's Background/Foreground brush, first
        // making sure that brush is a local, unfrozen instance this animation may touch.
        private static void AddColorAnimation(Storyboard storyBoard, FrameworkElement element,
            string propertyName, Color toColor, TimeSpan duration)
        {
            var property = FindColorProperty(element, propertyName);
            if (property == null || !(element.GetValue(property) is SolidColorBrush))
            {
                // No solid brush to tint (missing or gradient): animate width only.
                return;
            }

            EnsureLocalAnimatableBrush(element, property);

            var colorAnimation = new ColorAnimation
            {
                To = toColor,
                Duration = new Duration(duration)
            };
            Storyboard.SetTarget(colorAnimation, element);
            Storyboard.SetTargetProperty(colorAnimation,
                new PropertyPath("(0).(1)", property, SolidColorBrush.ColorProperty));
            storyBoard.Children.Add(colorAnimation);
        }

        // Replaces a shared/frozen brush with a local copy of the same color so the
        // storyboard has something it is allowed to animate.
        private static void EnsureLocalAnimatableBrush(FrameworkElement element, DependencyProperty property)
        {
            if (element.ReadLocalValue(property) is SolidColorBrush localBrush && !localBrush.IsFrozen)
            {
                return;
            }

            var color = (element.GetValue(property) as SolidColorBrush)?.Color ?? Colors.Transparent;
            element.SetValue(property, new SolidColorBrush(color));
        }

        // Background/Foreground are declared on different base types (Control, Panel,
        // Border); each dependency property is exposed as a public static
        // '<name>Property' field somewhere in the element's type hierarchy.
        private static DependencyProperty FindColorProperty(FrameworkElement element, string propertyName)
        {
            var field = element.GetType().GetField(
                propertyName + "Property",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return field?.GetValue(null) as DependencyProperty;
        }

        private static Color? ReadColor(FrameworkElement element, DependencyProperty property) =>
            property == null ? (Color?)null : (element.GetValue(property) as SolidColorBrush)?.Color;
    }
}
