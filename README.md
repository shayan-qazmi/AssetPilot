# Q Inventory Management System

<p align="center">
<img width="977" height="667" alt="image" src="https://github.com/user-attachments/assets/93c24bba-4226-40ad-a30e-6d8ccbe92021" />

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
- [Equipment Codes & QR Codes](#equipment-codes--qr-codes)
- [Maintenance Tracking](#maintenance-tracking)
- [Check In / Check Out](#check-in--check-out)
- [Deleting vs Decommissioning](#deleting-vs-decommissioning)
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

Q Inventory is a Windows desktop application that helps organizations track physical equipment (laptops, printers, scanners, tools, vehicles, etc.) from the moment they are added to the system until they are permanently retired.

**In plain English — it answers these questions:**
- What equipment do we own, and what condition is it in?
- Where is each item right now, and who has it?
- Who supplied it, and is it still under warranty?
- When was it given out, and when is it due back?
- Is it out on a quick short-term loan, or formally allocated?
- What happened to it over its entire lifetime — every repair, transfer, and return?
- Who made changes, and when?

---

## Screenshots

| Screen | Description |
|---|---|
| <img width="977" height="667" alt="image" src="https://github.com/user-attachments/assets/d3d06daf-65b5-4719-befa-f3c3e63abbce" />| Login screen |
| <img width="1919" height="1029" alt="image" src="https://github.com/user-attachments/assets/737f24b6-0799-4b0d-bf4c-140b22272b68" />| Dashboard with charts |
| <img width="1919" height="1029" alt="image" src="https://github.com/user-attachments/assets/d857625c-175e-488e-a428-2d8b9e113622" />| Equipment management |
| <img width="1914" height="1020" alt="image" src="https://github.com/user-attachments/assets/f4f85c97-6a7b-42c4-813b-6f84f7fe54e3" />| Add/Edit Equipment (with category, vendor, condition, warranty fields) |
| <img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/a2c99255-a304-49d0-9bb5-699bf5920042" />| Allocation workflow |
| <img width="1919" height="1029" alt="image" src="https://github.com/user-attachments/assets/72e2a451-bf64-432e-a5d9-79af996898c6" />| Check In / Check Out |
| <img width="1917" height="1031" alt="image" src="https://github.com/user-attachments/assets/3e6397db-68da-49ff-b0fa-4c64d239fd20" />| QR Scanner with history |
| <img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/930ad683-e28c-4134-bfd4-c9aa47d9b568" />| Reports screen |
| <img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/105c857f-26d3-4857-95e7-59afeaec6f51" />| Settings screen |

---

## Features

### Core Equipment Management
- Add, edit, delete equipment records
- Track: equipment code, QR code, name, make, model, serial number, purchase date, price, status, **category, vendor name, condition grade, warranty expiry, procurement type, notes**
- **Equipment Code and QR Code are now auto-generated** on save (`EQ001`, `QR001`, ...) — no manual entry, no risk of duplicate/mistyped codes
- **Procurement Type** is a free-text field that resolves to an existing type or creates a new one automatically, while still preserving a proper database relationship behind the scenes
- Status tracking: `in_store`, `allocated`, `under_repair`, `decommissioned`
- Color-coded status badges across all grids (green/amber/orange/red)
- Fuzzy search across all fields in every form

### Allocation Workflow
- **Allocate** — assign equipment to an employee at a specific location
- **Approve** — admin approves a pending allocation request
- **Return** — mark equipment as returned, status resets to `in_store` automatically
- **Transfer** — move equipment directly from one employee/location to another without returning first
- **Decommission** — permanently retire equipment while preserving its full history

### Maintenance Tracking (New)
- Marking equipment **"Under Repair"** now opens a dialog to log the issue — creates a dedicated maintenance record instead of just flipping a status flag
- Moving equipment **out of "Under Repair"** opens a resolution dialog to record what was done and the repair cost
- Full maintenance history (per item) is viewable in the Equipment Detail popup, with each record numbered and cost shown

### Check In / Check Out (New)
- A lightweight, no-approval loan tracker separate from the formal Allocation workflow
- Designed for short-term same-day borrows (e.g. a projector for a meeting) where a full allocation request/approval cycle would be overkill
- Check Out logs who took it and when it's expected back; Check In closes the loan and returns the item to `in_store`

### Smarter Delete Protection (Updated)
- Deleting equipment is now blocked if it has **any** allocation or transaction history at all — not just active ones — to protect audit trails from accidental data loss
- If blocked, the app now tells you directly to use **Decommission** instead, which retires the item permanently while keeping its full history intact

### QR Code System
- QR codes are auto-generated for every equipment item (no manual entry)
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
- Real-time stat cards covering equipment counts by status
- Donut chart — visual breakdown of equipment by status
- Bar chart — monthly allocations for the last 6 months

### Security & Access Control
- Role-based access: **Admin** (full access) and **Operator** (restricted access)
- Admin sees: all screens including Settings, and Delete Equipment
- Operator sees: everything except Settings; the Delete Equipment button is hidden
- Logged-in username and role displayed persistently in the sidebar
- "Keep me logged in" toggle switch on the login screen (pre-fills username only)
- Password show/hide toggle

### Audit Logging
- Every Add, Edit, Delete, Allocate, Return, Transfer, and Decommission action is logged automatically
- Each log entry records: what changed, old value, new value, who did it, exact timestamp
- Full audit trail viewable in the Logs screen with color-coded action types
- Logs are searchable and exportable to CSV
- *Note: returns processed via the Returns screen currently bypass this log — see Known Limitations*

### Branding & Executable
- Custom application icon compiled directly into the `.exe`
- Sidebar/dashboard logo embedded as a compiled resource rather than a loose file, so it displays correctly regardless of where the built `.exe` is copied to

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

Inventory Management System/
│
├── Forms/
│   ├── LoginForm.cs
│   ├── DashboardForm.cs
│   ├── EquipmentForm.cs
│   ├── AddEditEquipmentForm.cs         # now includes Category, Vendor, Condition, Warranty, Procurement Type (free text)
│   ├── EquipmentDetailPopup.cs         # now shows maintenance history
│   ├── EmployeesForm.cs
│   ├── LocationsForm.cs
│   ├── AllocationForm.cs
│   ├── ReturnsForm.cs
│   ├── TransferForm.cs
│   ├── DecommissionForm.cs
│   ├── LogsForm.cs
│   ├── QRScannerForm.cs
│   ├── CheckInOutForm.cs               # NEW — short-term loan tracking
│   ├── MaintenanceForm.cs              # NEW — log a new repair issue
│   ├── ResolveMaintenanceForm.cs       # NEW — close out a repair
│   ├── ReportsForm.cs
│   └── SettingsForm.cs
│
├── Models/
│   ├── Equipment.cs                    # extended: Category, Description, VendorName, ConditionGrade, WarrantyExpiry, Notes
│   ├── Employee.cs
│   ├── Allocation.cs
│   ├── Location.cs
│   ├── ProcurementType.cs
│   ├── Transaction.cs                  # NEW — backs Check In/Out
│   ├── MaintenanceRecord.cs            # NEW — backs Maintenance Tracking
│   ├── EquipmentLog.cs
│   ├── User.cs
│   ├── Role.cs
│   ├── UserRole.cs
│   ├── AppSetting.cs
│   └── InventoryDbContext.cs
│
├── Services/
│   ├── EquipmentService.cs             # auto-generates codes; delete now blocks on any history
│   ├── EmployeeService.cs
│   ├── LocationService.cs
│   ├── AllocationService.cs
│   ├── TransactionService.cs           # NEW — Check In/Out logic
│   ├── MaintenanceService.cs           # NEW — open/resolve maintenance records
│   ├── AuditService.cs
│   ├── AuthService.cs
│   └── ReportService.cs
│
├── Helpers/
│   ├── ThemeHelper.cs
│   ├── QRHelper.cs
│   ├── QRHelperWrapper.cs
│   ├── ExcelHelper.cs
│   ├── ServiceResult.cs
│   ├── RoundedPanel.cs
│   ├── LabelPrinter.cs
│   ├── FuzzySearchHelper.cs
│   └── CategoryStore.cs                # flat-file category list
│
├── Assets/
│   └── app.ico                         # compiled application icon
│
└── Properties/
    └── Resources.resx                  # embedded sidebar logo

---

## Prerequisites

Same as before — Windows 10/11 (64-bit), .NET 8 Runtime/SDK, XAMPP (MySQL + phpMyAdmin), Visual Studio 2022 or newer.

---

## Installation & Setup

*(unchanged from previous version — clone, open the `.sln`, restore NuGet packages, start MySQL via XAMPP, press F5)*

---

## Database Setup

*(unchanged — import `inventory_management.sql`, fix collation if needed, update the connection string in `InventoryDbContext.cs`, seed roles)*

> **Schema note:** the database now includes two additional tables — `transactions` (Check In/Out) and `maintenance_records` (repair history) — plus new columns on `equipment`: `category`, `description`, `vendor_name`, `condition_grade`, `warranty_expiry`, `notes`. These are already included if you're importing the latest `inventory_management.sql`.

---

## First Login

*(unchanged)*

---

## User Roles

| Feature | Admin | Operator |
|---|---|---|
| View Dashboard & Charts | ✅ | ✅ |
| Equipment — View / Add / Edit | ✅ | ✅ |
| Equipment — Delete | ✅ | ❌ Hidden |
| Equipment — QR Generate & Print | ✅ | ✅ |
| Equipment — Excel Import/Export | ✅ | ✅ |
| Employees — Full CRUD | ✅ | ✅ |
| Locations — Full CRUD | ✅ | ✅ |
| Allocation — Allocate/Approve/Return | ✅ | ✅ |
| Transfer | ✅ | ✅ |
| Returns History | ✅ | ✅ |
| Check In / Check Out | ✅ | ✅ |
| Decommission | ✅ | ✅ |
| Logs — View & Export | ✅ | ✅ |
| QR Scanner | ✅ | ✅ |
| Reports — View & Export | ✅ | ✅ |
| Settings | ✅ | ❌ Hidden |

---

## How to Use Each Screen

*(Dashboard, Employees, Locations, Allocation, Returns, Transfer, Logs, QR Scanner, Reports — unchanged from previous version)*

### Equipment
The main equipment list. Shows all items with color-coded status.

- **Add** — opens a dialog to enter equipment details: Name, Make, Model, Serial Number, Status, Purchase Date, Category, Vendor Name, Condition Grade, Warranty Expiry, Procurement Type (free text — matched or created automatically), Notes. **Equipment Code and QR Code are generated automatically — you no longer enter them.**
- **Edit** — select a row first, then click Edit. Changing status to "Under Repair" opens a maintenance-log dialog; changing it back opens a resolution dialog.
- **Delete** — blocked if the item has any allocation or transaction history (even fully closed/returned ones). Use Decommission for equipment with history. Admin only.
- **View Details** — full read-only view including complete maintenance history.
- **Show QR / Print QR** — as before.
- **Import Excel / Export** — as before.
- **Search** — fuzzy search across all fields.

### Check In / Check Out
A quick alternative to the full Allocation workflow, for short-term loans that don't need an approval step.

1. Navigate to **Check In/Out**.
2. To check out: enter or scan the equipment code, confirm who's taking it, click **Check Out**. Equipment status updates immediately.
3. To check in: enter or scan the code again, click **Check In**. Equipment returns to `in_store`.

### Decommission
Permanently retire equipment while preserving its full history — this is now the recommended way to remove equipment from active use, as opposed to Delete, which only works on items with zero history.

---

## Equipment Codes & QR Codes

Equipment Code (`EQ001`, `EQ002`, ...) and QR Code (`QR001`, `QR002`, ...) are now generated automatically by the system when equipment is added — there's no text box to fill in, and no risk of duplicates or typos. If you need to look up an item by these values, they're still shown in the Equipment grid and Detail popup as before; they're just no longer manually editable.

---

## Maintenance Tracking

When you mark equipment as **Under Repair**, the app now prompts you to describe the issue and creates a dedicated maintenance record rather than just changing a status label. When you move the equipment back out of Under Repair, you're prompted to log the resolution and the repair cost. Every maintenance record for an item is visible in its Detail popup, so you get a full repair history over the item's lifetime — not just its current state.

---

## Deleting vs Decommissioning

These are now two distinct actions with different purposes:

| | Delete | Decommission |
|---|---|---|
| Who can do it | Admin only | Admin & Operator |
| What it does | Permanently removes the equipment row from the database | Marks equipment as retired, keeps it in the database |
| History requirement | Blocked if the item has **any** allocation or transaction history | No restriction — this is exactly what it's for |
| Use case | Removing a mistaken/duplicate entry that was never used | Retiring equipment that's actually been in service |

If you try to delete equipment that has history, the app will tell you to decommission it instead.

---

## Barcode Scanner Hardware

*(unchanged — Honeywell Xenon 1900GSR-2 / Datalogic QuickScan QD2500 recommendation, keyboard wedge mode explanation)*

---

## Excel Import Format

*(unchanged column format for now — flag if you'd like the new fields — Category, Vendor Name, Condition Grade, Warranty Expiry — added as optional import columns too)*

---

## Barcode Label Printing

*(unchanged)*

---

## Reports

*(unchanged)*

---

## Dashboard Charts

*(unchanged)*

---

## Audit Logging

*(unchanged, but see Known Limitations regarding the Returns screen)*

---

## Settings

*(unchanged — General, Users, Change Password, Printing, Categories)*

---

## Fuzzy Search

*(unchanged)*

---

## Troubleshooting

*(carry over previous entries, plus:)*

### "Delete Failed" with an EF/association error, or "cannot be permanently deleted" message
This is expected behavior, not a bug — equipment with any allocation or transaction history can't be permanently deleted, to protect the audit trail. Use **Decommission** instead.

### App icon doesn't show on the built .exe after rebuilding
Windows caches icons aggressively. Move the `.exe` to a different folder once, or restart `explorer.exe`, to force a refresh.

---

## Known Limitations

- **Plain text passwords** — unchanged, still recommended to hash before production use.
- **Returns screen bypasses the audit log** — processing a return via the Returns screen does not currently write an `allocation_returned` log entry (Allocation → Deallocate does). Use Allocation's Deallocate action if a full audit trail matters for that return.
- **Categories are not database-backed** — stored in a local flat file; lost if the file is deleted or the app is moved without it.
- **No pagination** — all grids load every record at once.
- **Single database connection** — connection string is hardcoded in `InventoryDbContext.cs`.
- **Windows only, single machine** — as before.

---

## Future Improvements

- Hash passwords (BCrypt) before any real deployment
- Route Returns-screen processing through the same audit-logged service as Allocation Deallocate
- Move connection string to a config file
- Add pagination to large grids
- Wire up the currently-unused `ReportService.cs` or remove it
- Convert Categories to a proper database table

---

## Built With

Developed by **Syed Shayan Haider Kazmi** as part of a PSEB apprenticeship program at **QuantumRonics, Islamabad**.

---

## License

This project is for educational and internal organizational use.
All rights reserved © 2026 QuantumRonics / Syed Shayan Haider Kazmi.
