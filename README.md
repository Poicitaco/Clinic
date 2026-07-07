# ClinicManagement

Hướng dẫn thiết lập và chạy project trên Visual Studio.

## Yêu cầu

- Visual Studio có workload `.NET desktop development`
- .NET Framework 4.7.2 Developer Pack
- MySQL Server đang chạy ở cổng `3306`

## Thiết lập database

Có 2 cách dùng database.

### Cách 1: Không dùng database mẫu

Dùng cách này nếu muốn database sạch và để ứng dụng tự tạo dữ liệu demo ban đầu.

1. Tạo database:

```sql
CREATE DATABASE clinicmanagement CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. Chạy ứng dụng một lần trong Visual Studio.

Project dùng Entity Framework Migration nên khi app khởi động, hệ thống sẽ tự tạo bảng và seed dữ liệu demo cơ bản.

Tài khoản demo được tạo tự động:

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Quản lý | `admin@clinic.com` | `Manager@123` |
| Nha sĩ | `dentist@clinic.com` | `Manager@123` |
| Nha sĩ | `dentist2@clinic.com` | `Manager@123` |
| Nha sĩ | `dentist3@clinic.com` | `Manager@123` |
| Nha sĩ | `dentist4@clinic.com` | `Manager@123` |
| Lễ tân | `receptionist@clinic.com` | `Manager@123` |

Seed tự động cũng tạo sẵn nhân viên, dịch vụ, ca làm việc, lịch làm việc, bệnh nhân, lịch hẹn, bệnh án và hóa đơn demo.

### Cách 2: Dùng database mẫu có sẵn

Dùng cách này nếu muốn import nhanh bộ dữ liệu mẫu đã export sẵn.

1. Tạo database:

```sql
CREATE DATABASE clinicmanagement CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. Import database mẫu sạch:

```powershell
mysql -u root -p clinicmanagement < db_backups\clinicmanagement_sample_20260707_093813.sql
```

Nếu muốn dùng bản backup cũ hơn:

```powershell
mysql -u root -p clinicmanagement < db_backups\clinicmanagement_before_c3_dataset_20260706_062028.sql
```

## Cấu hình kết nối database

Mở file:

```text
ClinicManagement.UI\App.config
```

Kiểm tra connection string:

```xml
server=localhost;port=3306;database=clinicmanagement;uid=root;password=Itentad@1;
```

Nếu MySQL trên máy bạn dùng mật khẩu khác, sửa lại phần `password=...`.

## Chạy trên Visual Studio

1. Mở `ClinicManagement.slnx` bằng Visual Studio.
2. Set `ClinicManagement.UI` làm Startup Project.
3. Chọn cấu hình `Debug`.
4. Build solution.
5. Bấm `Start` để chạy ứng dụng.

Không dùng `dotnet build` cho project WPF .NET Framework này.

Lệnh MSBuild đúng nếu cần build bằng terminal:

```powershell
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ClinicManagement.UI\ClinicManagement.UI.csproj /t:Rebuild /p:Configuration=Debug
```
