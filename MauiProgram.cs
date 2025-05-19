using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using KynosPetClub.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KynosPetClub
{
    public static class MauiProgram
    {
        public static IConfiguration Configuration { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Configurar servicios
            builder.Services.AddSingleton<ApiService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            try
            {
                // Cargar la configuración desde appsettings.json como un recurso incorporado
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));

                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        var config = new ConfigurationBuilder()
                            .AddJsonStream(stream)
                            .Build();

                        Configuration = config;
                        Console.WriteLine("Configuración cargada con éxito");
                    }
                    else
                    {
                        Console.WriteLine("Stream es nulo para el recurso encontrado");
                    }
                }
                else
                {
                    Console.WriteLine("No se encontró el archivo appsettings.json como recurso incorporado");
                }
            }
            catch (Exception ex)
            {
                // Si hay un error al cargar la configuración, crear una configuración vacía
                Console.WriteLine($"Error al cargar la configuración: {ex.Message}");
            }

            // Si la configuración no se pudo cargar, crear una configuración en memoria
            if (Configuration == null)
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection() // Crea una configuración vacía
                    .Build();
                Configuration = config;
                Console.WriteLine("Se ha creado una configuración vacía");
            }

            return builder.Build();
        }
    }
}