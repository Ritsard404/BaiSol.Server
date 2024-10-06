
using FacilitatorLibrary.DTO.Request;

namespace FacilitatorLibrary.Services.Interfaces
{
    public interface IRequestSupply
    {
        Task<List<RequestsDTO>> SentRequestByProj(string userEmail);
        Task<List<AvailableRequestSupplies>> RequestSupplies(string userEmail, string supplyCtgry);
        Task<(bool, string)> AcknowledgeRequest(AcknowledgeRequestDTO acknowledgeRequest);

    }
}
