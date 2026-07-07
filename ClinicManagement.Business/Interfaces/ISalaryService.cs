using System.Collections.Generic;
using ClinicManagement.Core;

namespace ClinicManagement.Business.Interfaces
{
    public interface ISalaryService
    {
        List<SalaryConfiguration> GetAllConfigurations();
        void UpdateConfiguration(SalaryConfiguration config);
        
        List<SalaryRecord> CalculatePayroll(int month, int year);
        void FinalizePayroll(int month, int year);
        List<SalaryRecord> GetSalaryRecords(int month, int year);
    }
}
