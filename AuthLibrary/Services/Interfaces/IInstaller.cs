using AuthLibrary.DTO.Installer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.Services.Interfaces
{
    public interface IInstaller
    {
        Task<ICollection<AvailableInstallerDto>> GetAvailableInstaller();
    }
}
