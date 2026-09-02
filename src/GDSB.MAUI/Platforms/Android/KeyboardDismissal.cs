using Android.Content;
using Android.Views.InputMethods;
using Microsoft.Maui.ApplicationModel;

namespace GDSB.MAUI.Platforms.Android
{
    // O Android não fecha o teclado virtual sozinho só porque a página mudou - se um Entry (ex.: a
    // senha mestra no Unlock/CreateVault) estava com foco e o teclado aberto, ele fica flutuando
    // por cima da tela seguinte. Chamado explicitamente ao entrar no cofre (VaultPage.OnAppearing).
    public static class KeyboardDismissal
    {
        public static void Hide()
        {
            var activity = Platform.CurrentActivity;
            var decorView = activity?.Window?.DecorView;
            if (decorView is null)
                return;

            var inputMethodManager = activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
            inputMethodManager?.HideSoftInputFromWindow(decorView.WindowToken, HideSoftInputFlags.None);
        }
    }
}
