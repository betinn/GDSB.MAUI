namespace GDSB.MAUI.ViewModels
{
    // Agrupa os dois ViewModels que a UnlockPage hospeda por cima de si mesma - o convite de
    // biometria e o tutorial de primeiro acesso - só para manter o construtor do UnlockViewModel
    // dentro do limite de parâmetros do analisador estático, que a entrada do tutorial estourou
    // (passou de 7). Mesmo motivo e mesmo formato do VaultAccess.
    public sealed class UnlockOverlays(BiometricOptInCoordinator biometricOptIn, OnboardingViewModel onboarding)
    {
        public BiometricOptInCoordinator BiometricOptIn { get; } = biometricOptIn;

        public OnboardingViewModel Onboarding { get; } = onboarding;
    }
}
