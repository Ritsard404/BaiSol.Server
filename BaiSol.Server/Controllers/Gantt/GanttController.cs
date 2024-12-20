﻿using BaseLibrary.DTO.Gantt;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Project;
using System.Text.Json.Serialization;

namespace BaiSol.Server.Controllers.Gantt
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class GanttController(DataContext _dataContext, IGanttRepository _gantt) : ControllerBase
    {
        private class GanttResponse<T>
        {
            [JsonPropertyName("Items")]
            public T Items { get; set; }

            [JsonPropertyName("Count")]
            public int Count { get; set; }
        }
        public class Indicator
        {
            [JsonPropertyName("date")]
            public string? Date { get; set; }

            [JsonPropertyName("iconClass")]
            //public string? IconClass { get; set; } = "e-btn-icon e-notes-info e-icons e-icon-left e-gantt e-notes-info::before";
            public string? IconClass { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("tooltip")]
            public string? Tooltip { get; set; }
        }


        private class Gantt
        {
            [JsonPropertyName("TaskId")]
            public int TaskId { get; set; }

            [JsonPropertyName("TaskName")]
            public string? TaskName { get; set; }

            [JsonPropertyName("PlannedStartDate")]
            public DateTime? PlannedStartDate { get; set; }

            [JsonPropertyName("PlannedEndDate")]
            public DateTime? PlannedEndDate { get; set; }

            [JsonPropertyName("ActualStartDate")]
            public DateTime? ActualStartDate { get; set; }

            [JsonPropertyName("ActualEndDate")]
            public DateTime? ActualEndDate { get; set; }

            [JsonPropertyName("Progress")]
            public int? Progress { get; set; }

            [JsonPropertyName("Duration")]
            public int? Duration { get; set; }

            [JsonPropertyName("Predecessor")]
            public string? Predecessor { get; set; }

            [JsonPropertyName("ParentId")]
            public int? ParentId { get; set; }

            [JsonPropertyName("Indicators")]
            public List<Indicator>? Indicators { get; set; }

        }

        [HttpGet("{projId}")]
        public async Task<IActionResult> Get(string projId)
        {
            var tasks = await _dataContext.GanttData
                .Where(p => p.ProjId == projId)
                .ToListAsync();

            var mappedData = new List<Gantt>(); // Initialize mappedData outside the loop

            foreach (var task in tasks)
            {
                var taskProofs = await _dataContext.TaskProof
                    .Where(i => i.Task == task)
                    .ToListAsync();

                var indicators = new List<Indicator>();

                if (taskProofs.Any())
                {
                    foreach (var proof in taskProofs)
                    {
                        var indicator = new Indicator
                        {
                            Date = proof.ActualStart?.ToString("MM/dd/yyyy"),
                            Name = $"<span style=\"color:black; font-size: 10px; font-weight: 600; position: relative; z-index: 10; background-color: rgb(255, 255, 224);\">{proof.TaskProgress?.ToString()}%</span>",
                            Tooltip = proof.ActualStart?.ToString("MMMM dd, yyyy")
                        };

                        indicators.Add(indicator);
                    }
                }

                // Add mapped task with indicators to the list
                mappedData.Add(new Gantt
                {
                    TaskId = task.TaskId,
                    TaskName = task.TaskName,
                    PlannedStartDate = task.PlannedStartDate,
                    PlannedEndDate = task.PlannedEndDate,
                    ActualStartDate = task.ActualStartDate,
                    ActualEndDate = task.ActualEndDate,
                    Progress = task.Progress,
                    Duration = task.Duration,
                    Predecessor = task.Predecessor,
                    ParentId = task.ParentId,
                    Indicators = indicators // Add indicators list here
                });
            }

            // Now mappedData contains all the tasks with their indicators
            mappedData = mappedData
                .OrderBy(t => t.PlannedStartDate)
                .ThenBy(i => i.TaskId)
                .ToList();

            // Prepare response
            var response = new GanttResponse<List<Gantt>>
            {
                Items = mappedData, // Use mappedData with indicators
                Count = mappedData.Count
            };

            // Return the list as a JSON response
            return Ok(response);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> FacilitatorTasks(string projId)
        {
            var tasks = await _gantt.FacilitatorTasks(projId);

            return Ok(tasks);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> TasksToDo(string projId)
        {
            var tasks = await _gantt.TasksToDo(projId);

            return Ok(tasks);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> TaskToDo(string projId)
        {
            var tasks = await _gantt.TaskToDo(projId);

            return Ok(tasks);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> TaskToUpdateProgress(string projId)
        {
            var tasks = await _gantt.TaskToUpdateProgress(projId);

            return Ok(tasks);
        }

        [HttpPost("{projId}")]
        public async Task<IActionResult> Post([FromBody] GanttData[] data, string projId)
        {
            // Set the projId for each Gantt object
            foreach (var gantt in data)
            {
                gantt.ProjId = projId; // Assign projId to each Gantt item
            }

            //// Get existing TaskIds from the database
            //var existingIds = _dataContext.GanttData
            //    .Where(g => data.Select(d => d.TaskId).Contains(g.TaskId))
            //    .Select(g => g.TaskId)
            //    .ToHashSet(); // Use HashSet for faster lookups

            //// Generate unique TaskIds if they already exist
            //foreach (var gantt in data)
            //{
            //    while (existingIds.Contains(gantt.TaskId))
            //    {
            //        gantt.TaskId++; // Increment to find a unique TaskId
            //    }
            //    existingIds.Add(gantt.TaskId); // Add the new TaskId to the set
            //}


            await _dataContext.GanttData.AddRangeAsync(data);
            await _dataContext.SaveChangesAsync();
            return Ok(data);
        }

        [HttpPut("{projId}")]
        public async Task<IActionResult> Put([FromBody] GanttData[] value, string projId)
        {
            if (value == null || value.Length == 0)
            {
                return BadRequest("Data is null or empty.");
            }

            foreach (var updatedData in value)
            {
                var existingData = await _dataContext.GanttData
                    .FirstOrDefaultAsync(i => i.TaskId == updatedData.TaskId && i.ProjId == projId);

                var dateLimit = await _gantt.ProjectDateInfo(projId);

                if (existingData == null)
                {
                    return NotFound($"GanttData with TaskId {updatedData.TaskId} not found.");
                }

                if (updatedData.PlannedStartDate.HasValue && updatedData.PlannedStartDate.Value.Date < dateLimit.StartOffsetDate.Value.Date)
                {
                    return BadRequest("Planned start date is earlier than the allowed date range.");
                }

                if (updatedData.PlannedEndDate.HasValue && updatedData.PlannedEndDate.Value.Date > dateLimit.EndOffsetDate.Value.Date)
                {
                    return BadRequest("Planned end date is later than the allowed date range.");
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

        [HttpDelete("{projId}/{id:int}")]
        public async Task<IActionResult> Delete(int id, string projId)
        {
            // Find the existing record by TaskId
            var existingData = await _dataContext.GanttData
                    .FirstOrDefaultAsync(i => i.TaskId == id && i.ProjId == projId);

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

        [HttpPost("[action]")]
        public async Task<IActionResult> FinishTask(UploadTaskDTO finishTask)
        {
            var (isSuccess, Message) = await _gantt.HandleTask(finishTask, false);
            if (!isSuccess)
                return BadRequest(Message);

            return Ok(Message);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> StartTask(UploadTaskDTO finishTask)
        {
            var (isSuccess, Message) = await _gantt.HandleTask(finishTask, true);
            if (!isSuccess)
                return BadRequest(Message);

            return Ok(Message);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> SubmitTaskReport(UploadTaskDTO finishTask)
        {
            var (isSuccess, Message) = await _gantt.SubmitTaskReport(finishTask);
            if (!isSuccess)
                return BadRequest(Message);

            return Ok(Message);
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> UpdateTaskProgress(UpdateTaskProgress updateTaskProgress)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            updateTaskProgress.ipAddress = ipAddress;

            var (isSuccess, Message) = await _gantt.UpdateTaskProgress(updateTaskProgress);
            if (!isSuccess)
                return BadRequest(Message);

            return Ok(Message);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> TaskById(int id)
        {
            var task = await _gantt.TaskById(id);

            return Ok(task);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectDateInfo(string projId)
        {
            var task = await _gantt.ProjectDateInfo(projId);

            return Ok(task);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectProgress(string projId)
        {
            var task = await _gantt.ProjectProgress(projId);

            return Ok(task);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectStatus(string projId)
        {
            var task = await _gantt.ProjectStatus(projId);

            return Ok(task);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProjectDates(string projId)
        {
            var task = await _gantt.GetProjectDates(projId);

            return Ok(task.StartDate);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectActualWorkedDate(string projId)
        {
            var task = await _gantt.ProjectActualWorkedDate(projId);

            return Ok(task);
        }
    }
}
