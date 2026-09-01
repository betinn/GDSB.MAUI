using GDSB.Domain.Entities;
using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeVaultSessionService : IVaultSessionService
    {
        public VaultSettings Settings { get; private set; } = new();

        public List<VaultSettings> StartCalls { get; } = new();

        public int ClearCallCount { get; private set; }

        public void Start(VaultSettings settings)
        {
            Settings = settings;
            StartCalls.Add(settings);
        }

        public void Clear()
        {
            Settings = new();
            ClearCallCount++;
        }
    }
}
