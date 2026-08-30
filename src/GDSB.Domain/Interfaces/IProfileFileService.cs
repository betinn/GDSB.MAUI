using GDSB.Domain.Entities;

namespace GDSB.Domain.Interfaces
{
    // Ponto único de acesso a arquivo .GDSBX: esconde do resto do app se o arquivo lido é v1 (legado)
    // ou v2, e garante que tudo que é salvo vai sempre no formato novo (AES-GCM + PBKDF2).
    // "location" é opaco de propósito: um caminho de arquivo real (Windows) ou algo específico de
    // plataforma (no Android, um content:// URI do Storage Access Framework) - ver IVaultFileSystem.
    public interface IProfileFileService
    {
        ProfileOpenResult Open(string location, string password);
        void Save(string location, Profile profile, string password);
    }
}
