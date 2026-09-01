using GDSB.Domain.Entities;

namespace GDSB.MAUI.Services
{
    // Portador da configuração do cofre atualmente aberto - ClipboardService e IdleLockService são
    // singletons e não conhecem o Profile, então precisam de algo que sobreviva entre chamadas e
    // reflita o VaultSettings do cofre corrente. Start é chamado ao entrar na VaultPage (abrir ou
    // criar); Clear ao voltar pro Unlock, inclusive quando o auto-lock estoura.
    public interface IVaultSessionService
    {
        VaultSettings Settings { get; }

        void Start(VaultSettings settings);

        void Clear();
    }
}
