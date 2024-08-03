
using ProjectLibrary.DTO.Quote;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IMaterialQuote
    {
        Task<string> AddNewMaterialSupply(MaterialQuoteDto materialQuoteDto);
        Task<bool> Save();
    }
}
