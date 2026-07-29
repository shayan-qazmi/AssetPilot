# Q-Inventory — Application Standard Operating Procedure (SOP)

> **Ground-truth document** — every claim is traced from source code:
> UI form event handler → service method → EF Core / DbContext → database table + column.
> No assumption-based descriptions. Source file + method cited per section.
> Last generated: 2026-07-29

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

---

## 1. Overview & Roles

**Q-Inventory** is a Windows desktop application for tracking physical IT/office equipment across a company. It covers the full equipment lifecycle: procurement → storage → allocation → return / transfer → decommission.

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

1. `ApplyTheme()` — styles sidebar, top bar (yellow `#99C221`), card panels.
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

**Grid columns:** `Equipment Code` · `QR Code` · `Procurement Type` · `Name` · `Make` · `Model` · `Serial Number` · `Category` · `Status` · `Purchase Date`

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

**Save path:**
- Add: `db.Equipment.Add(equipment)` + `db.SaveChanges()`, then `AuditService.Log(id, "equipment_created", null, code)` → `equipment_logs`.
- Edit: loads entity, updates all fields, `db.SaveChanges()`. **No audit log written for edits.**
- `CreatedByUserId` is hardcoded to `1` in add mode. The logged-in user id is not used.

**Picture lifecycle:** Staged path stored; on save, old file deleted and new path persisted. On form close (cancel), staged file is deleted.

---

## 5. Employees

**Source:** `Forms/EmployeesForm.cs`, `Services/EmployeeService.cs`

### 5.1 List View

**Data load — `LoadEmployees(string searchTerm = "")`**
- No search: `EmployeeService.GetPage(_currentPage, _pageSize)` → server-side `SKIP/TAKE` on `employees` ordered by `FullName`.
- With search: `EmployeeService.GetAll()` → in-memory code-first / fuzzy across `FullName`, `Department`, `Email`, `Phone`.

**Grid columns:** `Employee Code` · `Full Name` · `Department` · `Email` · `Phone` · `Status` (Active/Inactive)

**Actions:**

| Button | Behaviour |
|--------|-----------|
| Add | Opens `AddEditEmployeeForm`. On OK → reload. |
| Edit | Calls `GetAll()`, finds by `EmployeeCode`, opens `AddEditEmployeeForm(emp)`. |
| Delete | Confirms → `EmployeeService.Delete(emp.Id)`. |
| Export | CSV of current page's grid. |

### 5.2 Service Methods — `EmployeeService`

**`Add(employee)`:**
1. Validates `FullName` non-empty.
2. Auto-generates `EmployeeCode` (`EMP###`) if blank.
3. Checks code uniqueness against `employees`.
4. Checks `Email` uniqueness if provided.
5. `db.Employees.Add` + `SaveChanges`.
6. **No audit log.**

**`Update(employee)`:**
1. Validates `FullName`.
2. Checks `Email` uniqueness (excluding self).
3. Loads entity, updates: `FullName`, `EmployeeCode`, `Email`, `Department`, `Phone`, `DefaultLocationId`, `IsActive`, `UpdatedAt = DateTime.Now`.
4. `SaveChanges`. **No audit log.**

**`Delete(id)`:**
1. Checks `Allocations WHERE EmployeeId = id AND Status = "approved"` — fails if any.
2. `db.Employees.Remove` + `SaveChanges` — **hard delete from `employees` table.**
3. **No audit log.** Historical (non-approved) allocation records keep a broken FK.

---

## 6. Locations

**Source:** `Forms/LocationsForm.cs`, `Services/LocationService.cs`

### 6.1 List View

**Data load — `LoadLocations()`:** `LocationService.GetAll()` → `db.Locations.ToList()`. No pagination — all records loaded at once.

**Grid columns:** `ID` (hidden) · `Location Name` · `Building` · `Room Number` · `Room Name`

**Actions:** Add / Edit (via `AddEditLocationForm`) / Delete (via `LocationService.Delete(id)`) / Export CSV.

No search or pagination. **No audit log for any location operation.**

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

### 7.2 List View — `AllocationForm`

