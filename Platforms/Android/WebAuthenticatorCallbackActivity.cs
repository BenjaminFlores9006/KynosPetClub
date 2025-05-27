using Android.App;
using Android.Content.PM;
using Android.OS; // This might not be strictly necessary for this class, but harmless
using Microsoft.Maui.Authentication; // Asegúrate de tener este using

namespace KynosPetClub // Asegúrate de que el namespace coincida con el de tu proyecto
{
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
                  Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
                  DataScheme = "com.companyname.kynospetclub", // ¡TU ESQUEMA PERSONALIZADO AQUÍ!
                  DataHost = "auth", // ¡EL HOST DE TU CALLBACK AQUÍ!
                  DataPath = "/callback")] // ¡EL PATH DE TU CALLBACK AQUÍ!
    public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity // ¡CORREGIDO!
    {
        // No necesitas agregar ningún código aquí a menos que tengas lógica específica de Android para el callback
        // La base WebAuthenticatorCallbackActivity ya maneja el procesamiento del Intent.
    }
}