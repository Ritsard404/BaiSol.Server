using FacilitatorLibrary.DTO.History;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.Services.Interfaces
{
    public interface IHistoryRepository
    {
        Task<ICollection<ClientProjectInfoDTO>> GetProjectHistories(string userEmail);
    }
}
