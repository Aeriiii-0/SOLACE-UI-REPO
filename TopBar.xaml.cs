using System.Windows;
using System.Windows.Controls;

namespace SOLUM_UI.Controls
{
    public partial class TopBar : UserControl
    {
        // pg title funct
        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(
                "PageTitle",
                typeof(string),
                typeof(TopBar),
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
        }

        // for user name on pill
        public void SetUser(string name, string role)
        {
            UserNameText.Text = name;
            UserRoleText.Text = role;
            UserInitial.Text = !string.IsNullOrEmpty(name)
                ? name[0].ToString().ToUpper()
                : "U";
        }
    }
}
