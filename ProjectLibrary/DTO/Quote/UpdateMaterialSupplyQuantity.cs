

namespace ProjectLibrary.DTO.Quote
{
    public class UpdateMaterialSupplyQuantity
    {
        public required int SuppId { get; set; }
        public required int MTLId { get; set; }
        public required int Quantity { get; set; }
    }
}
