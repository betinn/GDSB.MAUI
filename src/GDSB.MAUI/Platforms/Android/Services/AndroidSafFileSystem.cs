using System.Security.Cryptography;
using System.Text;
using GDSB.Domain.Interfaces;
using Microsoft.Maui.Storage;

namespace GDSB.MAUI.Platforms.Android.Services
{
    // ProfileFileService (Infrastructure) só enxerga uma "location" opaca. Aqui ela é normalmente
    // um content:// URI do Storage Access Framework, escolhido/criado pelo FilePickerService deste
    // projeto (ver lá o porquê). O backup (.bak/.v1.bak) de uma location content:// não pode morar
    // "do lado" do arquivo original - SAF não dá acesso à pasta-mãe de um documento avulso - então
    // fica no armazenamento privado do app, associado ao URI por um nome estável (hash), o que
    // ainda assim sobrevive a saves seguintes do mesmo cofre.
    public class AndroidSafFileSystem : IVaultFileSystem
    {
        private const string ContentScheme = "content://";

        public bool Exists(string location)
        {
            if (!IsContentUri(location))
                return File.Exists(location);

            try
            {
                using var stream = OpenContentInputStream(location);
                return stream is not null;
            }
            catch
            {
                return false;
            }
        }

        public byte[] ReadAllBytes(string location)
        {
            if (!IsContentUri(location))
                return File.ReadAllBytes(location);

            using var stream = OpenContentInputStream(location)
                ?? throw new FileNotFoundException($"Não foi possível abrir o cofre em {location}.");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        public void WriteAllBytes(string location, byte[] data)
        {
            if (!IsContentUri(location))
            {
                File.WriteAllBytes(location, data);
                return;
            }

            var resolver = global::Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("ContentResolver indisponível.");
            var uri = global::Android.Net.Uri.Parse(location);

            // "wt" = write + truncate: sem isso, alguns provedores concatenam em vez de substituir.
            using var stream = resolver.OpenOutputStream(uri!, "wt")
                ?? throw new FileNotFoundException($"Não foi possível gravar o cofre em {location}.");
            stream.Write(data, 0, data.Length);
        }

        public string GetBackupLocation(string location, string suffix)
        {
            if (!IsContentUri(location))
                return location + suffix;

            var backupDir = Path.Combine(FileSystem.AppDataDirectory, "vault-backups");
            Directory.CreateDirectory(backupDir);

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(location)));
            return Path.Combine(backupDir, hash + suffix);
        }

        private static bool IsContentUri(string location) =>
            location.StartsWith(ContentScheme, StringComparison.Ordinal);

        private static Stream? OpenContentInputStream(string location)
        {
            var resolver = global::Android.App.Application.Context.ContentResolver;
            var uri = global::Android.Net.Uri.Parse(location);
            return resolver?.OpenInputStream(uri!);
        }
    }
}
