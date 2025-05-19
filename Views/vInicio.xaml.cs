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
        // Aqu� ir�a la navegaci�n a la p�gina de mascotas
        // Por ejemplo:
        // await Navigation.PushAsync(new vMascotas(_usuarioActual));

        // Por ahora, solo mostraremos un mensaje
        await DisplayAlert("Pr�ximamente", "La gesti�n de mascotas estar� disponible pronto", "OK");
    }

    // Tambi�n podr�as agregar handlers para los botones del men� inferior
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Configurar eventos de los botones del men� inferior si es necesario
        btnHome.Clicked += (sender, e) => { /* Acci�n para Home */ };
        btnCalendar.Clicked += (sender, e) => { /* Acci�n para Calendar */ };
        btnCart.Clicked += (sender, e) => { /* Acci�n para Cart */ };
        btnUser.Clicked += (sender, e) => { /* Acci�n para User */ };
    }
}