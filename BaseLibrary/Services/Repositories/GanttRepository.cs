using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models.Gantt;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Repositories
{
    public class GanttRepository(DataContext _dataContext) : IGanttRepository
    {
        //public async Task BatchSaveAsync(ICRUDModel<GanttData> batchData)
        //{
        //    List<GanttData> uAdded = new List<GanttData>();
        //    List<GanttData> uChanged = new List<GanttData>();
        //    List<GanttData> uDeleted = new List<GanttData>();

        //    //if (batchData.added != null)
        //    //{
        //    //    await _dataContext.GanttData.AddRangeAsync(batchData.added);
        //    //}
        //    //if (batchData.changed != null)
        //    //{
        //    //    _dataContext.GanttData.UpdateRange(batchData.changed);
        //    //}
        //    //if (batchData.deleted != null)
        //    //{
        //    //    _dataContext.GanttData.RemoveRange(batchData.deleted);
        //    //}

        //    if (batchData.added != null && batchData.added.Count() > 0)
        //    {
        //        foreach (var rec in batchData.added)
        //        {
        //            // Await the asynchronous Create method
        //            var addedRecord = await Create(rec);
        //            uAdded.Add(addedRecord);  // Add the result to the uAdded list
        //        }
        //    }
        //    if (batchData.changed != null && batchData.changed.Count() > 0)
        //    {
        //        foreach (var rec in batchData.changed)
        //        {
        //            var changedRecord = await Edit(rec);
        //            uChanged.Add(changedRecord);  // Use uChanged here
        //        }
        //    }
        //    if (batchData.deleted != null && batchData.deleted.Count() > 0)
        //    {
        //        foreach (var rec in batchData.deleted)
        //        {
        //            var deletedRecord = await Delete(rec.TaskId);  // Fixed incorrect method call
        //            uDeleted.Add(deletedRecord);  // Use uDeleted here
        //        }
        //    }

        //    await _dataContext.SaveChangesAsync();
        //}

        //private async Task<GanttData> Create(GanttData value)
        //{
        //    _dataContext.GanttData.Add(value);
        //    await _dataContext.SaveChangesAsync();
        //    return value;
        //}

        //private async Task<GanttData> Delete(string value)
        //{
        //    var result = _dataContext.GanttData.Where(currentData => currentData.TaskId == value).FirstOrDefault();
        //    if (result != null)
        //    {
        //        _dataContext.GanttData.Remove(result);
        //        RemoveChildRecords(value);
        //        await _dataContext.SaveChangesAsync();
        //    }
        //    return result;
        //}

        //private async Task<GanttData> Edit(GanttData value)
        //{
        //    GanttData result = _dataContext.GanttData.Where(currentData => currentData.TaskId == value.TaskId).FirstOrDefault();

        //    if (result != null)
        //    {
        //        result.TaskId = value.TaskId;
        //        result.TaskName = value.TaskName;
        //        result.StartDate = value.StartDate;
        //        result.EndDate = value.EndDate;
        //        result.Duration = value.Duration;
        //        result.Progress = value.Progress;
        //        result.Predecessor = value.Predecessor;
        //        await _dataContext.SaveChangesAsync();
        //        return result;
        //    }
        //    else return null;
        //}

        public async Task<IEnumerable<GanttData>> GetGanttDataAsync()
        {
            return await _dataContext.GanttData.ToListAsync();
        }

        //private void RemoveChildRecords(string key)
        //{
        //    var childList = _dataContext.GanttData.Where(x => x.TaskId == key).ToList();
        //    foreach (var item in childList)
        //    {
        //        _dataContext.GanttData.Remove(item);
        //        RemoveChildRecords(item.TaskId);
        //    }
        //}

        //public async Task BatchUpdate(CRUDModel batchModel)
        //{
        //    if (batchModel.changed != null)
        //    {
        //        for (var i = 0; i < batchModel.changed.Count(); i++)
        //        {
        //            var value = batchModel.changed[i];
        //            GanttData result = await _dataContext.GanttData
        //                .Where(or => or.TaskId == value.TaskId)
        //                .FirstOrDefaultAsync();
        //            // Update the record fields using GanttData properties
        //            result.TaskId = value.TaskId;
        //            result.TaskName = value.TaskName;
        //            result.StartDate = value.StartDate;
        //            result.EndDate = value.EndDate;
        //            result.Duration = value.Duration;
        //            result.Progress = value.Progress;
        //            result.ParentID = value.ParentID;
        //            result.Predecessor = value.Predecessor;
        //        }
        //    }

        //    if (batchModel.deleted != null)
        //    {
        //        for (var i = 0; i < batchModel.deleted.Count(); i++)
        //        {
        //            _dataContext.GanttData.Remove(await _dataContext.GanttData.Where(or => or.TaskId.Equals(batchModel.deleted[i].TaskId)).FirstOrDefaultAsync());
        //            RemoveChilds(batchModel.deleted[i].TaskId);
        //        }

        //    }

        //    if (batchModel.added != null)
        //    {

        //        for (var i = 0; i < batchModel.added.Count(); i++)
        //        {
        //            _dataContext.GanttData.Add(batchModel.added[i]);
        //        }
        //    }
        //    await _dataContext.SaveChangesAsync();
        //}

        //public void RemoveChilds(int key)
        //{
        //    var childList = _dataContext.GanttData
        //        .Where(x => x.ParentID == key)
        //        .ToList();

        //    foreach (var item in childList)
        //    {
        //        _dataContext.Remove(item);
        //        RemoveChilds(item.TaskId);
        //    }
        //     _dataContext.SaveChanges();
        //}
    }
}
