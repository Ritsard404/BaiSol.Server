using AuthLibrary.DTO;
using AutoMapper;
using DataLibrary.Models;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.DTO.Project;

namespace BaiSol.Server.Helper
{
    public class MappingProfiles:Profile
    {
        public MappingProfiles()
        {
            CreateMap<Installer, InstallerDto>().ReverseMap();
            CreateMap<Material, MaterialDTO>().ReverseMap();
            CreateMap<Project, ProjectDto>().ReverseMap();
        }
    }
}
