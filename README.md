# ClinicManagement

Huong dan setup va chay project tren Visual Studio.

## Yeu cau

- Visual Studio co workload `.NET desktop development`
- .NET Framework 4.7.2 Developer Pack
- MySQL Server dang chay o port `3306`

## Setup database

1. Tao database:

```sql
CREATE DATABASE clinicmanagement CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

2. Import database mau:

```powershell
mysql -u root -p clinicmanagement < db_backups\clinicmanagement_sample_20260707_093813.sql
```

Neu muon dung backup cu hon:

```powershell
mysql -u root -p clinicmanagement < db_backups\clinicmanagement_before_c3_dataset_20260706_062028.sql
```

## Cau hinh ket noi database

Mo file:

```text
ClinicManagement.UI\App.config
```

Kiem tra connection string:

```xml
server=localhost;port=3306;database=clinicmanagement;uid=root;password=Itentad@1;
```

Neu MySQL tren may ban dung password khac, sua lai `password=...`.

## Chay tren Visual Studio

1. Mo `ClinicManagement.slnx` bang Visual Studio.
2. Set `ClinicManagement.UI` lam Startup Project.
3. Chon `Debug` configuration.
4. Build solution.
5. Bam `Start` de chay ung dung.

Khong dung `dotnet build` cho project WPF .NET Framework nay.

Lenh MSBuild dung neu can build bang terminal:

```powershell
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ClinicManagement.UI\ClinicManagement.UI.csproj /t:Rebuild /p:Configuration=Debug
```
