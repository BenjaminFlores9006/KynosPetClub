using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using Microsoft.Maui.ApplicationModel;

namespace KynosPetClub
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionView }, // Mantén este si es necesario, si Supabase redirige A VECES directamente a HTTPS
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "https",
        DataHost = "cfwybqaykyerljqfqtzk.supabase.co",
        DataPath = "/auth/v1/callback")]
    // El IntentFilter para com.companyname.kynospetclub://auth/callback se mueve a WebAuthenticatorCallbackActivity
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            // La llamada a Platform.OnNewIntent(intent); ya no es necesaria aquí si usas WebAuthenticatorCallbackActivity
            // Si la mantienes, no causará daño, pero la lógica principal de WebAuthenticator estará en la nueva actividad.

            if (intent?.Data != null)
            {
                Console.WriteLine($"📱 OAuth callback recibido en MainActivity: {intent.Data}");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}