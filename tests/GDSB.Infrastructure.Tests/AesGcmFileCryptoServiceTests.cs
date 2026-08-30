using GDSB.Domain.Entities;
using GDSB.Domain.Exceptions;
using GDSB.Infrastructure.Encryption.V2;
using System.Text.Json;
using Xunit;

namespace GDSB.Infrastructure.Tests
{
    public class AesGcmFileCryptoServiceTests
    {
        private const string Password = "senha-correta-123";

        private readonly AesGcmFileCryptoService _sut = new();

        private static Profile CreateSampleProfile() => new()
        {
            Nome = "Cofre de teste",
            Boxes = new List<SecretBox>
            {
                new()
                {
                    Favorito = true,
                    BoxName = "Email",
                    Url = "https://mail.example.com",
                    User = "usuario@example.com",
                    Pass = "senha-com-acentuação-áéíóú",
                    Obs = "observação",
                },
            },
        };

        [Fact]
        public void Encrypt_Then_Decrypt_ReturnsOriginalJson()
        {
            var profile = CreateSampleProfile();
            var json = JsonSerializer.Serialize(profile);

            var fileBytes = _sut.Encrypt(json, Password);
            var decryptedJson = _sut.Decrypt(fileBytes, Password);

            Assert.Equal(json, decryptedJson);

            var roundTripProfile = JsonSerializer.Deserialize<Profile>(decryptedJson);
            Assert.NotNull(roundTripProfile);
            Assert.Equal(profile.Nome, roundTripProfile!.Nome);
            Assert.Equal(profile.Boxes[0].Pass, roundTripProfile.Boxes[0].Pass);
        }

        [Fact]
        public void Decrypt_WithWrongPassword_ThrowsInvalidPasswordOrCorruptFileException()
        {
            var json = JsonSerializer.Serialize(CreateSampleProfile());
            var fileBytes = _sut.Encrypt(json, Password);

            Assert.Throws<InvalidPasswordOrCorruptFileException>(() => _sut.Decrypt(fileBytes, "senha-errada"));
        }

        [Fact]
        public void Decrypt_WithTamperedCiphertext_ThrowsInvalidPasswordOrCorruptFileException()
        {
            var json = JsonSerializer.Serialize(CreateSampleProfile());
            var fileBytes = _sut.Encrypt(json, Password);

            fileBytes[^1] ^= 0xFF;

            Assert.Throws<InvalidPasswordOrCorruptFileException>(() => _sut.Decrypt(fileBytes, Password));
        }

        [Fact]
        public void Encrypt_CalledTwice_ProducesDifferentSaltAndNonce()
        {
            var json = JsonSerializer.Serialize(CreateSampleProfile());

            var first = _sut.Encrypt(json, Password);
            var second = _sut.Encrypt(json, Password);

            var firstSalt = first[10..26];
            var secondSalt = second[10..26];
            var firstNonce = first[26..38];
            var secondNonce = second[26..38];

            Assert.False(firstSalt.SequenceEqual(secondSalt));
            Assert.False(firstNonce.SequenceEqual(secondNonce));
        }
    }
}
