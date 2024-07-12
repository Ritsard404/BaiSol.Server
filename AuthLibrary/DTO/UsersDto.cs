using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.DTO
{
    public class UsersDto
    {
        public required string Id { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string Role { get; set; }
        public string? AdminEmail { get; set; }
        public bool IsActive { get; set; }
        public bool IsSuspend { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? ClientAddress { get; set; }
        public decimal? ClientMonthlyElectricBill { get; set; }


    }
}
