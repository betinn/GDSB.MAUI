using GDSB.Domain.Interfaces;

namespace GDSB.MAUI.Services
{
    // Agrupa os dois serviços quase sempre usados juntos ao abrir, criar ou editar um cofre - ler/
    // gravar o arquivo e acompanhar a sessão corrente - só para manter os construtores de
    // UnlockViewModel/VaultSettingsViewModel dentro do limite de parâmetros do analisador estático
    // (eles já passavam de 7 antes deste agrupamento).
    public sealed class VaultAccess(IProfileFileService profileFileService, IVaultSessionService vaultSessionService)
    {
        public IProfileFileService ProfileFileService { get; } = profileFileService;

        public IVaultSessionService VaultSessionService { get; } = vaultSessionService;
    }
}
