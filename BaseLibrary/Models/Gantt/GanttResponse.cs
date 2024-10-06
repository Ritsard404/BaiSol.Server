using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaseLibrary.Models.Gantt
{
    public class GanttResponse<T>
    {

        [JsonPropertyName("Items")]
        public T Items { get; set; }

        [JsonPropertyName("Count")]
        public int Count { get; set; }
    }
}
