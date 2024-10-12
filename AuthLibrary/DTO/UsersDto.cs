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
        public required string Name { get; set; }
        public required string UserName { get; set; }
        public required string Role { get; set; }
        public string? AdminEmail { get; set; }
        public string? Status { get; set; }
        public string? UpdatedAt { get; set; }
        public string? CreatedAt { get; set; }
        public string? ClientContactNum { get; set; }
        public string? ClientAddress { get; set; }
        public string? Sex { get; set; }
        public decimal? kWCapacity { get; set; }


    }
}
