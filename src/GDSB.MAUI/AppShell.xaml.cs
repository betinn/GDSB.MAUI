namespace GDSB.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell(UnlockPage unlockPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent { Content = unlockPage, Route = nameof(UnlockPage) });
            Routing.RegisterRoute(nameof(VaultPage), typeof(VaultPage));
        }
    }
}
