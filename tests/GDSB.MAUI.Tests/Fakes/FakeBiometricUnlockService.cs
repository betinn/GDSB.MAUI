using GDSB.Domain.Interfaces;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeBiometricUnlockService : IBiometricUnlockService
    {
        public bool IsAvailable { get; set; }

        public bool IsEnabled { get; set; }

        public bool StoreKeyResult { get; set; } = true;

        public byte[]? TryUnlockResult { get; set; }

        public int DisableCallCount { get; private set; }

        public Task<bool> IsAvailableAsync() => Task.FromResult(IsAvailable);

        public Task<bool> IsEnabledAsync() => Task.FromResult(IsEnabled);

        public Task<bool> StoreKeyAsync(byte[] derivedKey) => Task.FromResult(StoreKeyResult);

        public Task<byte[]?> TryUnlockAsync() => Task.FromResult(TryUnlockResult);

        public Task DisableAsync()
        {
            DisableCallCount++;
            return Task.CompletedTask;
        }
    }
}
