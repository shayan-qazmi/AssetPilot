# Q-Inventory — Application Standard Operating Procedure (SOP)

> **Ground-truth document** — every claim is traced from source code:
> UI form event handler → service method → EF Core / DbContext → database table + column.
> No assumption-based descriptions. Source file + method cited per section.
> Last updated: 2026-08-17

**Tech Stack:** WinForms · C# · .NET 8 · Entity Framework Core · MariaDB
**Database:** `inventory_management` @ `192.168.18.240`

---

## Table of Contents

1. [Overview & Roles](#1-overview--roles)
2. [Login](#2-login)
3. [Dashboard](#3-dashboard)
4. [Equipment](#4-equipment)
5. [Employees](#5-employees)
6. [Locations](#6-locations)
7. [Allocation](#7-allocation)
8. [Returns](#8-returns)
9. [Transfer](#9-transfer)
10. [Decommission](#10-decommission)
11. [Deleted Items](#11-deleted-items)
12. [Logs (Audit Trail)](#12-logs-audit-trail)
13. [QR Scanner](#13-qr-scanner)
14. [Check In / Out](#14-check-in--out)
15. [Reports](#15-reports)
16. [Settings](#16-settings)
17. [Data Model Reference](#17-data-model-reference)
18. [Cross-Module Effects Summary](#18-cross-module-effects-summary)
19. [Known Issues & Observations](#19-known-issues--observations)
20. [New Features & Recent Changes (2026-08-10)](#20-new-features--recent-changes-2026-08-10)
21. [New Features & Recent Changes (2026-08-17)](#21-new-features--recent-changes-2026-08-17)

---

## 1. Overview & Roles

**Q-Inventory** is a Windows desktop application for tracking physical IT/office equipment across a company. It covers the full equipment lifecycle: procurement → storage → allocation → return / transfer → decommission.

Routine success confirmations across the app (save, restore, delete) now render as auto-dismissing toast notifications rather than blocking `MessageBox` dialogs. Destructive-action confirmations ("Are you sure?") still use `MessageBox`.

### Roles

Roles are stored in the `roles` table and linked via `user_roles`. Session identity is cached in `AuthService` (static) for the duration of the session. All forms read `AuthService.CurrentRole` and `AuthService.CurrentUserId`.

| Role | Access |
|------|--------|
| **Admin** | All screens and all actions including delete, Settings, user management |
| **Operator** | All screens **except** Settings. Delete Equipment button is hidden. |

`AdminOnlyButtons` in `DashboardForm` = `{ btnDecommission, btnSettings, btnDeletedItems }` — these are hidden for non-admin roles via `ApplyRolePermissions()`.

---

## 2. Login

**Source:** `Forms/LoginForm.cs`

### Login Flow — `btnLogin_Click`

1. Validates both `txtUsername` and `txtPassword` are non-empty; shows error if not.
2. Queries `db.Users.FirstOrDefault(u => u.Username == username && u.IsActive == true)`. If no match → `"Invalid username or password."` (same message for both not-found and wrong-password to prevent username enumeration).
3. Compares `user.PasswordHash == password` — **plain-text equality. Passwords are not hashed.**
4. On success: calls `AuthService.SetCurrentUser(user)` to cache the session.
5. Calls `SaveRememberedUser(username)` — if `_keepMeLoggedIn` is `true`, writes `{StartupPath}/remember_me.txt` with `["True", username]`. If `false`, deletes the file.
6. Stops the animation timer, sets `DialogResult = OK`, closes.

### "Keep Me Logged In" Toggle

- `pnlToggle` is a custom-painted pill toggle (GDI+). Clicking it flips `_keepMeLoggedIn`.
- `LoadRememberedUser()` (called in constructor): reads `remember_me.txt`. If `lines[0] == "True"`, pre-fills `txtUsername` and moves focus to `txtPassword`. **The password is never stored.** Authentication always requires manual password entry.

### Enter Key

`ProcessCmdKey` intercepts `Keys.Enter` → calls `btnLogin_Click`.

### Database Table Read

- `users` → `WHERE Username = @username AND IsActive = 1`

---

## 3. Dashboard

**Source:** `Forms/DashboardForm.cs`

### Construction

1. `ApplyTheme()` — styles sidebar, top bar, card panels, and builds the dark mode toggle switch.
2. `ApplyRolePermissions()` — hides `btnDecommission`, `btnSettings`, `btnDeletedItems` if role is not Admin.
3. `LoadStats()` — queries DB for summary card values and chart data.
4. `Shown` event re-calls `LoadStats()`.

### Summary Cards — `LoadStats()`

| Card | DB Query |
|------|----------|
| Total Equipment | `COUNT(*) FROM equipment WHERE IsDeleted = false` |
| In Store | `WHERE CurrentStatus = "in_store"` |
| Allocated | `WHERE CurrentStatus = "allocated"` |
| Under Repair | `WHERE CurrentStatus = "under_repair"` |
| Decommissioned | `WHERE CurrentStatus = "decommissioned"` |
| Employees | `COUNT(*) FROM employees` |
| Overdue | Allocations where `Status == "approved"` and `ExpectedReturnDate < today` (in-memory) |

Cards entrance with a bounce animation (`CardAnimState`, driven by `_animTimer` at 15 ms ticks).

### Charts (custom GDI+, no third-party library)

| Chart | Type | Data Source |
|-------|------|-------------|
| Status Breakdown | Donut | `_cntInStore`, `_cntAllocated`, `_cntUnderRepair`, `_cntDecommission` |
| Category Breakdown | Horizontal Bar | Distinct `Category` grouped counts from `equipment` |
| 6-Month Allocation Trend | Line | `allocations` grouped by `AllocationDate` month, last 6 months |

### Navigation

Each sidebar button calls `OpenChildForm(new XxxForm())` which docks the form inside `panelContent`. The title bar label is animated character-by-character via `AnimateTitleTo()`.

### 3.1 Dark Mode Toggle Switch

**Source:** `DashboardForm.BuildThemeToggle()`, `ThemeHelper.ToggleTheme()`, `ThemeHelper.LoadTheme()`

A pill-shaped animated toggle switch is embedded in the **top bar**, to the left of the notification bell. It renders as a 60×28 GDI+ custom panel:

- **Light side (left):** ☀️ sun icon in amber.
- **Dark side (right):** 🌙 moon icon in slate-blue.
- **Animated thumb:** A white circle slides smoothly across the pill track using `_toggleThumbProgress` (0.0 = light, 1.0 = dark) animated at 12 ms ticks with a step of `0.18` per tick.
- **Track colour:** Light mode → muted green (`#D6E0C8`); Dark mode → near-black (`#2D3037`).

**Toggle behaviour (`ToggleThemeAnimated`):**
1. Calls `ThemeHelper.ToggleTheme()` — flips `IsDarkMode`, serializes `{"IsDarkMode": true/false}` to `{AppDir}/theme.json`.
2. Starts the thumb slide animation timer.
3. Calls `SuspendLayout()` → `ApplyTheme()` → `LoadStats(instant: true)` → `ResumeLayout()` to re-paint the entire form in the new palette immediately.

**Theme persistence:** `ThemeHelper.LoadTheme()` is called at app startup (`Program.cs`) to restore the last chosen theme from `theme.json`.

**Dark palette key colours:**

| Element | Dark Value |
|---------|------------|
| Dashboard background | `RGB(18,20,24)` |
| Card background | `RGB(36,39,46)` |
| Sidebar | `RGB(14,16,19)` |
| Top bar | `RGB(45,48,55)` |
| Text primary | `RGB(220,224,232)` |
| Text secondary | `RGB(140,148,160)` |

### 3.2 Notification Bell

**Source:** top bar, all screens.

Shows a live count badge for:
- Overdue allocations (`Allocations WHERE Status == "approved" AND ExpectedReturnDate < today`)
- Open maintenance records (`MaintenanceRecords WHERE Status != "resolved"`)

Dropdown lists both categories with quick links to the relevant equipment.

---

## 4. Equipment

**Source:** `Forms/EquipmentForm.cs`, `Forms/AddEditEquipmentForm.cs`, `Services/EquipmentService.cs`

### 4.1 List View — `EquipmentForm`

**Data load — `LoadEquipment(string searchTerm = "")`**

- **No search:** `EquipmentService.GetPage(_currentPage, _pageSize)` → server-side `SKIP/TAKE` on `equipment WHERE IsDeleted = false` ordered by `EquipmentCode`. Returns `PagedResult<Equipment>`.
- **With search:** `EquipmentService.GetAll()` (full non-deleted table load), then in-memory:
  1. Exact `Contains` match on `QrCode` / `EquipmentCode` first.
  2. If no code match: `FuzzySearchHelper.IsFuzzyMatch` on `Name`, `Make`, `Model`, `SerialNumber`, `Category`.
  3. Ranked by `FuzzyScore`.
- Active category filter (`_activeCategoryFilter`) is applied after search.

**Category filter ComboBox:** Built by unioning `CategoryService.GetAll()` names with distinct `Equipment.Category` values from DB (case-insensitive, alphabetical).

**Grid columns:** `Equipment Code` · `QR Code` · `Procurement Type` · `Name` · `Make` · `Model` · `Serial Number` · `Category` · `Status` · `Condition` · `Location` (includes room name after a hyphen) · `Purchase Date` · `Price`

**Row colour by status:** `decommissioned` → red · `under_repair` → orange · `allocated` → blue · `in_store` → green

**Toolbar actions:**

| Button | Behaviour |
|--------|-----------|
| Add | Opens `AddEditEquipmentForm` (add mode). On OK → `LoadEquipment()`. |
| Edit | Finds employee by `EquipmentCode` from `_allEquipment`, opens `AddEditEquipmentForm` (edit mode). |
| Delete | `EquipmentService.Delete(id)` → sets `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`. Soft delete. **No audit log.** |
| Refresh | Clears search, resets page 1, reloads. |
| Export | Iterates visible grid rows → CSV. |
| Select Mode | "Select ✔" shows per-row checkbox; "Select All" calls `SelectAll()`. |

### 4.2 Add / Edit — `AddEditEquipmentForm` + `EquipmentService`

**Equipment Code & QR Code auto-generation (`EquipmentService.Add`):**
- If blank: `GenerateEquipmentCode(db)` finds max `EQ###` suffix, increments, loops until unique in `equipment`.
- Same pattern for `QrCode` with prefix `QR`.

**Fields → `equipment` columns:**

| UI Control | DB Column |
|-----------|-----------|
| `txtName` | `name` (required) |
| `txtMake` / `txtModel` | `make` / `model` |
| `txtSerialNumber` | `serial_number` |
| `cmbStatus` | `current_status` |
| `dtpPurchaseDate` | `purchase_date` |
| `cmbCategory` | `category` |
| `txtDescription` | `description` |
| `txtVendorName` | `vendor_name` |
| `cmbConditionGrade` | `condition_grade` |
| `chkWarrantyExpiry` + `dtpWarrantyExpiry` | `warranty_expiry` (null if unchecked) |
| `txtNotes` | `notes` |
| Procurement type text box | `procurement_type_id` (resolved via `ResolveProcurementTypeId`) |
| Picture staging | `picture_path` |

**`ResolveProcurementTypeId(string typeName)`:** If blank → null. Else queries `procurement_types` case-insensitively. If not found → inserts new row, returns new id.

**Status side-effects (`CmbStatus_SelectedIndexChanged`, suppressed during load):**
- → "Under Repair": Shows `MaintenanceRecordDialog`. On OK: inserts `MaintenanceRecord { Status = "open", StartDate = UtcNow }` into `maintenance_records`.
- Away from "Under Repair" (when DB was `under_repair`): finds open `MaintenanceRecord`, sets `Status = "completed"`, `EndDate = UtcNow`, saves.


### 4.3 Maintenance Service

**Source:** `Services/MaintenanceService.cs`

- `GetByEquipmentId(int equipmentId)`: Returns a list of maintenance records for the given equipment, ordered by newest first (`ReportedDate DESC`, `CreatedAt DESC`).
- `OpenRecord(int equipmentId, string issueDescription, string? servicedBy, decimal? cost)`: Validates the description, creates a new `MaintenanceRecord` with status `open`, sets `ReportedDate` to today, saves, and **writes an audit log**: `AuditService.Log(equipmentId, "maintenance_opened", "Status: In Store", "Issue: ... | Serviced By: ... | Est. Cost: ...")` → `equipment_logs`.
- `ResolveRecord(int recordId, DateOnly resolvedDate, decimal? finalCost)`: Marks the record as `resolved`, sets `ResolvedDate`, optionally updates cost, and **writes an audit log**: `AuditService.Log(equipmentId, "maintenance_resolved", "Status: Under Repair | Issue: ...", "Status: In Store | Resolved: ... | Final Cost: ...")` → `equipment_logs`.
- `GetOpenRecordForEquipment(int equipmentId)`: Retrieves the most recent record with status `open` or `in_progress` for the equipment.

These methods are consumed by the **Maintenance Record Dialog** shown when the equipment status changes to **Under Repair** or when exiting that status.

### 4.4 Equipment Detail Popup — `EquipmentDetailPopup`

**Source:** `Forms/EquipmentDetailPopup.cs`

Opened by double-clicking any row in the Equipment grid. Displays a two-column card popup:

**Left column — Equipment Information:**
`Equipment Code` · `QR Code` · `Name` · `Make` · `Model` · `Serial Number` · `Category` · `Status` (colour-coded badge) · `Condition` · `Purchase Date` · `Purchase Price` · `Procurement Type` · `Vendor Name` · `Warranty Expiry` · `Description` · `Notes` · `Picture` ("View Picture" button)

**Left column action buttons row (bottom of left column):**
- **📜 View Logs** — closes the popup and navigates the Dashboard to the Logs screen pre-filtered to the equipment's code via `dashboard.NavigateToLogs(equipmentCode)`.
- **🗑 Clear History** — permanently deletes all `MaintenanceRecord` rows for this equipment. Disabled (greyed) if no records exist. On confirm: `db.MaintenanceRecords.RemoveRange(...)` + `db.SaveChanges()`, then writes `AuditService.Log(equipmentId, "Maintenance History Cleared", "{N} record(s)", "0 records")` → `equipment_logs`. The popup card is rebuilt in-place after clearing via `RefreshCardContent()`.

**Right column — Current Allocation:**
`Current Location` · `Currently Assigned To` (employee name — clickable link that navigates to EmployeesForm pre-filtered to that employee's code) · `Allocation Date` · `Expected Return Date`

**Right column — Maintenance History (scrollable panel):**
Calls `MaintenanceService.GetByEquipmentId(_equipment.Id)` on popup construction. If no records: displays "No maintenance history recorded." Otherwise, lists each record in a scrollable `AutoScroll` panel with height capped to the left column's extent. Each record shows: `Record #N` (reported date) · `Status` (colour-coded badge: `open`=red/orange, `in_progress`=amber, `resolved`=green) · `Issue` · `Serviced By` · `Cost` (₨ formatted) · `Resolved` (date).

The dialog auto-sizes and scrolls if content exceeds the screen working area. A vertical separator divides the two columns.

**Save path (Equipment Add/Edit):**
- Add: `db.Equipment.Add(equipment)` + `db.SaveChanges()`, then `AuditService.Log(id, "equipment_created", null, code)` → `equipment_logs`.
- Edit: loads entity, updates all fields, `db.SaveChanges()`, then `AuditService.Log(id, "equipment_updated", oldSummary, newSummary)` → `equipment_logs`. The `oldSummary` and `newSummary` are produced by `AuditService.BuildEquipmentDiff(before, after)` which compares all fields field-by-field and emits only the changed ones as `"Field: value"` pipe-separated strings.
- `CreatedByUserId` is hardcoded to `1` in add mode. The logged-in user id is not used.

**Picture lifecycle:** Staged path stored; on save, old file deleted and new path persisted. On form close (cancel), staged file is deleted.

---

## 5. Employees

**Source:** `Forms/EmployeesForm.cs`, `Services/EmployeeService.cs`

### 5.1 List View

**Data load — `LoadEmployees(string searchTerm = "")`**
- No search: `EmployeeService.GetPage(_currentPage, _pageSize)` → server-side `SKIP/TAKE` on `employees` ordered by `FullName`.
- With search: `EmployeeService.GetAll()` → in-memory code-first / fuzzy across `FullName`, `Department`, `Email`, `Phone`.

**Grid columns:** `Employee Code` · `Full Name` · `Designation` · `Email` · `Phone` · `Status` (Active/Inactive)

**Actions:**

| Button | Behaviour |
|--------|-----------|
| Add | Opens `AddEditEmployeeForm`. On OK → reload. |
| Edit | Calls `GetAll()`, finds by `EmployeeCode`, opens `AddEditEmployeeForm(emp)`. |
| Delete | Confirms → `EmployeeService.Delete(emp.Id)`. |
| Export | CSV of current page's grid. |
| Import | CSV import of employees. |

**Double-click row:** Opens `EmployeeDetailPopup` showing a card with the employee's avatar (circle crop, default grey silhouette if no picture), `Employee Code`, `Designation`, `Email`, `Phone`, and `Status` badge. Profile picture is loaded via `AvatarHelper.TryLoadUserImage(PicturePath)`.

### 5.3 Add/Edit Employee Form — `AddEditEmployeeForm`

Fields: `Full Name` (required) · `Designation` · `Email` · `Phone` · `Profile Picture` (Upload/Remove buttons, optional) · `Is Active` checkbox.

Profile picture upload copies the file to the network share (`NetworkPaths.EmployeePicturesDir`) via `NetworkShareHelper.EnsureConnected()`. The filename saved is `{EmployeeCode}_{GUID}{ext}`. On cancel/close, staged pictures are discarded.

### 5.2 Service Methods — `EmployeeService`

**`Add(employee)`:**
1. Validates `FullName` non-empty.
2. Auto-generates `EmployeeCode` (`EMP###`) if blank.
3. Checks code uniqueness against `employees`.
4. Checks `Email` uniqueness if provided.
5. `db.Employees.Add` + `SaveChanges`.
6. `_audit.Log(0, "employee_created", null, code + name)` → `equipment_logs`. Note: `EquipmentId` is written as `0` since this action has no associated equipment record.

**`Update(employee)`:**
1. Validates `FullName`.
2. Checks `Email` uniqueness (excluding self).
3. Loads entity, updates: `FullName`, `EmployeeCode`, `Email`, `Department`, `Phone`, `DefaultLocationId`, `IsActive`, `UpdatedAt = DateTime.Now`.
4. `SaveChanges`, then `_audit.Log(0, "employee_updated", oldValue, newValue)` → `equipment_logs`.

**`Delete(id)`:**
1. Checks `Allocations WHERE EmployeeId = id AND Status = "approved"` — fails if any.
2. `db.Employees.Remove` + `SaveChanges` — **hard delete from `employees` table.**
3. `_audit.Log(0, "employee_deleted", deletedInfo, null)` → `equipment_logs`. Historical (non-approved) allocation records keep a broken FK.

---

## 6. Locations

**Source:** `Forms/LocationsForm.cs`, `Services/LocationService.cs`

### 6.1 List View

**Data load — `LoadLocations()`:** `LocationService.GetAll()` → `db.Locations.ToList()`. No pagination — all records loaded at once.

**Grid columns:** `ID` (hidden) · `Location Name` · `Building` · `Room Number` · `Room Name`

**Actions:** Add / Edit (via `AddEditLocationForm`) / Delete (via `LocationService.Delete(id)`) / Export CSV.

No search or pagination. Add/Edit/Delete each write to `equipment_logs` via `_audit.Log(0, ...)` (`location_created`/`location_updated`/`location_deleted`), with `EquipmentId` written as `0` since locations have no associated equipment record.

---

## 7. Allocation

**Source:** `Forms/AllocationForm.cs`, `Services/AllocationService.cs`

### 7.1 Status Model

| Value | Meaning |
|-------|---------|
| `pending` | Created, awaiting approval |
| `approved` | Active; `Equipment.CurrentStatus = "allocated"` |
| `returned` | Returned; `Equipment.CurrentStatus = "in_store"` |
| `rejected` | Rejected; equipment status unchanged |
| `Overdue` | **UI-only computed** — `approved` + `ExpectedReturnDate < today`. Never stored in DB. |
| `available` | **UI-only synthetic** — equipment not in any active allocation, not decommissioned/under repair. Never stored in DB. |

### 7.2 List View — `AllocationForm`

**Data sources merged into `_allSourceRows`:**
1. `Allocations WHERE Status != "returned"` with `Include(Equipment, Employee, Location, AllocatedByUser)`.
2. `Transactions WHERE TransactionType == "Check-Out" AND (Status == "Pending" OR Status == "Overdue")` — included only if `Equipment.CurrentStatus == "allocated"` AND not already in allocation ids. Mapped with `Source = "Check-In/Out"`, synthetic `Id = -(TransactionId)`.
3. Available rows: equipment not decommissioned/under repair and not in any active allocation or transaction. Synthetic `Id = -(2_000_000 + Equipment.Id)`.

**Grid columns:** `QR Code` · `Equipment Code` · `Equipment Name` · `Make` · `Employee` · `Location` (includes room name after a hyphen) · `Date Allocated` · `Expected Return` · `Status` · `Source`

**Equipment filter dropdown:** "Equipment: All" / "Equipment: Available" / "Equipment: Allocated" / "Equipment: Pending" (custom-drawn green ComboBox)

**Category filter dropdown:** Separate green `cmbAllocCategoryFilter` to further filter by category.

**Action button rules:** `btnApprove`, `btnReturn`, `btnDelete`, `btnEditReturn` are all **disabled** when a Check-In/Out sourced row is selected.

**Double-click row → `AllocationDetailPopup`:**
Opens `AllocationDetailPopup` with a card showing the equipment photo (or gear icon placeholder) + status badge on the left, and labeled detail rows on the right:
`QR Code / Equipment Code` · `Equipment Name` · `Serial / Make / Model` · `Employee` · `Location` · `Date Allocated` · `Expected Return` · `Source` (`Allocation` or `Check-In/Out`) · `Allocated By`.

Two action buttons appear below the picture/status area on the left:
- **🖼 View Picture** — opens the equipment picture in a `Form` with `PictureBox` (Zoom mode). Disabled / greyed if no picture is available.
- **📜 View Logs** — closes the popup and navigates the Dashboard to the Logs screen pre-filtered to this equipment's code via `dashboard.NavigateToLogs(_info.EquipmentCode)`.

### 7.3 Create Allocation

Opens `AddAllocationForm`. Equipment list excludes: active transactions, active allocations, decommissioned, under repair. Employee list: `IsActive == true` only.

`AllocationForm.btnAdd_Click` → `AllocationService.Allocate(allocation)`:
1. Checks `Equipment.CurrentStatus` is `"in_store"` or `"allocated"`.
2. Checks no existing `Allocation` with `Status == "approved"` or `"pending"` for that equipment.
3. If `allocation.Status` empty → sets to `"approved"` (bypasses pending workflow).
4. `db.Allocations.Add` + `SaveChanges`.
5. If `Status == "approved"` → `Equipment.CurrentStatus = "allocated"`.
6. After save: `_audit.Log(equipmentId, "allocation_requested", ...)` → `equipment_logs`.

### 7.4 Approve — `btnApprove_Click`

1. Blocks if any active `Transaction` (`Pending`/`Overdue`) exists for the equipment.
2. Sets `Allocation.Status = "approved"`, `ApprovalDate = UtcNow`, `ApprovedByUserId = 1` (**hardcoded, not current user**).
3. Sets `Equipment.CurrentStatus = "allocated"`.
4. `SaveChanges`.
5. `_audit.Log(equipmentId, "allocation_approved", "pending", "approved")`.

### 7.5 Return — `btnReturn_Click`

1. Confirms with user.
2. `AllocationService.Deallocate(id)`:
   - `Allocation.Status = "returned"`.
   - `Equipment.CurrentStatus = "in_store"`.
   - `SaveChanges`. **Does not set `ActualReturnDate`.**
3. `_audit.Log(equipmentId, "allocation_returned", "allocated", "in_store")`.

### 7.6 Delete Allocation — `btnDelete_Click`

Blocked if `Status == "approved"`. Hard-deletes the `allocations` row. **No audit log.**

### 7.7 Edit Expected Return Date — `btnEditReturn_Click`

Opens `EditExpectedReturnDialog`. On OK → updates `Allocation.ExpectedReturnDate`. Writes `_audit.Log(equipmentId, "Expected Return Date Updated", oldDate, newDate)` — free-form string, not a defined constant.

---

## 8. Returns

**Source:** `Forms/ReturnsForm.cs`

### 8.1 Data Load — `LoadReturns()`

Two sources merged:
1. `db.Allocations WHERE Status == "returned"` with `Include(Equipment, Employee, Location)`, ordered by `ActualReturnDate DESC`.
2. `db.Transactions WHERE Status == "Completed" AND ActualReturn != null` with `Include(Equipment)`.

Both mapped to `ReturnRow` view-model, merged, sorted descending by `SortDate`.

**Overdue flag:** `ActualReturnDate > ExpectedReturnDate` (both non-null).

### 8.2 Process Return — `BtnAdd_Click`

Shows `ProcessReturnForm` populated with `Allocations WHERE Status == "approved"`.

On OK:
1. Loads `Allocation` + `Equipment`.
2. `Allocation.Status = "returned"`, `Allocation.ActualReturnDate = frm.ReturnDate`.
3. `Equipment.CurrentStatus = "in_store"`, `Equipment.ConditionGrade = frm.ConditionGrade`.
4. `SaveChanges`.
5. `_audit.Log(equipmentId, "allocation_returned", "allocated", "in_store")`.

### 8.3 Edit Return — `BtnEdit_Click`

Only for `Allocation`-sourced rows. Updates `ActualReturnDate` and `ConditionGrade`. **No audit log.**

### 8.4 Undo Return — `BtnDelete_Click`

Reverts allocation to active:
- `Allocation.Status = "approved"`, `ActualReturnDate = null`.
- `Equipment.CurrentStatus = "allocated"`.
- `SaveChanges`.
- `_audit.Log(equipmentId, "allocation_returned", ...)` ← **same action type as a real return; misleading**.

---

## 9. Transfer

**Source:** `Forms/TransferForm.cs`

### 9.1 List View

`db.Allocations WHERE Status == "approved"` with `Include(Equipment, Employee, Location)`.

Double-clicking a row pre-seeds the `TransferDialog` with that allocation's id.

### 9.2 Execute Transfer — `OpenTransferDialog`

`TransferForm` calls `TransferDialog` → on OK:

1. Loads old `Allocation`.
2. `oldAlloc.Status = "returned"`, `oldAlloc.ActualReturnDate = DateOnly.FromDateTime(DateTime.Today)`.
3. Creates new `Allocation`:
   - `EquipmentId = oldAlloc.EquipmentId`
   - `EmployeeId = frm.NewEmployeeId`, `LocationId = frm.NewLocationId`
   - `Status = "approved"`, `AllocatedByUserId = 1`, `ApprovedByUserId = 1` (**both hardcoded**)
   - `AllocationDate = UtcNow`, `ApprovalDate = UtcNow`
   - `PreviousAllocationId = oldAlloc.Id`
4. `db.Allocations.Add(newAlloc)` + `SaveChanges`.
5. `_audit.Log(equipmentId, "transferred", "Employee {old}", "Employee {new}")`.

`Equipment.CurrentStatus` is **not changed** — remains `"allocated"` throughout.

> The old allocation row gets `Status = "returned"`, so it **appears in the Returns module** as if the equipment was returned, even though it was only transferred.

---

## 10. Decommission

**Source:** `Forms/DecommissionForm.cs`

### 10.1 List View

`EquipmentService.GetAll()` filtered in-memory to `CurrentStatus == "decommissioned"`.

**Reason / Date columns** are sourced from a secondary query per equipment: latest `EquipmentLog` where `ActionType == "decommission_approved"` or `"decommission_requested"`. If logs are cleared via Settings, these columns go blank.

### 10.2 Execute Decommission — `btnDecommission_Click`

Dialog shows only `in_store` and `under_repair` equipment (UI-layer filter only). User picks reason (`Damaged`, `Obsolete`, `Beyond Repair`, `End of Life`, `Lost`, `Other`) and optional notes.

On confirm:
1. `Equipment.CurrentStatus = "decommissioned"`.
2. `SaveChanges`.
3. `_audit.Log(equipment.Id, "decommission_approved", oldStatus, reason + " — " + notes)`.

**No allocation status check in the decommission save path.** Only the dialog's equipment filter (UI) prevents allocated items from being offered.

---

## 11. Deleted Items

**Source:** `Forms/DeletedItemsForm.cs`, `Services/EquipmentService.cs`

### 11.1 Data Load

`EquipmentService.GetDeleted()` → `db.Equipment WHERE IsDeleted = true, Include(ProcurementType), ORDER BY DeletedAt DESC`.

**Grid columns:** `Equipment Code` · `QR Code` · `Procurement Type` · `Name` · `Make` · `Model` · `Serial Number` · `Category` · `Purchase Date`

### 11.2 Selection Modes

`cmbSelectMode` → "Select ✔" shows checkbox column (per-row), "Select All" calls `SelectAll()`.

### 11.3 Restore — `EquipmentService.Restore(id)`

`Equipment.IsDeleted = false`, `Equipment.DeletedAt = null`. `SaveChanges`. **No audit log.**

### 11.4 Permanent Delete — `EquipmentService.HardDelete(id)`

`db.Equipment.Remove` + `SaveChanges`. **Hard delete. No audit log.** Will fail at DB level if related rows exist without `ON DELETE CASCADE`.

### 11.5 Double-Click Row

Inline dialog offers Restore / Delete / Cancel for the single row. Same service methods.

---

## 12. Logs (Audit Trail)

**Source:** `Forms/LogsForm.cs`, `Services/AuditService.cs`

### 12.1 `AuditService.Log(int equipmentId, string actionType, string? oldValue, string? newValue)`

1. Opens new `InventoryDbContext`.
2. Inserts `EquipmentLog { EquipmentId, ActionType, ChangedByUserId = _systemUserId, OldValue, NewValue, CreatedAt = UtcNow }`.
3. `SaveChanges`. **Exceptions are silently swallowed.**

`AuditService` constructor signature: `public AuditService(int? systemUserId = null)`. **When called with no argument, `_systemUserId` is now set to `AuthService.CurrentUserId` (the logged-in user's ID)** — not a hardcoded `1`. This means audit entries now correctly attribute actions to the active session user.

### 12.1a `AuditService.BuildEquipmentDiff(Equipment before, Equipment after)`

A static helper that compares two equipment snapshots field-by-field and returns `(string? OldSummary, string? NewSummary)` containing only the fields that actually changed, formatted as `"Field: value | Field: value"` pipe-delimited strings. Fields compared: `Name`, `Make`, `Model`, `Serial Number`, `Status` (human-readable), `Category`, `Purchase Date`, `Price`, `Description`, `Vendor`, `Condition`, `Warranty Expiry`, `Notes`, `Picture` (added/removed/replaced). Used by `AddEditEquipmentForm` to produce precise audit diffs instead of full serialized dumps.

### 12.2 Defined Action Type Constants

| Constant | Value written to `action_type` |
|----------|-------------------------------|
| `ActionEquipmentCreated` | `equipment_created` |
| `ActionEquipmentUpdated` | `equipment_updated` |
| `ActionEquipmentDeleted` | `equipment_deleted` |
| `ActionEquipmentRestored` | `equipment_restored` |
| `ActionLocationChanged` | `location_changed` |
| `ActionAllocationRequested` | `allocation_requested` |
| `ActionAllocationApproved` | `allocation_approved` |
| `ActionAllocationRejected` | `allocation_rejected` |
| `ActionAllocationReturned` | `allocation_returned` |
| `ActionDecommissionRequested` | `decommission_requested` |
| `ActionDecommissionApproved` | `decommission_approved` |
| `ActionDecommissionRejected` | `decommission_rejected` |
| `ActionTransferred` | `transferred` |
| `ActionCheckedOut` | `checked_out` |
| `ActionCheckedIn` | `checked_in` |
| `ActionEmployeeCreated` | `employee_created` |
| `ActionEmployeeUpdated` | `employee_updated` |
| `ActionEmployeeDeleted` | `employee_deleted` |
| `ActionLocationCreated` | `location_created` |
| `ActionLocationUpdated` | `location_updated` |
| `ActionLocationDeleted` | `location_deleted` |
| `ActionMaintenanceOpened` | `maintenance_opened` |
| `ActionMaintenanceResolved` | `maintenance_resolved` |
| *(free-form string)* | `Maintenance History Cleared` |

### 12.3 Who Writes Audit Logs

| Action | Action type |
|--------|-------------|
| Equipment Add | `equipment_created` |
| Equipment Edit | `equipment_updated` (via `BuildEquipmentDiff`) |
| Allocation Create | `allocation_requested` |
| Allocation Approve | `allocation_approved` |
| Allocation Return (from Allocation screen) | `allocation_returned` |
| Returns – Process Return | `allocation_returned` |
| Returns – Undo Return | `allocation_returned` ← misleading |
| Allocation – Edit Expected Return Date | Free string `"Expected Return Date Updated"` |
| Transfer | `transferred` |
| Decommission | `decommission_approved` |
| Check-Out | `checked_out` |
| Check-In | `checked_in` |
| Employee Add | `employee_created` |
| Employee Edit | `employee_updated` |
| Employee Delete | `employee_deleted` |
| Location Add | `location_created` |
| Location Edit | `location_updated` |
| Location Delete | `location_deleted` |
| Status → Under Repair | `maintenance_opened` |
| Status Away from Under Repair (resolved) | `maintenance_resolved` |

**Not logged:** Equipment soft-delete, restore, hard-delete, user management actions, password changes.

### 12.4 LogsForm Data Load

- No search: server-side `SKIP/TAKE` on `equipment_logs ORDER BY CreatedAt DESC`.
- With search: full load then in-memory code-first / fuzzy on `ActionType`, `ChangedByUser.FullName`, `OldValue`, `NewValue`.

**Grid columns:** `Equipment Code` · `QR Code` · `Action` (formatted) · `Changed By` · `Previous Value` · `New Value` · `Timestamp`

**Export:** CSV of current page. Action column colour-coded: green / blue / yellow / red / purple / orange per action type.

**Sortable columns:** Clicking any column header toggles ascending/descending sort. Sort state is preserved across page turns and search filter changes. Server-side sorting (via `IQueryable` switch expression) for un-filtered loads; in-memory sort for filtered loads.

**Double-click row → `LogDetailPopup`:**
Opens a `LogDetailPopup` modal (`Forms/LogDetailPopup.cs`) showing formatted detail:
- Green header strip with **📋 Log Details** title.
- `Action` · `Changed By` · `When` (PKT formatted) · `Equipment Code | QR Code` · `Name, Make, Model`.
- Horizontal divider.
- **Before** panel: `OldValue` split on `" | "`, each field shown as a bullet in a light-red (`#FFF5F5`) box (dark mode: `#321E1E`).
- **After** panel: `NewValue` split on `" | "`, each field shown as a bullet in a light-green (`#F0FDF4`) box (dark mode: `#19281E`).
- All panels auto-size with text wrapping — no fixed height clipping.
- Close button styled with primary accent green.

---

## 13. QR Scanner

**Source:** `Forms/QRScannerForm.cs`

**Read-only lookup tool.** No DB writes.

### 13.1 Input & Auto-Search

`txtQRCode` is focused on load. `TextChanged` → starts 300 ms debounce timer (`_scanTimer`). On tick or `Enter` key → `Search()`. `EquipmentSuggestHelper` provides a fuzzy suggestion dropdown.

### 13.2 Search Logic

```
db.Equipment
  .Include(Allocations → Employee)
  .Include(Allocations → Location)
  .Include(EquipmentLogs → ChangedByUser)
  .Include(ProcurementType)
  .FirstOrDefault(e =>
      e.QrCode == code ||
      e.EquipmentCode == code ||
      e.SerialNumber == code)
```

**Exact equality — partial codes do not match.**

**On match:** Populates detail card with all equipment fields + colour-coded `CurrentStatus`. Current location/employee from first `Allocation WHERE Status == "approved"`. History grid: all `EquipmentLogs` ordered by `CreatedAt DESC` (no pagination).

---

## 14. Check In / Out

**Source:** `Forms/CheckInOutForm.cs`, `Forms/CheckInOutHistoryPanel.cs`, `Services/TransactionService.cs`

### 14.1 Purpose

Short-term informal borrows. Writes to `transactions` table (distinct from the `allocations` table used by the Allocation module).

### 14.2 Search

Same 300 ms debounce + `EquipmentSuggestHelper` as QR Scanner. Exact equality match on `QrCode`, `EquipmentCode`, `SerialNumber`.

Auto-selects tab: `CurrentStatus == "allocated"` → Check-In tab; otherwise → Check-Out tab.

Active transaction loaded: `Transactions WHERE EquipmentId = id AND (Status = "Pending" OR Status = "Overdue") ORDER BY TransactionDate DESC LIMIT 1` → stored as `_activeTransaction`.

### 14.3 Check-Out — `TransactionService.CheckOut(transaction)`

**UI validations first:**
1. `CurrentStatus` must be `"in_store"`.
2. No `Allocation` with `Status == "pending"` or `"approved"` may exist.
3. `txtIssuedTo` must not be empty.
4. A location must be selected in `cmbLocationOut`.

**Service:**
1. Checks no existing `Transaction` with `Status == "Pending"` or `"Overdue"`.
2. Sets `TransactionType = "Check-Out"`, `Status = "Pending"`, `TransactionDate = UtcNow`, `CreatedAt = UtcNow`.
3. `db.Transactions.Add(transaction)`.
4. `Equipment.CurrentStatus = "allocated"`.
5. `SaveChanges`.

**Fields written to `transactions`:** `EquipmentId`, `IssuedTo`, `Department` (auto-filled from location + room selection), `OperatorName` (pre-filled from `AuthService.CurrentFullName`), `ExpectedReturn`, `ConditionBefore`, `Remarks`.

`_audit.Log(equipmentId, "checked_out", "in_store", "Issued to: {IssuedTo}")` → `equipment_logs`.

### 14.4 Check-In — `TransactionService.CheckIn(transactionId, conditionAfter, remarks)`

1. `Transaction.ActualReturn = DateOnly.FromDateTime(DateTime.Today)`.
2. `Transaction.Status = "Completed"`.
3. `Transaction.ConditionAfter = conditionAfter`, `Transaction.Remarks = remarks`.
4. `Equipment.CurrentStatus = "in_store"`.
5. `SaveChanges`.

**`Equipment.ConditionGrade` is NOT updated.** Only `Transaction.ConditionAfter` is stored.

`_audit.Log(equipmentId, "checked_in", "allocated", "Condition: {conditionAfter}")` → `equipment_logs`.

### 14.5 Overdue Update — `TransactionService.UpdateStatus()`

Sets `Transaction.Status = "Overdue"` where `Status == "Pending"` and `ExpectedReturn < today`. Called externally (startup path not traced in this audit).

### 14.6 Per-Equipment History Grid (Scan View)

Last 10 `Transaction` records for the scanned equipment. Displayed below the Check-In/Check-Out tab card. Display-only.

### 14.7 Check-In/Out History Screen — `CheckInOutHistoryPanel`

**Source:** `Forms/CheckInOutHistoryPanel.cs`

Accessed via the **🕘 History** button in the Check In/Out form toolbar. Clicking it hides the scan bar and main content and overlays the full-screen history panel. The **◀ Back** button inside the history panel returns to the scan view.

**Data load (`LoadData`):**
```
db.Transactions
  .Include(t => t.Equipment)
  .OrderByDescending(t => t.TransactionDate)
  .ToList()
```
All transactions are loaded into memory; then each is expanded into up to two display entries:
- **Check-Out entry:** the original transaction row.
- **Check-In entry (synthetic):** generated only when `t.ActualReturn.HasValue` — represents the return event as a separate row with `Type = "Check-In"` and `Status = "Completed"`.

Entries are re-sorted descending by their effective timestamp (return timestamp for check-in rows, `TransactionDate` for check-out rows).

**Grid columns:**
`Equipment Code` · `Equipment Name` · `Type` (`Check-Out` in blue / `Check-In` in green) · `Issued To` · `Department` · `Operator` · `Expected Return` · `Status` (colour-coded: Pending=amber, Completed=green, Overdue=red) · `Condition Before` · `Condition After` · `Date` · `Timestamp`

**Search:** Real-time filter with 300 ms TextChanged hook. Code/QR exact substring match first; falls back to fuzzy match on Equipment Name, Issued To, Department, Operator, Status. Results re-ranked by fuzzy score.

**Pagination:** ComboBox page-size selector (25 / 50 / 100 / 250 / 500 / 1000). Prev/Next page buttons with `Page N of M` indicator and `"Showing X of Y records"` label.

**Export (📤):** Saves current page to CSV via `SaveFileDialog` → `File.WriteAllText`. Filename pattern: `CheckInOutHistory_{yyyyMMdd_HHmm}.csv`.

---

## 15. Reports

**Source:** `Forms/ReportsForm.cs`

**Read-only.** No DB writes. Company name loaded from `db.AppSettings.FirstOrDefault()?.CompanyName`.

### 15.1 Equipment Inventory Report

Category selection from context menu (same union logic as Equipment module filter). Data: `EquipmentService.GetAll()`, optionally filtered by category. Includes `Allocations → Location` so the `Location` column can show the active allocation's location.

**Columns:** `Equipment Code` · `QR Code` · `Name` · `Make` · `Model` · `Serial Number` · `Status` · `Location`

### 15.2 Employee Allocation Summary

`EmployeeService.GetAll()` + `AllocationService.GetAll()`. Active employees only (`IsActive == true`). Employees with `totalCount == 0` are **skipped**.

**Columns:** `Employee Code` · `Employee` · `Department` · `Email` · `Items Currently Held` (count `approved`) · `Total Ever Allocated`

### 15.3 Active Allocations Report

`AllocationService.GetAll()` filtered to `Status == "approved"` or `"pending"`. Includes pending items.

**Columns:** `Equipment Code` · `QR Code` · `Equipment Name` · `Allocated To` · `Location` · `Allocated On` · `Expected Return` · `Status`

### 15.4 Returns History Report

`AllocationService.GetAll()` filtered to `Status == "returned"`. **Does not include Check-In/Out completions (`transactions WHERE Status = "Completed"`).** Duration > 30 days highlighted in warning colour. Null `ActualReturnDate` → duration calculated to today.

### 15.5 Export — Excel

`ClosedXML`. Row 1: `CompanyName` merged. Row 2: headers (bold, green). Rows 3+: data from preview grid cell values (strings, not re-queried from DB). `AdjustToContents()`.

### 15.6 Export — PDF / Print

`System.Drawing.Printing.PrintDocument` + `PrintPreviewDialog`. GDI+ text at fixed column widths. Multi-page via `HasMorePages`. No file created — user prints from the preview dialog.

---

## 16. Settings

**Source:** `Forms/SettingsForm.cs`

Entirely code-built; no Designer layout. Left sidebar slides in on load via a timer animation. **Six sections** (five for Operators; System Configuration is Admin-only).

### 16.1 General — Company Information

**Load (`LoadSettings`):** `db.AppSettings.FirstOrDefault()`. If none → inserts default row (`CompanyName = "Q-Inventory"`, `Theme = "Light"`, `LabelWidthMm = 62`, `LabelHeightMm = 29`).

**Save (`btnSave_Click`):**
- Validates `CompanyName` non-empty; `LabelWidthMm` and `LabelHeightMm` positive integers.
- Upserts `AppSetting`. Sets `Theme = "Light"` (**hardcoded**, no theme switching implemented).
- `SaveChanges`. On General panel: shows inline `"✓ Saved"` pill (Save button disabled until Company Name is changed — dirty-check via `_originalCompanyName`). On Printing panel: shows `MessageBox`.

**System card:** App version from `Assembly.GetName().Version`. Last backup from `%AppData%\QInventory\lastbackup.txt`. Database name from loaded `AppSetting.DbName + " · " + DbServer`.

**Danger Zone:**

| Button | Action |
|--------|--------|
| Clear all logs | Confirms with MessageBox → loads all `EquipmentLog` rows, `RemoveRange`, `SaveChanges`. Shows toast with deleted count. |
| Reset application data | Opens `ShowTypedConfirm` dialog — user must type `"RESET"` to enable the confirm button. Deletes all `Allocations` (one `SaveChanges`), then all `Equipment` (second `SaveChanges`). **No transaction wrapper — partial failure possible.** Users, roles, categories, settings unaffected. |

### 16.2 Backup / Restore

**Backup:** Prompts for credentials (Host, Database, Username, Password dialog) → spawns `mysqldump.exe -h {host} -u {user} [-p{pass}] {db}` as a process. Reads `StandardOutput` to `.sql` file. Writes timestamp to `%AppData%\QInventory\lastbackup.txt`. Locates `mysqldump.exe` via: cached path in `%AppData%\QInventory\mysqltools.txt` → common dirs (`C:\xampp\mysql\bin`, MySQL 8.0/8.4, WAMP64) → `OpenFileDialog` prompt. **Password passed as CLI argument — visible in process listings.** Success/failure shown as toast notification.

**Restore:** Selects `.sql` file → confirms destructive overwrite with MessageBox → spawns `mysql.exe`, pipes file to `StandardInput`. Success/failure shown as toast. Recommends app restart after.

### 16.3 Users Management

**Source:** `Forms/SettingsForm.cs` (Users panel) + `Forms/AddEditUserForm.cs`

**Load:** `db.Users.ToList()` + `db.Roles.ToList()` + `db.UserRoles.ToList()` → role resolved per user.

**Grid columns:** `ID` · `Username` · `Full Name` · `Email` · `Role` · `Status` (🟢 Active / 🔴 Inactive, colour-coded).

| Action | Detail |
|--------|--------|
| Add User | Opens `AddEditUserForm` (add mode). On OK → `LoadUsers()`. |
| Edit User | Opens `AddEditUserForm(user)` (edit mode). Username field disabled in edit mode. |
| Toggle Active | Flips `User.IsActive`. Blocked if `id == AuthService.CurrentUserId`. Uses MessageBox confirmation. |
| Delete User | Removes `UserRoles WHERE UserId = id`, then `Users.Remove(entity)`. Blocked if own account. Shows toast. FK violation shown as toast error. |

**Double-click row → `UserDetailPopup`:** Shows user avatar, username, full name, email, role, status.

**`AddEditUserForm` — fields:**
- `Username` (required, unique; disabled in edit mode)
- `Full Name` (required)
- `Email` (optional, unique across users)
- `New Password` / `Confirm Password` (required in add mode; blank = keep existing in edit mode)
- `Role` (required ComboBox from `roles` table)
- **Profile Picture** (optional): Upload button → `OpenFileDialog` (JPG/PNG/BMP/GIF) → staged locally. On save, copied to `NetworkPaths.UserPicturesDir` as `{username}_{GUID}{ext}` via `NetworkShareHelper.EnsureConnected()`. Remove button clears the picture. Avatar drawn as circle crop via `AvatarHelper.DrawAvatar`.
- `Is Active Account` checkbox

**No audit log for any user operation.**

### 16.4 Change Password

1. Validates all fields non-empty.
2. Validates `newPassword == confirmPassword`.
3. Validates `newPassword.Length >= 4`.
4. `user.PasswordHash == currentPassword` — **plain-text comparison**.
5. `user.PasswordHash = newPassword` — **stored in plain text**.
6. `SaveChanges`.

**Visual improvements:**
- All three password fields are rounded input panels with an **eye (👁) icon** for reveal/hide toggle.
- A live **password strength meter** (coloured progress bar + label: Weak/Medium/Strong) appears beneath the New Password field as the user types. Scoring: length ≥ 4 (+1), ≥ 8 (+1), mixed case (+1), digit (+1), symbol (+1).
- Security Tips card on the right panel lists best-practice guidance.

Only the 4-character minimum is enforced server-side.

### 16.5 Printing

Fields: `PrinterName` (blank = system default), `LabelWidthMm` (62), `LabelHeightMm` (29). Same `btnSave_Click` as General.

**Test Print:** Generates QR code for `"TEST-LABEL-001"` via `QRCoder`, renders to `PrintPreviewDialog` at configured label size.

### 16.6 Categories

**Load:** `CategoryService.GetAll()` → `db.Categories.ToList()`. Displayed in a custom owner-drawn `ListBox` with numbered items.

**Add:** `CategoryService.Add(name)` — validates non-empty, checks uniqueness, inserts into `categories`. Enter key in the text field triggers add.

**Delete:** `CategoryService.Delete(id)` → `db.Categories.Remove` + `SaveChanges`. **Hard delete. Does not update `Equipment.Category` — orphaned string values remain on equipment records.**

### 16.7 System Configuration — Admin Only

**Source:** `Forms/SettingsForm.cs` (`BuildSystemConfigPanel()`) + `Services/DbConnectionSettingsService.cs`

Visible only when `AuthService.IsAdmin` is true. Manages the database connection credentials independently of the application settings table.

**Database Connection Details card:**

| Field | Detail |
|-------|--------|
| Database Server (IP) | The MariaDB host address |
| Database Name | Schema name |
| Database Username | DB login user |
| Database Password | Hidden input with eye-icon toggle (👁) |

**Buttons:**

| Button | Behaviour |
|--------|-----------|
| ✔ Test Connection | Builds a connection string from the form values, creates a temporary `InventoryDbContext` with those options, calls `db.Database.CanConnect()`. Shows ✅ or ❌ in inline label. Enables "Save Connection" only on success. |
| 💾 Save Connection | Calls `DbConnectionSettingsService.Save(settings, plainPassword)`. Password is **DPAPI-encrypted** (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`) before writing to `%AppData%\QInventory\dbconnection.json`. Shows toast on success. |

**Connection Status card:** Displays `Current Status` (🟢 Connected / 🔴 Disconnected), `Active Server` (database · host), `Last Successful Connection` (timestamp). Values are populated by `RefreshFromSaved()` which loads the saved settings and tests the connection on panel load.

**`DbConnectionSettingsService` key methods:**
- `Load()`: Deserializes `dbconnection.json` → `DbConnectionSettings`.
- `Save(settings, plainPassword)`: DPAPI-encrypts password → serializes to JSON.
- `DecryptPassword(settings)`: DPAPI-decrypts `settings.EncryptedPassword`.
- `TestConnection(settings, plainPassword, out error)`: Uses EF Core `Database.CanConnect()` — no extra NuGet dependency.
- `SeedIfMissing(...)`: One-time migration — if no settings file exists, seeds from the previously hardcoded connection string so existing installs don't break on upgrade.

**File storage:** `%AppData%\QInventory\dbconnection.json`

---

## 17. Data Model Reference

### All DB Tables

| C# DbSet | DB Table | Notes |
|----------|----------|-------|
| `Equipment` | `equipment` | `current_status` is a string enum |
| `Allocations` | `allocations` | Equipment → Employee → Location |
| `Employees` | `employees` | |
| `Locations` | `locations` | |
| `EquipmentLogs` | `equipment_logs` | Audit trail |
| `Transactions` | `transactions` | Check In/Out records |
| `MaintenanceRecords` | `maintenance_records` | Repair history |
| `ProcurementTypes` | `procurement_types` | Lookup |
| `Roles` | `roles` | |
| `Users` | `users` | `password_hash` stores plain text |
| `UserRoles` | `user_roles` | UserId + RoleId junction |
| `AppSettings` | `app_settings` | Single row expected |
| `Categories` | `categories` | |

### Equipment Status Lifecycle

```
in_store      -->  allocated       (Allocation approved OR Check-Out)
in_store      -->  under_repair    (Edit: status → Under Repair)
in_store      -->  decommissioned  (Decommission)
under_repair  -->  in_store        (Maintenance resolved)
under_repair  -->  decommissioned  (Decommission)
allocated     -->  in_store        (Return / Deallocate / Check-In)
allocated     -->  allocated       (Transfer — status unchanged, new allocation row)
```

Raw DB values (used in string comparisons throughout):

| DB Value | UI Display |
|----------|------------|
| `in_store` | In Store |
| `allocated` | Allocated |
| `under_repair` | Under Repair |
| `decommissioned` | Decommissioned |

### Key Relationships

```
Equipment  (1) --< Allocations        (N)  [EquipmentId FK]
Employee   (1) --< Allocations        (N)  [EmployeeId FK]
Location   (1) --< Allocations        (N)  [LocationId FK]
Equipment  (1) --< Transactions       (N)  [EquipmentId FK]
Equipment  (1) --< EquipmentLogs      (N)  [EquipmentId FK]
Equipment  (1) --< MaintenanceRecords (N)  [EquipmentId FK]
Users      (1) --< EquipmentLogs      (N)  [ChangedByUserId FK]
Users      (1) --< UserRoles          (N)  [UserId FK]
Roles      (1) --< UserRoles          (N)  [RoleId FK]
```

### Delete Guards

| Action | Guard |
|--------|-------|
| Delete Equipment (soft) | No guard — sets `IsDeleted = true`. |
| Hard Delete Equipment | DB FK failure if related rows exist without CASCADE. |
| Delete Employee | Blocked if any `approved` Allocation exists. |
| Delete Location | Via `LocationService.Delete` (guard not fully traced — check service). |
| Delete User | FK error from DB if referenced (not pre-checked for allocations/logs). |
| Decommission | UI filter only; no DB-level allocation check in save path. |

---

## 18. Cross-Module Effects Summary

| Action | Module | Tables Written | Audit Log? |
|--------|--------|----------------|------------|
| Add Equipment | Equipment | `equipment`, `equipment_logs`, optionally `procurement_types` | ✅ `equipment_created` |
| Edit Equipment | Equipment | `equipment`, `equipment_logs`, optionally `maintenance_records`, `procurement_types` | ✅ `equipment_updated` |
| Soft Delete | Equipment | `equipment` (`is_deleted`, `deleted_at`) | ❌ |
| Restore | Deleted Items | `equipment` | ❌ |
| Hard Delete | Deleted Items | `equipment` (row removed) | ❌ |
| Add Employee | Employees | `employees`, `equipment_logs` | ✅ `employee_created` |
| Edit Employee | Employees | `employees`, `equipment_logs` | ✅ `employee_updated` |
| Delete Employee | Employees | `employees` (hard delete), `equipment_logs` | ✅ `employee_deleted` |
| Add / Edit / Delete Location | Locations | `locations`, `equipment_logs` | ✅ `location_created`/`updated`/`deleted` |
| Create Allocation | Allocation | `allocations`, `equipment_logs` | ✅ `allocation_requested` |
| Approve Allocation | Allocation | `allocations`, `equipment`, `equipment_logs` | ✅ `allocation_approved` |
| Return (Allocation screen) | Allocation | `allocations`, `equipment`, `equipment_logs` | ✅ `allocation_returned` |
| Delete Allocation | Allocation | `allocations` (hard delete) | ❌ |
| Edit Expected Return | Allocation | `allocations`, `equipment_logs` | ✅ free-string |
| Process Return | Returns | `allocations`, `equipment`, `equipment_logs` | ✅ `allocation_returned` |
| Edit Return Record | Returns | `allocations`, `equipment` | ❌ |
| Undo Return | Returns | `allocations`, `equipment`, `equipment_logs` | ✅ `allocation_returned` (misleading) |
| Transfer | Transfer | `allocations` (×2), `equipment_logs` | ✅ `transferred` |
| Decommission | Decommission | `equipment`, `equipment_logs` | ✅ `decommission_approved` |
| Check-Out | Check In/Out | `transactions`, `equipment`, `equipment_logs` | ✅ `checked_out` |
| Check-In | Check In/Out | `transactions`, `equipment`, `equipment_logs` | ✅ `checked_in` |
| Update Overdue | Check In/Out | `transactions` | ❌ |
| Add / Toggle / Delete User | Settings | `users`, `user_roles` | ❌ |
| Change Password | Settings | `users` | ❌ |
| Add Category | Settings | `categories` | ❌ |
| Delete Category | Settings | `categories` (hard delete; `Equipment.Category` not updated) | ❌ |
| Save Settings | Settings | `app_settings` | ❌ |
| Clear All Logs | Settings | `equipment_logs` (all rows deleted) | n/a |
| Reset Application Data | Settings | `allocations` (all), `equipment` (all) | ❌ |

---

## 19. Known Issues & Observations

| # | Issue | Detail |
|---|-------|--------|
| 1 | **Passwords stored in plain text** | `Users.PasswordHash` stores raw passwords. No hashing. All comparisons are plain-string equality. |
| 2 | **~~`ChangedByUserId` always `1`~~** | ✅ **FIXED** — `AuditService` constructor now defaults `_systemUserId = AuthService.CurrentUserId`. All audit entries now correctly attribute changes to the logged-in user. |
| 3 | **Undo Return writes `allocation_returned`** | Same action key as a real return — cannot distinguish undo from return in the logs without reading `OldValue`/`NewValue`. |
| 4 | **Transfer pollutes Returns view** | The old allocation's `Status = "returned"` causes it to appear in the Returns module grid and Returns History report. |
| 5 | **`ApprovedByUserId = 1` hardcoded** | Both Approve and Transfer hardcode this. The acting user is never recorded in `allocations.approved_by_user_id`. |
| 6 | **`AllocationService.Allocate` silently approves** | If `allocation.Status` is empty or blank, it is promoted to `"approved"` without requiring a separate approve step — bypassing the `pending → approved` workflow. |
| 7 | **"Overdue" is UI-only** | Allocations past their expected return date remain `"approved"` in the DB. The status is computed on every load. |
| 8 | **Category delete orphans equipment records** | Deleting a category from the `categories` table does not update `Equipment.Category`. Orphaned strings persist and continue to appear in filter dropdowns. |
| 9 | **Reset application data has no transaction wrapper** | Allocations deleted in one `SaveChanges`, equipment in a second. If the second fails, allocations are gone but equipment remains. |
| 10 | **Backup password visible in process list** | `mysqldump` password is passed as `-p{pass}` in the command-line argument. |
| 11 | **Danger Zone Decommission reason lost if logs cleared** | `DecommissionForm` reads the reason from `equipment_logs`. Clearing logs via Settings removes the reason from the decommission view. |
| 12 | **Returns History Report excludes Check-In/Out completions** | The report only queries `allocations WHERE Status = "returned"`. Completed `transactions` are not shown. |
| 13 | **Soft delete has no FK guard** | `EquipmentService.Delete` sets `IsDeleted = true` with no check for active allocations or transactions. Allocated equipment can be soft-deleted. |
| 14 | **LocationsForm has no pagination** | All locations are loaded at once. Will be slow at scale. |
| 15 | **Employee hard-delete breaks historical allocations** | Non-approved (historical) allocation rows keep the `EmployeeId` FK after the employee is deleted. |
| 16 | **Audit failure is silently swallowed** | `AuditService.Log` catches all exceptions. If the DB insert fails (connectivity, FK), the triggering operation has already committed and no error is shown. |
| 17 | **"Expected Return Date Updated" is a free-form string** | Not a defined `AuditService` constant. `LogsForm.FormatAction` has no case for it — it displays verbatim, inconsistent with other formatted action types. |
| 18 | **`CreatedByUserId = 1` hardcoded in Equipment Add** | The add form sets `_equipment.CreatedByUserId = 1` unconditionally; the session user is not recorded. |
| 19 | **DB password DPAPI-bound to OS user** | The password stored in `dbconnection.json` uses `ProtectedData.Protect` with `DataProtectionScope.CurrentUser`. The file cannot be decrypted on a different Windows user account or machine. |
| 20 | **`BuildSystemConfigPanel` has a duplicate `return outer`** | Lines 2307–2308 both return `outer` — the second `return` is dead code (compile-time unreachable). No functional impact. |

---

## 20. New Features & Recent Changes (2026-08-10)

This section summarizes all significant features and improvements added as of the 2026-08-10 update.

### 20.1 System Configuration Panel (Admin-Only)

A new sixth Settings tab — **System Configuration** — is available exclusively to Admin users. It provides a live database connection management UI:
- Fields for `Database Server (IP)`, `Database Name`, `Database Username`, `Database Password` (with eye-toggle).
- **Test Connection** button that validates connectivity without saving.
- **Save Connection** button (enabled only after a successful test) that DPAPI-encrypts the password and writes to `%AppData%\QInventory\dbconnection.json`.
- **Connection Status** card showing live status (🟢/🔴), active server, and last successful connection time.

### 20.2 Profile Pictures — Users & Employees

- **Users (`AddEditUserForm`):** Upload / Remove buttons for profile picture. File is copied to the network share (`NetworkPaths.UserPicturesDir`) as `{username}_{GUID}{ext}`. Picture is displayed as a circle-cropped avatar via `AvatarHelper.DrawAvatar`. Double-clicking a user row opens `UserDetailPopup` showing the avatar.
- **Employees (`AddEditEmployeeForm`):** Same Upload/Remove pattern using `NetworkPaths.EmployeePicturesDir`. Double-clicking an employee row opens `EmployeeDetailPopup` showing avatar, `Employee Code`, `Designation`, `Email`, `Phone`, `Status` badge.

### 20.3 Detail Popups on Double-Click

| Screen | Double-click opens |
|--------|--------------------|
| Equipment grid | `EquipmentDetailPopup` — full two-column card: Equipment Info (left) + Current Allocation + Maintenance History (right) |
| Allocation grid | `AllocationDetailPopup` — equipment photo + labeled allocation rows + View Picture button |
| Logs grid | `LogDetailPopup` — Action, Changed By, When, Equipment code; Before/After value panels |
| Employees grid | `EmployeeDetailPopup` — avatar, Employee Code, Designation, Email, Phone, Status |
| Users grid (Settings) | `UserDetailPopup` — avatar, Username, Full Name, Email, Role, Status |

### 20.4 Maintenance History in Equipment Detail

`EquipmentDetailPopup` now shows a **Maintenance History** section in the right column. It calls `MaintenanceService.GetByEquipmentId()` and lists each record with: Report date, Status (colour-coded badge: open/in_progress/resolved), Issue, Serviced By, Cost, Resolved date.

### 20.5 Audit Service — Session-Aware & Field-Level Diffs

- **`ChangedByUserId` is now the real logged-in user** (`AuthService.CurrentUserId`) instead of the hardcoded `1`.
- **`BuildEquipmentDiff`** produces field-level change summaries: only changed fields appear in audit `OldValue`/`NewValue`, formatted as `"Field: old | Field: old"`.
- **New constants:** `ActionEquipmentDeleted`, `ActionEquipmentRestored`, `ActionMaintenanceOpened`, `ActionMaintenanceResolved`.
- **Maintenance events** are now audited: opening a maintenance record writes `maintenance_opened`; resolving one writes `maintenance_resolved` — both with detailed before/after descriptions.

### 20.6 Allocation Form — Enhanced UI & Filtering

- **Category filter:** Separate `cmbAllocCategoryFilter` ComboBox in the toolbar filters by equipment category.
- **Equipment filter:** Renamed items include `"Equipment: Available"`, `"Equipment: Allocated"`, `"Equipment: Pending"` — custom-drawn green ComboBox.
- **`AllocationDetailPopup`:** Rich double-click popup showing equipment photo, QR/equipment code, employee, location, dates, source, and who allocated.
- **`AllocatedBy` field** displayed in the allocation detail (resolved from `AllocatedByUser`).

### 20.7 Settings — UX Improvements

- **Sidebar navigation:** Now has a fading hover animation (`FadeNavButtonColor`) for smooth colour transitions between inactive/hover/active states. Active item styled as a green rounded pill.
- **Company Name dirty-check:** Save button disabled until the company name differs from the original loaded value. Shows inline `"✓ Saved"` pill on save.
- **Change Password:** All three fields use rounded input panels with eye-toggle icons and a live strength meter bar (Weak/Medium/Strong with colour).
- **Toast notifications** replace `MessageBox` for all success/info/error feedback (backup, restore, user actions, log clear, reset, connection save).
- **Users grid:** Double-click opens `UserDetailPopup`; rows show 🟢/🔴 status dots with colour-coded text; inactive rows displayed in muted grey.

### 20.8 Log Detail Popup

`LogDetailPopup` (`Forms/LogDetailPopup.cs`) is a new modal opened by double-clicking any row in the Logs grid. It renders the full audit entry in a structured card: Action, Changed By, When, Equipment Code, then colour-coded **Before** (light red) and **After** (light green) panels with bullet-separated field values.

### 20.9 Dashboard — Recent Activities Panel

The Dashboard `Recent Activities` panel (visible in the screenshot) lists the latest allocation/return/transfer events with timestamps (e.g. `"7h ago"`), equipment name, and action description. This is an in-dashboard live feed sourced from `equipment_logs ORDER BY CreatedAt DESC`.

---

## 21. New Features & Recent Changes (2026-08-17)

This section documents all significant features and improvements added as of the **2026-08-17** update.

### 21.1 Dark Mode Toggle Switch

**Source:** `DashboardForm.BuildThemeToggle()` · `ThemeHelper.ToggleTheme()` · `ThemeHelper.LoadTheme()`

An animated pill-shaped **☀️ / 🌙 toggle switch** is now embedded in the top bar of the Dashboard, immediately left of the notification bell.

| Aspect | Detail |
|--------|--------|
| **Widget** | 60×28 GDI+ custom `Panel` named `"pnlThemeToggle"` |
| **Thumb animation** | `_toggleThumbProgress` float (0–1) driven by a 12 ms `Timer` at step 0.18 per tick |
| **Track colour** | Light: `#D6E0C8` · Dark: `#2D3037` |
| **Click action** | `ToggleThemeAnimated()` — flips `ThemeHelper.IsDarkMode`, persists to `{AppDir}/theme.json`, repaints entire dashboard |
| **Persistence** | `ThemeHelper.LoadTheme()` reads `theme.json` at `Program.Main()` startup |
| **Scope** | Re-renders on theme change: sidebar, top bar, stat cards, charts, all child forms opened after the toggle |

See §3.1 for full colour palette and implementation details.

### 21.2 Check-In/Out History Screen

**Source:** `Forms/CheckInOutHistoryPanel.cs`

A new full-screen history view inside the Check In/Out form, accessed via the **🕘 History** toolbar button. Hides the scan UI entirely and overlays a paginated transaction history.

**Key behaviours:**
- Each `Transaction` row is expanded into up to 2 display entries: the Check-Out entry + a synthetic Check-In entry (if `ActualReturn.HasValue`).
- Entries sorted by effective timestamp (return time for check-in rows, `TransactionDate` for check-out rows) descending.
- Real-time fuzzy search across Equipment Code, QR Code, Equipment Name, Issued To, Department, Operator, Status.
- Pagination: page sizes 25 / 50 / 100 / 250 / 500 / 1000. `Prev` / `Next` buttons with `Page N of M` and `Showing X of Y records` display.
- **📤 Export** button saves current page to CSV (`CheckInOutHistory_{yyyyMMdd_HHmm}.csv`).
- **◀ Back** button returns to the scan view.

See §14.7 for complete column list and data-load details.

### 21.3 Maintenance History in Equipment Detail Popup

**Source:** `Forms/EquipmentDetailPopup.cs` · `Services/MaintenanceService.cs`

The `EquipmentDetailPopup` right column now includes a **scrollable Maintenance History panel** loaded via `MaintenanceService.GetByEquipmentId(_equipment.Id)` at popup construction time.

- The panel uses `AutoScroll = true` with its height capped to the remaining space beneath the Current Allocation section.
- If no records exist, displays `"No maintenance history recorded."`
- For each record: `Record #N` (date) · `Status` badge (color-coded) · `Issue` · `Serviced By` · `Cost` (₨) · `Resolved` date.
- Status badge colours: `open`=`#DC5528` · `in_progress`=`#D97706` · `resolved`=`#228B22`.

### 21.4 Clear Maintenance History Button

**Source:** `EquipmentDetailPopup.AddLogsAndClearButtons()` · `BtnClearMaintenance_Click()`

A **🗑 Clear History** button appears in the bottom row of the left column, next to the **📜 View Logs** button.

| State | Appearance |
|-------|------------|
| Has records | Red-tinted (`#FEE2E2` bg, `#B91C1C` text, `#FCA5A5` border). Clickable. |
| No records | Greyed out (`#F3F4F6` bg, `#9CA3AF` text). Disabled. |

**On confirm (`MessageBox.YesNo`):**
1. `db.MaintenanceRecords.Where(m => m.EquipmentId == id).ToList()` → `RemoveRange` → `SaveChanges`.
2. `AuditService.Log(id, "Maintenance History Cleared", "{N} record(s)", "0 records")` → `equipment_logs`.
3. In-memory `_maintenanceRecords.Clear()`.
4. `RefreshCardContent()` — rebuilds card content in-place, updating button state and maintenance panel without reopening the popup.

**Note:** The `"Maintenance History Cleared"` action type is a free-form string (not an `AuditService` constant). `LogsForm.FormatAction()` has a case for it and displays `"Maintenance History Cleared"` with no colour badge.

### 21.5 View Logs Button in Detail Popups

A **📜 View Logs** button now appears inside two detail popups, providing one-click navigation to the filtered Logs screen:

| Popup | Button location | Behaviour |
|-------|----------------|----------|
| `EquipmentDetailPopup` | Bottom of left column, next to Clear History | Closes popup → `DashboardForm.NavigateToLogs(equipmentCode)` |
| `AllocationDetailPopup` | Left side, below View Picture | Closes popup → `DashboardForm.NavigateToLogs(_info.EquipmentCode)` |

Both buttons use the blue-tinted style (`#EEF2FF` bg, `ThemeHelper.Header` text, `#C7D2FE` border) matching the View Picture button aesthetic.

`NavigateToLogs(string equipmentCode)` opens `LogsForm(equipmentCode)` — a constructor overload that pre-populates `txtSearch` with the equipment code so the grid immediately filters to that equipment's log entries.

### 21.6 Log Detail Popup Improvements

**Source:** `Forms/LogDetailPopup.cs`

The `LogDetailPopup` (double-click on any Logs grid row) received the following improvements:

- **Auto-sizing text rows:** Each row measures text height using `TextRenderer.MeasureText` with `TextFormatFlags.WordBreak` — no fixed-height clipping. Long values (e.g. complex diff strings) wrap correctly.
- **Structured equipment identity row:** Equipment Code, QR Code, Name, Make, and Model are shown separately (`"Equipment Code | QR Code"` row + `"Name, Make, Model"` row).
- **Dark-mode aware Before/After panels:** Section backgrounds adapt to `ThemeHelper.IsDarkMode`:
  - Light → Before: `#FFF5F5` / After: `#F0FDF4`
  - Dark → Before: `#321E1E` / After: `#19281E`
- **Model field** is resolved from the underlying `_allLogs` collection by log ID (not a grid column), avoiding layout shifts.

### 21.7 Allocation Detail Popup — View Picture + View Logs Buttons

**Source:** `Forms/AllocationDetailPopup.cs`

Two buttons are now stacked in the left icon panel of `AllocationDetailPopup`, below the equipment code and status badge:

1. **🖼 View Picture** — always present. Active (blue tint) if `AvatarHelper.TryLoadEquipmentImage(_info.PicturePath)` resolves a non-null image; greyed + disabled otherwise. Opens the resolved `Image` in a standalone black `Form` with `PictureBoxSizeMode.Zoom`.
2. **📜 View Logs** — always active. Closes the popup and calls `DashboardForm.NavigateToLogs(_info.EquipmentCode)` to open the Logs screen filtered to the equipment.

### 21.8 Check-Out Form — Location + Room Dropdowns

**Source:** `CheckInOutForm.PopulateLocationDropdowns()` · `RefreshRoomDropdown()` · `UpdateDepartmentFromLocation()`

The Check-Out tab now features linked **Location** and **Room** dropdowns:

- `cmbLocationOut` is populated from `LocationService.GetAll()` — distinct `LocationName` values, alphabetical.
- `cmbRoomOut` cascades off `cmbLocationOut`: selecting a location populates rooms from matching `Location` rows. Disabled if the selected location has no rooms.
- `txtDepartment` is **auto-filled** based on the selected Location + Room combination: `"{LocationName} - {Room {RoomNumber} (RoomName)}"`.
- A validation now requires a Location to be selected before a Check-Out can be confirmed.

### 21.9 Logs Form — Sortable Columns & Maintenance Action Types

**Source:** `Forms/LogsForm.cs`

- **Column sort:** Clicking any `DataGridView` column header sets `_sortColumn` and toggles `_sortAscending`. The active sort is applied via a `Func<IQueryable<EquipmentLog>, IOrderedQueryable<EquipmentLog>>` switch. **Page resets to 1 on sort change.**
- **New action type colours:**
  - `maintenance_opened` → `#FFE082` bg / `#825A00` text
  - `maintenance_resolved` → `#C5E1A5` bg / `#33691E` text
- **`FormatAction` additions:**
  - `"maintenance_opened"` → `"Maintenance Opened"`
  - `"maintenance_resolved"` → `"Maintenance Resolved"`
  - `"Maintenance History Cleared"` → `"Maintenance History Cleared"` (pass-through)

### 21.10 Cross-Module Effects — New Actions

| Action | Module | Tables Written | Audit Log? |
|--------|--------|----------------|------------|
| Clear Maintenance History | Equipment Detail Popup | `maintenance_records` (range delete) | ✅ `Maintenance History Cleared` |
| View Logs (from detail popup) | Equipment / Allocation Detail | *(navigation only, no write)* | ❌ |
