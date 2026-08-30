namespace GDSB.Domain.Interfaces
{
    // Biometria nunca substitui a senha mestra - ela é a única fonte real da chave de criptografia.
    // O que este serviço guarda, atrás de hardware protegido por biometria (Android Keystore com
    // setUserAuthenticationRequired / Windows Hello + DPAPI), é só um atalho: um segredo (os bytes
    // da senha mestra) selado de um jeito que só pode ser extraído depois de uma nova checagem de
    // biometria. Nunca fica em texto puro fora desse hardware. StoreKeyAsync só deve ser chamado
    // depois de um Open manual bem-sucedido - a senha mestra continua sempre disponível como
    // fallback (troca de aparelho, biometria indisponível, chave invalidada).
    public interface IBiometricUnlockService
    {
        Task<bool> IsAvailableAsync();

        Task<bool> IsEnabledAsync();

        Task<bool> StoreKeyAsync(byte[] derivedKey);

        // null se biometria indisponível, o usuário cancelar/errar, ou a chave do sistema tiver
        // sido invalidada (ex.: nova digital cadastrada no Android) - a UI cai de volta pro campo
        // de senha sem tratamento especial.
        Task<byte[]?> TryUnlockAsync();

        Task DisableAsync();
    }
}
