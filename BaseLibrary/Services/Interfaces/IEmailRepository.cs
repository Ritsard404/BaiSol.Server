
using BaiSol.Server.Models.Email;

namespace BaseLibrary.Services.Interfaces
{
    public interface IEmailRepository
    {
        void SendEmail(EmailMessage emailMessage);
    }
}
