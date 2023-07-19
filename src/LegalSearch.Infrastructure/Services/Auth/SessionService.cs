using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Services.Auth
{
    public class SessionService : ISessionService
    {
        
        private readonly ILogger<SessionService> logger;
        private readonly IHttpContextAccessor contextAccessor;
        // private readonly IDistributedCache cache;
        private readonly object claimsLocker = new();
        
        private UserSession userSession;
        private StaffSession staffSession;
        private Dictionary<string, Claim> claimsDictionary;
        
        private Dictionary<string, string> permissionDictionary;
        private static readonly object permissionLocker = new();
        
        
        public SessionService(ILogger<SessionService> logger, IHttpContextAccessor contextAccessor)
        {
            this.logger = logger;
            this.contextAccessor = contextAccessor;
            // this.cache = cache;
        }

        public UserSession? GetUserSession()
        {
            try
            {
                if (userSession is not null) return userSession;

                userSession = ProcessUserSession();
                if (userSession is not null)
                    logger.LogInformation("Resolved New Session For User {User}", userSession!.Name);

                return userSession;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to resolve session");
                return null;
            }
        }

        public StaffSession? GetStaffSession()
        {
            try
            {
                if (staffSession is not null) return staffSession;

                staffSession = ProcessStaffSession();
                if (staffSession is not null)
                    logger.LogInformation("Resolved New Session For Staff {User}", staffSession!.Name);

                return staffSession;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to resolve session");
                return null;
            }
        }

        private StaffSession? ProcessStaffSession()
        {
            if (contextAccessor.HttpContext is null || !contextAccessor.HttpContext.User.Claims.Any()) return null;
            if (claimsDictionary is null) InitClaimsDict();

            var userIdClaim = claimsDictionary[nameof(StaffSession.UserId)];
            var userId = userIdClaim.Value;
            if (string.IsNullOrEmpty(userId)) userId = string.Empty;

            var nameClaim = claimsDictionary[nameof(StaffSession.Name)];
            var name = nameClaim.Value;
            if (string.IsNullOrEmpty(name)) name = string.Empty;

            var displayNameClaim = claimsDictionary[nameof(StaffSession.DisplayName)];
            var displayName = displayNameClaim.Value;
            if (string.IsNullOrEmpty(displayName)) displayName = string.Empty;

            var phoneClaim = claimsDictionary[nameof(StaffSession.PhoneNumber)];
            var phoneNumber = phoneClaim.Value;
            if (string.IsNullOrEmpty(phoneNumber)) phoneNumber = string.Empty;

            var emailClaim = claimsDictionary![nameof(StaffSession.Email)];
            var email = emailClaim.Value;
            if (string.IsNullOrEmpty(email)) email = string.Empty;

            var departmentClaim = claimsDictionary[nameof(StaffSession.Department)];
            var department = departmentClaim.Value;
            if (string.IsNullOrEmpty(department)) department = string.Empty;

            var managerNameClaim = claimsDictionary[nameof(StaffSession.ManagerName)];
            var managerName = managerNameClaim.Value;
            if (string.IsNullOrEmpty(managerName)) managerName = string.Empty;

            var managerDepartmentClaim = claimsDictionary[nameof(StaffSession.ManagerDepartment)];
            var managerDepartment = managerDepartmentClaim.Value;
            if (string.IsNullOrEmpty(managerDepartment)) managerDepartment = string.Empty;

            var branchIdClaim = claimsDictionary[nameof(StaffSession.BranchId)];
            var branchId = branchIdClaim.Value;
            if (string.IsNullOrEmpty(branchId)) branchId = string.Empty;

            var groupClaim = claimsDictionary[nameof(StaffSession.Groups)];
            var group = groupClaim.Value;
            if (string.IsNullOrEmpty(group)) group = string.Empty;

            var solClaim = claimsDictionary[nameof(StaffSession.Sol)];
            var sol = solClaim.Value;
            if (string.IsNullOrEmpty(group)) sol = string.Empty;

            return new StaffSession(userId, name, displayName, phoneNumber, email, department, managerName, managerDepartment,
                branchId, group, sol);
        }

        private UserSession? ProcessUserSession()
        {
            if (contextAccessor.HttpContext is null || !contextAccessor.HttpContext.User.Claims.Any()) return null;
            if (claimsDictionary is null) InitClaimsDict();

            var userIdClaim = claimsDictionary[nameof(UserSession.UserId)];
            var userId = userIdClaim.Value;
            if (string.IsNullOrEmpty(userId)) userId = string.Empty;
            
            var nameClaim = claimsDictionary[nameof(UserSession.Name)];
            var name = nameClaim.Value;
            if (string.IsNullOrEmpty(name)) name = string.Empty;
            
            var displayNameClaim = claimsDictionary[nameof(UserSession.DisplayName)];
            var displayName = displayNameClaim.Value;
            if (string.IsNullOrEmpty(displayName)) displayName = string.Empty;
            
            var phoneClaim = claimsDictionary[nameof(UserSession.PhoneNumber)];
            var phoneNumber = phoneClaim.Value;
            if (string.IsNullOrEmpty(phoneNumber)) phoneNumber = string.Empty;
            
            var emailClaim = claimsDictionary![nameof(UserSession.Email)];
            var email = emailClaim.Value;
            if (string.IsNullOrEmpty(email)) email = string.Empty;

            var departmentClaim = claimsDictionary[nameof(UserSession.Department)];
            var department = departmentClaim.Value;
            if (string.IsNullOrEmpty(department)) department = string.Empty;

            var branchIdClaim = claimsDictionary[nameof(UserSession.BranchId)];
            var branchId = branchIdClaim.Value;
            if (string.IsNullOrEmpty(branchId)) branchId = string.Empty;

            var solClaim = claimsDictionary[nameof(UserSession.Sol)];
            var sol = solClaim.Value;
            if (string.IsNullOrEmpty(sol)) sol = string.Empty;

            return new UserSession(userId, name, displayName, phoneNumber, email, department, branchId, sol);
        }
     
        private void InitClaimsDict()
        {
            var claims = contextAccessor.HttpContext!.User.Claims;
            claimsDictionary = new Dictionary<string, Claim>();
            foreach (var claim in claims)
            {
                lock (claimsLocker)
                {
                    claimsDictionary[claim.Type] = claim;
                }
            }
        }

        public Task<bool> HasPermissionAsync(params string[] permissions)
        {
            throw new System.NotImplementedException();
        }
    }
}
