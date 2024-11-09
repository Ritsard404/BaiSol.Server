using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Report
{
    public class TaskCounts
    {
        public required int AllTasks { get; set; }
        public required int FinishedTasks { get; set; }
    }
}
