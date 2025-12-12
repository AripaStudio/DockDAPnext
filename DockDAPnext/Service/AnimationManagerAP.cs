using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DockDAP.Ruls
{
    public static class AnimationManagerAP
    {
        public static void AnimateWidth(FrameworkElement targetElement,
            double toWidth,
            Color toBackgroundColor,
            Color toForegroundColor,
            TimeSpan duration)
        {
            var Widthanimation = new DoubleAnimation
            {
                To = toWidth,
                Duration = new Duration(duration)
            };

            var Backanimation = new ColorAnimation
            {
                To = toBackgroundColor,
                Duration = new Duration(duration)
            };
            var Foreanimation = new ColorAnimation
            {
                To = toForegroundColor,
                Duration = new Duration(duration)
            };

            var storyBoard = new Storyboard();
            storyBoard.Children.Add(Widthanimation);
            storyBoard.Children.Add(Backanimation);
            storyBoard.Children.Add(Foreanimation);

            Storyboard.SetTarget(Widthanimation, targetElement);
            Storyboard.SetTargetProperty(Widthanimation, new PropertyPath("Width"));

            Storyboard.SetTarget(Backanimation, targetElement);
            Storyboard.SetTargetProperty(Backanimation, new PropertyPath("(Background).(SolidColorBrush.Color)"));

            Storyboard.SetTarget(Foreanimation, targetElement);
            Storyboard.SetTargetProperty(Foreanimation, new PropertyPath("(Foreground).(SolidColorBrush.Color)"));

            storyBoard.Begin();
        }


        public static void AnimateWidthList(List<FrameworkElement> targetElementList, double toWidth, TimeSpan duration,
            Color toBackgroundColor,
            Color toForegroundColor)
        {
            foreach (var frameworkElement in targetElementList)
            {
                AnimateWidth(frameworkElement, toWidth, toBackgroundColor, toForegroundColor, duration);
            }
        }


        public static void AddHoverAnimation(FrameworkElement element,
            double hoverWidth, Color hoverColor, Color normalColor,
            TimeSpan duration)
        {
            element.MouseEnter += (sender, args) =>
            {
                AnimateWidth(element, hoverWidth, hoverColor, normalColor, duration);
            };

            element.MouseLeave += (sender, args) =>
            {
                AnimateWidth(element, 100, normalColor, Colors.Black, duration);
            };
        }
    }
}