using System.Reflection;
using System;
using System.Windows;

namespace SOLUM_UI
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
            var infoVersion = System.Reflection.Assembly.GetExecutingAssembly()?.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string appVersion = !string.IsNullOrWhiteSpace(infoVersion) ? ("v" + infoVersion) : ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            TxtAppVersion.Text = "Version " + appVersion;
        }
    }
}
