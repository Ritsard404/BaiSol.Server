using DataLibrary.Models;
using ProjectLibrary.DTO.Requisition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IRequisition
    {
        Task<(bool, string)> RequestSupply(AddRequestDTO addRequest);
        Task<List<Requisition>> AllRequest();
        Task<List<Requisition>> SentRequestByProj(string projId);
        Task<(bool, string)> ApproveRequest(StatusRequestDTO approveRequest);
        Task<(bool, string)> DeclineRequest(StatusRequestDTO declineRequest);
        Task<(bool, string)> UpdateRequest(UpdateQuantity updateQuantity);
        Task<(bool, string)> DeleteRequest(DeleteRequest deleteRequest);

        Task<List<AvailableRequestSupplies>> RequestSupplies(string projId, string supplyCtgry);
    }
}
