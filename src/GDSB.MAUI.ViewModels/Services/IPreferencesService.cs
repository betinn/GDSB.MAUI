namespace GDSB.MAUI.Services
{
    // Encapsula as poucas operações de Microsoft.Maui.Storage.Preferences usadas pelos ViewModels
    // de biometria (BiometricOptInCoordinator/UnlockViewModel) - sem isso os ViewModels dependem
    // direto de uma API estática de plataforma e não podem ser testados fora do runtime do MAUI.
    public interface IPreferencesService
    {
        string? GetString(string key, string? defaultValue);

        bool GetBool(string key, bool defaultValue);

        void SetString(string key, string value);

        void SetBool(string key, bool value);

        void Remove(string key);
    }
}
