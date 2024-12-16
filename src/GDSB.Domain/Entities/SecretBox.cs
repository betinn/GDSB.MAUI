using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.Domain.Entities
{
    public class SecretBox
    {
        public bool Favorito { get; set; }
        public string BoxNmae { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass {  get; set; } = string.Empty;
        public string Obs {  get; set; } = string.Empty;
    }
}
