namespace LegalSearch.Domain.Entities.User.CustomerServiceOfficer
{
    public class CustomerServiceOfficer : User
    {
        public string ManagerName { get; set; }
        public string ManagerDepartment { get; set; }
        public string Department { get; set; }
        public string StaffId { get; init; }
        public string BranchId { get; set; }
        public string Sol { get; set; }


        // Role properties
        public Guid? RoleId { get; set; }
        public Role.Role Role { get; set; }
    }
}
