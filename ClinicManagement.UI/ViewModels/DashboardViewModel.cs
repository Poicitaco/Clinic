using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.UI.Utilities;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Business.Services;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using LiveCharts;
using LiveCharts.Wpf;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace ClinicManagement.UI.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IStatisticService _statisticService;
        private readonly UnitOfWork _unitOfWork;

        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
                LoadStatistics();
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
                LoadStatistics();
            }
        }

        private string _selectedGroupBy = "Day";
        public string SelectedGroupBy
        {
            get => _selectedGroupBy;
            set
            {
                _selectedGroupBy = value;
                OnPropertyChanged(nameof(SelectedGroupBy));
                LoadStatistics();
            }
        }

        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged(nameof(TotalRevenue));
            }
        }

        private int _totalInvoices;
        public int TotalInvoices
        {
            get => _totalInvoices;
            set
            {
                _totalInvoices = value;
                OnPropertyChanged(nameof(TotalInvoices));
            }
        }

        // LiveCharts properties
        public SeriesCollection RevenueSeries { get; set; }
        public List<string> RevenueLabels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        public SeriesCollection CategorySeries { get; set; }

        public ObservableCollection<ServiceCategory> Categories { get; private set; }
        public ObservableCollection<Service> Services { get; private set; }
        public ObservableCollection<Employee> Dentists { get; private set; }

        private ServiceCategory _selectedCategory;
        public ServiceCategory SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); RefreshServiceFilter(); LoadStatistics(); }
        }

        private Service _selectedService;
        public Service SelectedService
        {
            get => _selectedService;
            set { _selectedService = value; OnPropertyChanged(nameof(SelectedService)); LoadStatistics(); }
        }

        private Employee _selectedDentist;
        public Employee SelectedDentist
        {
            get => _selectedDentist;
            set { _selectedDentist = value; OnPropertyChanged(nameof(SelectedDentist)); LoadStatistics(); }
        }

        public ICommand ExportExcelCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public DashboardViewModel()
        {
            _unitOfWork = new UnitOfWork(new ClinicDbContext());
            _statisticService = new StatisticService(_unitOfWork);

            RevenueSeries = new SeriesCollection();
            RevenueLabels = new List<string>();
            YFormatter = value => value.ToString("N0") + " đ";
            CategorySeries = new SeriesCollection();
            Categories = new ObservableCollection<ServiceCategory>(_unitOfWork.ServiceCategories.GetAll().OrderBy(c => c.Name));
            Services = new ObservableCollection<Service>();
            Dentists = new ObservableCollection<Employee>(_unitOfWork.Employees.Find(e => e.Role == EmployeeRole.Dentist).OrderBy(e => e.FullName));

            ExportExcelCommand = new RelayCommand(param => ExportToExcel());
            ClearFiltersCommand = new RelayCommand(param => ClearFilters());
            RefreshServiceFilter();

            LoadStatistics();
        }

        private void LoadStatistics()
        {
            if (StartDate > EndDate) return;

            var report = _statisticService.GetInvoiceReport(StartDate, EndDate, SelectedCategory?.Id, SelectedService?.Id, SelectedDentist?.Id);
            TotalRevenue = report.Sum(r => r.Revenue);
            TotalInvoices = report.Sum(r => r.InvoiceCount);

            var revenueData = SelectedGroupBy == "Month"
                ? report.GroupBy(r => DateTime.ParseExact(r.TimeLabel, "dd/MM/yyyy", null).ToString("MM/yyyy"))
                    .Select(g => new RevenueData { TimeLabel = g.Key, TotalRevenue = g.Sum(r => r.Revenue) })
                    .OrderBy(r => DateTime.ParseExact("01/" + r.TimeLabel, "dd/MM/yyyy", null))
                    .ToList()
                : report.GroupBy(r => r.TimeLabel)
                    .Select(g => new RevenueData { TimeLabel = g.Key, TotalRevenue = g.Sum(r => r.Revenue) })
                    .OrderBy(r => DateTime.ParseExact(r.TimeLabel, "dd/MM/yyyy", null))
                    .ToList();
            
            RevenueSeries.Clear();
            RevenueLabels.Clear();

            var values = new ChartValues<decimal>();
            foreach (var item in revenueData)
            {
                values.Add(item.TotalRevenue);
                RevenueLabels.Add(item.TimeLabel);
            }

            RevenueSeries.Add(new LineSeries
            {
                Title = "Doanh thu",
                Values = values
            });

            var categoryData = report
                .GroupBy(r => r.CategoryName)
                .Select(g => new CategoryRevenueData { CategoryName = g.Key, TotalRevenue = g.Sum(r => r.Revenue) })
                .ToList();
            CategorySeries.Clear();

            foreach (var item in categoryData)
            {
                CategorySeries.Add(new PieSeries
                {
                    Title = item.CategoryName,
                    Values = new ChartValues<decimal> { item.TotalRevenue },
                    DataLabels = true,
                    LabelPoint = chartPoint => $"{chartPoint.Y:N0} đ ({chartPoint.Participation:P})"
                });
            }
        }

        private void RefreshServiceFilter()
        {
            var services = _unitOfWork.Services.GetAll().AsQueryable();
            if (SelectedCategory != null)
                services = services.Where(s => s.CategoryId == SelectedCategory.Id);

            Services.Clear();
            foreach (var service in services.OrderBy(s => s.Name))
                Services.Add(service);

            if (SelectedService != null && !Services.Any(s => s.Id == SelectedService.Id))
                SelectedService = null;
        }

        private void ClearFilters()
        {
            SelectedCategory = null;
            SelectedService = null;
            SelectedDentist = null;
            LoadStatistics();
        }

        private void ExportToExcel()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}.xlsx",
                Title = "Xuất báo cáo Excel"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var data = _statisticService.GetInvoiceReport(StartDate, EndDate, SelectedCategory?.Id, SelectedService?.Id, SelectedDentist?.Id);

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Báo cáo");

                        // Headers
                        worksheet.Cell(1, 1).Value = "Ngày";
                        worksheet.Cell(1, 2).Value = "Danh mục";
                        worksheet.Cell(1, 3).Value = "Dịch vụ";
                        worksheet.Cell(1, 4).Value = "Nha sĩ";
                        worksheet.Cell(1, 5).Value = "Số lượng Hóa đơn";
                        worksheet.Cell(1, 6).Value = "Doanh thu (VND)";

                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                        // Data
                        int row = 2;
                        foreach (var item in data)
                        {
                            worksheet.Cell(row, 1).Value = item.TimeLabel;
                            worksheet.Cell(row, 2).Value = item.CategoryName;
                            worksheet.Cell(row, 3).Value = item.ServiceName;
                            worksheet.Cell(row, 4).Value = item.DentistName;
                            worksheet.Cell(row, 5).Value = item.InvoiceCount;
                            worksheet.Cell(row, 6).Value = item.Revenue;
                            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();
                        workbook.SaveAs(dialog.FileName);
                    }

                    MessageBox.Show("Xuất báo cáo thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
