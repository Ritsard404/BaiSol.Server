using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthLibrary.DTO.Facilitator
{
    public class RemoveFacilitatorDto
    {
        public required string facilitatorId { get; set; }
        public required string projectId { get; set; }

    }
}
