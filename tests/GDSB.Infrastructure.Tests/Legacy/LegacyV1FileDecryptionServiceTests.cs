using GDSB.Domain.Entities;
using GDSB.Infrastructure.Encryption.Legacy;
using Xunit;

namespace GDSB.Infrastructure.Tests.Legacy
{
#pragma warning disable CS0618 // testando deliberadamente o leitor legado obsoleto
    public class LegacyV1FileDecryptionServiceTests
    {
        private const string Password = "senha-legada-123";

        private readonly LegacyV1FileDecryptionService _sut = new();

        private static Profile CreateSampleProfile() => new()
        {
            Nome = "Cofre legado",
            Boxes = new List<SecretBox>
            {
                new()
                {
                    Favorito = true,
                    BoxName = "Banco",
                    Url = "https://banco.example.com",
                    User = "usuario-legado",
                    Pass = "senha-com-acentuação-áéíóú",
                    Obs = "conta corrente",
                },
            },
        };

        [Fact]
        public void GetProfileDecrypted_WithValidV1File_ReturnsOriginalProfile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                var profile = CreateSampleProfile();
                LegacyV1FixtureBuilder.WriteV1File(path, profile, Password);

                var result = _sut.GetProfileDecrypted(File.ReadAllText(path), Password);

                Assert.Equal(profile.Nome, result.Nome);
                Assert.Equal(profile.Boxes[0].BoxName, result.Boxes[0].BoxName);
                Assert.Equal(profile.Boxes[0].Pass, result.Boxes[0].Pass);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void GetProfileDecrypted_WithWrongPassword_Throws()
        {
            var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.GDSBX");
            try
            {
                LegacyV1FixtureBuilder.WriteV1File(path, CreateSampleProfile(), Password);

                Assert.ThrowsAny<Exception>(() => _sut.GetProfileDecrypted(File.ReadAllText(path), "senha-errada"));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
#pragma warning restore CS0618
}
