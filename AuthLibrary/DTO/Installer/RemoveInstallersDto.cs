using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.DTO.Installer
{
    public class RemoveInstallersDto
    {
        public required List<int> InstallerIds { get; set; }
        public required string ProjectId { get; set; }
    }
}
