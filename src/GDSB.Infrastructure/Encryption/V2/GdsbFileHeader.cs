using GDSB.Domain.Exceptions;
using System;
using System.IO;

namespace GDSB.Infrastructure.Encryption.V2
{
    // Layout (54 bytes antes do ciphertext):
    //   0   4B   magic = "GDSB"
    //   4   1B   versão do formato = 0x02
    //   5   1B   KDF id (0x01 = PBKDF2-HMAC-SHA256; 0x02 reservado p/ Argon2id)
    //   6   4B   iterações do KDF (uint32, little-endian)
    //   10  16B  salt
    //   26  12B  nonce do AES-GCM
    //   38  16B  tag de autenticação do AES-GCM
    //   54  ...  ciphertext
    // Guardar iterações e KDF id no próprio arquivo permite atualizar a recomendação da
    // OWASP em arquivos novos sem quebrar a leitura de arquivos já gravados.
    public sealed record GdsbFileHeader(byte Version, byte KdfId, uint Iterations, byte[] Salt, byte[] Nonce, byte[] Tag)
    {
        public const byte CurrentVersion = 0x02;
        public const byte Pbkdf2Sha256KdfId = 0x01;
        public const byte Argon2idKdfId = 0x02;

        public const int SaltSizeBytes = 16;
        public const int NonceSizeBytes = 12;
        public const int TagSizeBytes = 16;
        public const int SizeBytes = 4 + 1 + 1 + 4 + SaltSizeBytes + NonceSizeBytes + TagSizeBytes;

        private static readonly byte[] Magic = { (byte)'G', (byte)'D', (byte)'S', (byte)'B' };

        public void Write(Stream stream)
        {
            stream.Write(Magic);
            stream.WriteByte(Version);
            stream.WriteByte(KdfId);
            stream.Write(GetLittleEndianBytes(Iterations));
            stream.Write(Salt);
            stream.Write(Nonce);
            stream.Write(Tag);
        }

        public static GdsbFileHeader Read(Stream stream)
        {
            var magic = ReadExact(stream, Magic.Length);
            if (!magic.AsSpan().SequenceEqual(Magic))
                throw new InvalidPasswordOrCorruptFileException();

            var version = ReadByteOrThrow(stream);
            if (version != CurrentVersion)
                throw new InvalidPasswordOrCorruptFileException();

            var kdfId = ReadByteOrThrow(stream);
            var iterations = ToUInt32LittleEndian(ReadExact(stream, 4));
            var salt = ReadExact(stream, SaltSizeBytes);
            var nonce = ReadExact(stream, NonceSizeBytes);
            var tag = ReadExact(stream, TagSizeBytes);

            return new GdsbFileHeader(version, kdfId, iterations, salt, nonce, tag);
        }

        private static byte[] GetLittleEndianBytes(uint value) => new[]
        {
            (byte)value,
            (byte)(value >> 8),
            (byte)(value >> 16),
            (byte)(value >> 24),
        };

        private static uint ToUInt32LittleEndian(byte[] bytes) =>
            (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));

        private static byte ReadByteOrThrow(Stream stream)
        {
            var value = stream.ReadByte();
            if (value < 0)
                throw new InvalidPasswordOrCorruptFileException();

            return (byte)value;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            var buffer = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                    throw new InvalidPasswordOrCorruptFileException();

                offset += read;
            }

            return buffer;
        }
    }
}
