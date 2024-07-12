using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.DTO
{
    public class UsersWithRole
    {
        public AppUsers Users { get; set; }
        public string Role { get; set; }
    }
}
