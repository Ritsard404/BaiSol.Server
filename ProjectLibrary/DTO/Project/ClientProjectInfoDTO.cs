namespace ProjectLibrary.DTO.Project
{
    public class ClientProjectInfoDTO
    {
        public required string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public required decimal DiscountRate { get; set; }
        public required decimal VatRate { get; set; }
        public required string clientId { get; set; }
        public required string clientFName { get; set; }
        public required string clientLName { get; set; }
        public required string clientContactNum { get; set; }
        public required string clientAddress { get; set; }
        public required decimal clientMonthlyElectricBill { get; set; }
    }
}
