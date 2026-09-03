using GDSB.MAUI.ViewModels;
using GDSB.MAUI.Views;

namespace GDSB.MAUI;

public partial class BackupRecoveryPage : ContentPage
{
    private readonly BackupRecoveryViewModel _viewModel;

    public BackupRecoveryPage(BackupRecoveryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.BackupDeleted += OnBackupDeleted;
        _viewModel.AllBackupsDeleted += OnAllBackupsDeleted;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Initialize();

        // O painel de ajuda só atende enquanto esta página está à vista: com Shell, a página
        // anterior continua carregada na pilha, e sem isso o "?" de uma tela abriria o painel da
        // outra também.
        HelpSheet.StartListening();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HelpSheet.StopListening();
    }

    // S2325 ("make static") é falso positivo: SealOverlay é um elemento nomeado do XAML (campo de
    // instância gerado por InitializeComponent), mas o Sonar não resolve esse tipo de campo
    // gerado - mesmo caso de VaultPage.OnSecretDeleted.
#pragma warning disable S2325
    private async void OnBackupDeleted(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Delete, "Backup excluído", 740);

    private async void OnAllBackupsDeleted(object? sender, EventArgs e)
        => await SealOverlay.PlayAsync(LockSealMode.Delete, "Backups excluídos", 740);
#pragma warning restore S2325
}
