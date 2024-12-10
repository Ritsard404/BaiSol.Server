using BaseLibrary.DTO.Payment;
using static BaseLibrary.Services.Repositories.PaymentRepository;

namespace BaseLibrary.Services.Interfaces
{
    public interface IPayment
    {

        Task<ICollection<GetClientPaymentDTO>> GetClientPayments(string projId);
        Task<ICollection<GetClientPaymentDTO>> GetAllPayments();
        Task<ICollection<AllPaymentsDTO>> GetAllPayment();
        Task<ICollection<SalesReportDTO>> SalesReport();
        Task<(bool, string)> CreatePayment(CreatePaymentDTO createPayment);
        Task<(bool, string)> AcknowledgePayment(AcknowledgePaymentDTO acknowledgePayment);
        Task<(bool, string)> PayOnCash(PayOnCashDTO payOnCash);
        Task<bool> IsProjectPayedDownpayment(string projId);
        Task<decimal> GetTotalProjectExpense(string projId);
        Task<decimal> GetPaymentProgress(string projId);
    }
}
