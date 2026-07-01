# Q Inventory Management System

<p align="center">
  <img src="screenshots/login.png" alt="Q Inventory Login Screen" width="700"/>
</p>

> A professional, fully-featured desktop Inventory Management System built with **C# .NET 8 WinForms** and **MySQL**, developed as part of a PSEB apprenticeship program at QuantumRonics, Islamabad. Designed to manage the complete lifecycle of physical equipment — from procurement and allocation to transfer, maintenance, and decommission.

---

## Table of Contents

- [What This App Does](#what-this-app-does)
- [Screenshots](#screenshots)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Database Setup](#database-setup)
- [First Login](#first-login)
- [User Roles](#user-roles)
- [How to Use Each Screen](#how-to-use-each-screen)
- [QR Code System](#qr-code-system)
- [Barcode Scanner Hardware](#barcode-scanner-hardware)
- [Excel Import Format](#excel-import-format)
- [Barcode Label Printing](#barcode-label-printing)
- [Reports](#reports)
- [Dashboard Charts](#dashboard-charts)
- [Audit Logging](#audit-logging)
- [Settings](#settings)
- [Fuzzy Search](#fuzzy-search)
- [Troubleshooting](#troubleshooting)
- [Known Limitations](#known-limitations)
- [Future Improvements](#future-improvements)
- [Built With](#built-with)

---

## What This App Does

Q Inventory is a Windows desktop application that helps organizations track physical equipment (laptops, printers, scanners, tools, etc.) from the moment they are added to the system until they are permanently retired.

**In plain English — it answers these questions:**
- What equipment do we own?
- Where is each item right now?
- Who has it?
- When was it given to them?
- When is it due back?
- What happened to it over its lifetime?
- Who made changes and when?

---

## Screenshots

| Screen | Description |
|---|---|
| <img width="697" height="556" alt="image" src="https://github.com/user-attachments/assets/cb292a2c-9b95-42cc-82ea-1935d0ee3e07" />| Animated login screen |
| <img width="958" height="598" alt="image" src="https://github.com/user-attachments/assets/70b8dcf3-9ef6-45c8-a80f-293155a30a92" />| Dashboard with charts |
| <img width="959" height="599" alt="image" src="https://github.com/user-attachments/assets/06a38928-dec4-4b09-bdbc-b6b023accaca" />| Equipment management |
| <img width="959" height="599" alt="image" src="https://github.com/user-attachments/assets/6bc64649-58ba-49fa-b9ee-1d42ff318c85" />| Allocation workflow |
| <img width="959" height="599" alt="image" src="https://github.com/user-attachments/assets/6b3fe50a-d62d-4648-90a4-6effd66e22ce" />| QR Scanner with history |
| <img width="958" height="599" alt="image" src="https://github.com/user-attachments/assets/22851793-705b-4c1a-9729-bb9b268eb82e" />| Reports screen |
| <img width="959" height="599" alt="image" src="https://github.com/user-attachments/assets/0ba22a98-02d2-477c-8e4e-51532022e367" />| Settings screen |



---

## Features

### Core Equipment Management
- Add, edit, delete equipment records
- Track: equipment code, QR code, name, make, model, serial number, purchase date, price, status, location
- Status tracking: `in_store`, `allocated`, `under_repair`, `decommissioned`
- Color-coded status badges across all grids (green/amber/orange/red)
- Fuzzy search across all fields in every form

### Allocation Workflow
- **Allocate** — assign equipment to an employee at a specific location
- **Approve** — admin approves a pending allocation request
- **Return** — mark equipment as returned, status resets to `in_store` automatically
- **Transfer** — move equipment directly from one employee/location to another without returning first
- **Decommission** — permanently retire equipment with a reason (Damaged, Obsolete, Beyond Repair, End of Life, Lost, Other)

### QR Code System
- Auto-generate QR codes for every equipment item
- Show QR popup with print support
- QR Scanner screen: scan physical labels with a USB barcode scanner to instantly look up any item
- USB barcode scanner works plug-and-play (keyboard wedge mode) — no drivers or SDK needed
- Complete equipment history shown alongside the scanned item details

### Data Management
- **Excel bulk import** — import dozens of equipment records at once from a `.xlsx` file
- **Excel export** — export any grid to Excel with a styled green header
- **CSV export** — available on every form (Equipment, Employees, Locations, Allocation, Returns, Transfer, Decommission, Logs)
- **PDF/Print reports** — generate printable reports with proper pagination

### Reports
Four report types, each with live on-screen preview, Excel export, and PDF print preview:
1. Equipment Inventory — full list with status and location
2. Employee Allocation Summary — who has what and how many items
3. Active Allocations — all currently assigned equipment
4. Returns History — all returned items with duration in days

### Dashboard Analytics
- 4 real-time stat cards: Total Equipment, Allocated, In Stock, Total Employees
- Donut chart — visual breakdown of equipment by status
- Bar chart — monthly allocations for the last 6 months
- Quick Summary strip — 5-column utilization overview with percentages

### Security & Access Control
- Role-based access: **Admin** (full access) and **Operator** (restricted access)
- Admin sees: all screens including Decommission and Settings
- Operator sees: Equipment, Employees, Locations, Allocation, Returns, Transfer, Logs, QR Scanner, Reports
- Logged-in username and role displayed persistently in the sidebar
- "Keep me logged in" toggle switch on the login screen
- Password show/hide toggle

### Audit Logging
- Every Add, Edit, Delete, Allocate, Return, Transfer, and Decommission action is logged automatically
- Each log entry records: what changed, old value, new value, who did it, exact timestamp
- Full audit trail viewable in the Logs screen with color-coded action types
- Logs are searchable and exportable to CSV
- Equipment history is also shown directly in the QR Scanner screen when an item is looked up

### UI/UX
- Animated login screen with illustrated character, rising bubbles, and parallax depth effects
- iOS-style animated toggle switch
- Dark green themed sidebar with rounded hover effects
- Professional green header bar
- Company logo in sidebar
- Row count label on every grid ("Showing X records")
- Delete confirmation dialogs throughout
- Fuzzy search on every search bar

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# .NET 8 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core 8 |
| Database | MySQL 8 |
| MySQL Provider | Pomelo.EntityFrameworkCore.MySql |
| QR Generation | QRCoder |
| Excel Import/Export | ClosedXML |
| Charts | Pure GDI+ (no external library) |
| PDF/Print | Windows PrintDocument + PrintPreviewDialog |
| Platform | Windows 10/11 x64 |

---

## Project Structure

```
Inventory Management System/
│
├── Forms/                          # All UI screens
│   ├── LoginForm.cs                # Animated login with role auth
│   ├── DashboardForm.cs            # Main container with sidebar + charts
│   ├── EquipmentForm.cs            # Equipment CRUD + QR
│   ├── EmployeesForm.cs            # Employee management
│   ├── LocationsForm.cs            # Location management
│   ├── AllocationForm.cs           # Allocate / Approve / Return
│   ├── ReturnsForm.cs              # Returns history
│   ├── TransferForm.cs             # Equipment transfers
│   ├── DecommissionForm.cs         # Decommission management
│   ├── LogsForm.cs                 # Audit trail viewer
│   ├── QRScannerForm.cs            # USB scanner + equipment lookup
│   ├── ReportsForm.cs              # Report generation + export
│   └── SettingsForm.cs             # App config + label printing
│
├── Models/                         # Database table classes
│   ├── Equipment.cs
│   ├── Employee.cs
│   ├── Allocation.cs
│   ├── Location.cs
│   ├── EquipmentLog.cs
│   ├── User.cs
│   ├── Role.cs
│   ├── UserRole.cs
│   ├── AppSetting.cs
│   └── InventoryDbContext.cs       # EF Core database context
│
├── Services/                       # Business logic layer
│   ├── EquipmentService.cs         # Equipment CRUD + validation
│   ├── EmployeeService.cs          # Employee CRUD + validation
│   ├── LocationService.cs          # Location CRUD + validation
│   ├── AllocationService.cs        # Allocate / Return / conflict checks
│   ├── AuditService.cs             # Audit logging
│   ├── AuthService.cs              # Login + role lookup
│   └── ReportService.cs            # Report data queries
│
├── Helpers/                        # Utility classes
│   ├── ThemeHelper.cs              # All colors, fonts, styling methods
│   ├── QRHelper.cs                 # QR code generation
│   ├── ExcelHelper.cs              # Excel import + export
│   ├── ServiceResult.cs            # Consistent error handling wrapper
│   └── RoundedPanel.cs             # Custom rounded input panel control
|   ├── Labelprinter.cs            
│   └── FuzzysearchHelper.cs
│   └── QRHelperWrapper.cs             │
└── Images/
    └── logo.png                    # Company logo (sidebar + login)
```

---

## Prerequisites

Before installing, make sure you have these on your Windows machine:

| Requirement | Download Link |
|---|---|
| Windows 10 or 11 (64-bit) | Already installed |
| .NET 8 Runtime or SDK | https://dotnet.microsoft.com/download/dotnet/8.0 |
| XAMPP (includes MySQL + phpMyAdmin) | https://www.apachefriends.org/ |
| Visual Studio 2022 (to build from source) | https://visualstudio.microsoft.com/ |

---

## Installation & Setup

### Step 1 — Clone or download the project

```bash
git clone https://github.com/your-username/q-inventory.git
cd q-inventory
```

Or download the ZIP from GitHub and extract it.

### Step 2 — Open in Visual Studio

Double-click `Inventory Management System.sln` to open the solution in Visual Studio 2022.

### Step 3 — Install NuGet packages

Visual Studio should restore these automatically. If not, open **Tools → NuGet Package Manager → Package Manager Console** and run:

```powershell
Install-Package QRCoder
Install-Package ClosedXML
Install-Package Pomelo.EntityFrameworkCore.MySql
Install-Package Microsoft.EntityFrameworkCore
```

### Step 4 — Start MySQL

Open **XAMPP Control Panel** and click **Start** next to **MySQL**. Make sure it turns green. The app cannot connect to the database without this running.

### Step 5 — Build and run

Press **F5** in Visual Studio to build and launch the app.

---

## Database Setup

### Import the database

1. Open your browser and go to `http://localhost/phpmyadmin`
2. Click **Import** in the top menu
3. Click **Choose File** and select the `inventory_management.sql` file from the project folder
4. Click **Go**
5. Wait for the success message — you should see the `inventory_management` database appear in the left sidebar

### If you get a collation error during import

This happens when your MySQL version is older than 8.0. Fix it before importing:

1. Open the `.sql` file in **Notepad++** or **VS Code**
2. Press `Ctrl+H` (Find and Replace)
3. Find: `utf8mb4_0900_ai_ci`
4. Replace with: `utf8mb4_general_ci`
5. Click **Replace All**
6. Save the file and try importing again

### Configure the connection string

Open `Models/InventoryDbContext.cs` and find this line:

```csharp
optionsBuilder.UseMySql(
    "server=localhost;database=inventory_management;user=root;password=;",
    ServerVersion.Parse("8.0.46-mysql"));
```

If your MySQL password is not empty, update the `password=` part. If you're using a different server or port, update `server=` and add `port=3306` if needed.

### Set up roles

Run these SQL commands in phpMyAdmin (click the `inventory_management` database → SQL tab):

```sql
-- Create roles if they don't exist
INSERT INTO roles (role_name, description)
VALUES ('Admin', 'Full system access'),
       ('Operator', 'Standard day-to-day access');

-- Assign Admin role to your first user (check actual user id first)
SELECT id, username FROM users;

-- Then assign role (replace 1 with actual user id)
INSERT INTO user_roles (user_id, role_id) VALUES (1, 1);
```

### Create an Operator user

```sql
-- Create the user
INSERT INTO users (username, full_name, password_hash, is_active)
VALUES ('operator', 'Test Operator', 'your_password_here', 1);

-- Find the new user's id
SELECT id FROM users WHERE username = 'operator';

-- Assign Operator role (replace 2 with actual user id, 2 is Operator role id)
INSERT INTO user_roles (user_id, role_id) VALUES (2, 2);
```

---

## First Login

When you run the app for the first time, the login screen appears. Check your `users` table in phpMyAdmin to find existing usernames:

```sql
SELECT username, full_name FROM users;
```

Passwords are stored as plain text by default in this version (suitable for internal/demo use). Enter the username and password exactly as stored in `password_hash`.

> **Note for production use:** Replace plain-text password storage with BCrypt hashing using the `BCrypt.Net-Next` NuGet package before deploying to a real organization.

---

## User Roles

| Feature | Admin | Operator |
|---|---|---|
| View Dashboard & Charts | ✅ | ✅ |
| Equipment — View | ✅ | ✅ |
| Equipment — Add/Edit/Delete | ✅ | ✅ |
| Equipment — QR Generate & Print | ✅ | ✅ |
| Equipment — Excel Import/Export | ✅ | ✅ |
| Employees — Full CRUD | ✅ | ✅ |
| Locations — Full CRUD | ✅ | ✅ |
| Allocation — Allocate/Approve/Return | ✅ | ✅ |
| Transfer | ✅ | ✅ |
| Returns History | ✅ | ✅ |
| Logs — View & Export | ✅ | ✅ |
| QR Scanner | ✅ | ✅ |
| Reports — View & Export | ✅ | ✅ |
| Decommission | ✅ | ❌ Hidden |
| Settings | ✅ | ❌ Hidden |

---

## How to Use Each Screen

### Dashboard
The home screen. Shows 4 stat cards at the top (Total Equipment, Allocated, In Stock, Total Employees), a donut chart breaking down equipment by status, a bar chart of monthly allocations, and a quick summary strip. All data is live from the database. Navigate using the left sidebar.

### Equipment
The main equipment list. Shows all items with color-coded status:
- 🟢 Green = In Stock (available)
- 🟠 Amber = Allocated (assigned to someone)
- 🟧 Orange = Under Repair
- 🔴 Red = Decommissioned

**Add** — opens a dialog to enter equipment details. Equipment Code and Serial Number must be unique.
**Edit** — select a row first, then click Edit.
**Delete** — select a row, click Delete, confirm in the popup. Cannot delete allocated equipment.
**Show QR** — select a row, click Show QR to see the QR code image. Click Print QR to print a physical label.
**Import Excel** — bulk import equipment from a `.xlsx` file (see Excel Import Format section).
**Export** — exports the current visible list to Excel.
**Search** — fuzzy search across equipment code, name, make, model, serial number.

### Employees
Manage staff records. Each employee has a code, full name, department, email, phone, and active/inactive status. Only active employees appear in allocation dropdowns.

### Locations
Physical locations where equipment can be deployed (e.g. "Server Room", "Finance Office", "Lab 1"). Each location has a name, building, and room number.

### Allocation
The core workflow screen:

1. **Allocate** — click Allocate, select an available equipment item (only `in_store` items appear), select an employee, select a location, optionally set an expected return date. Creates a pending allocation.
2. **Approve** — select a pending allocation row, click Approve. Status changes to `approved` and equipment status changes to `allocated`.
3. **Return** — select an approved allocation, click Return. Equipment status changes back to `in_store`. A return record is created.
4. **Delete** — only pending or rejected allocations can be deleted.
5. **Select All** — selects all rows (useful for bulk review).

### Returns
Read-only history of all returned equipment. Shows who had it, when it was allocated, when it was returned, and how many days they had it. Rows where equipment was held for more than 30 days are highlighted in amber.

### Transfer
Move equipment directly from one employee/location to another without going through a Return first. Click **New Transfer**, select the currently-allocated equipment you want to move, select the new employee and location. The old allocation is automatically closed and a new one is created linked to it.

### Decommission
Permanently retire equipment. Only equipment with status `in_store` or `under_repair` can be decommissioned. Click **Decommission Item**, select the equipment, choose a reason (Damaged / Obsolete / Beyond Repair / End of Life / Lost / Other), add optional notes. The equipment status changes to `decommissioned` and the action is logged.

### Logs
Full audit trail of every action taken in the system. Color coded by action type:
- 🟢 Green = Equipment created
- 🔵 Blue = Allocation actions
- 🟡 Yellow = Returns
- 🔴 Red = Rejections / Decommissions
- 🟣 Purple = Transfers
- 🟠 Orange = Location changes

Searchable by equipment code, action type, user name, old value, or new value. Exportable to CSV.

### QR Scanner
Designed to work with a physical USB barcode scanner. How to use:
1. Navigate to the QR Scanner screen (the input box auto-focuses)
2. Point your USB scanner at a physical QR label stuck on equipment
3. The scanner instantly "types" the code into the text box and the app auto-searches
4. Equipment details appear on the left panel, full history on the right
5. Can also type manually or paste a code and click Search, or press Enter

### Reports
Select a report type (Equipment / Employee / Allocation / Return) to preview it on screen. Then export via:
- **Export Excel** — saves a styled `.xlsx` file
- **Export PDF** — opens Windows print preview (save as PDF using "Microsoft Print to PDF" or send to a physical printer)

### Settings
**Application Settings:**
- Company Name — used on printed labels
- Theme — Light/Dark (Light is currently active)
- Server / Database — stored for reference (does not change live connection)

**Barcode Label Printing:**
- Printer Name — type your printer's exact Windows name (leave blank for default)
- Label Width/Height — in millimeters (default 62×29mm, a common label size)
- Test Print Label — opens a print preview of a sample label to verify your printer and size settings before printing real equipment labels

---

## QR Code System

### How it works end-to-end

1. When you add equipment, a QR code field stores a unique code (e.g. `QR001`)
2. In the Equipment screen, click **Show QR** on any row to see the generated QR image
3. Click **Print QR** to print a physical adhesive label
4. Stick the label on the physical piece of equipment
5. Later, when you need to look up that item, open the QR Scanner screen
6. Scan the label with a USB scanner — the app instantly shows all details and history

### What the QR code contains

Each QR code encodes a short text string (the `qr_code` field value, e.g. `QR001`). It is not a URL. It is plain text. Your app's QR Scanner searches for this text in the database across QR code, equipment code, and serial number fields.

### Testing QR codes without a physical scanner

To verify a QR code is correct without buying hardware:
- Open your iPhone Camera app and point it at the QR code on screen or on a printed label
- If iOS opens a search or website, ignore the redirect — instead download **Qrafter** (free on App Store) which shows raw decoded text without any smart redirect behavior
- The decoded text should match the `qr_code` column value for that equipment item in your database

---

## Barcode Scanner Hardware

### Recommended scanner

**Honeywell Xenon 1900GSR-2** or **Datalogic QuickScan QD2500**

Both are 2D area-imager scanners that read QR codes, which is what this app generates. **Do not buy a 1D laser scanner** (like the Honeywell Voyager 1250g) — those cannot read QR codes.

| | Honeywell Xenon 1900GSR-2 | Datalogic QuickScan QD2500 |
|---|---|---|
| Reads QR codes | ✅ Yes | ✅ Yes |
| Interface | USB, Keyboard Wedge | USB, Keyboard Wedge |
| Drop resistance | 6 ft to concrete | 5 ft to concrete |
| Warranty | 5 years | 5 years |
| Configuration tool | EasySet / scan sheets | Aladdin 3.0 (free) |

### Do I need an SDK?

**No.** Both scanners work in USB Keyboard Wedge mode out of the box — they plug into USB and act exactly like a keyboard. No drivers, no SDK, no coding required. The scanner just "types" the decoded text into whatever field is focused and presses Enter. Your app is already built to handle this exactly.

### How to connect

1. Plug the USB cable into any USB port on your Windows PC
2. Windows detects it automatically (no driver installation needed)
3. Open Notepad and scan any barcode — if text appears, it's working
4. Open the QR Scanner screen in your app
5. Scan — the app searches automatically

### Confirming keyboard wedge mode

When you buy the scanner, ask the seller: **"Is this pre-configured for USB Keyboard Wedge / HID mode?"**

If it needs configuration, both models come with a printed manual containing special "setup barcodes" you scan to switch modes — no computer or software needed.

---

## Excel Import Format

To bulk import equipment, create an Excel file (`.xlsx`) with exactly these column headers in **Row 1**, data starting from **Row 2**:

| Column | Header | Example | Notes |
|---|---|---|---|
| A | equipment_code | EQ005 | Must be unique |
| B | qr_code | QR005 | Must be unique |
| C | name | Dell Latitude 5420 | Required |
| D | make | Dell | Optional |
| E | model | 5420 | Optional |
| F | serial_number | SN98765 | Must be unique |
| G | current_status | in_store | Must be one of: `in_store`, `allocated`, `decommissioned`, `under_repair` |
| H | purchase_date | 2024-01-15 | Format: YYYY-MM-DD. Optional |

**Notes:**
- Row 1 must be the header row exactly as shown above (the import skips it automatically)
- Empty rows are ignored silently
- Rows with duplicate codes or serial numbers will fail individually but won't stop the rest from importing
- After import, click Refresh to see the new records

---

## Barcode Label Printing

The Settings screen has a **Barcode Label Printing** section that connects to the Equipment form's QR print feature.

### Setup
1. Go to **Settings**
2. Enter your printer's exact name as it appears in Windows (Start → Printers & Scanners → find your printer name)
3. Set label dimensions in mm (check your label packaging — 62×29mm is a common Brother/Zebra size)
4. Click **Test Print Label** to preview a sample label and confirm the size and layout look right
5. Click **Save**

### Printing a real label
1. Go to **Equipment**
2. Select the item you want to label
3. Click **Show QR**
4. Click **Print QR**
5. A print preview opens showing the label with: company name, equipment code, equipment name, status, and date
6. Click Print to send it to your label printer

### Recommended label printers
- **Brother QL series** (e.g. QL-800, QL-820NWB) — compact, affordable, common in Pakistan
- **Zebra ZD series** (e.g. ZD230, ZD421) — enterprise grade, more expensive

---

## Reports

### How to generate a report

1. Click **Reports** in the sidebar
2. Click one of the 4 report type buttons:
   - **Equipment** — full inventory list
   - **Employee** — allocation summary per person
   - **Allocation** — currently active allocations
   - **Return** — completed returns history
3. The report previews instantly in the grid below
4. Click **Export Excel** to save as `.xlsx` with a styled header
5. Click **Export PDF** to open a print preview (multi-page with proper pagination)

### PDF export note
The PDF option uses Windows' built-in print system. In the print preview dialog, select **Microsoft Print to PDF** as the printer to save a PDF file, or select your actual printer to print on paper.

---

## Dashboard Charts

The dashboard home screen (visible when you first log in and when you click Dashboard in the sidebar) shows three chart areas below the stat cards:

### Donut Chart (Equipment Status)
A circular chart showing the proportion of equipment in each status. The number in the center shows total equipment count. The legend below shows counts per status.

### Bar Chart (Monthly Allocations)
Shows how many allocations were created each month for the last 6 months. Useful for spotting busy periods or trends.

### Quick Summary Strip
Five columns showing: Available (in_store), Allocated, Under Repair, Decommissioned, and Active Staff — each with a count and a percentage of total.

### Note on first load
On first login, the charts may appear slightly smaller until you navigate to another screen and back to Dashboard. This is a known WinForms layout timing issue — it corrects itself automatically on second view.

---

## Audit Logging

Every significant action in the app is automatically logged — no extra steps needed. The log records:

- **What** — action type (Equipment Created, Allocation Approved, Returned to Store, etc.)
- **Which item** — linked to the specific equipment record
- **Before/After** — old value and new value for changes
- **Who** — the logged-in user who performed the action
- **When** — exact timestamp (UTC)

### Viewing logs
Go to **Logs** in the sidebar. Search by equipment code, action type, user name, or value. Export to CSV using the Export button.

### Log color codes
| Color | Meaning |
|---|---|
| 🟢 Green | Equipment created |
| 🔵 Blue | Allocation requested or approved |
| 🟡 Yellow | Equipment returned |
| 🔴 Red | Allocation rejected or decommissioned |
| 🟣 Purple | Equipment transferred |
| 🟠 Orange | Location changed |

---

## Fuzzy Search

Every search bar in the app uses fuzzy search — you don't need to type the exact word or code. It finds results that approximately match what you typed, so partial words, slight typos, or fragment matches all work.

For example searching `dell` in Equipment will find `Dell Latitude`, `Dell EliteBook`, etc. Searching `john` in Employees will find `John Smith`, `Johnathan Ali`, etc.

This is available on all forms: Equipment, Employees, Locations, Allocation, Returns, Transfer, Decommission, Logs, QR Scanner, and Reports.

---

## Troubleshooting

### App won't start / connection error on login
- Open XAMPP Control Panel and make sure **MySQL is Started** (green)
- Check the connection string in `Models/InventoryDbContext.cs` matches your MySQL server/password

### "Unknown column" error when opening a form
- Your database schema may be out of sync with the code models
- Re-import the `inventory_management.sql` file via phpMyAdmin

### Collation error during database import
- See the "If you get a collation error" section under Database Setup above

### QR code prints but scanner can't read it
- Make sure your scanner is a **2D imager** not a 1D laser scanner
- Try scanning from different angles or distances
- Test the QR code with Qrafter app on your phone first to confirm it's readable

### Scanner plugged in but nothing happens in QR Scanner screen
- Make sure you clicked into the QR Scanner screen so the text box has keyboard focus
- Open Notepad, click in it, and scan — if text appears in Notepad, the scanner is working and the issue is focus/navigation in your app
- Make sure the scanner is in USB Keyboard Wedge mode (check with the seller or scan the HID mode setup barcode from the user manual)

### Dashboard charts look squished on first login
- Navigate to any other screen and back to Dashboard — the charts will resize correctly
- This is a known WinForms layout timing issue being tracked for a future fix

### "Non-nullable field" compile errors
- Right-click project → Properties → Build → set **Nullable** to **Enable**
- Rebuild the solution

### Import Excel fails with "Error adding equipment"
- Check the `equipment` table structure in phpMyAdmin — `created_by_user_id` is required
- Make sure your Excel file has the correct column headers in row 1 exactly as specified
- Check that equipment codes and serial numbers don't already exist in the database

---

## Known Limitations

- **Plain text passwords** — passwords are stored as plain text. Suitable for internal/demo use only. For production, implement BCrypt hashing.
- **Single database connection** — the connection string is hardcoded in `InventoryDbContext.cs`. Moving it to `appsettings.json` is recommended for production.
- **Windows only** — this is a WinForms app and runs only on Windows 10/11.
- **Single machine** — the app runs locally. Remote access requires either RDP to a Windows Server or converting to a web app.
- **Dark mode** — the Theme setting has a Dark option but it is a placeholder; dark mode is not yet implemented.
- **Dashboard first-load chart sizing** — charts may appear smaller on first login, correcting on navigation back to Dashboard.
- **One concurrent user per machine** — WinForms desktop apps are not inherently multi-user on the same machine.

---

## Future Improvements

- Move connection string to `appsettings.json` config file
- Implement BCrypt password hashing for production security
- Add user management screen (create/edit/deactivate users from inside the app)
- Implement dark mode theme
- Fix dashboard first-load chart sizing
- Add email notifications for overdue equipment returns
- Add maintenance scheduling and reminders
- Convert to ASP.NET Core Blazor web app for true remote/multi-user access
- Add role for "Manager" with approval-only access
- Add equipment image upload support
- Add barcode (1D Code128) generation as alternative to QR codes

---

## Built With

Developed by **Syed Shayan Haider Kazmi** as part of a PSEB apprenticeship program at **QuantumRonics, Islamabad**.

- C# .NET 8 / Windows Forms
- Entity Framework Core 8 + Pomelo MySQL provider
- QRCoder, ClosedXML
- MySQL 8 via XAMPP
- Visual Studio 2022

---

## License

This project is for educational and internal organizational use.
All rights reserved © 2026 QuantumRonics / Syed Shayan Haider Kazmi.
