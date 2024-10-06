using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Project;
using System.Text.Json.Serialization;

namespace BaiSol.Server.Controllers.Gantt
{
    [Route("api/[controller]")]
    [ApiController]
    public class GanttController(IGanttRepository _ganttRepository, DataContext _dataContext) : ControllerBase
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
                .Include(s=>s.SubTasks)
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
            if (data == null || data.Length == 0)
            {
                return BadRequest("No data provided.");
            }


            // Process each GanttData entry
            foreach (var ganttData in data)
            {
                // Check if SubTasks are not null and have elements
                if (ganttData.SubTasks != null && ganttData.SubTasks.Any())
                {
                    //foreach (var subTask in ganttData.SubTasks)
                    //{
                    //    // Set the foreign key to link SubTask with its GanttData
                    //    subTask.GanttDataId = ganttData.TaskId; // Ensure you have a GanttDataId in SubTask
                    //}

                    // Add SubTasks to the DbContext
                    await _dataContext.SubTask.AddRangeAsync(ganttData.SubTasks);
                }
            }

            // Add each GanttData object to the DbContext
            await _dataContext.GanttData.AddRangeAsync(data);
            await _dataContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = data[0].TaskId }, data); // Return 201 Created
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] GanttData[] data)
        {
            if (data == null || data.Length == 0)
            {
                return BadRequest("No data provided.");
            }

            foreach (var task in data)
            {
                var result = await _dataContext.GanttData.FirstOrDefaultAsync(or => or.TaskId == task.TaskId);

                if (result == null)
                {
                    return NotFound($"Task with ID {task.TaskId} not found."); // Return 404 Not Found
                }

                // Update the record fields
                //result.TaskId = task.TaskId;
                result.TaskName = task.TaskName;
                result.PlannedStartDate = task.PlannedStartDate;
                result.PlannedEndDate = task.PlannedEndDate;
                result.ActualStartDate = task.ActualStartDate;
                result.ActualEndDate = task.ActualEndDate;


                if (result.SubTasks != null)
                {
                    foreach (var item in result.SubTasks)
                    {
                        var subResult = await _dataContext.SubTask.FirstOrDefaultAsync(or => or.TaskId == item.TaskId);


                        if (subResult != null)
                        {

                            //subResult.TaskId = item.TaskId;
                            subResult.TaskName = item.TaskName;
                            subResult.PlannedStartDate = item.PlannedStartDate;
                            subResult.PlannedEndDate = item.PlannedEndDate;
                            subResult.ActualStartDate = item.ActualStartDate;
                            subResult.ActualEndDate = item.ActualEndDate;
                            subResult.ActualStartDate = item.ActualStartDate;
                            subResult.ActualEndDate = item.ActualEndDate;
                            subResult.Progress = item.Progress;
                            subResult.Predecessor = item.Predecessor;
                        }

                    }
                }


            }

            await _dataContext.SaveChangesAsync();

            return Ok(data); // Return 200 OK
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Try to find the main task (GanttData) with its subtasks
            var task = await _dataContext.GanttData
                .Include(s => s.SubTasks)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            // If the main task exists, delete it and its subtasks
            if (task != null)
            {
                if (task.SubTasks?.Any() == true)
                {
                    _dataContext.SubTask.RemoveRange(task.SubTasks);
                }

                _dataContext.GanttData.Remove(task);
            }
            else
            {
                // If not a main task, check if it's a subtask
                var subTask = await _dataContext.SubTask.FirstOrDefaultAsync(st => st.TaskId == id);
                if (subTask != null)
                {
                    _dataContext.SubTask.Remove(subTask);
                }
                else
                {
                    return NotFound($"Task or subtask with ID {id} not found.");
                }
            }

            await _dataContext.SaveChangesAsync();
            return NoContent(); // Return 204 No Content
        }

    }
}
