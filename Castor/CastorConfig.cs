using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Castor
{
    public class CastorConfig
    {
        public string ArchiveName { get; set; }
        public bool Z2f { get; set; }
        public string[] IncludeFolders { get; set; }
        public string[] ExcludeFolders { get; set; }
    }
}
