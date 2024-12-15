namespace GDSB.MAUI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        public AppShell(FileFinderDecrypter fileFinderDecrypter)
        {
            InitializeComponent();
            Items.Add(new ShellContent { Content = fileFinderDecrypter });
        }
    }

}
