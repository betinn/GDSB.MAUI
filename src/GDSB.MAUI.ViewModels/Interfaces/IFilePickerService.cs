using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.MAUI.Interfaces
{
    // Location é opaca de propósito - um caminho de arquivo real (Windows) ou algo específico de
    // plataforma (no Android, um content:// URI persistido do Storage Access Framework) - quem
    // consome só repassa pra IProfileFileService, nunca interpreta o valor. DisplayName já vem
    // pronto pra mostrar na UI (no Android, Location não é legível).
    public record PickedFile(string Location, string DisplayName);

    public interface IFilePickerService
    {
        Task<PickedFile?> PickFileNameAsync();

        Task<string> PickSaveLocationAsync(string suggestedName);
    }
}
