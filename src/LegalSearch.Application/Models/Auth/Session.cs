namespace LegalSearch.Application.Models.Auth
{
    public abstract record BaseSession(string UserId, string Name, string DisplayName, string PhoneNumber, string Email);

    public sealed record UserSession(string UserId, string Name, string DisplayName, string PhoneNumber, string Email,
        string Department, string BranchId, string Sol) : BaseSession(UserId, Name, DisplayName, PhoneNumber, Email);

    public sealed record StaffSession(string UserId, string Name, string DisplayName, string PhoneNumber, string Email,
        string Department, string ManagerName, string ManagerDepartment, string BranchId,
        string Groups, string Sol) : BaseSession(UserId, Name, DisplayName, PhoneNumber, Email);
}
