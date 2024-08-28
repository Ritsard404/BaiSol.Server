using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.DTO.Facilitator
{
    public class AssignFacilitatorToProjectDto
    {
        public required string AdminEmail { get; set; }
        public required string FacilitatorId { get; set; }
        public required string ProjectId { get; set; }
    }
}
