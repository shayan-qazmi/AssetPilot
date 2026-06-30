# Q Inventory Management System

A fully featured desktop Inventory Management System built with **C# .NET 8 WinForms** and **MySQL**, designed for managing equipment lifecycle from procurement to decommission.

---

## Screenshots

> Login Screen
<img width="701" height="554" alt="image" src="https://github.com/user-attachments/assets/adc83b9f-a28b-4a34-aa62-b5087a7c1939" />

 · Dashboard
 <img width="959" height="595" alt="image" src="https://github.com/user-attachments/assets/594aafe0-3bd0-470e-b3d3-848745c03b2b" />
 · Equipment Management
 <img width="957" height="599" alt="image" src="https://github.com/user-attachments/assets/d08b3318-556f-4313-971e-cf525cb99683" />
 
· QR Scanner 
<img width="959" height="599" alt="image" src="https://github.com/user-attachments/assets/ef051855-1222-4516-b14f-39b1e76bac16" />


---

## Features

### Core
- Equipment CRUD with QR code generation and printing
- Employee and location management
- Allocation workflow — allocate, approve, return, transfer
- Decommission tracking with reason logging
- Full audit trail on every action with timestamps

### Dashboard
- Live donut chart for equipment status breakdown
- Monthly allocations bar chart (last 6 months)
- Quick summary strip with utilization percentages
- 4 real-time stat cards (Total, Allocated, In Stock, Employees)

### Security & Access
- Role-based access control (Admin / Operator)
- Admin sees all screens including Decommission and Settings
- Operator has restricted sidebar access
- Logged-in user name and role shown persistently in sidebar

### Data Management
- Excel bulk import and export (ClosedXML)
- CSV export on every form
- Search and live filtering on every grid
- Row count display on all forms

### UI/UX
- Animated login screen with illustrated character and particle effects
- iOS-style toggle switch for "Keep me logged in"
- Dark green themed sidebar with rounded hover effects
- Status color coding across all grids
- Delete confirmation dialogs throughout
- QR Scanner with USB barcode scanner auto-detection

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# .NET 8 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core 8 |
| Database | MySQL 8 via Pomelo Provider |
| QR Generation | QRCoder |
| Excel | ClosedXML |
| Charts | GDI+ (no external library) |
| Platform | Windows |

---

## Project Structure

```
Inventory Management System/
├── Forms/
│   ├── LoginForm.cs
│   ├── DashboardForm.cs
│   ├── EquipmentForm.cs
│   ├── EmployeesForm.cs
│   ├── LocationsForm.cs
│   ├── AllocationForm.cs
│   ├── ReturnsForm.cs
│   ├── TransferForm.cs
│   ├── DecommissionForm.cs
│   ├── LogsForm.cs
│   ├── QRScannerForm.cs
│   ├── ReportsForm.cs
│   └── SettingsForm.cs
├── Models/
│   ├── Equipment.cs
│   ├── Employee.cs
│   ├── Allocation.cs
│   ├── Location.cs
│   ├── EquipmentLog.cs
│   ├── User.cs
│   ├── Role.cs
│   ├── UserRole.cs
│   └── InventoryDbContext.cs
├── Services/
│   ├── EquipmentService.cs
│   ├── EmployeeService.cs
│   ├── LocationService.cs
│   ├── AllocationService.cs
│   ├── AuditService.cs
│   ├── AuthService.cs
│   └── ReportService.cs
├── Helpers/
│   ├── ThemeHelper.cs
│   ├── QRHelper.cs
│   ├── ExcelHelper.cs
│   ├── ServiceResult.cs
│   └── RoundedPanel.cs
└── Images/
    └── logo.png
```

---

## Prerequisites

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [XAMPP](https://www.apachefriends.org/) (for MySQL) or any MySQL 8 server
- Visual Studio 2022 (recommended)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/your-username/q-inventory.git
cd q-inventory
```

### 2. Set up the database

Start MySQL in XAMPP, then open phpMyAdmin and import the database:

- Go to `http://localhost/phpmyadmin`
- Click **Import**
- Select the `inventory_management.sql` file from the repo
- Click **Go**

If you get a collation error during import, open the `.sql` file in a text editor and replace all instances of `utf8mb4_0900_ai_ci` with `utf8mb4_general_ci` before importing.

### 3. Configure the connection string

Open `Models/InventoryDbContext.cs` and update the connection string if needed:

```csharp
optionsBuilder.UseMySql(
    "server=localhost;database=inventory_management;user=root;password=;",
    ServerVersion.Parse("8.0.46-mysql"));
```

### 4. Install NuGet packages

Open the solution in Visual Studio, then install:

```
QRCoder
ClosedXML
Microsoft.EntityFrameworkCore
Pomelo.EntityFrameworkCore.MySql
```

Or via Package Manager Console:

```powershell
Install-Package QRCoder
Install-Package ClosedXML
Install-Package Pomelo.EntityFrameworkCore.MySql
```

### 5. Set up roles and users

Run these SQL queries in phpMyAdmin to set up roles:

```sql
INSERT INTO roles (role_name, description)
VALUES ('Admin', 'Full system access'),
       ('Operator', 'Standard day-to-day access');

-- Assign Admin role to your first user (replace 1 with actual user id)
INSERT INTO user_roles (user_id, role_id) VALUES (1, 1);
```

### 6. Run the application

Press **F5** in Visual Studio or run:

```bash
dotnet run
```

---

## Default Login

Check your `users` table in phpMyAdmin for existing accounts. The password is stored as plain text by default — replace with BCrypt hashing for production use.

---

## Database Schema

The system uses the following main tables:

- `equipment` — asset inventory with status tracking
- `employees` — staff records
- `locations` — physical locations/rooms
- `allocations` — equipment assignments with approval workflow
- `equipment_logs` — full audit trail
- `users` / `roles` / `user_roles` — authentication and access control
- `app_settings` — persistent application configuration

---

## Role-Based Access

| Feature | Admin | Operator |
|---|---|---|
| Equipment CRUD | ✅ | ✅ |
| Employee CRUD | ✅ | ✅ |
| Allocate / Return | ✅ | ✅ |
| Transfer | ✅ | ✅ |
| View Logs | ✅ | ✅ |
| Reports | ✅ | ✅ |
| Decommission | ✅ | ❌ |
| Settings | ✅ | ❌ |

---

## Excel Import Format

To bulk import equipment, create an `.xlsx` file with these columns in row 1:

| A | B | C | D | E | F | G | H |
|---|---|---|---|---|---|---|---|
| equipment_code | qr_code | name | make | model | serial_number | current_status | purchase_date |

Valid values for `current_status`: `in_store`, `allocated`, `decommissioned`, `under_repair`

---

## USB Barcode / QR Scanner

The QR Scanner form auto-detects USB scanner input. USB scanners act as keyboards — plug in, click into the QR Scanner form, and scan. The app searches automatically after the scanner finishes sending input.

---

## Built With

Developed as part of a PSEB apprenticeship program at QuantumRonics, Islamabad.

---

## License

This project is for educational and internal use. All rights reserved.
