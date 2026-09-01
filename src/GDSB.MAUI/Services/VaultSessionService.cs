using GDSB.Domain.Entities;

namespace GDSB.MAUI.Services
{
    public class VaultSessionService : IVaultSessionService
    {
        public VaultSettings Settings { get; private set; } = new();

        public void Start(VaultSettings settings) => Settings = settings;

        public void Clear() => Settings = new();
    }
}
