using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SOLUM_UI
{
    public partial class MainWindow : Window
    {
        private Button _activeButton;

        public static readonly DependencyProperty IsNavActiveProperty =
            DependencyProperty.RegisterAttached("IsNavActive", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static void SetIsNavActive(DependencyObject obj, bool value) => obj.SetValue(IsNavActiveProperty, value);
        public static bool GetIsNavActive(DependencyObject obj) => (bool)obj.GetValue(IsNavActiveProperty);

        public static string CurrentUserName { get; set; } = "Admin User";
        public static string CurrentUserRole { get; set; } = "Administrator";

        private Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();

        public MainWindow()
        {
            InitializeComponent();
            RegisterNavButtons();
            Loaded += (s, e) =>
            {
                TopBarControl.SetUser(CurrentUserName, CurrentUserRole);
                ActivateButton("Dashboard");
                MainFrame.Navigate(new DashboardPage());
            };
        }

        private void RegisterNavButtons()
        {
            foreach (Button btn in FindAllButtons(this))
            {
                if (btn.Tag != null)
                {
                    string tag = btn.Tag.ToString();
                    if (!_navButtons.ContainsKey(tag))
                        _navButtons.Add(tag, btn);
                }
            }
        }

        private List<Button> FindAllButtons(DependencyObject parent)
        {
            List<Button> result = new List<Button>();
            if (parent == null) return result;
            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                DependencyObject dep = child as DependencyObject;
                if (dep == null) continue;
                Button btn = dep as Button;
                if (btn != null) result.Add(btn);
                result.AddRange(FindAllButtons(dep));
            }
            return result;
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;
            string tag = btn.Tag != null ? btn.Tag.ToString() : string.Empty;
            ActivateButton(tag);
            NavigateTo(tag);
        }

        private void ActivateButton(string tag)
        {
            foreach (Button b in _navButtons.Values)
                SetIsNavActive(b, false);

            if (_navButtons.ContainsKey(tag))
            {
                SetIsNavActive(_navButtons[tag], true);
                _activeButton = _navButtons[tag];
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void NavigateTo(string tag)
        {
            switch (tag)
            {
                case "SoloParentRecords":
                    TopBarControl.PageTitle = "Solo Parent Records";
                    break;
                default:
                    TopBarControl.PageTitle = tag;
                    break;
            }

            switch (tag)
            {
                case "Dashboard":
                    MainFrame.Navigate(new DashboardPage());
                    break;
                case "SoloParentRecords":
                    SoloParentRecordsPage.CurrentUserName = CurrentUserName;
                    SoloParentRecordsPage.CurrentUserRole = CurrentUserRole;
                    MainFrame.Navigate(new SoloParentRecordsPage());
                    break;
                case "Wallet":
                    break;
                case "Analytics":
                    break;
                case "Settings":
                    break;
                case "Logout":
                    HandleLogout();
                    break;
            }
        }

        private void HandleLogout()
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to logout?", "Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }

        private void TopBarControl_Loaded(object sender, RoutedEventArgs e) { }
    }
}
