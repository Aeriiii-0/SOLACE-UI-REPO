using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SOLUM_UI.Controls
{
    public partial class TopBar : UserControl
    {
        public static readonly RoutedEvent ProfileClickedEvent =
            EventManager.RegisterRoutedEvent("ProfileClicked", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(TopBar));

        public static readonly RoutedEvent ChangePasswordRequestedEvent =
            EventManager.RegisterRoutedEvent("ChangePasswordRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(TopBar));

        public static readonly RoutedEvent LogoutRequestedEvent =
            EventManager.RegisterRoutedEvent("LogoutRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(TopBar));

        public event RoutedEventHandler ProfileClicked
        {
            add => AddHandler(ProfileClickedEvent, value);
            remove => RemoveHandler(ProfileClickedEvent, value);
        }

        public event RoutedEventHandler ChangePasswordRequested
        {
            add => AddHandler(ChangePasswordRequestedEvent, value);
            remove => RemoveHandler(ChangePasswordRequestedEvent, value);
        }

        public event RoutedEventHandler LogoutRequested
        {
            add => AddHandler(LogoutRequestedEvent, value);
            remove => RemoveHandler(LogoutRequestedEvent, value);
        }

        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register("PageTitle", typeof(string), typeof(TopBar),
                new PropertyMetadata(string.Empty, OnPageTitleChanged));

        public string PageTitle
        {
            get => (string)GetValue(PageTitleProperty);
            set => SetValue(PageTitleProperty, value);
        }

        private static void OnPageTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TopBar bar)
                bar.PageTitleText.Text = e.NewValue?.ToString() ?? string.Empty;
        }

        public TopBar()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                var window = System.Windows.Window.GetWindow(this);
                if (window != null)
                    window.PreviewMouseDown += Window_PreviewMouseDown;
            };
            Unloaded += (s, e) =>
            {
                var window = System.Windows.Window.GetWindow(this);
                if (window != null)
                    window.PreviewMouseDown -= Window_PreviewMouseDown;
            };
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ProfileMenu.IsOpen)
            {
                if (e.OriginalSource is System.Windows.DependencyObject src)
                {
                    if (IsDescendant(ProfilePill, src) || IsDescendant((System.Windows.DependencyObject)ProfileMenu.Child, src))
                        return;
                }
                ProfileMenu.IsOpen = false;
            }
            if (FaqPopup.IsOpen)
            {
                if (e.OriginalSource is System.Windows.DependencyObject src2)
                {
                    if (IsDescendant(BtnFaq, src2) || IsDescendant((System.Windows.DependencyObject)FaqPopup.Child, src2))
                        return;
                }
                FaqPopup.IsOpen = false;
            }
        }

        private static bool IsDescendant(System.Windows.DependencyObject parent, System.Windows.DependencyObject child)
        {
            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current)
                       ?? LogicalTreeHelper.GetParent(current);
            }
            return false;
        }

        public void SetUser(string name, string role)
        {
            UserNameText.Text    = name;
            UserRoleText.Text    = role;
            MenuUserName.Text    = name;
            MenuUserRole.Text    = role;
            UserInitial.Text     = !string.IsNullOrEmpty(name) ? name[0].ToString().ToUpper() : "U";
        }

        private void ProfilePill_Click(object sender, MouseButtonEventArgs e)
        {
            ProfileMenu.IsOpen = !ProfileMenu.IsOpen;
        }

        private void Menu_ViewProfile(object sender, MouseButtonEventArgs e)
        {
            ProfileMenu.IsOpen = false;
            RaiseEvent(new RoutedEventArgs(ProfileClickedEvent));
        }

        private void Menu_ChangePassword(object sender, MouseButtonEventArgs e)
        {
            ProfileMenu.IsOpen = false;
            RaiseEvent(new RoutedEventArgs(ChangePasswordRequestedEvent));
        }

        private void Menu_Logout(object sender, MouseButtonEventArgs e)
        {
            ProfileMenu.IsOpen = false;
            RaiseEvent(new RoutedEventArgs(LogoutRequestedEvent));
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            ToastNotification.Show("Notifications", "No new notifications.", ToastType.Info);
        }

        private void Faq_Click(object sender, RoutedEventArgs e)
        {
            FaqPopup.IsOpen = !FaqPopup.IsOpen;
            if (FaqPopup.IsOpen) ProfileMenu.IsOpen = false;
        }

        private void FaqClose_Click(object sender, RoutedEventArgs e)
        {
            FaqPopup.IsOpen = false;
        }
    }
}
