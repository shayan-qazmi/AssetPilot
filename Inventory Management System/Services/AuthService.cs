// Services/AuthService.cs
using Inventory_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Inventory_Management_System.Services
{
    /// <summary>
    /// Tracks who is currently logged in and what role they hold for the
    /// duration of the app session. Set once at login, read everywhere else
    /// that needs to make a permission decision.
    /// </summary>
    public static class AuthService
    {
        public const string RoleAdmin    = "Admin";
        public const string RoleOperator = "Operator";

        public static int CurrentUserId { get; private set; }
        public static string CurrentUsername { get; private set; } = string.Empty;
        public static string CurrentFullName { get; private set; } = string.Empty;
        public static string CurrentRole { get; private set; } = RoleOperator; // safe default = least privilege

        public static bool IsAdmin => string.Equals(CurrentRole, RoleAdmin, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Call this immediately after a successful login. Looks up the
        /// user's role via the UserRole junction table. If no role row
        /// exists yet (e.g. roles table is empty/unseeded), the very first
        /// user ever created (lowest Id) is treated as Admin so there is
        /// always at least one way into the Settings/Decommission screens —
        /// every other user defaults safely to Operator.
        /// </summary>
        public static void SetCurrentUser(User user)
        {
            CurrentUserId   = user.Id;
            CurrentUsername = user.Username;
            CurrentFullName = user.FullName;
            CurrentRole     = RoleOperator;

            try
            {
                using var db = new InventoryDbContext();

                var roleName = db.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == user.Id)
                    .Select(ur => ur.Role.RoleName)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(roleName))
                {
                    CurrentRole = roleName;
                    return;
                }

                // No role assignment found at all (common if the roles /
                // user_roles tables are empty). Fall back: the very first
                // user account in the system is granted Admin so the app
                // remains usable until roles are seeded properly.
                bool anyRoleRowsExist = db.UserRoles.Any();
                if (!anyRoleRowsExist)
                {
                    int? firstUserId = db.Users.OrderBy(u => u.Id).Select(u => u.Id).FirstOrDefault();
                    if (firstUserId.HasValue && firstUserId.Value == user.Id)
                        CurrentRole = RoleAdmin;
                }
            }
            catch
            {
                // DB/lookup failure — stay on the safe default (Operator)
                // rather than risk granting Admin access.
                CurrentRole = RoleOperator;
            }
        }

        public static void ClearCurrentUser()
        {
            CurrentUserId   = 0;
            CurrentUsername = string.Empty;
            CurrentFullName = string.Empty;
            CurrentRole     = RoleOperator;
        }
    }
}