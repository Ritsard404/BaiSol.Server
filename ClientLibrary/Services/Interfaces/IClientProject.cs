using ClientLibrary.DTO.CLientProjectDTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Services.Interfaces
{
    public interface IClientProject
    {
        Task<ProjectId> GetClientProject(string userEmail);
    }
}
