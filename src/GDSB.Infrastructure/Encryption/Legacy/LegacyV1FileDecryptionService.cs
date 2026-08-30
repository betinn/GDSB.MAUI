using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GDSB.Infrastructure.Encryption.Legacy
{
    // Leitor do formato .GDSBX v1 (diagnosticado na Fase 0 como fraco: IV fixo, senha ciclada sem KDF).
    // Existe só para abrir arquivos antigos e migrá-los para o formato v2 (AesGcmFileCryptoService) ao salvar.
    // Nunca deve voltar a gravar nada neste formato.
    [Obsolete("Somente leitura de arquivos .GDSBX v1 legados, para migração. Não usar para gravar arquivos novos — use IFileCryptoServiceV2.")]
    public class LegacyV1FileDecryptionService : IFileDecryptionService
    {
        //chumbado mesmo pq o jovem roberto fez isso sem nem pensar duas vezes
        //um dia será corrigido
        private static byte[] _aesIVBase = new byte[16] { 239, 68, 204, 163, 219, 235, 157, 26, 55, 162, 251, 0, 207, 131, 254, 254 };
        public Profile GetProfileDecrypted(string filePath, string password)
        {
            var profileBytes = Convert.FromBase64String(ExtractJsonData(File.ReadAllText(filePath), "profileEncrypted"));

            var decryptedAesJson = DecryptStringFromBytes_Aes(profileBytes, GetPasswordStringIntoByte(password), _aesIVBase);

            var aesText = Convert.FromBase64String(ExtractJsonData(decryptedAesJson, "Montain"));
            var aesByteFirst = Convert.FromBase64String(ExtractJsonData(decryptedAesJson, "bytekyte"));
            var aesSecByte = Convert.FromBase64String(ExtractJsonData(decryptedAesJson, "secbyte"));

            var profile = JsonConvert.DeserializeObject<Profile>(DecryptStringFromBytes_Aes(aesText, aesByteFirst, aesSecByte));

            if (profile == null)
                throw new NullReferenceException("Profile is null");

            return profile;
        }
        private static string ExtractJsonData(string json, string property)
        {
            try
            {
                using (JsonDocument jdoc = JsonDocument.Parse(json))
                {
                    JsonElement root = jdoc.RootElement;
                    if (root.TryGetProperty(property, out JsonElement element))
                    {
                        var stringContent = element.GetString();
                        if (!string.IsNullOrEmpty(stringContent))
                            return stringContent;
                        else
                            throw new ArgumentException("stringContent from element was null");
                    }
                    else
                        throw new ArgumentException($"{property} not found");
                }
            }
            catch(Exception ex)
            {
                throw new ArgumentException($"ProfileData not found on filePath. Property: {property}. exception message: {ex.Message}");
            }
        }
        private static byte[] GetPasswordStringIntoByte(string senha) // isso aqui é uma vergonha, pq fizestes isso jovem roberto?
        {
            byte[] newKey = new byte[32];
            var bts = Encoding.ASCII.GetBytes(senha);
            int countPassLengh = 0;
            for (int i = 0; i < newKey.Length; i++)
            {
                if (countPassLengh == bts.Length)
                    countPassLengh = 0;
                newKey[i] = bts[countPassLengh];
                countPassLengh++;
            }
            return newKey;
        }
        private static string DecryptStringFromBytes_Aes(byte[] profileContentEncrypted, byte[] PasswordBytes, byte[] IVBase)
        {
            if (profileContentEncrypted == null || profileContentEncrypted.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (PasswordBytes == null || PasswordBytes.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IVBase == null || IVBase.Length <= 0)
                throw new ArgumentNullException("IV");

            string plaintext = null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = PasswordBytes;
                aesAlg.IV = IVBase;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(profileContentEncrypted))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}
