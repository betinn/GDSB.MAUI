using GDSB.MAUI.Services;
using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class VaultPage : ContentPage
{
    private readonly VaultViewModel _viewModel;
    private readonly ILocalizationService _localization;
    private CancellationTokenSource? _toastCts;

    public VaultPage(VaultViewModel viewModel, ILocalizationService localization)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _localization = localization;
        BindingContext = _viewModel;
        _viewModel.ToastRequested += OnToastRequested;
        _viewModel.SecretCreated += OnSecretCreated;
        _viewModel.SecretUpdated += OnSecretUpdated;
        _viewModel.SecretDeleted += OnSecretDeleted;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _viewModel.OnSizeChanged(width);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // O painel de ajuda só atende enquanto esta página está à vista: com Shell, a página
        // anterior continua carregada na pilha, e sem isso o "?" de uma tela abriria o painel da
        // outra também.
        HelpSheet.StartListening();

#if ANDROID
        // O teclado virtual pode continuar aberto do Entry de senha da tela anterior
        // (Unlock/CreateVault) - fecha explicitamente ao entrar no cofre.
        Platforms.Android.KeyboardDismissal.Hide();
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HelpSheet.StopListening();
    }

    private async void OnToastRequested(object? sender, string message)
    {
        if (_toastCts is not null)
            await _toastCts.CancelAsync();
        var cts = new CancellationTokenSource();
        _toastCts = cts;

        ToastLabel.Text = message;
        await ToastBorder.FadeTo(1, 120);

        try
        {
            await Task.Delay(1600, cts.Token);
            await ToastBorder.FadeTo(0, 220);
        }
        catch (TaskCanceledException)
        {
            // Um novo toast chegou antes do delay acabar - a chamada nova assume o fade-out.
        }
    }

    // Os três selos, por cima da lista já atualizada. Só chegam aqui quando a gravação no
    // arquivo deu certo - ver os eventos correspondentes no VaultViewModel.
    // S2325 ("make static") é falso positivo: SealOverlay é um elemento nomeado do XAML (campo de
    // instância gerado por InitializeComponent), mas o Sonar não resolve esse tipo de campo
    // gerado - mesmo caso de HasErrorMessage/CanInteract em VaultSettingsViewModel.
#pragma warning disable S2325
    private async void OnSecretCreated(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Create, _localization.Get("Seal_SecretCreated"), 760);

    private async void OnSecretUpdated(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Update, _localization.Get("Seal_SettingsSaved"), 620);

    private async void OnSecretDeleted(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Delete, _localization.Get("Seal_SecretDeleted"), 740);
#pragma warning restore S2325
}