**Data sources merged into `_allSourceRows`:**
1. `Allocations WHERE Status != "returned"` with `Include(Equipment, Employee, Location, AllocatedByUser)`.
2. `Transactions WHERE TransactionType == "Check-Out" AND (Status == "Pending" OR Status == "Overdue")` — included only if `Equipment.CurrentStatus == "allocated"` AND not already in allocation ids. Mapped with `Source = "Check-In/Out"`, synthetic `Id = -(TransactionId)`.
3. Available rows: equipment not decommissioned/under repair and not in any active allocation or transaction. Synthetic `Id = -(2_000_000 + Equipment.Id)`.

**Equipment filter dropdown:** "All" / "Available" / "Allocated" / "Pending"

**Action button rules:** `btnApprove`, `btnReturn`, `btnDelete`, `btnEditReturn` are all **disabled** when a Check-In/Out sourced row is selected.

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

`_systemUserId` defaults to `1`. Every caller uses `new AuditService()` with no argument. **All audit entries record `ChangedByUserId = 1` regardless of who is logged in.**

### 12.2 Defined Action Type Constants

| Constant | Value written to `action_type` |
|----------|-------------------------------|
| `ActionEquipmentCreated` | `equipment_created` |
| `ActionLocationChanged` | `location_changed` |
| `ActionAllocationRequested` | `allocation_requested` |
| `ActionAllocationApproved` | `allocation_approved` |
| `ActionAllocationRejected` | `allocation_rejected` |
| `ActionAllocationReturned` | `allocation_returned` |
| `ActionDecommissionRequested` | `decommission_requested` |
| `ActionDecommissionApproved` | `decommission_approved` |
| `ActionDecommissionRejected` | `decommission_rejected` |
| `ActionTransferred` | `transferred` |

### 12.3 Who Writes Audit Logs

| Action | Action type |
|--------|-------------|
| Equipment Add | `equipment_created` |
| Allocation Create | `allocation_requested` |
| Allocation Approve | `allocation_approved` |
| Allocation Return (from Allocation screen) | `allocation_returned` |
| Returns – Process Return | `allocation_returned` |
| Returns – Undo Return | `allocation_returned` ← misleading |
| Allocation – Edit Expected Return Date | Free string `"Expected Return Date Updated"` |
| Transfer | `transferred` |
| Decommission | `decommission_approved` |

**Not logged:** Equipment edit/delete, Employee CRUD, Location CRUD, soft-delete, restore, hard-delete, Check In, Check Out.

### 12.4 LogsForm Data Load

- No search: server-side `SKIP/TAKE` on `equipment_logs ORDER BY CreatedAt DESC`.
- With search: full load then in-memory code-first / fuzzy on `ActionType`, `ChangedByUser.FullName`, `OldValue`, `NewValue`.

**Grid columns:** `ID` · `Equipment Code` · `QR Code` · `Action` (formatted) · `Changed By` · `Previous Value` · `New Value` · `Timestamp`

**Export:** CSV of current page. Action column colour-coded: green / blue / yellow / red / purple / orange per action type.

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

**Source:** `Forms/CheckInOutForm.cs`, `Services/TransactionService.cs`

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

**Service:**
1. Checks no existing `Transaction` with `Status == "Pending"` or `"Overdue"`.
2. Sets `TransactionType = "Check-Out"`, `Status = "Pending"`, `TransactionDate = UtcNow`, `CreatedAt = UtcNow`.
3. `db.Transactions.Add(transaction)`.
4. `Equipment.CurrentStatus = "allocated"`.
5. `SaveChanges`.

**Fields written to `transactions`:** `EquipmentId`, `IssuedTo`, `Department`, `OperatorName` (pre-filled from `AuthService.CurrentFullName`), `ExpectedReturn`, `ConditionBefore`, `Remarks`.

**No audit log.**

### 14.4 Check-In — `TransactionService.CheckIn(transactionId, conditionAfter, remarks)`

1. `Transaction.ActualReturn = DateOnly.FromDateTime(DateTime.Today)`.
2. `Transaction.Status = "Completed"`.
3. `Transaction.ConditionAfter = conditionAfter`, `Transaction.Remarks = remarks`.
4. `Equipment.CurrentStatus = "in_store"`.
5. `SaveChanges`.

