using GDSB.Domain.Entities;
using GDSB.Domain.Interfaces;
using GDSB.MAUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.MAUI.Services
{
    public class FileDataService(IFileDecryptionService fileDecryptionService) : IFileDataService
    {
        private readonly IFileDecryptionService _fileDecryptionService = fileDecryptionService;
        public Profile GetProfile(string filePath, string password)
        {
            return _fileDecryptionService.GetProfileDecrypted(filePath, password);
        }
    }
}
