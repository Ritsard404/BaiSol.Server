using System.ComponentModel.DataAnnotations;

namespace AuthLibrary.DTO
{
    public class ClientDto
    {

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.PhoneNumber)]
        public string ClientContactNum { get; set; } = string.Empty;

        [Required]
        public string AdminEmail { get; set; } = string.Empty;

        public required string ClientAddress { get; set; }
        public decimal ClientMonthlyElectricBill { get; set; }
    }
}