**`Equipment.ConditionGrade` is NOT updated.** Only `Transaction.ConditionAfter` is stored. **No audit log.**

### 14.5 Overdue Update — `TransactionService.UpdateStatus()`

Sets `Transaction.Status = "Overdue"` where `Status == "Pending"` and `ExpectedReturn < today`. Called externally (startup path not traced in this audit).

### 14.6 History Grid

Last 10 `Transaction` records for the equipment. Display-only.

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

Entirely code-built; no Designer layout. Left sidebar slides in on load via a timer animation. Five sections.

### 16.1 General — Company Information

**Load (`LoadSettings`):** `db.AppSettings.FirstOrDefault()`. If none → inserts default row (`CompanyName = "Q-Inventory"`, `Theme = "Light"`, `LabelWidthMm = 62`, `LabelHeightMm = 29`).

**Save (`btnSave_Click`):**
- Validates `CompanyName` non-empty; `LabelWidthMm` and `LabelHeightMm` positive integers.
- Upserts `AppSetting`. Sets `Theme = "Light"` (**hardcoded**, no theme switching implemented).
- `SaveChanges`. On General panel: shows inline `"✓ Saved"` pill. On Printing panel: shows `MessageBox`.

**System card:** App version from `Assembly.GetName().Version`. Last backup from `%AppData%\QInventory\lastbackup.txt`. Database name from loaded `AppSetting.DbName + " · " + DbServer`.

**Danger Zone:**

| Button | Action |
|--------|--------|
| Clear all logs | Loads all `EquipmentLog` rows, `RemoveRange`, `SaveChanges`. |
| Reset application data | Requires user to type `"RESET"`. Deletes all `Allocations` (one `SaveChanges`), then all `Equipment` (second `SaveChanges`). **No transaction wrapper — partial failure possible.** Users, roles, categories, settings unaffected. |

### 16.2 Backup / Restore

**Backup:** Prompts for credentials → spawns `mysqldump.exe -h {host} -u {user} [-p{pass}] {db}` as a process. Reads `StandardOutput` to `.sql` file. Writes timestamp to `lastbackup.txt`. Locates `mysqldump.exe` via: cached path → common dirs (`C:\xampp\mysql\bin`, MySQL 8.0/8.4, WAMP) → `OpenFileDialog`. **Password passed as CLI argument — visible in process listings.**

**Restore:** Selects `.sql` file → spawns `mysql.exe`, pipes file to `StandardInput`. Recommends app restart after.

### 16.3 Users Management

**Load:** `db.Users.ToList()` + `db.Roles.ToList()` + `db.UserRoles.ToList()` → role resolved per user.

| Action | Detail |
|--------|--------|
| Add User | Opens `AddEditUserForm`. |
| Edit User | Opens `AddEditUserForm(user)`. |
| Toggle Active | Flips `User.IsActive`. Blocked if `id == AuthService.CurrentUserId`. |
| Delete User | Removes `UserRoles WHERE UserId = id`, then `Users.Remove(entity)`. Blocked if own account. FK violation surfaced as error. |

**No audit log for any user operation.**

### 16.4 Change Password

1. Validates all fields non-empty.
2. Validates `newPassword == confirmPassword`.
3. Validates `newPassword.Length >= 4`.
4. `user.PasswordHash == currentPassword` — **plain-text comparison**.
5. `user.PasswordHash = newPassword` — **stored in plain text**.
6. `SaveChanges`.

Password strength meter is UI-only; only the 4-character minimum is enforced.

### 16.5 Printing

Fields: `PrinterName` (blank = system default), `LabelWidthMm` (62), `LabelHeightMm` (29). Same `btnSave_Click` as General.

**Test Print:** Generates QR code for `"TEST-LABEL-001"` via `QRCoder`, renders to `PrintPreviewDialog` at configured label size.

### 16.6 Categories

**Load:** `CategoryService.GetAll()` → `db.Categories.ToList()`.

