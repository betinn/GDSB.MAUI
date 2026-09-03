using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class CreateVaultPage : ContentPage
{
    public CreateVaultPage(CreateVaultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Haste erguida: o cofre desta tela ainda não foi selado.
        BrandMark.Drawable = new BrandMarkDrawable { Open = true };
    }

    // O painel de ajuda só atende enquanto esta página está à vista: com Shell, a página anterior
    // continua carregada na pilha, e sem isso o "?" de uma tela abriria o painel da outra também.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HelpSheet.StartListening();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HelpSheet.StopListening();
    }
}
