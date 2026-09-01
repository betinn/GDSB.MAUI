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
    }
}
