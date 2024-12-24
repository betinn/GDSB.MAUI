using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDSB.Domain.Entities
{
    public class Profile
    {
        public string Nome { get; set; } = string.Empty;
        public List<SecretBox> Boxes { get; set; } = new List<SecretBox>();

    }
}
