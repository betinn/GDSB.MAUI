using GDSB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.Domain.Interfaces
{
    public interface IFileDecryptionService
    {
        Profile GetProfileDecrypted(string filePath, string password);
    }
}
