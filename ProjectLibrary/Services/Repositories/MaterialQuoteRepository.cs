
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace ProjectLibrary.Services.Repositories
{
    public class MaterialQuoteRepository(DataContext _dataContext) : IMaterialQuote
    {
        public async Task<string> AddNewMaterialSupply(MaterialQuoteDto materialQuoteDto)
        {
            var clientProject = await _dataContext.Project
                .FirstOrDefaultAsync(proj => proj.ProjId == materialQuoteDto.ProjId);


            //if (clientProject == null)
            //{
            //    return "Project not found.";
            //}

            var projectMaterial = await _dataContext.Material
                .FirstOrDefaultAsync(m => m.MTLId == materialQuoteDto.MTLId);


            //if (projectMaterial == null)
            //{
            //    return "Material not found.";
            //}

            var newSupply = new Supply
            {
                MTLQuantity = materialQuoteDto.MTLQuantity,
                Material = projectMaterial,
                Project = clientProject

            };
            // Add the new Supply entity to the context
            _dataContext.Supply.Add(newSupply);

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";

        }


        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }
    }
}
