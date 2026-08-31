namespace GDSB.MAUI.Views;

public partial class BiometricOptInView : ContentView
{
    public BiometricOptInView()
    {
        InitializeComponent();
        BiometricIcon.Drawable = new FingerprintDrawable();
    }
}
