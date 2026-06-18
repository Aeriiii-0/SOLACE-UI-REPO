using System.Windows.Controls;

namespace SOLUM_UI
{
    public partial class UserProfilePage : Page
    {
        public UserProfilePage()
        {
            InitializeComponent();
            Loaded += (s, e) => PopulateProfile();
        }

        private void PopulateProfile()
        {
            string name = MainWindow.CurrentUserName;
            string role = MainWindow.CurrentUserRole;

            TxtDisplayName.Text = name;
            TxtDisplayRole.Text = role;
            TxtDisplayEmail.Text = name;
            TxtFullName.Text = name;
            TxtRole.Text = role;
            TxtEmail.Text = name;

            TxtInitial.Text = !string.IsNullOrEmpty(name)
                ? name[0].ToString().ToUpper()
                : "U";
        }
    }
}
