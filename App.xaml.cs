using Microsoft.Maui.Controls;

namespace Wordle
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //MainPage = new AppShell();
            MainPage = new NavigationPage(new MainPage());
            //MainPage = new NavigationPage(new LoginPage());
        }
    }
}
