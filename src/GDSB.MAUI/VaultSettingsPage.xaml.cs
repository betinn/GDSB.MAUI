using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class VaultSettingsPage : ContentPage
{
    private readonly VaultSettingsViewModel _viewModel;

    public VaultSettingsPage(VaultSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.SettingsSaved += OnSettingsSaved;
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

    // S2325 ("make static") é falso positivo: SealOverlay é um elemento nomeado do XAML (campo de
    // instância gerado por InitializeComponent), mas o Sonar não resolve esse tipo de campo
    // gerado - mesmo caso de VaultPage.OnSecretUpdated e de HasErrorMessage/CanInteract em
    // VaultSettingsViewModel.
#pragma warning disable S2325
    private async void OnSettingsSaved(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Update, "Alterações salvas", 620);
#pragma warning restore S2325
}
