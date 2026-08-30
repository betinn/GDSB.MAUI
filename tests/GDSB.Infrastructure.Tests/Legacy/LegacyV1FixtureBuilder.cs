using GDSB.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GDSB.Infrastructure.Tests.Legacy
{
    // Fabrica um arquivo .GDSBX v1 válido replicando o algoritmo legado (já diagnosticado como fraco:
    // IV fixo, senha ciclada sem KDF) só para poder testar LegacyV1FileDecryptionService/ProfileFileService
    // contra um arquivo real desse formato — não existe nenhum arquivo de exemplo no repositório.
    // Isto NUNCA deve ser usado fora de teste: é o inverso do leitor legado, não uma capacidade nova do app.
    internal static class LegacyV1FixtureBuilder
    {
        private static readonly byte[] AesIvBase = { 239, 68, 204, 163, 219, 235, 157, 26, 55, 162, 251, 0, 207, 131, 254, 254 };

        public static void WriteV1File(string path, Profile profile, string password)
        {
            var profileJson = JsonSerializer.Serialize(profile);

            var innerKey = RandomNumberGenerator.GetBytes(32);
            var innerIv = RandomNumberGenerator.GetBytes(16);
            var aesText = EncryptStringToBytes_Aes(profileJson, innerKey, innerIv);

            var innerJson = JsonSerializer.Serialize(new
            {
                Montain = Convert.ToBase64String(aesText),
                bytekyte = Convert.ToBase64String(innerKey),
                secbyte = Convert.ToBase64String(innerIv),
            });

            var outerKey = GetPasswordStringIntoByte(password);
            var profileBytes = EncryptStringToBytes_Aes(innerJson, outerKey, AesIvBase);

            var outerJson = JsonSerializer.Serialize(new { profileEncrypted = Convert.ToBase64String(profileBytes) });

            File.WriteAllText(path, outerJson);
        }

        private static byte[] GetPasswordStringIntoByte(string senha)
        {
            var newKey = new byte[32];
            var bts = Encoding.ASCII.GetBytes(senha);
            var countPassLengh = 0;
            for (var i = 0; i < newKey.Length; i++)
            {
                if (countPassLengh == bts.Length)
                    countPassLengh = 0;
                newKey[i] = bts[countPassLengh];
                countPassLengh++;
            }
            return newKey;
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
        {
            using var aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.IV = iv;

            using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return msEncrypt.ToArray();
        }
    }
}
