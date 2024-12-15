using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.MAUI.Interfaces
{
    public interface IFilePickerService
    {
        Task<string> PickFileNameAsync();
    }
}
