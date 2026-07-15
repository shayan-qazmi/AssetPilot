# Q-Inventory — Application Standard Operating Procedure (SOP)

> **Ground-truth document** — derived exclusively from source code analysis of every file  
> under `Forms/`, `Services/`, `Models/`, and `Helpers/`.  
> Last generated: 2026-07-15

---

## Table of Contents

1. [Overview](#1-overview)
2. [Module-by-Module Breakdown](#2-module-by-module-breakdown)
   - 2.1 [Login](#21-login)
   - 2.2 [Dashboard](#22-dashboard)
   - 2.3 [Equipment](#23-equipment)
   - 2.4 [Employees](#24-employees)
   - 2.5 [Locations](#25-locations)
   - 2.6 [Allocation](#26-allocation)
   - 2.7 [Returns](#27-returns)
   - 2.8 [Transfer](#28-transfer)
   - 2.9 [Decommission](#29-decommission)
   - 2.10 [Audit Logs](#210-audit-logs)
   - 2.11 [QR Scanner](#211-qr-scanner)
   - 2.12 [Check In / Check Out](#212-check-in--check-out)
   - 2.13 [Reports](#213-reports)
   - 2.14 [Settings](#214-settings)
3. [Data Model Reference](#3-data-model-reference)
4. [Key Workflows — Step-by-Step](#4-key-workflows--step-by-step)
5. [Role-Based Access](#5-role-based-access)
6. [Known Limitations & Edge Cases](#6-known-limitations--edge-cases)

---

## 1. Overview

**Q-Inventory** (branded "Inventory Management System") is a Windows desktop application (WinForms, .NET/EF Core, MySQL backend) for tracking physical IT/office equipment across a company. It covers the full equipment lifecycle: procurement → storage → allocation → return/transfer → decommission.

### Users & Roles

There are two roles in the system, stored in the `roles` table and linked to users via `user_roles`:

| Role | Description |
|---|---|
| **Admin** | Full access to all screens and actions including user management, settings, and delete operations. |
| **Operator** | Limited access. Cannot access Settings, cannot delete equipment, and some destructive actions are hidden. |

The logged-in user's identity and role are cached for the session in `AuthService` (a static singleton). Every form that enforces role-based restrictions reads from `AuthService.CurrentRole` and `AuthService.CurrentUserId`.

---

## 2. Module-by-Module Breakdown

---

### 2.1 Login

**File:** `Forms/LoginForm.cs`  
**Purpose:** Authenticates users and establishes the session.

#### UI Elements

| Control | Description |
|---|---|
| `txtUsername` | Username text box |
| `txtPassword` | Password text box (masked by default) |
| `lblShowHidePassword` | Click to toggle password visibility |
| `pnlToggle` | Custom-painted pill toggle for "Keep me logged in" |
| `btnLogin` | Submits credentials |
| `btnCancel` | Calls `Application.Exit()` — closes the entire app |
| `lblError` | Inline red error label |

#### Login Flow (btnLogin_Click)

1. Validate that both `txtUsername` and `txtPassword` are non-empty; show error if not.
2. Query `Users` table: `WHERE Username = @username AND IsActive = true`.
3. If no user found → show `"Invalid username or password."` (same message for not-found and wrong-password to avoid username enumeration).
4. Compare `user.PasswordHash == password` (plain-text comparison — passwords are stored in plain text).
5. On success: call `AuthService.SetCurrentUser(user)` to cache the session.
6. If "Keep me logged in" is ON, write `remember_me.txt` to the app's startup directory with the username.
7. Set `DialogResult = OK` and close; the caller opens `DashboardForm`.

#### Validation Rules

- Both fields must be non-empty.
- The user account must exist AND have `IsActive = true`.
- Password must match the stored `PasswordHash` exactly (case-sensitive, plain text).

#### Database Read

- Table: `users` — `WHERE Username = @username AND IsActive = 1`

#### "Keep Me Logged In"

- Stored in `{Application.StartupPath}/remember_me.txt`
- Format: line 1 = `"True"`, line 2 = username
- On next launch, if the file exists and line 1 is `"True"`, the username is pre-filled and focus goes to the password field.
- If toggled OFF on login, the file is deleted.

#### Enter Key

`ProcessCmdKey` intercepts the `Enter` key and triggers `btnLogin_Click`.

---

### 2.2 Dashboard

**File:** `Forms/DashboardForm.cs`  
**Purpose:** Main application shell. Hosts the sidebar navigation, renders summary statistics cards, and displays live charts.

#### Layout

- Left sidebar contains navigation buttons for each module.
- Right content area swaps in the active child form panel.
- Top toolbar shows the logged-in user's name and role.

#### Summary Cards

On load, queries the database and renders four animated bounce-in summary cards:

| Card | Query | What It Shows |
|---|---|---|
| Total Equipment | `COUNT(*) FROM equipment` | Total equipment records |
| Available (In Store) | `WHERE CurrentStatus = 'in_store'` | Count ready for allocation |
| Allocated | `WHERE CurrentStatus = 'allocated'` | Count currently allocated |
| Under Repair | `WHERE CurrentStatus = 'under_repair'` | Count in maintenance |

#### Charts (All custom GDI+, no third-party charting library)

| Chart | Type | What It Shows |
|---|---|---|
| Status Breakdown | Donut/Pie | Proportion of equipment in each status |
| Category Breakdown | Horizontal Bar | Equipment count per category |
| 6-Month Allocation Trend | Line Chart | New allocations per month for last 6 months |

#### Role-Based Navigation

- **Admin**: All buttons visible.
- **Operator**: The **Settings** button is hidden entirely.

---

### 2.3 Equipment

**Files:** `Forms/EquipmentForm.cs`, `Forms/AddEditEquipmentForm.cs`, `Forms/EquipmentDetailPopup.cs`  
**Service:** `Services/EquipmentService.cs`

#### EquipmentForm — Equipment Grid

| Button/Control | Action |
|---|---|
| Search box | Fuzzy-search across code, name, make, model, serial number, status |
| Category dropdown | Filter grid to a specific category or "All Categories" |
| Add Equipment | Opens `AddEditEquipmentForm` (add mode) |
| Edit | Opens `AddEditEquipmentForm` pre-populated (edit mode) |
| View Details | Opens `EquipmentDetailPopup` for selected item |
| Delete | Calls `EquipmentService.Delete(id)`. Blocked if any allocation or transaction history exists. Hidden for Operators. |
| Export | Exports visible grid to CSV |

**Grid columns:** `Equipment Code | Name | Category | Status | Make | Model | Serial Number | Procurement`

Status column shows human-friendly display names and is color-coded:
- green = in_store, yellow = allocated, orange = under_repair, red = decommissioned

#### AddEditEquipmentForm — Add / Edit Equipment

**Fields:**

| Field | Validation |
|---|---|
| Equipment Name | Required |
| Make, Model, Serial Number | Optional |
| Category | Dropdown from `CategoryStore.GetAll()` |
| Procurement Type | Dropdown from `procurement_types` table |
| Current Status | Dropdown (display names map to raw DB enum values) |
| Notes | Optional multiline |

**Status Transition Side-Effects (`CmbStatus_SelectedIndexChanged`):**

- **Entering "Under Repair":** Opens `MaintenanceForm` dialog. On OK: `MaintenanceService.OpenMaintenanceRecord()` creates a new record with `Status = "open"`. Equipment → `"under_repair"`. On cancel: dropdown reverts.
- **Leaving "Under Repair"** (when DB status was `under_repair`): Opens `ResolveMaintenanceForm` dialog. On OK: `MaintenanceService.ResolveMaintenanceRecord()` resolves the open record with notes and cost. On cancel: dropdown reverts to `"under_repair"`.

**Save:**
- **Add mode:** `EquipmentService.Add()` — auto-generates `EquipmentCode` (e.g. `EQ007`) and `QRCode` (e.g. `QR007`). Writes `equipment_created` audit log.
- **Edit mode:** `EquipmentService.Update()`. If location changed, writes `location_changed` audit log.

**Delete guard (EquipmentService.Delete):**  
Blocks if any `Allocation` or `Transaction` record exists for the equipment — even closed/returned ones.

#### EquipmentDetailPopup — Read-Only Detail View

- Full equipment info + active allocation + complete maintenance history.
- Maintenance records labeled "Record #1", "Record #2", ... (1-based loop index).
- Cost formatted as `₨{Cost:N2}` (e.g. `₨14.00`). No cost → `"—"`.

---

### 2.4 Employees

**File:** `Forms/EmployeesForm.cs` | **Service:** `Services/EmployeeService.cs`

| Button | Action |
|---|---|
| Add Employee | Opens `AddEditEmployeeForm` (add mode) |
| Edit | Opens `AddEditEmployeeForm` pre-filled |
| Delete | `EmployeeService.Delete(id)`. Blocked if any `approved` allocation exists for the employee. |
| Export CSV | Exports visible grid rows to CSV |
| Search box | Fuzzy-filters by name, department, designation |

**Validation (AddEditEmployeeForm):** Full Name required. Email optional but must be valid format if provided.

**Database:** Reads/writes `employees`. Delete guard checks `allocations WHERE EmployeeId = @id AND Status = 'approved'`.

---

### 2.5 Locations

**File:** `Forms/LocationsForm.cs` | **Service:** `Services/LocationService.cs`

| Button | Action |
|---|---|
| Add Location | Opens `AddEditLocationForm` (add mode) |
| Edit | Opens `AddEditLocationForm` for selected location |
| Delete | `LocationService.Delete(id)`. Blocked if any `approved` allocation references this location. |

**Validation:** Location Name required.

> **Note:** No search/filter bar — it was removed as no filtering logic was ever implemented.

---

### 2.6 Allocation

**File:** `Forms/AllocationForm.cs` | **Service:** `Services/AllocationService.cs`

#### Three Sections

**New Allocation:**

1. Select equipment (only `in_store` items shown).
2. Select employee, location, dates.
3. Click Create → `AllocationService.Allocate()` → `Allocation.Status = "pending"`. Audit: `allocation_requested`.

**Approve/Reject Pending:**

- Approve: `AllocationService.Approve()` → `Allocation.Status = "approved"`, `Equipment.CurrentStatus = "allocated"`. Audit: `allocation_approved`.
- Reject: `AllocationService.Reject()` → `Allocation.Status = "rejected"`. Equipment status unchanged. Audit: `allocation_rejected`.

**Active Allocations / Deallocate:**

- Deallocate: `AllocationService.Deallocate()` → `Allocation.Status = "returned"`, `ActualReturnDate = today`, `Equipment.CurrentStatus = "in_store"`. Audit: `allocation_returned`.

**Validation:** Equipment, employee, location, and allocation date all required. Equipment must be `in_store`.

---

### 2.7 Returns

**File:** `Forms/ReturnsForm.cs`

> **Architecture Note:** This form writes directly to `InventoryDbContext` inline — it does NOT call `AllocationService.Deallocate()`. As a result, **no** `allocation_returned` audit log entry is written when processing returns through this screen.

**Flow:**
1. Grid shows `Status = "approved"` allocations.
2. Select row → click Process Return.
3. Sets `Allocation.Status = "returned"`, `ActualReturnDate = today`, `Equipment.CurrentStatus = "in_store"`.

---

### 2.8 Transfer

**File:** `Forms/TransferForm.cs`

**Flow:**
1. Select active (`approved`) allocation to transfer.
2. Select new employee and/or location.
3. Click Transfer:
   - Old allocation: `Status = "returned"`, `ActualReturnDate = today`.
   - New allocation created: `Status = "approved"`, same equipment, new employee/location.
   - Equipment stays `"allocated"`.
   - Audit: `transferred`.

---

### 2.9 Decommission

**File:** `Forms/DecommissionForm.cs`

- Grid only shows `in_store` and `under_repair` equipment.
- On confirm:
  - `Equipment.CurrentStatus = "decommissioned"`.
  - Any `approved` allocations for this equipment are automatically closed.
  - Audit: `decommission_approved`.

**Rule:** `"allocated"` equipment cannot be decommissioned — must be returned first.

---

### 2.10 Audit Logs

**File:** `Forms/LogsForm.cs` | **Service:** `Services/AuditService.cs`

**Grid:** `Log ID | Equipment Code | Action | Changed By | Previous Value | New Value | Timestamp`

| Button | Action |
|---|---|
| Refresh | Clears search and reloads all logs newest-first |
| Export CSV | Saves displayed rows to `AuditLogs_{yyyyMMdd_HHmm}.csv` |
| Search box | Fuzzy-filters all columns; results re-ranked by fuzzy score |

**Action types and color-coding:**

| DB Value | Display | Color |
|---|---|---|
| `equipment_created` | Equipment Created | Green |
| `allocation_requested` / `allocation_approved` | Allocation Requested/Approved | Blue |
| `allocation_returned` | Returned to Store | Yellow |
| `allocation_rejected` / `decommission_*` | Rejected/Decommissioned | Red |
| `transferred` | Transferred | Purple |
| `location_changed` | Location Changed | Orange |

**Database:** Reads `equipment_logs` with `Include(Equipment)` and `Include(ChangedByUser)`, ordered `CreatedAt DESC`. Never writes directly — all writes go through `AuditService`.

---

### 2.11 QR Scanner

**Files:** `Forms/QRScannerForm.cs`, `Forms/QRScannerForm.Designer.cs`

> **Architecture Warning:** Logic is in `.Designer.cs`, controls are in `.cs` — reversed from standard WinForms convention.

**Purpose:** Scan or manually enter a QR/barcode to look up equipment.  
**Flow:** Enter code → query `equipment.QRCode` → display details → optionally open `EquipmentDetailPopup`.

---

### 2.12 Check In / Check Out

**File:** `Forms/CheckInOutForm.cs` | **Service:** `Services/TransactionService.cs`

**Distinction from Allocation:** Creates `Transaction` records for short-term temporary borrows; distinct from formal `Allocation` records.

**Check Out:** `TransactionService.CheckOut(equipmentCode, operatorName)` — verifies no active transaction exists, creates `Transaction (Status = "active")`, sets `Equipment.CurrentStatus = "allocated"`.

**Check In:** `TransactionService.CheckIn(equipmentCode)` — finds the active transaction, sets `Status = "completed"`, `CheckInTime = now`, sets `Equipment.CurrentStatus = "in_store"`.

Operator name pre-populated from `AuthService.CurrentUser.FullName`.

---

### 2.13 Reports

**File:** `Forms/ReportsForm.cs`  

> **Note:** `Services/ReportService.cs` exists but is never used. `ReportsForm` calls `EquipmentService`, `EmployeeService`, and `AllocationService` directly.

| Button | Report | Data / Columns |
|---|---|---|
| Equipment Report | Equipment Inventory | Code, Name, Make, Model, Serial, Status, Location (from active allocation) |
| Employee Report | Employee Allocation Summary | Employee, Dept, Items Currently Held, Total Ever Allocated. Skips employees with zero history. |
| Allocation Report | Active Allocations | Code, Name, Allocated To, Location, Dates, Status. Includes `pending` AND `approved`. |
| Returns Report | Returns History | Code, Name, Returned By, Allocated On, Return Date, Duration (days). Warns if >30 days. |

**Export Excel:** Uses ClosedXML → `.xlsx` with bold green header row, auto-sized columns.  
**Export PDF:** Uses `PrintDocument` + `PrintPreviewDialog` (Windows built-in) — no extra NuGet package. Multi-page supported.

---

### 2.14 Settings

**File:** `Forms/SettingsForm.cs` | **Helper:** `Helpers/CategoryStore.cs`

> **Architecture Note:** Entirely code-built — no Designer layout. All panels created in `BuildUI()`. Left sidebar slides in on load via a timer animation.

#### General Settings (Table: `app_settings`)

| Field | Validation | Action |
|---|---|---|
| Company Name | Required, non-empty | Saved to `AppSettings.CompanyName` |
| Save button | Company name required; label dimensions must be positive integers | Writes to `app_settings` (creates row if missing) |
| Reset button | — | Restores fields from last saved `_current` snapshot |

Defaults on first run: Company Name = `"Q-Inventory"`, Label 62mm × 29mm.

#### Users Management (Tables: `users`, `roles`, `user_roles`)

| Button | Action |
|---|---|
| Add User | Opens `AddEditUserForm` (add mode) |
| Edit | Opens `AddEditUserForm` pre-filled |
| Toggle Active | Flips `IsActive`. Cannot deactivate own account. Requires confirmation. |
| Delete User | Removes from `users` + `user_roles`. Cannot delete own account. FK violations caught and shown as error. |

Inactive users displayed in grey.

#### Change Password

Changes password for the **currently logged-in user only**.

| Rule | Detail |
|---|---|
| All fields required | Current, new, confirm |
| New == Confirm | Must match exactly |
| Minimum length | At least 4 characters |
| Current password verified | Plain-text comparison against `Users.PasswordHash` |

On success: fields cleared, green success message shown.

#### Printing (Table: `app_settings`)

| Field | Default | Detail |
|---|---|---|
| Printer Name | `""` (default printer) | Target printer for barcode labels |
| Label Width (mm) | 62 | Label stock width |
| Label Height (mm) | 29 | Label stock height |
| Test Print Label | — | Sends a test label to configured printer |
| Save | — | Writes all printing settings to `app_settings` |

#### Categories (Backed by `Helpers/CategoryStore.cs` — flat file, NOT a DB table)

| Control | Action |
|---|---|
| New Category text box | Enter or press Enter to add |
| Add button | `CategoryStore.Add(name)` — validates non-empty and no duplicate |
| Categories list | Owner-drawn, shows numbered items |
| Delete Selected | `CategoryStore.Delete(name)` after confirmation |

---

## 3. Data Model Reference

### All Tables

| C# DbSet | DB Table | Notes |
|---|---|---|
| `Allocations` | `allocations` | Equipment → Employee → Location |
| `Employees` | `employees` | — |
| `Equipment` | `equipment` | `CurrentStatus` is MySQL ENUM |
| `EquipmentLocationHistories` | `equipment_location_histories` | Location change history |
| `EquipmentLogs` | `equipment_logs` | Audit log |
| `Locations` | `locations` | — |
| `ProcurementTypes` | `procurement_types` | Lookup |
| `Roles` | `roles` | — |
| `Users` | `users` | `PasswordHash` = plain text |
| `UserRoles` | `user_roles` | Junction: UserId + RoleId |
| `AppSettings` | `app_settings` | Single row expected |
| `Transactions` | `transactions` | Check In/Out records |
| `MaintenanceRecords` | `maintenance_records` | Repair history |

### Equipment Status Lifecycle

```
in_store      -->  allocated       (Allocation approved)
in_store      -->  under_repair    (Edit: status changed to Under Repair)
in_store      -->  decommissioned  (Decommission)
under_repair  -->  in_store        (Maintenance resolved)
under_repair  -->  decommissioned  (Decommission)
allocated     -->  in_store        (Return / Deallocate)
allocated     -->  allocated       (Transfer — status unchanged, new allocation row)
```

Raw DB ENUM values (never change — used in string comparisons throughout codebase):

| DB Value | UI Display |
|---|---|
| `in_store` | In Store |
| `allocated` | Allocated |
| `under_repair` | Under Repair |
| `decommissioned` | Decommissioned |

### Key Relationships

```
Equipment  (1) --< Allocations          (N)  [EquipmentId FK]
Employee   (1) --< Allocations          (N)  [EmployeeId FK]
Location   (1) --< Allocations          (N)  [LocationId FK]
Equipment  (1) --< MaintenanceRecords   (N)  [EquipmentId FK]
Equipment  (1) --< EquipmentLogs        (N)  [EquipmentId FK]
Equipment  (1) --< Transactions         (N)  [EquipmentId FK]
Users      (1) --< EquipmentLogs        (N)  [ChangedByUserId FK]
Users      (1) --< UserRoles            (N)  [UserId FK]
Roles      (1) --< UserRoles            (N)  [RoleId FK]
```

### Delete Guards

| Action | Guard |
|---|---|
| Delete Equipment | Blocked if ANY Allocation or Transaction record exists (any status) |
| Delete Employee | Blocked if any `approved` Allocation exists |
| Delete Location | Blocked if any `approved` Allocation references it |
| Delete User | FK error from DB if referenced elsewhere (not pre-checked) |
| Decommission | Closes any active (approved) allocations automatically |

---

## 4. Key Workflows — Step-by-Step

### Add New Equipment

1. Equipment → Add Equipment.
2. Fill Name (required), optional Make/Model/Serial.
3. Select Category (from CategoryStore) and Procurement Type.
4. Status defaults to "In Store".
5. Save → `EquipmentService.Add()` generates `EquipmentCode` (EQ001...) and `QRCode` (QR001...).
6. Audit log: `equipment_created`.

### Allocate Equipment

1. Allocation → New Allocation tab.
2. Select `in_store` equipment, employee, location, dates.
3. Create → `Status = "pending"`. Audit: `allocation_requested`.
4. Approve tab → Approve → `Status = "approved"`, equipment → `"allocated"`. Audit: `allocation_approved`.

### Return Equipment

**Via Returns screen:** Select approved allocation → Process Return → `Status = "returned"`, equipment → `"in_store"`. *(No audit log written — see Limitation #2)*

**Via Allocation → Active Allocations:** Select → Deallocate → same result + audit log written.

### Transfer Equipment

1. Transfer → select active allocation → pick new employee/location → Transfer.
2. Old allocation closed (`returned`), new created (`approved`). Equipment stays `"allocated"`. Audit: `transferred`.

### Send for Repair

1. Equipment → Edit → change Status to "Under Repair".
2. `MaintenanceForm` dialog → enter issue description → OK.
3. `MaintenanceRecord` created (`open`). Equipment → `"under_repair"`.

### Resolve Maintenance

1. Equipment → Edit on `under_repair` item → change Status away from "Under Repair".
2. `ResolveMaintenanceForm` → enter resolution + cost → OK.
3. `MaintenanceRecord` resolved. Equipment status updated.

### Decommission

1. Decommission → select `in_store` or `under_repair` item → Decommission → confirm.
2. Equipment → `"decommissioned"`. Active allocations auto-closed. Audit: `decommission_approved`.

### Delete Equipment (Admin only)

1. Equipment → select item → Delete (Admin only button).
2. `EquipmentService.Delete()` — blocked if any allocation or transaction history exists.

### Check Out / Check In

1. Check In/Out → enter equipment code → Check Out. Transaction created, equipment → `"allocated"`.
2. Same screen → enter code → Check In. Transaction completed, equipment → `"in_store"`.

---

## 5. Role-Based Access

| Feature / Action | Admin | Operator |
|---|---|---|
| View all screens | Yes | Yes (except Settings) |
| Access Settings screen | Yes | No — button hidden |
| Add / Edit Equipment | Yes | Yes |
| Delete Equipment | Yes | No — button hidden |
| Add / Edit / Delete Employees | Yes | Yes |
| Add / Edit / Delete Locations | Yes | Yes |
| Create / Approve / Reject Allocations | Yes | Yes |
| Process Returns | Yes | Yes |
| Transfer Equipment | Yes | Yes |
| Decommission Equipment | Yes | Yes |
| View Audit Logs | Yes | Yes |
| Export Logs / Reports | Yes | Yes |
| Manage Users (Settings) | Yes | No |
| Change Own Password (Settings) | Yes | No (no Settings access) |
| Manage Categories | Yes | No |
| Manage Printing Settings | Yes | No |

> Role is determined at login: `AuthService.SetCurrentUser(user)` caches role from `user_roles` + `roles` lookup. All forms read `AuthService.CurrentRole`.

---

## 6. Known Limitations & Edge Cases

| # | Issue | Detail |
|---|---|---|
| 1 | **Passwords stored in plain text** | `Users.PasswordHash` stores raw passwords. No hashing (bcrypt, SHA, etc.). All auth is plain-string equality. |
| 2 | **ReturnsForm bypasses service layer** | `ReturnsForm` writes directly to `InventoryDbContext`. No `AuditService` call, so no `allocation_returned` log is written when using this screen. |
| 3 | **ReportService is unused** | `Services/ReportService.cs` exists but is never called. `ReportsForm` uses other services directly. |
| 4 | **QRScannerForm file layout reversed** | Logic in `.Designer.cs`, controls in `.cs` — opposite of standard WinForms convention. |
| 5 | **Categories not database-backed** | `CategoryStore` persists to a local flat file, not a DB table. Data is lost if the file is deleted or app is moved. |
| 6 | **Employee report skips employees with no allocation history** | `if (totalCount == 0) continue;` — employees who have never had an allocation are completely omitted. |
| 7 | **Delete User has no FK pre-check** | User + user_roles rows are removed, but no pre-check for allocations/logs/transactions references. DB FK error is caught and displayed if violated. |
| 8 | **Single AppSetting row assumption** | `db.AppSettings.FirstOrDefault()` — only the first row is loaded. Multiple rows (from manual DB edits) cause silent data inconsistency. |
| 9 | **"Remember Me" stores username in plain text** | `remember_me.txt` in app startup directory. Password is never stored. |
| 10 | **Status transition guards only in Edit form** | Maintenance open/resolve dialogs only trigger in `AddEditEquipmentForm`. Status changes via other code paths bypass the dialog flow. |
| 11 | **Allocation Report includes "pending" items** | Labelled "Active Allocations Report" but also includes `pending` allocations, which may confuse users. |
| 12 | **Returns Report duration uses today if return date is null** | `ActualReturnDate` null → duration calculated to today, producing large/misleading numbers for never-returned items. |
| 13 | **No grid pagination** | All grids load every record at once. Large datasets may be slow or unresponsive. |
| 14 | **"Keep Me Logged In" only pre-fills username** | The toggle pre-fills the username field only. The user must still type their password — authentication is not bypassed. |
