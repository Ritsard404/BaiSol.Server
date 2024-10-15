﻿using BaseLibrary.DTO.Payment;

namespace BaseLibrary.Services.Interfaces
{
    public interface IPayment
    {
        Task<ICollection<GetClientPaymentDTO>> GetClientPayments(string projId);
        Task<ICollection<GetClientPaymentDTO>> GetAllPayments();
        Task<(bool, string)> CreatePayment(CreatePaymentDTO createPayment);
        Task<(bool, string)> AcknowledgePayment(AcknowledgePaymentDTO acknowledgePayment);
    }
}
