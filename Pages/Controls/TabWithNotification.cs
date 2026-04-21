namespace ISIInstitute.Controls.PageModels
{
    public class TabWithNotification : FlyoutItem
    {
        public static readonly BindableProperty NotificationVisibleProperty = BindableProperty.Create(nameof(NotificationVisible), typeof(bool), typeof(TabWithNotification), false, BindingMode.TwoWay, null, NotificationVisibleChanged);

        public static readonly BindableProperty NotificationNameProperty = BindableProperty.Create(
            nameof(NotificationName), typeof(string), typeof(TabWithNotification), "", BindingMode.TwoWay, null, NotificationNameChanged);

        private static void NotificationNameChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TabWithNotification)bindable;
            control.NotificationName = newValue.ToString();
        }
        private static void NotificationVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TabWithNotification)bindable;
            control.NotificationVisible = (bool)newValue;
        }

        public bool NotificationVisible
        {
            get => (bool)GetValue(NotificationVisibleProperty);
            set => SetValue(NotificationVisibleProperty, value);
        }

        public string NotificationName
        {
            get => (string)GetValue(NotificationNameProperty);
            set => SetValue(NotificationNameProperty, value);
        }

    }
}