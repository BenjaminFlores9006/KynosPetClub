using KynosPetClub.Views;

namespace KynosPetClub
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = new NavigationPage(new vLogIn());
            return new Window(loginPage);
        }
    }
}