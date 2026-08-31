using GDSB.Domain.Interfaces;

namespace GDSB.Infrastructure
{
    // Implementação padrão: location é sempre um caminho de arquivo real (Windows, testes, e
    // qualquer plataforma sem necessidade de um esquema de armazenamento próprio).
    public class LocalFileSystem : IVaultFileSystem
    {
        public bool Exists(string location) => File.Exists(location);

        public byte[] ReadAllBytes(string location) => File.ReadAllBytes(location);

        public void WriteAllBytes(string location, byte[] data) => File.WriteAllBytes(location, data);

        // O prefixo "bkp_" (em vez de só um sufixo no fim) é de propósito: numa listagem de
        // arquivos com pouco espaço horizontal (celular), um nome de cofre médio/grande já
        // trunca o final antes de mostrar ".bak"/.".v1.bak", e o backup fica indistinguível do
        // arquivo real. No início do nome, a marca aparece sempre, truncamento ou não.
        public string GetBackupLocation(string location, string suffix)
        {
            var directory = Path.GetDirectoryName(location);
            var backupFileName = "bkp_" + Path.GetFileName(location) + suffix;
            return string.IsNullOrEmpty(directory) ? backupFileName : Path.Combine(directory, backupFileName);
        }
    }
}
