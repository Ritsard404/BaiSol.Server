using DataLibrary.Data;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace BaiSol.Server.Controllers.Gantt
{
    [Route("api/[controller]")]
    [ApiController]
    public class GanttController(DataContext _dataContext) : ControllerBase
    {
        public class GanttResponse<T>
        {
            [JsonPropertyName("Items")]
            public T Items { get; set; }

            [JsonPropertyName("Count")]
            public int Count { get; set; }
        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var dataList = await _dataContext.GanttData
                .ToListAsync();

            var response = new GanttResponse<List<GanttData>>
            {
                Items = dataList,
                Count = dataList.Count
            };

            return Ok(response); // Return 200 OK response with the data
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GanttData[] data)
        {
            await _dataContext.GanttData.AddRangeAsync(data);
            await _dataContext.SaveChangesAsync();
            return Ok(data);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] GanttData[] value)
        {
            if (value == null || value.Length == 0)
            {
                return BadRequest("Data is null or empty.");
            }

            foreach (var updatedData in value)
            {
                var existingData = await _dataContext.GanttData.FindAsync(updatedData.TaskId);

                if (existingData == null)
                {
                    return NotFound($"GanttData with TaskId {updatedData.TaskId} not found.");
                }

                // Update properties
                existingData.TaskName = updatedData.TaskName;
                existingData.PlannedStartDate = updatedData.PlannedStartDate;
                existingData.PlannedEndDate = updatedData.PlannedEndDate;
                existingData.ActualStartDate = updatedData.ActualStartDate;
                existingData.ActualEndDate = updatedData.ActualEndDate;
                existingData.Progress = updatedData.Progress;
                existingData.Duration = updatedData.Duration;
                existingData.Predecessor = updatedData.Predecessor;
                existingData.ParentId = updatedData.ParentId;
            }

            await _dataContext.SaveChangesAsync();
            return Ok(value);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Find the existing record by TaskId
            var existingData = await _dataContext.GanttData.FindAsync(id);

            if (existingData == null)
            {
                // Return a NotFound result if the record does not exist
                return NotFound($"GanttData with TaskId {id} not found.");
            }

            // Remove the existing record
            _dataContext.GanttData.Remove(existingData);

            // Save changes to the database
            await _dataContext.SaveChangesAsync();

            return Ok(id);
        }

    }
}
