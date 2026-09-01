using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeVaultBackupStore : IVaultBackupStore
    {
        public List<VaultBackupInfo> Items { get; } = new();

        public List<string> DeleteAllForCalls { get; } = new();

        public VaultBackupInfo Store(string originLocation, string vaultName, byte[] previousBytes, VaultBackupKind kind)
        {
            var info = new VaultBackupInfo(
                Id: $"{originLocation}::backup",
                DisplayName: $"BKP - {vaultName}.GDSBX.bak",
                VaultName: vaultName,
                OriginLocation: originLocation,
                Kind: kind,
                CreatedAtUtc: DateTime.UtcNow,
                SizeBytes: previousBytes.Length);
            Items.Add(info);
            return info;
        }

        public IReadOnlyList<VaultBackupInfo> List() => Items;

        public byte[] Read(VaultBackupInfo info) => Array.Empty<byte>();

        public void Delete(VaultBackupInfo info) => Items.Remove(info);

        public void DeleteAllFor(string originLocation)
        {
            DeleteAllForCalls.Add(originLocation);
            Items.RemoveAll(i => i.OriginLocation == originLocation);
        }
    }
}
