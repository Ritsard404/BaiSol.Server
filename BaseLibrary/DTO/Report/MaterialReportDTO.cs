namespace BaseLibrary.DTO.Report
{
    public class MaterialReportDTO
    {
        public int SuppId { get; set; }
        public int? MTLQuantity { get; set; }
        public required string AssignedPrice { get; set; }


        public required string ProjId { get; set; }
        public required string MTLCode { get; set; }
        public required string MTLDescript { get; set; }
        public required string CurrentPrice { get; set; }
        public int MTLQOH { get; set; }
        public required string MTLUnit { get; set; }
        public required string MTLCategory { get; set; }
        public required string UpdatedAt { get; set; }
        public required string CreatedAt { get; set; }
    }
}
