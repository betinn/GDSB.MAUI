namespace GDSB.Domain.Interfaces
{
    // Abstrai onde e como os bytes de um cofre são lidos/gravados. ProfileFileService nunca sabe
    // se "location" é um caminho de arquivo real (Windows, ou qualquer path comum) ou algo
    // específico de plataforma (no Android, um content:// URI persistido do Storage Access
    // Framework) — só assim uma gravação depois de abrir um arquivo sincronizado (Google Drive,
    // OneDrive) chega de fato no arquivo original, e não numa cópia local perdida.
    public interface IVaultFileSystem
    {
        bool Exists(string location);
        byte[] ReadAllBytes(string location);
        void WriteAllBytes(string location, byte[] data);
    }
}
