using GDSB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.MAUI.Interfaces
{
    public interface IFileDataService
    {
        Profile GetProfile(string filePath, string password);
    }
}
