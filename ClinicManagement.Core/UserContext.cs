using System;

namespace ClinicManagement.Core
{
    public static class UserContext
    {
        public static Employee CurrentUser { get; set; }

        public static void CheckRole(params EmployeeRole[] allowedRoles)
        {
            if (CurrentUser == null)
            {
                throw new UnauthorizedAccessException("Bạn chưa đăng nhập.");
            }

            bool isAllowed = false;
            foreach(var role in allowedRoles)
            {
                if (CurrentUser.Role == role)
                {
                    isAllowed = true;
                    break;
                }
            }

            if (!isAllowed)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này.");
            }
        }
    }
}