**Add:** `CategoryService.Add(name)` — validates non-empty, checks uniqueness, inserts into `categories`.

**Delete:** `CategoryService.Delete(id)` → `db.Categories.Remove` + `SaveChanges`. **Hard delete. Does not update `Equipment.Category` — orphaned string values remain on equipment records.**

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
| Edit Equipment | Equipment | `equipment`, optionally `maintenance_records`, `procurement_types` | ❌ |
| Soft Delete | Equipment | `equipment` (`is_deleted`, `deleted_at`) | ❌ |
| Restore | Deleted Items | `equipment` | ❌ |
| Hard Delete | Deleted Items | `equipment` (row removed) | ❌ |
| Add Employee | Employees | `employees` | ❌ |
| Edit Employee | Employees | `employees` | ❌ |
| Delete Employee | Employees | `employees` (hard delete) | ❌ |
| Add / Edit / Delete Location | Locations | `locations` | ❌ |
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
| Check-Out | Check In/Out | `transactions`, `equipment` | ❌ |
| Check-In | Check In/Out | `transactions`, `equipment` | ❌ |
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
| 2 | **`ChangedByUserId` always `1`** | `AuditService` defaults `_systemUserId = 1`. No caller passes `AuthService.CurrentUserId`. All log entries attribute changes to user #1. |
| 3 | **Equipment Edit writes no audit log** | Status changes, field updates, and location changes are not logged. |
| 4 | **Check-In / Check-Out write no audit log** | These operations are invisible to the Logs module. |
| 5 | **Undo Return writes `allocation_returned`** | Same action key as a real return — cannot distinguish undo from return in the logs without reading `OldValue`/`NewValue`. |
| 6 | **Transfer pollutes Returns view** | The old allocation's `Status = "returned"` causes it to appear in the Returns module grid and Returns History report. |
| 7 | **`ApprovedByUserId = 1` hardcoded** | Both Approve and Transfer hardcode this. The acting user is never recorded. |
| 8 | **`AllocationService.Allocate` silently approves** | If `allocation.Status` is empty or blank, it is promoted to `"approved"` without requiring a separate approve step — bypassing the `pending → approved` workflow. |
| 9 | **"Overdue" is UI-only** | Allocations past their expected return date remain `"approved"` in the DB. The status is computed on every load. |
| 10 | **Category delete orphans equipment records** | Deleting a category from the `categories` table does not update `Equipment.Category`. Orphaned strings persist and continue to appear in filter dropdowns. |
| 11 | **Reset application data has no transaction wrapper** | Allocations deleted in one `SaveChanges`, equipment in a second. If the second fails, allocations are gone but equipment remains. |
| 12 | **Backup password visible in process list** | `mysqldump` password is passed as `-p{pass}` in the command-line argument. |
| 13 | **Danger Zone Decommission reason lost if logs cleared** | `DecommissionForm` reads the reason from `equipment_logs`. Clearing logs via Settings removes the reason from the decommission view. |
| 14 | **Returns History Report excludes Check-In/Out completions** | The report only queries `allocations WHERE Status = "returned"`. Completed `transactions` are not shown. |
| 15 | **Soft delete has no FK guard** | `EquipmentService.Delete` sets `IsDeleted = true` with no check for active allocations or transactions. Allocated equipment can be soft-deleted. |
| 16 | **LocationsForm has no pagination** | All locations are loaded at once. Will be slow at scale. |
| 17 | **Employee hard-delete breaks historical allocations** | Non-approved (historical) allocation rows keep the `EmployeeId` FK after the employee is deleted. |
| 18 | **Audit failure is silently swallowed** | `AuditService.Log` catches all exceptions. If the DB insert fails (connectivity, FK), the triggering operation has already committed and no error is shown. |
| 19 | **"Expected Return Date Updated" is a free-form string** | Not a defined `AuditService` constant. `LogsForm.FormatAction` has no case for it — it displays verbatim, inconsistent with other formatted action types. |
| 20 | **`CreatedByUserId = 1` hardcoded in Equipment Add** | The add form sets `_equipment.CreatedByUserId = 1` unconditionally; the session user is not recorded. |
