namespace ProjectLibrary.DTO.Project
{
    public class ClientProjectInfoDTO
    {
        public required string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public required decimal Discount { get; set; }
        public required decimal VatRate { get; set; }
        public required string clientId { get; set; }
        public required string clientFName { get; set; }
        public required string clientLName { get; set; }
        public required string clientContactNum { get; set; }
        public required string clientAddress { get; set; }
        public required string SystemType { get; set; }
        public required decimal kWCapacity { get; set; }
        public required string Sex { get; set; }
        public required bool isMale { get; set; }
    }
}
