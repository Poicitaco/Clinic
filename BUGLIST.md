# DANH SÁCH LỖI (BUG LIST) - CLINIC MANAGEMENT

Danh sách này thống kê toàn bộ các lỗi đã biết, từ nghiêm trọng đến nhỏ nhặt, trong hệ thống quản lý phòng khám tính đến thời điểm hiện tại. **KHÔNG** phát triển thêm tính năng mới cho đến khi danh sách này được duyệt và xử lý xong.

---

### 1. Phân quyền hiển thị Menu giao diện (UI Role Visualization)
- **Mức độ:** High
- **File/Class:** `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Nguyên nhân gốc:** Dù phía xử lý ngầm (Backend/Service) đã chặn quyền (gọi thao tác sẽ văng lỗi Unauthorized), nhưng phía Giao diện (UI) vẫn hiển thị toàn bộ menu như "Quản lý nhân sự", "Bảng lương", "Cấu hình lương" cho Nha sĩ và Lễ tân. Người dùng có thể bấm vào và gặp thông báo lỗi, gây trải nghiệm rất tệ.
- **Cách sửa dự kiến:** Ẩn (Collapse) các menu này trong `MainWindow.xaml.cs` ngay khi Load giao diện nếu `UserContext.CurrentUser.Role != EmployeeRole.Manager`.

### 2. Thiếu chặn trùng lịch hẹn của Nha sĩ (Overlapping Appointments)
- **Mức độ:** High
- **File/Class:** `AppointmentService.cs` (Hàm `AddAppointment`, `UpdateAppointment`)
- **Nguyên nhân gốc:** Hiện tại hệ thống chỉ kiểm tra thời gian Kết thúc phải lớn hơn thời gian Bắt đầu, nhưng KHÔNG kiểm tra xem Nha sĩ (Dentist) đó đã có lịch hẹn trùng giờ hay chưa.
- **Cách sửa dự kiến:** Thêm truy vấn (Query) trong DB để đếm số lượng lịch hẹn của `DentistId` mà khoảng thời gian `(StartTime, EndTime)` bị giao cắt (overlap) với lịch mới.

### 3. Logic nhân sự thôi việc trong tương lai chưa tự động khóa tài khoản
- **Mức độ:** Medium
- **File/Class:** `EmployeeService.cs` (Hàm `TerminateContract` và `Login`)
- **Nguyên nhân gốc:** Khi Quản lý set ngày thôi việc (`ResignationDate`) ở tương lai, tài khoản chưa bị khóa ngay. Tuy nhiên, khi ngày đó đến, hệ thống không có "job" tự động chạy để chuyển trạng thái `Account.IsActive = false`. Nha sĩ đã nghỉ việc vẫn có thể đăng nhập.
- **Cách sửa dự kiến:** Sửa hàm `Login` trong `EmployeeService`: Khi đăng nhập, nếu nhân viên có `ResignationDate.HasValue` và `<= DateTime.Now`, lập tức từ chối đăng nhập và báo "Nhân viên đã nghỉ việc".

### 4. Bất đồng bộ Cache User đang đăng nhập
- **Mức độ:** Low
- **File/Class:** `LoginViewModel.cs`, `UserProfileViewModel.cs`
- **Nguyên nhân gốc:** Đang sử dụng song song 2 cơ chế để lưu User hiện tại: `UserContext.CurrentUser` (tĩnh) và biến Dictionary `Application.Current.Properties["CurrentUser"]`. Việc này dễ gây rác bộ nhớ và không đồng nhất trạng thái nếu 1 trong 2 quên không update.
- **Cách sửa dự kiến:** Xóa bỏ toàn bộ việc gán và đọc biến từ `Application.Current.Properties`, chỉ sử dụng nguồn duy nhất (Single Source of Truth) là `UserContext.CurrentUser`.

---

### CÁC LỖI ĐÃ ĐƯỢC FIX (Chờ test xác nhận):

### 5. Lỗi chốt lương (Entity Framework Conflicting Changes)
- **Mức độ:** Critical
- **Trạng thái:** Đã fix code
- **File/Class:** `SalaryService.cs`
- **Nguyên nhân gốc:** Lỗi khi tạo `SalaryFormulaSnapshot`. Entity Framework bắt buộc thực thể phụ (Dependent) trong quan hệ 1-1 phải có Khóa chính bằng Khóa ngoại. Khi tạo snapshot bằng lệnh `Add()` rời rạc mà thiếu gán Id, EF cấp Id=0 cho tất cả, gây ra lỗi trùng Key.
- **Cách sửa:** Đã sửa bằng cách gán `Id = record.Id` và móc trực tiếp qua Navigation Property (`record.FormulaSnapshot = snapshot;`).

### 6. Cửa sổ thêm nhân viên che khuất ô chọn Học vị
- **Mức độ:** High
- **Trạng thái:** Đã fix code
- **File/Class:** `EmployeeFormWindow.xaml`
- **Nguyên nhân gốc:** Cửa sổ thiết lập cứng `Height="550"`, không đủ chỗ chứa toàn bộ các control nên đẩy ô Học vị co lại kích thước 0px, khiến người dùng không nhìn thấy để chọn và liên tục bị văng lỗi Validation.
- **Cách sửa:** Đã đổi thành `SizeToContent="Height"`.

### 7. Giao diện báo nhầm Role lúc đăng nhập ("Quản trị viên")
- **Mức độ:** Medium
- **Trạng thái:** Đã fix code
- **File/Class:** `MainWindow.xaml`, `MainWindow.xaml.cs`
- **Nguyên nhân gốc:** Label thông tin cá nhân dưới cùng bên trái bị code cứng bằng text "Quản trị viên" thay vì lấy từ dữ liệu thật.
- **Cách sửa:** Đã gán động `UserContext.CurrentUser.Role` trong `MainWindow()`.

---
*Vui lòng phản hồi xem bạn muốn ưu tiên xử lý lỗi số 1, 2, 3 hay 4 trước. Hoặc nếu bạn phát hiện lỗi mới, hãy cho tôi biết để tôi bổ sung vào BUGLIST.*
