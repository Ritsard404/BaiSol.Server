using AuthLibrary.DTO;
using AutoMapper;
using DataLibrary.Models;

namespace BaiSol.Server.Helper
{
    public class MappingProfiles:Profile
    {
        public MappingProfiles()
        {
            CreateMap<Installer, InstallerDto>().ReverseMap();
        }
    }
}
