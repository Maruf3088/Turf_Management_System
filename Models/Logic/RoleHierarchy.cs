using System.Collections.Generic;
using System.Linq;

namespace turf_management_system.Models.Logic
{
    public static class RoleHierarchy
    {
        // Level 0: Platform Admins
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
        public const string SupportAdmin = "SupportAdmin";
        public const string FinanceAdmin = "FinanceAdmin";
        public const string OperationsAdmin = "OperationsAdmin";

        // Level 1: Property Owners
        public const string TurfOwner = "TurfOwner";

        // Level 2: Turf Management
        public const string TurfManager = "TurfManager";

        // Level 3: Turf Staff
        public const string Receptionist = "Receptionist";
        public const string Groundskeeper = "Groundskeeper";
        public const string Cashier = "Cashier";
        public const string SecurityGuard = "SecurityGuard";

        // Level 4: Customer
        public const string User = "User";

        public static List<string> GetCreatableRoles(string currentRole)
        {
            return currentRole switch
            {
                SuperAdmin => new List<string> { Admin, SupportAdmin, FinanceAdmin, OperationsAdmin, TurfOwner, User },
                Admin => new List<string> { TurfOwner, User },
                TurfOwner => new List<string> { TurfManager, Receptionist, Groundskeeper, Cashier, SecurityGuard },
                TurfManager => new List<string> { Receptionist, Groundskeeper, Cashier, SecurityGuard },
                _ => new List<string>() // Others cannot create any roles
            };
        }

        public static int GetRoleLevel(string roleName)
        {
            return roleName switch
            {
                SuperAdmin => 0,
                Admin or SupportAdmin or FinanceAdmin or OperationsAdmin => 1,
                TurfOwner => 2,
                TurfManager => 3,
                Receptionist or Groundskeeper or Cashier or SecurityGuard => 4,
                User => 5,
                _ => 100
            };
        }

        public static bool CanCreate(string creatorRole, string targetRole)
        {
            var allowed = GetCreatableRoles(creatorRole);
            return allowed.Contains(targetRole);
        }

        public static bool CanManage(string currentRole, string targetRole)
        {
            // Level 0 can manage everyone except themselves (if logic requires)
            // Generally, a higher level (smaller number) can manage lower levels (larger numbers)
            return GetRoleLevel(currentRole) < GetRoleLevel(targetRole);
        }
    }
}
