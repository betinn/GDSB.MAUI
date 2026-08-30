using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;

namespace GDSB.MAUI.Tests.Fakes
{
    internal sealed class FakeProfileFileService : IProfileFileService
    {
        public Func<string, string, ProfileOpenResult>? OpenHandler { get; set; }

        public List<(string Location, Profile Profile, string Password)> SaveCalls { get; } = new();

        public ProfileOpenResult Open(string location, string password) =>
            OpenHandler?.Invoke(location, password)
                ?? throw new InvalidOperationException("OpenHandler not configured.");

        public void Save(string location, Profile profile, string password) =>
            SaveCalls.Add((location, profile, password));
    }
}
