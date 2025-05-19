using KynosPetClub.Models;

namespace KynosPetClub.Views;

public partial class vInicio : ContentPage
{
    private Usuario _usuarioActual;

    public vInicio()
    {
        InitializeComponent();
    }

    public vInicio(Usuario usuario)
    {
        InitializeComponent();
        _usuarioActual = usuario;

        // Personalizar el saludo con el nombre del usuario
        if (_usuarioActual != null)
        {
            lblSaludo.Text = $"Hola, {_usuarioActual.nombre}";
        }
    }

    private async void btnMascotas_Clicked(object sender, EventArgs e)
    {
        // Aquí iría la navegación a la página de mascotas
        // Por ejemplo:
        // await Navigation.PushAsync(new vMascotas(_usuarioActual));

        // Por ahora, solo mostraremos un mensaje
        await DisplayAlert("Próximamente", "La gestión de mascotas estará disponible pronto", "OK");
    }

    // También podrías agregar handlers para los botones del menú inferior
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Configurar eventos de los botones del menú inferior si es necesario
        btnHome.Clicked += (sender, e) => { /* Acción para Home */ };
        btnCalendar.Clicked += (sender, e) => { /* Acción para Calendar */ };
        btnCart.Clicked += (sender, e) => { /* Acción para Cart */ };
        btnUser.Clicked += (sender, e) => { /* Acción para User */ };
    }
}