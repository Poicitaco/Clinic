using System;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.Business.Interfaces;
using ClinicManagement.Business.Services;
using ClinicManagement.Core;
using ClinicManagement.DataAccess;
using ClinicManagement.DataAccess.UnitOfWork;
using ClinicManagement.UI.Utilities;

namespace ClinicManagement.UI.ViewModels
{
    public class WorkShiftFormViewModel : ViewModelBase
    {
        private readonly IWorkShiftService _workShiftService;
        private readonly WorkShift _workShift;
        private readonly bool _isEditMode;

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private DateTime? _startTime;
        public DateTime? StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(nameof(StartTime)); }
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(nameof(EndTime)); }
        }

        public string Title => _isEditMode ? "CẬP NHẬT CA LÀM VIỆC" : "THÊM CA LÀM VIỆC MỚI";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public Action<bool> CloseAction { get; set; }

        public WorkShiftFormViewModel(WorkShift workShift = null)
        {
            var unitOfWork = new UnitOfWork(new ClinicDbContext());
            _workShiftService = new WorkShiftService(unitOfWork);

            _isEditMode = workShift != null;
            _workShift = workShift ?? new WorkShift();

            if (_isEditMode)
            {
                Name = _workShift.Name;
                StartTime = DateTime.Today.Add(_workShift.StartTime);
                EndTime = DateTime.Today.Add(_workShift.EndTime);
            }
            else
            {
                StartTime = DateTime.Today.Add(new TimeSpan(8, 0, 0));
                EndTime = DateTime.Today.Add(new TimeSpan(12, 0, 0));
            }

            SaveCommand = new RelayCommand(param => Save());
            CancelCommand = new RelayCommand(param => Cancel());
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("Vui lòng nhập tên ca làm việc.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _workShift.Name = Name;
                _workShift.StartTime = StartTime?.TimeOfDay ?? new TimeSpan(8, 0, 0);
                _workShift.EndTime = EndTime?.TimeOfDay ?? new TimeSpan(12, 0, 0);

                if (_isEditMode)
                {
                    _workShiftService.UpdateWorkShift(_workShift);
                }
                else
                {
                    _workShiftService.AddWorkShift(_workShift);
                }

                MessageBox.Show("Lưu thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }
    }
}
