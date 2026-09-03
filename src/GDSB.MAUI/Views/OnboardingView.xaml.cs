namespace GDSB.MAUI.Views;

public partial class OnboardingView : ContentView
{
    public OnboardingView()
    {
        InitializeComponent();

        // A mesma marca da tela de desbloqueio, aberta: no primeiro slide ela ilustra o conteúdo
        // do arquivo, que ainda não foi selado por nenhuma senha.
        VaultFileMark.Drawable = new BrandMarkDrawable { ShowBadge = false, Open = true };

        // Mesma configuração do ícone no botão real de UnlockPage - a amostra tem que ser o
        // controle, não uma imitação parecida.
        BiometricSampleIcon.Drawable = new FingerprintDrawable
        {
            Stroke = Colors.White,
            StrokeWidth = 1.7f,
            Compact = true,
        };
    }
}
