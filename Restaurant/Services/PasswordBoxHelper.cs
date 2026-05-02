using System.Windows;
using System.Windows.Controls;

namespace Restaurant.Helpers
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty IsEmptyProperty =
            DependencyProperty.RegisterAttached(
                "IsEmpty",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(true));

        public static bool GetIsEmpty(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEmptyProperty);
        }

        public static void SetIsEmpty(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEmptyProperty, value);
        }

        public static readonly DependencyProperty MonitorPasswordProperty =
            DependencyProperty.RegisterAttached(
                "MonitorPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnMonitorPasswordChanged));

        public static bool GetMonitorPassword(DependencyObject obj)
        {
            return (bool)obj.GetValue(MonitorPasswordProperty);
        }

        public static void SetMonitorPassword(DependencyObject obj, bool value)
        {
            obj.SetValue(MonitorPasswordProperty, value);
        }

        private static void OnMonitorPasswordChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                    SetIsEmpty(passwordBox, string.IsNullOrEmpty(passwordBox.Password));
                }
                else
                {
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetIsEmpty(passwordBox, string.IsNullOrEmpty(passwordBox.Password));
            }
        }
    }
}