using System;
using System.Windows;
using ClinicManagement.Core;

namespace ClinicManagement.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            if (UserContext.CurrentUser != null)
            {
                txtAvatarLetter.Text = !string.IsNullOrEmpty(UserContext.CurrentUser.FullName) 
                                       ? UserContext.CurrentUser.FullName.Substring(0, 1).ToUpper() 
                                       : "U";
                txtUserEmail.Text = UserContext.CurrentUser.Email;
                
                switch (UserContext.CurrentUser.Role)
                {
                    case EmployeeRole.Manager: txtUserRole.Text = "Quản lý"; break;
                    case EmployeeRole.Dentist: txtUserRole.Text = "Nha sĩ"; break;
                    case EmployeeRole.Receptionist: txtUserRole.Text = "Lễ tân"; break;
                    default: txtUserRole.Text = UserContext.CurrentUser.Role.ToString(); break;
                }

                ApplyRoleMenuVisibility(UserContext.CurrentUser.Role);
            }

            if (UserContext.CurrentUser != null && UserContext.CurrentUser.Role == EmployeeRole.Manager)
            {
                NavEmployee_Click(null, null); // Load mặc định trang nhân sự cho Quản lý
            }
            else if (UserContext.CurrentUser != null && UserContext.CurrentUser.Role == EmployeeRole.Dentist)
            {
                NavRecord_Click(null, null);
            }
            else if (UserContext.CurrentUser != null && UserContext.CurrentUser.Role == EmployeeRole.Receptionist)
            {
                NavAppointment_Click(null, null);
            }
            else
            {
                NavProfile_Click(null, null);
            }
        }

        private void ApplyRoleMenuVisibility(EmployeeRole role)
        {
            btnProfile.Visibility = role == EmployeeRole.Dentist || role == EmployeeRole.Receptionist
                ? Visibility.Visible
                : Visibility.Collapsed;
            btnMySchedule.Visibility = role == EmployeeRole.Dentist || role == EmployeeRole.Receptionist
                ? Visibility.Visible
                : Visibility.Collapsed;
            btnMySalary.Visibility = role == EmployeeRole.Dentist || role == EmployeeRole.Receptionist
                ? Visibility.Visible
                : Visibility.Collapsed;

            btnDashboard.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            btnEmployee.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            btnWorkShift.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            btnSchedule.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            btnSalaryConfig.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            btnSalaryList.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;
            btnService.Visibility = role == EmployeeRole.Manager ? Visibility.Visible : Visibility.Collapsed;

            btnAppointment.Visibility = role == EmployeeRole.Manager || role == EmployeeRole.Receptionist
                ? Visibility.Visible
                : Visibility.Collapsed;
            btnRecord.Visibility = role == EmployeeRole.Manager || role == EmployeeRole.Dentist
                ? Visibility.Visible
                : Visibility.Collapsed;
            btnInvoice.Visibility = role == EmployeeRole.Manager || role == EmployeeRole.Receptionist
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void NavEmployee_Click(object sender, RoutedEventArgs e)
        {
            var context = new DataAccess.ClinicDbContext();
            var unitOfWork = new DataAccess.UnitOfWork.UnitOfWork(context);
            var employeeService = new Business.EmployeeService(unitOfWork);
            
            var employeeListViewModel = new ViewModels.EmployeeListViewModel(employeeService);
            employeeListViewModel.ShowFormDialog = (formVm) =>
            {
                var window = new Views.EmployeeFormWindow
                {
                    DataContext = formVm,
                    Owner = this
                };
                window.ShowDialog();
            };

            MainContent.Content = new Views.EmployeeListView
            {
                DataContext = employeeListViewModel
            };
            employeeListViewModel.LoadData();
        }

        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.DashboardView
            {
                DataContext = new ViewModels.DashboardViewModel()
            };
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            UserContext.CheckRole(EmployeeRole.Dentist, EmployeeRole.Receptionist);
            MainContent.Content = new Views.UserProfileView
            {
                DataContext = new ViewModels.UserProfileViewModel()
            };
        }

        private void NavMySchedule_Click(object sender, RoutedEventArgs e)
        {
            UserContext.CheckRole(EmployeeRole.Dentist, EmployeeRole.Receptionist);
            MainContent.Content = new Views.PersonalScheduleView
            {
                DataContext = new ViewModels.PersonalScheduleViewModel()
            };
        }

        private void NavMySalary_Click(object sender, RoutedEventArgs e)
        {
            UserContext.CheckRole(EmployeeRole.Dentist, EmployeeRole.Receptionist);
            MainContent.Content = new Views.PersonalSalaryView
            {
                DataContext = new ViewModels.PersonalSalaryViewModel()
            };
        }

        private void NavWorkShift_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.WorkShiftListView
            {
                DataContext = new ViewModels.WorkShiftListViewModel()
            };
        }

        private void NavSchedule_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.ScheduleView
            {
                DataContext = new ViewModels.ScheduleViewModel()
            };
        }

        private void NavSalaryConfig_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.SalaryConfigView
            {
                DataContext = new ViewModels.SalaryConfigViewModel()
            };
        }

        private void NavSalaryList_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.SalaryListView
            {
                DataContext = new ViewModels.SalaryListViewModel()
            };
        }

        private void NavService_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Views.ServiceListView
            {
                DataContext = new ViewModels.ServiceListViewModel()
            };
        }

        private void NavAppointment_Click(object sender, RoutedEventArgs e)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);
            MainContent.Content = new Views.AppointmentListView
            {
                DataContext = new ViewModels.AppointmentListViewModel()
            };
        }

        private void NavRecord_Click(object sender, RoutedEventArgs e)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Dentist);
            MainContent.Content = new Views.RecordListView
            {
                DataContext = new ViewModels.RecordListViewModel()
            };
        }

        private void NavInvoice_Click(object sender, RoutedEventArgs e)
        {
            UserContext.CheckRole(EmployeeRole.Manager, EmployeeRole.Receptionist);
            MainContent.Content = new Views.InvoiceListView
            {
                DataContext = new ViewModels.InvoiceListViewModel()
            };
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            UserContext.CurrentUser = null;

            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
