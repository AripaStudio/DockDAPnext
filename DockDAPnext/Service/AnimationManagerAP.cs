using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DockDAPnext.Service
{
    // Hover helper for buttons: a RenderTransform scale, because animating Width reflows the layout.
    public static class AnimationManagerAp
    {
        public static void AddHoverScale(FrameworkElement element, double scale, TimeSpan duration)
        {
            if (element == null)
            {
                return;
            }

            var transform = new ScaleTransform(1, 1);
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = transform;

            element.MouseEnter += (sender, args) => AnimateScale(transform, scale, duration);
            element.MouseLeave += (sender, args) => AnimateScale(transform, 1, duration);
        }

        private static void AnimateScale(ScaleTransform transform, double to, TimeSpan duration)
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            transform.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(to, new Duration(duration)) { EasingFunction = easing });
            transform.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(to, new Duration(duration)) { EasingFunction = easing });
        }
    }
}
