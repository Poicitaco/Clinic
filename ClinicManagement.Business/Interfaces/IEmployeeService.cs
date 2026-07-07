using System;
using System.Collections.Generic;
using ClinicManagement.Core;

namespace ClinicManagement.Business
{
    public interface IEmployeeService
    {
        Employee CreateEmployee(Employee employee);
        Employee UpdateEmployee(Employee employee);
        void TerminateContract(int employeeId, DateTime resignationDate);
        void ChangeLoginCredentials(int employeeId, string username, string newPassword, string confirmPassword);
        bool ChangePassword(int employeeId, string oldPassword, string newPassword);
        string RequestPasswordReset(string username);
        void ResetPassword(string token, string newPassword, string confirmPassword);
        Employee Login(string username, string password);
        List<Employee> GetAllEmployees();
        Employee GetEmployeeById(int id);
    }
}
