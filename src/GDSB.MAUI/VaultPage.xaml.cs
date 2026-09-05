using System.ComponentModel;
using GDSB.MAUI.Services;
using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class VaultPage : ContentPage
{
    // Fração da altura da folha a partir da qual soltar o arrasto fecha em vez de voltar, e um
    // piso em dp para folhas curtas (poucos campos), onde uma fração pequena viraria só alguns
    // pixels e fecharia com quase nenhum arrasto.
    private const double SheetDismissFractionThreshold = 0.28;
    private const double SheetDismissMinThreshold = 80;
    private static readonly Color EditorScrimColor = Color.FromRgba(0, 0, 0, 0.6);

    private readonly VaultViewModel _viewModel;
    private readonly ILocalizationService _localization;
    private CancellationTokenSource? _toastCts;

    // TranslationY da folha quando o arrasto começou, e o último TotalY visto no Running - ver
    // comentário em OnEditorSheetPanUpdated sobre por que o Completed do Android não serve.
    private double _sheetPanStart;
    private double _sheetLastTotalY;

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
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

    // Fechar pelo "X", pelo toque no scrim ou salvando não mexe em TranslationY - só o próprio
    // arrasto faz isso. Zerar aqui na reabertura (em vez de só ao final do arrasto) garante que os
    // quatro caminhos de fechar deixem a folha pronta pra próxima abertura, mesmo se o gesto tiver
    // sido interrompido de um jeito que ResetSheet não tenha rodado.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VaultViewModel.IsCompactEditorOpen) && _viewModel.IsCompactEditorOpen)
            ResetSheet();
    }

    private void OnEditorSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                EditorSheet.CancelAnimations();
                _sheetPanStart = EditorSheet.TranslationY;
                _sheetLastTotalY = 0;

#if ANDROID
                // Fecha o teclado antes de começar a arrastar: com SafeAreaEdges="SoftInput" a
                // página encolhe/estica quando o teclado abre ou fecha, e deixar isso acontecer no
                // meio do arrasto reancoraria a folha embaixo do dedo.
                Platforms.Android.KeyboardDismissal.Hide();
#endif
                break;

            case GestureStatus.Running:
                _sheetLastTotalY = e.TotalY;
                ApplyEditorSheetOffset(Math.Max(0, _sheetPanStart + e.TotalY));
                break;

            // Canceled junto com Completed: no Android o gesto costuma terminar em Canceled quando
            // o sistema assume o toque no meio do caminho, e sem tratar isso a folha ficaria parada
            // onde o dedo a deixou.
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                // TotalY chega zerado no Completed do Android - por isso o deslocamento final usa
                // o último valor visto no Running, não e.TotalY.
                SettleEditorSheet(Math.Max(0, _sheetPanStart + _sheetLastTotalY));
                break;
        }
    }

    private void ApplyEditorSheetOffset(double offset)
    {
        EditorSheet.TranslationY = offset;

        var height = EditorSheet.Height;
        var progress = height > 0 ? Math.Clamp(offset / height, 0, 1) : 0;
        EditorSheetOverlay.BackgroundColor = Color.FromRgba(0, 0, 0, 0.6 * (1 - progress));
    }

    private async void SettleEditorSheet(double offset)
    {
        var height = EditorSheet.Height;
        var threshold = Math.Max(SheetDismissMinThreshold, height * SheetDismissFractionThreshold);

        if (offset >= threshold)
        {
            // Completa a saída antes de fechar de fato, senão o IsVisible=False do binding cortaria
            // a folha no meio da tela em vez de deixá-la terminar de sair.
            await EditorSheet.TranslateTo(0, height, 160, Easing.CubicIn);
            _viewModel.CloseEditorCommand.Execute(null);
            ResetSheet();
        }
        else
        {
            await EditorSheet.TranslateTo(0, 0, 140, Easing.CubicOut);
            ResetSheet();
        }
    }

    private void ResetSheet()
    {
        EditorSheet.CancelAnimations();
        EditorSheet.TranslationY = 0;
        EditorSheetOverlay.BackgroundColor = EditorScrimColor;
    }
}
