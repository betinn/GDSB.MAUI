using GDSB.Domain.Exceptions;
using GDSB.Domain.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GDSB.Infrastructure.Encryption.V2
{
    public class AesGcmFileCryptoService : IFileCryptoServiceV2
    {
        private const int KeySizeBytes = 32;
        private const int DefaultIterations = 210_000;

        public byte[] Encrypt(string json, string password)
        {
            ArgumentNullException.ThrowIfNull(json);
            ArgumentNullException.ThrowIfNull(password);

            var salt = RandomNumberGenerator.GetBytes(GdsbFileHeader.SaltSizeBytes);
            var nonce = RandomNumberGenerator.GetBytes(GdsbFileHeader.NonceSizeBytes);
            var iterations = DefaultIterations;

            var key = DeriveKey(password, salt, iterations);
            try
            {
                var plaintext = Encoding.UTF8.GetBytes(json);
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[GdsbFileHeader.TagSizeBytes];

                using (var aesGcm = new AesGcm(key, GdsbFileHeader.TagSizeBytes))
                {
                    aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
                }

                var header = new GdsbFileHeader(
                    GdsbFileHeader.CurrentVersion,
                    GdsbFileHeader.Pbkdf2Sha256KdfId,
                    (uint)iterations,
                    salt,
                    nonce,
                    tag);

                using var output = new MemoryStream();
                header.Write(output);
                output.Write(ciphertext);
                return output.ToArray();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        public string Decrypt(byte[] fileBytes, string password)
        {
            ArgumentNullException.ThrowIfNull(fileBytes);
            ArgumentNullException.ThrowIfNull(password);

            using var input = new MemoryStream(fileBytes, writable: false);

            GdsbFileHeader header;
            try
            {
                header = GdsbFileHeader.Read(input);
            }
            catch (InvalidPasswordOrCorruptFileException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidPasswordOrCorruptFileException(ex);
            }

            if (header.KdfId != GdsbFileHeader.Pbkdf2Sha256KdfId)
                throw new InvalidPasswordOrCorruptFileException();

            var ciphertext = new byte[fileBytes.Length - input.Position];
            _ = input.Read(ciphertext, 0, ciphertext.Length);

            var key = DeriveKey(password, header.Salt, (int)header.Iterations);
            try
            {
                var plaintext = new byte[ciphertext.Length];

                using (var aesGcm = new AesGcm(key, GdsbFileHeader.TagSizeBytes))
                {
                    try
                    {
                        aesGcm.Decrypt(header.Nonce, ciphertext, header.Tag, plaintext);
                    }
                    catch (CryptographicException ex)
                    {
                        throw new InvalidPasswordOrCorruptFileException(ex);
                    }
                }

                return Encoding.UTF8.GetString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }

        private static byte[] DeriveKey(string password, byte[] salt, int iterations) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeySizeBytes);
    }
}
