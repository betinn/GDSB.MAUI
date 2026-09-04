using System.Security.Cryptography;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Services;
using Microsoft.Maui.Storage;
using Windows.Security.Credentials.UI;

namespace GDSB.MAUI.Platforms.Windows.Services
{
    // Prioridade menor que a versão Android desta fase (o app é usado sobretudo no tablet), mas
    // segue o mesmo princípio: o segredo selado (a senha mestra) só sai do disco depois de uma
    // verificação nova do Windows Hello (UserConsentVerifier). O DPAPI (ProtectedData, escopo
    // CurrentUser) protege o arquivo em repouso; quem garante que reabrir exige o usuário de novo
    // é o RequestVerificationAsync antes de cada leitura, não o DPAPI sozinho.
    public class BiometricUnlockService : IBiometricUnlockService
    {
        private const string FileName = "biometric-unlock.bin";

        private readonly ILocalizationService _localization;

        public BiometricUnlockService(ILocalizationService localization)
        {
            _localization = localization;
        }

        private string ActivateMessage => _localization.Get("Platform_WindowsBiometricActivateMessage");

        private string UnlockMessage => _localization.Get("Platform_WindowsBiometricUnlockMessage");

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                return availability == UserConsentVerifierAvailability.Available;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<bool> IsEnabledAsync() => Task.FromResult(File.Exists(GetFilePath()));

        public async Task<bool> StoreKeyAsync(byte[] derivedKey)
        {
            try
            {
                var result = await UserConsentVerifier.RequestVerificationAsync(ActivateMessage);
                if (result != UserConsentVerificationResult.Verified)
                    return false;

                var protectedBytes = ProtectedData.Protect(derivedKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
                await File.WriteAllBytesAsync(GetFilePath(), protectedBytes);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<byte[]?> TryUnlockAsync()
        {
            var path = GetFilePath();
            if (!File.Exists(path))
                return null;

            try
            {
                var result = await UserConsentVerifier.RequestVerificationAsync(UnlockMessage);
                if (result != UserConsentVerificationResult.Verified)
                    return null;

                var protectedBytes = await File.ReadAllBytesAsync(path);
                return ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task DisableAsync()
        {
            var path = GetFilePath();
            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }

        private static string GetFilePath() => Path.Combine(FileSystem.AppDataDirectory, FileName);
    }
}
