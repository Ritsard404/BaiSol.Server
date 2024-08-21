using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.DTO.Installer
{
    public class AvailableInstallerDto
    {
        public int InstallerId { get; set; }
        public required string Name { get; set; }
        public required string Position { get; set; }
    }
}
