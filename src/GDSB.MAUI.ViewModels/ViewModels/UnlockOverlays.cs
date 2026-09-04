namespace GDSB.MAUI.ViewModels
{
    // Agrupa os ViewModels extras que a UnlockViewModel só repassa para a UnlockPage compor - o
    // convite de biometria, o tutorial de primeiro acesso e o seletor de idioma - só para manter o
    // construtor do UnlockViewModel dentro do limite de parâmetros do analisador estático, que a
    // entrada do tutorial já tinha estourado antes (passou de 7). Mesmo motivo e mesmo formato do
    // VaultAccess.
    public sealed class UnlockOverlays(
        BiometricOptInCoordinator biometricOptIn,
        OnboardingViewModel onboarding,
        LanguageSelectorViewModel language)
    {
        public BiometricOptInCoordinator BiometricOptIn { get; } = biometricOptIn;

        public OnboardingViewModel Onboarding { get; } = onboarding;

        public LanguageSelectorViewModel Language { get; } = language;
    }
}
