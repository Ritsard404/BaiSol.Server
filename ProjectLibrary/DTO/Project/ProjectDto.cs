namespace ProjectLibrary.DTO.Project
{
    public class ProjectDto
    {
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public required string ClientId { get; set; }
        public required string SystemType { get; set; }
        public required decimal kWCapacity { get; set; }
    }
}
