using KynosPetClub.Views;

namespace KynosPetClub
{
    public partial class App : Application
    {
        public static NavigationPage MainNavigationPage { get; private set; }

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = new vLogIn();
            MainNavigationPage = new NavigationPage(loginPage);
            return new Window(MainNavigationPage);
        }
    }
}