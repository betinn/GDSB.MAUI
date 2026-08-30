using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.MAUI.Interfaces
{
    // O retorno de ambos os métodos é opaco de propósito - um caminho de arquivo real (Windows)
    // ou algo específico de plataforma (no Android, um content:// URI persistido do Storage Access
    // Framework) - quem consome só repassa pra IProfileFileService, nunca interpreta o valor.
    public interface IFilePickerService
    {
        Task<string> PickFileNameAsync();

        Task<string> PickSaveLocationAsync(string suggestedName);
    }
}
