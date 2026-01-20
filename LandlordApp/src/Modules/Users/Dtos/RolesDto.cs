namespace Lander.src.Modules.Users.Dtos
{
    public class RolesDto
    {
    }
    public class GetRolesDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public Guid CreatedByGuid { get; set; }
        public DateTime CeatedDate { get; set; }
        public Guid ModifiedByGuid { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
