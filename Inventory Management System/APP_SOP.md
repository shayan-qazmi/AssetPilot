# AssetPilot — Application Standard Operating Procedure (SOP)

> **Ground-truth document** — every claim is traced from source code:
> UI form event handler → service method → EF Core / DbContext → database table + column.
> No assumption-based descriptions. Source file + method cited per section.
> Last updated: 2026-09-13

**Tech Stack:** WinForms · C# · .NET 8 · Entity Framework Core · MariaDB / MySQL
**Database:** `inventory_management` @ `localhost` (or server IP)

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
22. [New Features & Recent Changes (2026-09-05)](#22-new-features--recent-changes-2026-09-05)
23. [New Features & Recent Changes (2026-09-13)](#23-new-features--recent-changes-2026-09-13)
24. [New Features & Comprehensive Overhaul (2026-11-10)](#24-new-features--comprehensive-overhaul-2026-11-10)

---

## 1. Overview & Roles

**AssetPilot** is a Windows desktop application for tracking physical IT/office equipment across an organization. It covers the full equipment lifecycle: procurement → storage → allocation → return / transfer → decommission.

Routine success confirmations across the app (save, restore, delete, approve, edit) render as auto-dismissing toast notifications styled to match their respective action buttons (e.g. green for Approve, yellow for Edit, purple for Undo/Restore, red for Delete/Decommission) rather than blocking `MessageBox` dialogs. Destructive-action confirmations ("Are you sure?") still use `MessageBox`.

### Roles & Access Control

Roles are stored in the `roles` table and linked via `user_roles`. Session identity is cached in `AuthService` (static) for the duration of the session. All forms read `AuthService.CurrentRole` and `AuthService.CurrentUserId`.

| Role | Access |
|------|--------|
| **Admin** | All screens and all actions including delete, Settings, user management, decommission |
| **Operator** | All screens **except** Settings, Decommission, and Deleted Items. Delete Equipment button is hidden. |

`AdminOnlyButtons` in `DashboardForm` = `{ btnDecommission, btnSettings, btnDeletedItems }` — these are hidden for non-admin roles via `ApplyRolePermissions()`. If non-admin users attempt to access restricted forms, access is denied and a warning toast notification is displayed.

---

## 2. Login

**Source:** `Forms/LoginForm.cs`

### 2.1 Login Flow — `btnLogin_Click`

1. Validates both `txtUsername` and `txtPassword` are non-empty; shows error if not.
2. Queries `db.Users.FirstOrDefault(u => u.Username == username && u.IsActive == true)`. If no match → `"Invalid username or password."` (same generic message for both not-found and wrong-password to prevent username enumeration).
3. Compares password using `PasswordHelper.Verify(password, user.PasswordHash)` — uses **BCrypt hashing** for all modern accounts, with automatic backward compatibility for unmigrated legacy plaintext accounts.
4. On success: calls `AuthService.SetCurrentUser(user)` to cache the session.
5. Calls `SaveRememberedUser(username)` — if `_keepMeLoggedIn` is `true`, writes `{StartupPath}/remember_me.txt` with `["True", username]`. If `false`, deletes the file.
6. Stops the animation timer, sets `DialogResult = OK`, closes.

### 2.2 "Keep Me Logged In" Toggle

- `pnlToggle` is a custom-painted pill toggle (GDI+). Clicking it flips `_keepMeLoggedIn`.
- `LoadRememberedUser()` (called in constructor): reads `remember_me.txt`. If `lines[0] == "True"`, pre-fills `txtUsername` and moves focus to `txtPassword`. **The password is never stored.** Authentication always requires manual password entry.

### 2.3 Enter Key & UI Feedback

- `ProcessCmdKey` intercepts `Keys.Enter` → triggers `btnLogin_Click`.
- Incorrect credentials trigger a gentle horizontal shake animation (`ShakeControl`) on the input field containers with red border flash (`#DC2626`).

### 2.4 Self-Service Password Recovery ("Forgot Password")

The login screen features an offline, self-service password reset panel (`pnlForgot`) with dual verification methods:

- **Method Selector Tabs:**
  - `✉ Work Email`: Requires the registered work email matching `user.Email`.
  - `🔑 Recovery PIN`: Requires either the user's personal recovery PIN or the organization master PIN (`RecoveryPinHelper`).
- **Input Padding & Layout:**
  - Verification textbox (`txtVerify`) has `Left = 54` with a 22px clear gutter past the left icon (`✉` or `🔑`), ensuring text and placeholders (`name@company.com`, `Enter personal PIN or Admin PIN`) are never clipped.
- **Strict Input Enforcement:**
  - **Recovery PIN mode:** Allows **numerals/numbers only** (`char.IsDigit`). All letters, spaces, and symbols are rejected on `KeyPress`, and pasted non-digits are automatically stripped.
  - **Work Email mode:** Rejects spaces (`e.KeyChar == ' '`) and disallows characters outside valid email sets (`[a-zA-Z0-9@._-+]`). Rejects typing duplicate `@` signs, and strips whitespace on paste.
  - Toggling between tabs automatically resets the input field to prevent carrying mismatched characters across modes.
- **Verification Flow:**
  - Verification is evaluated live on `TextChanged` (`CheckVerification()`).
  - When verified, green checkmark `✓` appears, input border turns emerald (`#10B981`), hint confirms match, and `New Password` / `Confirm New Password` fields and `Reset Password` button are enabled.
  - On reset: hashes new password with `PasswordHelper.Hash()`, updates `PasswordChangedAt` and `UpdatedAt`, commits to database, logs event to `SecurityAuditLogger`, shows success toast, and returns user to the login screen with username prefilled.

### 2.5 Database Table Read

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

| Card | DB Query / Source | Behaviour |
|------|-------------------|-----------|
| Total Equipment | `COUNT(*) FROM equipment WHERE IsDeleted = false` | Stat summary |
| In Store | `WHERE CurrentStatus = "in_store"` | Stat summary |
| Allocated | `WHERE CurrentStatus = "allocated"` | Stat summary |
| Under Repair | `WHERE CurrentStatus = "under_repair"` | Stat summary |
| Decommissioned | `WHERE CurrentStatus = "decommissioned"` | Stat summary |
| Employees | `COUNT(*) FROM employees` | Stat summary |
| Currently Overdue | Allocations where `Status == "approved"` and `ExpectedReturnDate < today` | Dark amber (`#FFA000`). Clicking opens Allocation screen pre-filtered to overdue items. |

Cards entrance with a bounce animation (`CardAnimState`, driven by `_animTimer` at 15 ms ticks).

### Charts (custom GDI+, no third-party library)

| Chart | Type | Data Source |
|-------|------|-------------|
| Status Breakdown | Donut | `_cntInStore`, `_cntAllocated`, `_cntUnderRepair`, `_cntDecommission` |
| Category Breakdown | Horizontal Bar | Distinct `Category` grouped counts from `equipment` |
| 6-Month Allocation Trend | Line | `allocations` grouped by `AllocationDate` month, last 6 months |

### Top Employees by Holdings Card

Displays top employees with active equipment holdings.
- **Rank-based multi-colored progress bars** representing allocation volume.
- **Allocation count label** displayed on the left side of each bar.
- **Hover tooltip:** Moving the cursor over any employee name or bar displays detailed employee information and a complete breakdown of their held allocations.

### Sidebar Collapse & Animation

- **Sidebar Toggle Button (`_btnSidebarToggle`):** Located at the top of the sidebar.
- Clicking the toggle smoothly collapses or expands the navigation sidebar using `_sidebarCollapseTimer`.
- In collapsed mode, navigation items display icon-only view while preserving full form loading functionality.

### Global Search (Ctrl + K)

- **Keyboard Shortcut:** `Ctrl + K` opens the Global Search dialog (`EntitySuggestHelper`).
- Performs real-time fuzzy search across Equipment, Allocations, Employees, and Locations.
- Direct selection jumps directly to the target record or screen.

### 3.1 Dark Mode Toggle Switch

**Source:** `DashboardForm.BuildThemeToggle()`, `ThemeHelper.ToggleTheme()`, `ThemeHelper.LoadTheme()`

A pill-shaped animated toggle switch is embedded in the **top bar**, to the left of the notification bell. Hovering over the theme change emoji displays a visual hover highlight.

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

- Displays a live notification bell with hover visual effect.
- Shows a live count badge for overdue allocations (`Allocations WHERE Status == "approved" AND ExpectedReturnDate < today`), open maintenance records (`MaintenanceRecords WHERE Status != "resolved"`), and low stock items.
- Dropdown popup (`NotificationHelper.ShowNotificationPopup`) lists all alerts with direct click navigation.

---

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
| Import | Imports equipment from a CSV file. **Hover tooltip:** "Make sure the category of the equipment you are about to import is already added in the Category screen in Settings." |
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

**Data load — `LoadLocations()`:** `LocationService.GetAll()` → `db.Locations.ToList()`.

**Grid columns:** `ID` (hidden) · `Location Name` · `Building` · `Room Number` · `Room Name`

**Actions:** Add / Edit (via `AddEditLocationForm`) / Delete (via `LocationService.Delete(id)`) / Export CSV / Live Search.

**Pagination:**
- Standardized pagination with `PageSize = 50` records per page.
- Bottom pagination control with page summary (`Page X of Y`), `Prev` / `Next` navigation buttons, and responsive grid layout.

Add/Edit/Delete each write to `equipment_logs` via `_audit.Log(0, ...)` (`location_created`/`location_updated`/`location_deleted`), with `EquipmentId` written as `0` since locations have no associated equipment record.

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

- **Eligibility Filtering:** The decommission dialog query excludes any equipment currently allocated (`CurrentStatus == "allocated"` or active/pending allocation in `allocations`), and any equipment with an active or overdue checkout in `transactions`. Only assets currently in store or under repair are eligible to be decommissioned.
- **Save-Path Guard:** Both the dialog and the form verify against `allocations` and `transactions` before updating the status, preventing decommissioning of active assets and prompting the operator to process returns first.
- **Reason & Notes:** User picks reason (`Damaged`, `Obsolete`, `Beyond Repair`, `End of Life`, `Lost`, `Other`) and optional contextual notes.

On confirm:
1. `Equipment.CurrentStatus = "decommissioned"`.
2. `SaveChanges()`.
3. `_audit.Log(equipment.Id, "decommission_approved", oldStatus, reason + " — " + notes)`.

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
| `ActionReturnUndone` | `return_undone` |
| `ActionDecommissionRequested` | `decommission_requested` |
| `ActionDecommissionApproved` | `decommission_approved` |
| `ActionDecommissionRejected` | `decommission_rejected` |
| `ActionDecommissionUndone` | `decommission_undone` |
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
| *(free-form string)* | `Allocation Request Deleted` |
| *(injected by `SettingsForm`)* | `settings_data_delete` |

### 12.3 Who Writes Audit Logs

| Action | Action type |
|--------|-------------|
| Equipment Add | `equipment_created` |
| Equipment Edit | `equipment_updated` (via `BuildEquipmentDiff`) |
| Allocation Create | `allocation_requested` |
| Allocation Approve | `allocation_approved` |
| Allocation Delete (pending/available rows) | `Allocation Request Deleted` (free-form string) |
| Allocation Return (from Allocation screen) | `allocation_returned` |
| Returns – Process Return | `allocation_returned` |
| Returns – Undo Return | `return_undone` ✅ (**fixed** — was `allocation_returned`) |
| Allocation – Edit Expected Return Date | Free string `"Expected Return Date Updated"` |
| Transfer | `transferred` |
| Decommission | `decommission_approved` |
| Decommission – Restore (Undo) | `decommission_undone` |
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
| Settings – Clear All Logs | `settings_data_delete` (+ `SecurityAuditLogger`) |
| Settings – Reset Application Data | `settings_data_delete` (+ `SecurityAuditLogger`) |

**Not logged:** Equipment soft-delete, restore, hard-delete, user management actions, password changes.

### 12.4 LogsForm Data Load & Advanced Filter Architecture

**Toolbar & Header Controls:**
- `Refresh` button (clears search, resets to page 1, refreshes data and autocomplete suggestions).
- `Export` dropdown/button (exports active view to CSV with full audit details).
- Rounded Search Bar with real-time debounced search & autocomplete (`EntitySuggestHelper`).
- `● Live – updated [time]` dynamic status indicator reflecting the last query timestamp in Pakistan Time (PKT).
- Density selector (`Compact`, `Comfortable`, `Cozy`).
- `Reset` button (clears search, resets category pills to `All`, user dropdown to `All users`, date dropdown to `All time`, and resets pagination).

**Action Category Filter Pills:**
Located on the secondary filter row with dedicated color matching that reflects the row badge colors:
- `All`: Charcoal (`#1F2937`) when active.
- `Created`: Dark green (`#1B5E20`) — filters `equipment_created`, `employee_created`, `location_created`.
- `Updated`: Deep orange (`#E65100`) — filters `equipment_updated`, `employee_updated`, `location_updated`, `Expected Return Date Updated`.
- `Allocation`: Indigo (`#393CB4`) — filters `allocation_requested`, `allocation_approved`, `allocation_rejected`, `allocation_returned`, `allocation_overdue`, `return_undone`, `Allocation Request Deleted`.
- `Check In/Out`: Dark blue (`#01579B`) — width 110px preventing text cutoff — filters `checked_out`, `checked_in`.
- `Transfers`: Deep purple (`#4A148C`) — filters `transferred`.
- `Decommission`: Red (`#D32F2F`) — width 118px preventing text cutoff — filters `decommission_requested`, `decommission_approved`, `decommission_undone`, `decommission_rejected`.
- `Deleted`: Deep red (`#B71C1C`) — filters `equipment_deleted`, `employee_deleted`, `location_deleted`, `equipment_restored`, `settings_data_delete`.

**User & Date Range Filters:**
- `All users` dropdown populated dynamically with distinct system users.
- `Date Range` dropdown defaults to **`All time`** (with options for `Last 7 days`, `Last 30 days`, `All time`, and `Custom...` with an inline popup date picker).

**Query Execution:**
- Filters are applied server-side on `IQueryable<EquipmentLog>` before pagination (`SKIP`/`TAKE`).
- With search filter: fuzzy/exact matching on equipment code, QR, user name, and diff values.

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

**Equipment Inspection & Fast Action Hub.** Primarily a read-only lookup tool with direct shortcuts to transactional operations (Check-Out, Check-In, Allocate).

### 13.1 Input & Auto-Search

- `txtQRCode` is focused on load. `TextChanged` → starts 300 ms debounce timer (`_scanTimer`). On tick or `Enter` key → `Search()`.
- `EquipmentSuggestHelper` provides a fuzzy auto-suggestion dropdown for quick keyboard matching.
- **Camera Scanning:** Integrated live webcam feed using AForge and ZXing with camera device selection, freeze/unfreeze frame control, and continuous barcode decoding.

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

### 13.3 Match View & Equipment History

- **Detail Card:** Populates all equipment fields, current allocation (employee and location), procurement type, purchase date, and colour-coded status badge.
- **Equipment History Grid:** Populates equipment audit logs capped to the **50 most recent entries** (`Take(50)`).
  - Title displays dynamic record counter: `📜 Equipment History (showing 50 of {totalLogCount})` or `📜 Equipment History ({count})`.
  - Color-coded badges for all action types via `AuditLogDisplay`.
  - Double-clicking any history row opens the full `LogDetailPopup`.
- **Fast Action Hub:** Direct buttons for `Check Out`, `Check In`, `View QR`, `View Picture`, `View Details`, `Print Label`, and `Allocate`.
- **Batch Capabilities:**
  - **Recent Scans Strip:** Keeps the last 8 scanned items as quick-access chips.
  - **Bulk Scan Mode:** Aggregates scanned codes in memory with one-click CSV export (`BulkScan_{timestamp}.csv`).

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

**Source:** `Forms/ReportsForm.cs`, `Forms/ReportsForm.Designer.cs`

**Modern Analytics & Reporting Engine.** Read-only reporting suite with real-time KPI generation, dynamic column controls, multi-format exports, and single-row responsive filtering.

### 15.1 UI Architecture & Default View

- **Default Initial State:** On opening the Reports screen, **no data list is loaded by default**.
  - KPI summary cards display neutral placeholder values (`—`).
  - The DataGrid remains empty, prompting the user to select an operational report tab.
  - **Pagination is hidden on the default view.** In its place at the bottom bar, the application developer attribution watermark is displayed:
    `Designed and Developed by 🐙@shayan-qazmi/📷@shayanqazmi`
  - As soon as any report tab is selected, data queries the database, KPI cards compute live metrics, the grid populates, and the watermark smoothly transitions into the active pagination bar (`Showing 1-25 of 385`).

### 15.2 Five Operational Report Tabs

Located on the top toolbar with distinct thematic accent colors:

| Tab | Theme Color | Target Dataset & Purpose |
|-----|-------------|--------------------------|
| **Equipment ▾** | Indigo (`#4F46E5`) | Full equipment asset register with split-button category picker (`All Categories` or specific category). Shows code, name, category, make/model, assignment, location, status. |
| **Employee** | Purple (`#7C3AED`) | Active workforce equipment holdings summary. Summarizes total items ever allocated vs. items currently held by each employee. |
| **Allocation** | Amber/Orange (`#EA580C`) | Active and pending asset allocations. Lists allocated employee, location, assignment dates, and expected returns. |
| **Returns** | Royal Blue (`#4338CA`) | Asset return logs and custody duration analysis. Flags allocations exceeding 30 days. |
| **Check In/Out** | Sky Blue (`#0284C7`) | Daily operational check-out/check-in borrow transaction log. |

### 15.3 Export Capabilities

Four one-click export actions on the top right:

- **Print:** System GDI+ multi-page formatted printing via `PrintDocument` with native print preview.
- **Export PDF:** Direct PDF export via print subsystem.
- **Export Excel:** Formatted `.xlsx` spreadsheet generation using `ClosedXML`. Includes bold company header, color-coded table headers, and automatic column width adjustment (`AdjustToContents()`).
- **Export CSV:** Standardized comma-separated values export with RFC 4180 quoting and character escaping.

### 15.4 Single-Row Filter Toolbar

Designed to fit all controls cleanly on a single row without wrapping:

1. **`Apply` Button:** High-contrast emerald green button (`#10B981`) placed on the far left.
2. **Date Quick-Filter Pills:** `Today`, `7 days`, `30 days` (active default), `This year`, `Custom...`.
   - Selecting a date filter filters records against the relevant date attribute (e.g. `PurchaseDate`, `AllocationDate`, `ActualReturnDate`, or `TransactionDate`).
   - **Custom Date Range Dialog:** Dedicated modal dialog (`Width 340, Height 230`) providing `From Date` and `To Date` pickers and an unclipped `Apply Range` button.
3. **Vertical Divider:** Separates date controls from dimensional dropdowns.
4. **Location Dropdown (`All locations ▾`):** Dynamically populated from active database locations.
5. **Status Dropdown (`Any status ▾`):** Filters by `allocated`, `in_store`, `under_repair`, `decommissioned`, or `pending`.
   *(Note: The category dropdown was integrated directly into the Equipment tab to preserve single-line toolbar layout).*
6. **Live Search Box (`Filter rows...`):** Real-time text filter across all visible grid columns.
7. **`Reset` Button:** Restores all filters to defaults (`30 days`, all locations, any status, clear search), unhides all table columns, and clears active grid sorting.

### 15.5 KPI Summary Cards

Four dynamic KPI summary cards displayed above the grid:

- **Total rows:** Indigo accent bar (`#7C3AED`). Total record count matching current filter criteria.
- **Allocated:** Amber accent bar (`#F59E0B`). Count of assets currently assigned or checked out.
- **In store:** Emerald green accent bar (`#10B981`). Count of assets available in inventory.
- **Needs attention:** Red accent bar (`#EF4444`). Count of assets marked `under_repair`, `decommissioned`, or overdue.

### 15.6 Grid Display & Formatting

- **Status Badge Styling:**
  - `Under Repair`: Displayed with formatted text (no underscore) and **light blue badge** (`#E0F2FE` background / `#0284C7` text).
  - `In Store`: Soft green badge (`#DCFCE7` / `#15803D`).
  - `Allocated`: Soft amber badge (`#FEF3C7` / `#B45309`).
  - `Decommissioned`: Soft red badge (`#FEE2E2` / `#B91C1C`).
- **Column Customization (`Columns`):** Dropdown menu with checkboxes to toggle any column's visibility dynamically.
- **Sorting (`Sort ▾`):** Interactive column header sorting and dedicated sort menu (Ascending / Descending).
- **Pagination:** Configurable page size (`25 per page`, `50 per page`, `100 per page`), `Prev` / `Next` buttons, and record position summary.

---

## 16. Settings

**Source:** `Forms/SettingsForm.cs`

Entirely code-built; no Designer layout. Left sidebar slides in on load via a timer animation. **Six sections** (five for Operators; System Configuration is Admin-only).

### 16.1 General — Company Information

**Load (`LoadSettings`):** `db.AppSettings.FirstOrDefault()`. If none → inserts default row (`CompanyName = "AssetPilot"`, `Theme = "Light"`, `LabelWidthMm = 62`, `LabelHeightMm = 29`).

**Save (`btnSave_Click`):**
- Validates `CompanyName` non-empty; `LabelWidthMm` and `LabelHeightMm` positive integers.
- Upserts `AppSetting`. Sets `Theme = "Light"` (**hardcoded**, no theme switching implemented).
- `SaveChanges`. On General panel: shows inline `"✓ Saved"` pill (Save button disabled until Company Name is changed — dirty-check via `_originalCompanyName`). On Printing panel: shows `MessageBox`.

**System card:** App version from `Assembly.GetName().Version`. Last backup from `%AppData%\AssetPilot\lastbackup.txt`. Database name from loaded `AppSetting.DbName + " · " + DbServer`.

**Danger Zone:**

| Button | Action |
|--------|--------|
| Clear all logs | Confirms with MessageBox → loads all `EquipmentLog` rows, `RemoveRange`, `SaveChanges`. Shows toast with deleted count. |
| Reset application data | Opens `ShowTypedConfirm` dialog — user must type `"RESET"` to enable the confirm button. Deletes all `Allocations` (one `SaveChanges`), then all `Equipment` (second `SaveChanges`). **No transaction wrapper — partial failure possible.** Users, roles, categories, settings unaffected. |

### 16.2 Backup / Restore

**Backup:** Prompts for credentials (Host, Database, Username, Password dialog) → spawns `mysqldump.exe -h {host} -u {user} [-p{pass}] {db}` as a process. Reads `StandardOutput` to `.sql` file. Writes timestamp to `%AppData%\AssetPilot\lastbackup.txt`. Locates `mysqldump.exe` via: cached path in `%AppData%\AssetPilot\mysqltools.txt` → common dirs (`C:\xampp\mysql\bin`, MySQL 8.0/8.4, WAMP64) → `OpenFileDialog` prompt. **Password passed as CLI argument — visible in process listings.** Success/failure shown as toast notification.

**Restore:** Selects `.sql` file → confirms destructive overwrite with MessageBox → spawns `mysql.exe`, pipes file to `StandardInput`. Success/failure shown as toast. Recommends app restart after.

### 16.3 Users Management

**Source:** `Forms/SettingsForm.cs` (Users panel) + `Forms/AddEditUserForm.cs`

**Load:** `db.Users.ToList()` + `db.Roles.ToList()` + `db.UserRoles.ToList()` → role resolved per user.

**Grid columns:** `ID` · `Username` · `Full Name` · `Email` · `Role` · `Status` (🟢 Active / 🔴 Inactive, colour-coded).

| Action | Detail |
|--------|--------|
| Add User | Opens `AddEditUserForm` (add mode). On OK → `LoadUsers()`. Logs to `SecurityAuditLogger`. |
| Edit User | Opens `AddEditUserForm(user)` (edit mode). Username field disabled in edit mode. Logs to `SecurityAuditLogger`. |
| Toggle Active | Flips `User.IsActive`. Blocked if `id == AuthService.CurrentUserId`. Uses MessageBox confirmation. Logs to `SecurityAuditLogger`. |
| Delete User | Removes `UserRoles WHERE UserId = id`, then `Users.Remove(entity)`. Blocked if own account. Shows toast. Logs to `SecurityAuditLogger`. |

**Double-click row → `UserDetailPopup`:** Shows user avatar, username, full name, email, role, status.

**`AddEditUserForm` — Mandatory Account Recovery Credential Policy:**
- Banner alert: `⚠️ Enter either PIN or Email or both (Mandatory for recovery)`
- **Username** (required, unique; disabled in edit mode)
- **Full Name** (required)
- **Work Email** (valid email format, unique across users)
- **Recovery PIN** (4-8 digit numeric PIN for offline password resets; stored securely via `RecoveryPinHelper.SetUserPin`)
- At least one recovery credential (Email or Recovery PIN) is **mandatory**; saving is blocked if both are empty.
- **New Password** / **Confirm Password** (required in add mode; blank = keep existing in edit mode). Hashed via `PasswordHelper.Hash()`.
- **Role** (required ComboBox from `roles` table)
- **Profile Picture** (optional): Upload button → `OpenFileDialog` (JPG/PNG/BMP/GIF) → staged locally. Stored in `NetworkPaths.UserPicturesDir` or local avatar store.
- **Is Active Account** checkbox

All administrative user management actions write security audit events to `%AppData%\AssetPilot\security_audit.log`.

### 16.4 Change Password

1. Validates all fields non-empty.
2. Validates `newPassword == confirmPassword`.
3. Validates `newPassword.Length >= 6`.
4. Validates current password using `PasswordHelper.Verify(currentPassword, user.PasswordHash)` with BCrypt hashing and legacy migration fallback.
5. Hashes new password using `PasswordHelper.Hash(newPassword)` (BCrypt).
6. Updates `user.PasswordChangedAt = DateTime.UtcNow`, `user.UpdatedAt = DateTime.UtcNow`.
7. `SaveChanges()`.
8. Logs password change event to `SecurityAuditLogger`.

**Visual improvements:**
- All three password fields are rounded input panels with an **eye (👁) icon** for reveal/hide toggle.
- A live **password strength meter** (coloured progress bar + label: Weak/Medium/Strong) appears beneath the New Password field as the user types. Scoring: length ≥ 4 (+1), ≥ 8 (+1), mixed case (+1), digit (+1), symbol (+1).
- Security Tips card on the right panel lists best-practice guidance.

### 16.4a Offline Recovery PIN Configuration

**Source:** `Helpers/RecoveryPinHelper.cs`, `Forms/SettingsForm.cs` (`ShowRecoveryPinDialog()`)

Enables administrators to view and configure the organization-wide master recovery PIN:
- **Default Master PIN:** `123456` (100% numeric, compatible with the login recovery PIN input requirements).
- **Custom PIN:** Stored securely at `%AppData%\AssetPilot\recovery_pin.txt`.
- **Reset to Default:** Dedicated button to reset the organization master PIN back to `123456`.
- **Per-User Recovery PINs:** Managed via `%AppData%\AssetPilot\user_pins.json` with thread-safe file serialization. Verified by `RecoveryPinHelper.VerifyPin(pin, username)`.

### 16.5 Printing

Fields: `PrinterName` (blank = system default), `LabelWidthMm` (62), `LabelHeightMm` (29). Same `btnSave_Click` as General.

**Test Print:** Generates QR code for `"TEST-LABEL-001"` via `QRCoder`, renders to `PrintPreviewDialog` at configured label size.

### 16.6 Categories

**Load:** `CategoryService.GetAll()` → `db.Categories.ToList()`. Displayed in a custom owner-drawn `ListBox` with numbered items.

**Add:** `CategoryService.Add(name)` — validates non-empty, checks uniqueness, inserts into `categories`. Enter key in the text field triggers add.

**Delete:** `CategoryService.Delete(id, reassignOrphanedToUncategorized)`:
- If no equipment is using the category: deletes the category directly.
- If equipment is assigned to the category: prompts the administrator with the count of affected assets. If confirmed, all affected equipment records have their category updated to `"Uncategorized"` (ensuring `"Uncategorized"` exists in `categories`), preventing any orphaned category strings from persisting on equipment or filter dropdowns. If declined, deletion is safely cancelled.

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
| 💾 Save Connection | Calls `DbConnectionSettingsService.Save(settings, plainPassword)`. Password is **DPAPI-encrypted** (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`) before writing to `%AppData%\AssetPilot\dbconnection.json`. Shows toast on success. |

**Connection Status card:** Displays `Current Status` (🟢 Connected / 🔴 Disconnected), `Active Server` (database · host), `Last Successful Connection` (timestamp). Values are populated by `RefreshFromSaved()` which loads the saved settings and tests the connection on panel load.

**`DbConnectionSettingsService` key methods:**
- `Load()`: Deserializes `dbconnection.json` → `DbConnectionSettings`.
- `Save(settings, plainPassword)`: DPAPI-encrypts password → serializes to JSON.
- `DecryptPassword(settings)`: DPAPI-decrypts `settings.EncryptedPassword`.
- `TestConnection(settings, plainPassword, out error)`: Uses EF Core `Database.CanConnect()` — no extra NuGet dependency.
- `SeedIfMissing(...)`: One-time migration — if no settings file exists, seeds from the previously hardcoded connection string so existing installs don't break on upgrade.

**File storage:** `%AppData%\AssetPilot\dbconnection.json`

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
| Delete Allocation (pending / available) | Allocation | `allocations` (hard delete), `equipment_logs` | ✅ `Allocation Request Deleted` |
| Delete Allocation (approved) | Allocation | Blocked | — |
| Edit Expected Return | Allocation | `allocations`, `equipment_logs` | ✅ free-string |
| Process Return | Returns | `allocations`, `equipment`, `equipment_logs` | ✅ `allocation_returned` |
| Edit Return Record | Returns | `allocations`, `equipment` | ❌ |
| Undo Return | Returns | `allocations`, `equipment`, `equipment_logs` | ✅ `return_undone` |
| Transfer | Transfer | `allocations` (×2), `equipment_logs` | ✅ `transferred` |
| Decommission | Decommission | `equipment`, `equipment_logs` | ✅ `decommission_approved` |
| Decommission → Restore | Decommission | `equipment`, `equipment_logs` | ✅ `decommission_undone` |
| Check-Out | Check In/Out | `transactions`, `equipment`, `equipment_logs` | ✅ `checked_out` |
| Check-In | Check In/Out | `transactions`, `equipment`, `equipment_logs` | ✅ `checked_in` |
| Update Overdue | Check In/Out | `transactions` | ❌ |
| Add / Toggle / Delete User | Settings | `users`, `user_roles` | ❌ (SecurityAuditLogger file only) |
| Change Password (own) | Settings | `users` | ❌ (SecurityAuditLogger file only) |
| Admin Change Password (other user) | Settings (AddEditUserForm) | `users` | ❌ (SecurityAuditLogger file only) |
| Add Category | Settings | `categories` | ❌ |
| Delete Category | Settings | `categories` (hard delete; `Equipment.Category` not updated) | ❌ |
| Save Settings | Settings | `app_settings` | ❌ |
| Clear All Logs | Settings | `equipment_logs` (all rows deleted) + `SecurityAuditLogger` + `settings_data_delete` log | ✅ `settings_data_delete` |
| Reset Application Data | Settings | Logs, Location Histories, Maintenance Records, Transactions, Allocations, Equipment, Employees, Locations (in FK order) + `SecurityAuditLogger` + `settings_data_delete` log | ✅ `settings_data_delete` |

---

## 19. Known Issues & Observations

| # | Issue | Status / Detail |
|---|-------|-----------------|
| 1 | **~~Passwords stored in plain text~~** | ✅ **FIXED** — Upgraded to **BCrypt** hashing (`PasswordHelper.Hash` / `PasswordHelper.Verify`) across Login, Settings, User Management, and Password Reset. Unmigrated legacy accounts are automatically verified and upgraded upon subsequent password updates. |
| 2 | **~~`ChangedByUserId` always `1`~~** | ✅ **FIXED** — `AuditService` constructor defaults `_systemUserId = AuthService.CurrentUserId`. All audit entries correctly attribute actions to the authenticated session user. |
| 3 | **~~Undo Return writes `allocation_returned`~~** | ✅ **FIXED** — `BtnDelete_Click` in `ReturnsForm` logs `AuditService.ActionReturnUndone` (`"return_undone"`). Displays as "Returned to Active Allocation" in logs. |
| 4 | **Transfer pollutes Returns view** | The previous allocation row is marked `Status = "returned"` during a transfer, causing it to appear in the Returns module grid and Returns History report without a distinguishing `"transferred"` flag. |
| 5 | **~~`ApprovedByUserId` / `CreatedByUserId` hardcoded~~** | ✅ **FIXED** — Dynamic user attribution implemented. Equipment creation (`AddEditEquipmentForm.cs`, `EquipmentService.cs`), allocation creation/approval (`AllocationForm.cs`, `AllocationService.cs`), and transfers (`TransferForm.cs`) dynamically attribute records to `AuthService.CurrentUserId` with fallback only for offline unauthenticated seeds. |
| 6 | **`AllocationService.Allocate` silently approves** | If `allocation.Status` is empty or null, it is promoted directly to `"approved"` without requiring a separate approve step — bypassing the `pending → approved` approval queue. |
| 7 | **"Overdue" is UI-only** | Allocations past their expected return date remain `"approved"` in the database. Overdue status is computed dynamically in the UI on load. |
| 8 | **~~Category delete orphans equipment records~~** | ✅ **FIXED** — `CategoryService.Delete` and `SettingsForm` verify equipment usage. If items use the category, the administrator is prompted to reassign them to `"Uncategorized"`. Affected equipment records are batch-updated so no orphaned strings persist. |
| 9 | **~~Reset application data has no single transaction wrapper~~** | ✅ **FIXED** — Wrapped in a single atomic database transaction (`using var tx = db.Database.BeginTransaction()`) with automatic commit/rollback. Includes automatic pre-reset safety backup (`.sql` via `--defaults-extra-file` or structured `.json` snapshot fallback) archived to `%AppData%\AssetPilot\Backups\`. |
| 10 | **~~Backup password visible in process list~~** | ✅ **FIXED** — Backups and pre-reset dumps invoke `mysqldump` using secure temporary option files (`--defaults-extra-file="{cnfFile}"`) rather than passing `-p{password}` in process command-line arguments. |
| 11 | **Danger Zone Decommission reason lost if logs cleared** | `DecommissionForm` reads the decommission reason from `equipment_logs`. Clearing logs via Settings removes the displayed reason from the decommission view. |
| 12 | **~~Returns History Report excludes Check-In/Out completions~~** | ✅ **FIXED** — Modernized Returns History report in `ReportsForm.cs` unifies both allocation returns and completed borrow transactions (`transactions WHERE Status = 'Completed'`) with distinct source badges (`Allocation`, `Transferred`, `Check In/Out`). |
| 13 | **~~Soft delete has no active allocation / transaction guard~~** | ✅ **FIXED** — `EquipmentService.Delete`, `DeleteRange`, and `EquipmentForm.btnDelete_Click` rigorously verify against active allocations, pending allocations, and active/overdue borrow check-outs, preventing deletion and prompting the user to return assets first. |
| 14 | **~~LocationsForm has no pagination~~** | ✅ **FIXED** — Standardized pagination with `PageSize = 50`, `Prev` / `Next` buttons, and record position summary implemented in `LocationsForm.cs`. |
| 15 | **Employee hard-delete breaks historical allocations** | Non-approved (historical) allocation rows keep the `EmployeeId` foreign key after an employee is hard-deleted. |
| 16 | **Audit failure is silently swallowed** | `AuditService.Log` catches all exceptions and logs them via `Logger.LogError`. If the database write fails, the triggering business transaction commits regardless. |
| 17 | **"Expected Return Date Updated" is a free-form string** | Not an `AuditService` constant, though explicitly mapped and badge-styled in `LogsForm`. |
| 18 | **~~`CreatedByUserId = 1` hardcoded in Equipment Add~~** | ✅ **FIXED** — Unified under Issue #5 fix. |
| 19 | **DB password DPAPI-bound to OS user** | Database credentials in `dbconnection.json` are encrypted via `ProtectedData.Protect` with `CurrentUser` scope, preventing file sharing across different Windows accounts. |
| 20 | **`BuildSystemConfigPanel` has duplicate `return outer`** | Lines 2307–2308 in `SettingsForm.cs` both return `outer`. The second statement is unreachable dead code. |
| 21 | **`Allocation Request Deleted` is a free-form audit string** | Hardcoded audit string rather than an `AuditService` constant. |
| 22 | **~~Decommission dialog accepts all non-decommissioned equipment~~** | ✅ **FIXED** — `btnDecommission_Click` pre-filters available equipment to exclude any items that are currently allocated or checked out. The dialog and save paths strictly reject decommissioning active assets and instruct operators to process returns first. |



---

### 19.1 Prioritized Limitations Roadmap & Removal Sequence

To systematically eliminate remaining technical debt and operational edge cases, remaining items are prioritized into three distinct tiers:

```
┌────────────────────────────────────────────────────────────────────────┐
│  TIER 1: Critical & Architectural Integrity [ALL COMPLETED ✅]         │
│  • ✅ Dynamic ApprovedBy / CreatedBy User Attribution (#5, #18)        │
│  • ✅ Equipment Soft Delete Active & Pending Allocation Guard (#13)    │
│  • ✅ Database Backup Credential Protection via Option Files (#10)     │
│  • ✅ Atomic Transaction Wrapper & Dual-Mode Pre-Reset Backup (#9)     │
├────────────────────────────────────────────────────────────────────────┤
│  TIER 2: Operational & Workflow Consistency                           │
│  • ✅ Category Deletion Reassignment & Uncategorized Guard (#8)       │
│  • ✅ Unified Returns & Completed Check-Outs Reporting (#12)          │
│  • Distinguish Transferred vs. Returned Allocations (#4)               │
│  • Enforce Explicit Pending → Approved Workflow (#6)                  │
│  • Employee Deletion Historical Foreign Key Sanitization (#15)        │
│  • Audit Write Failure User-Facing Feedback (#16)                     │
├────────────────────────────────────────────────────────────────────────┤
│  TIER 3: Quality of Life, Edge Cases & Visual Polish                   │
│  • ✅ Decommission Allocated/Checked-Out Pre-Filter & Guard (#22)     │
│  • Persist Decommission Reason in Equipment Record (#11)              │
│  • Promote Free-Form Audit Strings to AuditService Constants (#17, #21)│
│  • Remove Dead Code in System Configuration Panel (#20)               │
│  • Cross-Machine Database Credential Encryption Option (#19)          │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 20. New Features & Recent Changes (2026-08-10)

This section summarizes all significant features and improvements added as of the 2026-08-10 update.

### 20.1 System Configuration Panel (Admin-Only)

A new sixth Settings tab — **System Configuration** — is available exclusively to Admin users. It provides a live database connection management UI:
- Fields for `Database Server (IP)`, `Database Name`, `Database Username`, `Database Password` (with eye-toggle).
- **Test Connection** button that validates connectivity without saving.
- **Save Connection** button (enabled only after a successful test) that DPAPI-encrypts the password and writes to `%AppData%\AssetPilot\dbconnection.json`.
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

---

## 22. New Features & Recent Changes (2026-09-05)

This section documents all significant features and improvements added as of the **2026-09-05** update.

### 22.1 Application Rebrand — AssetPilot

All user-visible text, window titles, metadata, and hardcoded strings previously reading "Q-Inventory", "QInventory", or "Q Inventory" have been replaced with "AssetPilot" throughout:

- Login screen title, splash/loading animation branding.
- Dashboard header, sidebar logo/branding label.
- About dialog and all footers.
- `AppSetting` default `CompanyName` seed value (`"AssetPilot"`).
- `app_settings` table default row.
- Assembly metadata: `Product`, `Title`, `Description`, `Company` (in `AssetPilot.csproj`).
- Settings panel System card, and all log/notification/exception messages.
- File system paths: `%AppData%\AssetPilot\` (previously `%AppData%\QInventory\`).

> **Note:** Namespaces, class names, folder names, and database schema/table names were intentionally left unchanged.

### 22.2 Local Picture Storage — Localhost / XAMPP Support

**Source:** `Helpers/NetworkPaths.cs`, `Helpers/NetworkShareHelper.cs`, `Helpers/PictureStorageHelper.cs`

Previous versions stored equipment, employee, and user profile pictures exclusively on a remote network share (`\\192.168.18.240\Qinventorydata`). The system has been updated to support local picture storage when the app is running on localhost (XAMPP/MySQL).

**Fallback directory resolution (`NetworkPaths.cs`):**

| Picture Type | Remote Path | Local Fallback |
|---|---|---|
| Equipment pictures | `\\{server}\AssetPilotData\equipment_pictures\` | `%AppData%\AssetPilot\equipment_pictures\` |
| Employee pictures | `\\{server}\AssetPilotData\employee_pictures\` | `%AppData%\AssetPilot\employee_pictures\` |
| User pictures | `\\{server}\AssetPilotData\user_pictures\` | `%AppData%\AssetPilot\user_pictures\` |

**`PictureStorageHelper`:** Abstracts save/load operations. Attempts the network share first; falls back to local `%AppData%\AssetPilot\` directories if the share is unreachable or not configured.

**`NetworkShareHelper.EnsureConnected()`:** Attempts to establish the network share connection. On failure (e.g. `localhost` deployments), silently continues and allows the local fallback path to be used.

**Upload behaviour (Add/Edit forms):** The Upload button opens an `OpenFileDialog` (JPG/PNG/BMP/GIF). The selected file is copied to the resolved pictures directory (network or local fallback) as `{code}_{GUID}{ext}`. The resolved path is stored in `picture_path` on save.

### 22.3 Toast Notification Color System

Toast notifications across all screens are now styled to match the color of their corresponding action buttons for consistent visual feedback:

| Action | Toast Color |
|--------|-------------|
| Approve / Save / Success | Green |
| Edit / Warning / Info | Yellow/Amber |
| Delete / Decommission / Error | Red |
| Restore / Undo | Purple |
| Check-In/Out | Blue |

**Specific screen alignment:**

- **Decommission screen:** Decommission action toast uses the same red as the Decommission button.
- **Deleted Items screen:** Restore toast = purple (matches Restore button); Delete toast = red.
- **Settings → User Management:** Edit toast = yellow (matches Edit button); Toggle/Delete toasts match respective button colors.
- **Allocation screen:** Approve toast = green (matches Approve button).

### 22.4 Allocation Screen — Status Column Formatting

The `Status` column in the Allocation grid now displays human-readable text with proper capitalization and no underscores:

| DB / Internal Value | Displayed as |
|---|---|
| `in_store` | `In Store` |
| `allocated` | `Allocated` |
| `pending` | `Pending` |
| `Overdue` (UI-computed) | `Overdue` |

The `Source` column similarly displays properly cased values (e.g. `Allocation`, `Check-In/Out`).

### 22.5 Overdue Highlighting — Allocations & Logs

**Allocation screen:** Rows where the allocation is overdue (i.e. `Status == "approved"` and `ExpectedReturnDate < today`) are highlighted with dark yellow/amber (`Color.FromArgb(255, 160, 0)`) background — the same amber used on the Currently Overdue dashboard card — instead of the previously used red.

**Logs screen:** The `"Allocation Overdue"` action badge is displayed in dark yellow/amber (matching `#FFA000`) and with spaces between words (no underscore). `LogsForm.FormatAction()` handles this mapping.

### 22.6 Check-In / Out Bug Fixes

**Source:** `Forms/CheckInOutForm.cs`, `Services/TransactionService.cs`

Two bugs were resolved:

1. **Check-Out of In-Store equipment was blocked:** The Check-In/Out form was incorrectly preventing Check-Out of equipment with `CurrentStatus = "in_store"`. The `btnCheckOut_Click` validation logic was corrected so that equipment already `in_store` with no active transaction can be checked out without error.

2. **Allocation screen — cannot allocate some equipment:** The `AllocationService.Allocate()` method was silently rejecting equipment that had a previously completed (returned) allocation. The guard condition was tightened to only block when an `"approved"` or `"pending"` allocation already exists, allowing equipment with only historical `"returned"` allocations to be re-allocated.

### 22.7 Equipment Import Tooltip

**Source:** `Forms/EquipmentForm.cs`

The **Import** button in the Equipment form toolbar now displays a tooltip when hovered:

> *"Make sure the category of the equipment you are about to import is already added in the Category screen in Settings."*

This reminds users to pre-create categories before bulk importing, preventing orphaned category strings.

### 22.8 Dashboard — Sidebar Navigation

- **Navigation:** Each sidebar button calls `OpenChildForm(new XxxForm())` which docks the form inside `panelContent`. The title bar label is animated character-by-character via `AnimateTitleTo()`.
- **Sidebar collapse toggle (`_btnSidebarToggle`):** Fully visible and not clipped at any window height. Uses `_sidebarCollapseTimer` for smooth slide animation.
- **Hover effects:** Both the theme toggle emoji (☀️/🌙) and the notification bell (🔔) display visual hover highlights when the cursor moves over them.

### 22.9 Cross-Module Effects — 2026-09-05 Changes

| Action | Module | Tables Written | Audit Log? |
|--------|--------|----------------|------------|
| Upload Equipment Picture (localhost) | Equipment Add/Edit | `equipment` (`picture_path`) | ✅ `equipment_updated` (diff) |
| Upload Employee Picture (localhost) | Employee Add/Edit | `employees` (`picture_path`) | ✅ `employee_updated` |
| Upload User Picture (localhost) | Settings → Users | `users` (`picture_path`) | ❌ |
| Check-Out (corrected) | Check In/Out | `transactions`, `equipment` | ✅ `checked_out` |
| Allocate (guard fixed) | Allocation | `allocations`, `equipment` | ✅ `allocation_requested` |

---

## 23. New Features & Recent Changes (2026-09-13)

This section documents all significant features, fixes, and improvements found in the codebase as of the **2026-09-13** review. Changes span the QR Scanner, Allocation, Returns, Decommission, Deleted Items, Settings, Logs, and the shared audit infrastructure.

---

### 23.1 QR Scanner — Major Overhaul

**Source:** `Forms/QRScannerForm.cs`

The QR Scanner has been substantially enhanced beyond its original read-only lookup role. It is now a rich equipment inspection hub with direct navigation and batch capabilities.

#### 23.1.1 Recent Scans Strip

A horizontal chip-strip (`panelRecent`) below the search bar records the last **8** successfully scanned equipment codes as clickable buttons:

- Each chip is auto-sized and styled with a subtle border.
- Clicking any chip immediately re-runs the search for that code.
- The strip is maintained in `_recentScans` (newest first). Duplicates are de-duped (case-insensitive) and the oldest entry is evicted once the cap of 8 is reached.
- Sourced from `AddRecentScan(code)` called on every successful match.

#### 23.1.2 Bulk Scan Mode

A **Bulk Scan** checkbox (`chkBulkMode`) enables session-level batch scanning:

- When enabled, every successful scan appends an entry to `_bulkScanCodes: List<(Code, Name, ScannedAt)>`.
- A toast notification appears after each scan confirming the running count: `"Added to bulk list (N scanned)"`.
- The **📤 Export Bulk** button (`btnExportBulk`) becomes visible and exports the accumulated list to CSV:
  - Filename pattern: `BulkScan_{yyyyMMdd_HHmm}.csv`
  - Columns: `Equipment Code`, `Equipment Name`, `Scanned At`
- Toggling bulk mode off clears the session list.

#### 23.1.3 View QR Code Button — `btnViewQR`

A new **🔍 View QR** button generates and displays the equipment's QR code bitmap (via `QRHelper.GenerateQRCode(qrContent, 10)`):

- Opens a 340×400 `FixedDialog` containing the QR bitmap (`PictureBox`, Zoom mode) + the QR content as a text label.
- Includes a **🖶 Print Label** button that delegates to `btnPrintLabel_Click`. *(Label printing stub — shows toast `"Label printing isn't wired up yet."` as of this version.)*
- If no QR code is assigned, shows an info toast instead of opening the viewer.

#### 23.1.4 Check-In / Out Navigation Button — `btnCheckInOut`

A new **🔄 Check-In/Out** button (green tint) allows direct navigation to the Check In/Out form with the current equipment pre-loaded:

- `dashboard?.NavigateToCheckInOut(code)` — opens `CheckInOutForm` with the equipment's code pre-searched.
- Requires a scan result to be loaded; shows a warning toast if no equipment is selected.

#### 23.1.5 View Logs Button — `btnViewLogs`

An existing-concept **📜 View Logs** button (blue tint) is now present directly in the QR Scanner toolbar area:

- Calls `dashboard?.NavigateToLogs(code)` → opens `LogsForm` pre-filtered to the scanned equipment's code.
- Shows a warning toast if no equipment is loaded.

#### 23.1.6 Warranty Expiry Status Indicator

The `WarrantyExpiry` value label (`lblWarrantyValue`) now shows dynamic expiry warnings inline:

| Condition | Display | Color |
|-----------|---------|-------|
| Expired | `"yyyy-MM-dd  (Expired)"` | `ThemeHelper.Danger` (red) |
| Expiring within 30 days | `"yyyy-MM-dd  (Expires in Nd)"` | `ThemeHelper.Warning` (amber) |
| Valid | `"yyyy-MM-dd"` | `ThemeHelper.TextPrimary` |

#### 23.1.7 "Did You Mean?" Suggestions on No Match

When a search term returns no equipment:

- Runs a fuzzy match (`FuzzySearchHelper.IsFuzzyMatch`) across `EquipmentCode` and `Name` for all equipment.
- Shows up to **3** matching suggestions as clickable chip buttons in `panelSuggestions` with a "Did you mean:" label.
- Clicking a suggestion fills the search box and runs the search immediately.

#### 23.1.8 Hero Picture Panel & View Picture Button

After a successful scan, the left side now renders a **hero photo** (`picHero`, custom-painted `Panel`):

- If `equipment.PicturePath` resolves via `AvatarHelper.TryLoadEquipmentImage`, the image is displayed as a rounded-corner (radius 12) clipped rectangle.
- If no image is available, shows a `🖥` emoji placeholder on an indigo background.
- **🖼 View Picture** button (`btnViewPicture`): enabled only if a picture is found. Opens the image in a 340×440 standalone black-background `Form` with `PictureBoxSizeMode.Zoom`.
- When no picture is found, the button text changes to `"🖼 No Picture"`, becomes disabled and grey.

#### 23.1.9 Source Screen Tag in History Grid

Audit log `NewValue` fields may contain a `"Source Screen: ..."` tag written by services that include contextual origin information (e.g., `"... | Source Screen: Allocation Screen"`).

The QR Scanner's history grid (`dgvHistory`) now strips this tag from the display value and appends it to the `Action` column instead:

- **Action column** shows: `"Allocation Requested (via Allocation Screen)"`
- **New Value column** shows only the data, without the `Source Screen` segment.

Implemented via `ExtractSourceScreen(ref string newValue)` — mirrors the same pattern in `LogsForm.dgvLogs_CellDoubleClick`.

#### 23.1.10 History Export — `btnExportHistory`

A new **📤 Export History** button exports the currently loaded equipment's log grid to CSV:

- Prompts with `SaveFileDialog`.
- Filename: `{EquipmentCode}_History_{yyyyMMdd_HHmm}.csv`
- Columns: `Action`, `Previous Value`, `New Value`, `Changed By`, `Timestamp`
- Shows a toast on success or failure.

#### 23.1.11 Double-Click Log Row → LogDetailPopup

Double-clicking any row in `dgvHistory` now opens a `LogDetailPopup` modal:

- Passes action, user, date, equipment code, QR code, name, make, model, old value, and new value.
- Same popup as the Logs screen double-click — dark-mode aware Before/After panels.

#### 23.1.12 Shared `AuditLogDisplay` Static Class

A new internal static class `AuditLogDisplay` is defined at the bottom of `QRScannerForm.cs`. It mirrors `LogsForm.FormatAction` and the colour-mapping logic so both screens render action labels and badge colours identically:

- `FormatActionLabel(string? raw)` — maps raw `action_type` strings to display labels (same table as `LogsForm.FormatAction`).
- `GetActionColors(string? actionType)` — returns `(Color Back, Color Fore)` for action badge styling.

---

### 23.2 Decommission Screen — Restore Button

**Source:** `Forms/DecommissionForm.cs` · `BuildRestoreButton()` · `BtnRestoreDecomm_Click()`

A new **↩ Restore** button (`btnRestoreDecomm`, teal) has been added to the Decommission form toolbar.

**Behaviour:**
1. Validates a row is selected; shows info toast (teal) otherwise.
2. Shows `MessageBox.YesNo` confirmation: `"Are you sure you want to set '{name}' ({code}) back to In Store?"`.
3. On confirm:
   - `entity.CurrentStatus = "in_store"`.
   - `db.SaveChanges()`.
   - `_audit.Log(entity.Id, AuditService.ActionDecommissionUndone, "Status: Decommissioned", "Status: In Store | Source Screen: Decommission Screen")` → `equipment_logs`.
4. Shows teal success toast and reloads the grid.

**Audit log display:** `"decommission_undone"` → `"Restored from Decommission"` (lighter teal badge in Logs grid, distinct from the general `"equipment_restored"` teal).

**Decommission dialog change:** The dialog now shows **all non-decommissioned equipment** (not just `in_store`/`under_repair`). Allocated items are now also selectable for decommission.

---

### 23.3 Allocation Form — Enhancements

**Source:** `Forms/AllocationForm.cs`

#### 23.3.1 Combined "Filters" Button with Dropdown Menu

A single **🔽 Filters** button (`_btnFilters`) opens a two-level `ContextMenuStrip` on hover or click:

- **Status** submenu: All / In Store / Allocated / Pending / Overdue.
- **Category** submenu: All Categories + individual categories from `CategoryService.GetAll()` union with `Equipment.Category` distinct values.
- The button label updates dynamically: `"🔽 Filters (Overdue, Laptops)"`.
- Menu closes when cursor leaves both button and open menu (hover-tracking timer at 40 ms, including open submenus via `GetOpenMenuScreenBounds`).

#### 23.3.2 "Overdue" Status Filter

Filters `_allSourceRows` to rows where `Status == "Overdue"` (UI-computed).

#### 23.3.3 AllocatedBy Column

`AllocationRow` includes `AllocatedBy` (resolved from `AllocatedByUser.FullName`). Grid has a corresponding sortable column.

#### 23.3.4 Gmail-style Checkbox Select Header

Replaces the old `cmbSelectMode` with a persistent split toolbar control `[☐ ▾]`:

- Left part (`_chkHeaderBox`): toggles all visible row checkboxes (`ColSelect` column).
- Right part (`▾`): opens a `ContextMenuStrip` with **All** and **None** items.
- Header checkbox auto-syncs (all/some/none) via `SyncHeaderCheckbox()`.
- Same pattern implemented in `DeletedItemsForm`.

#### 23.3.5 Sortable Columns

Clicking any column header sorts the grid (maintained in `_sortColumn` / `_sortAscending`). Sort indicator (`▲` / `▼`) shown in header. Page resets to 1 on sort change.

#### 23.3.6 Pagination

Page sizes: 10 / 25 / 50 / 100 / 250 / 500 / 1000 (default 25). Prev / Next buttons, `Page N of M` label.

#### 23.3.7 Allocation Delete — Now Audited

`btnDelete_Click`:

- Blocked for `Status == "approved"` rows.
- For non-approved rows: hard-deletes `allocations` row, then logs:
  - `_audit.Log(equipmentId, "Allocation Request Deleted", a.Status, "Deleted | Source Screen: Allocation Screen")` → `equipment_logs`.
- Displays with an orange-red badge in the Logs grid.

---

### 23.4 Returns Form — Fixes & Enhancements

**Source:** `Forms/ReturnsForm.cs`

#### 23.4.1 Undo Return — Now Uses `return_undone`

**Fixed:** `BtnDelete_Click` now logs `AuditService.ActionReturnUndone` (`"return_undone"`) instead of `ActionAllocationReturned`:

```csharp
_audit.Log(alloc.EquipmentId, AuditService.ActionReturnUndone,
    oldValue, $"{code} - approved (return undone)");
```

Displays as **"Returned to Active Allocation"** with an orange badge in Logs.

#### 23.4.2 Active Allocation Guard on Undo

Before undoing, checks for another active approved allocation on the same equipment:

```csharp
var activeAlloc = db.Allocations.FirstOrDefault(a =>
    a.EquipmentId == alloc.EquipmentId && a.Status == "approved" && a.Id != alloc.Id);
```

If found: shows warning toast `"Can't undo — '{code}' is currently allocated to {employee}."` and aborts.

#### 23.4.3 ReturnProcessedAt Cleared on Undo

`alloc.ReturnProcessedAt = null` on undo — clears the completed-return timestamp.

#### 23.4.4 Timestamp Column (Pakistan Standard Time)

Added in code via `SetupTimestampColumn()`. Rendered via `FormatPkt()`:

- Converts UTC `ReturnProcessedAt` → Pakistan Standard Time (Windows: `"Pakistan Standard Time"`, Linux: `"Asia/Karachi"`, fallback: custom UTC+5).
- Format: `"dd MMM yyyy, hh:mm tt"`.

#### 23.4.5 ReturnDetailPopup — Double-Click Row

Double-clicking any row opens a `ReturnDetailPopup` with full return metadata including Source (Allocation/Check-In-Out) and Overdue flag.

#### 23.4.6 Check-In/Out Rows Blocked from Edit & Undo

Rows from transactions have `ReturnID < 0`. Edit and Undo buttons guard for this with warning toasts.

---

### 23.5 Deleted Items — Pagination

**Source:** `Forms/DeletedItemsForm.cs`

Full pagination: page sizes 25 / 50 / 100 / 250 / 500 / 1000 (default 25). `Prev` / `Next` buttons, page indicator, row count label.

---

### 23.6 Settings — Security Audit Trail

**Source:** `Forms/SettingsForm.cs`, `Helpers/SecurityAuditLogger.cs`

#### 23.6.1 `SecurityAuditLogger` — File-Based Immutable Audit Trail

Static helper writing append-only entries to `%AppData%\AssetPilot\security_audit.log`:

- Survives Clear Logs and Reset App Data (no delete path in app).
- Entry format: `"yyyy-MM-dd HH:mm:ss | {FullName} (@{Username}) | {action} | {details}"`.

| Trigger | Action String |
|---------|--------------|
| Clear All Logs | `"Cleared all activity logs"` |
| Reset Application Data | `"Reset application data"` |
| Delete User | `"Deleted user"` |
| Change Own Password | `"Changed own password"` |
| Admin Change Another User's Password (`AddEditUserForm`) | `"Changed user's password (admin)"` |

#### 23.6.2 `settings_data_delete` — Re-Inserted Non-Deletable `equipment_logs` Entry

`WriteSettingsDeletionAuditLog(action, details)` is called after bulk deletes. Inserts a new `equipment_logs` row:

- `ActionType = "settings_data_delete"` → displayed as `"⚠ Settings Data Deleted"`.
- Full row highlighted red in Logs grid (`DefaultCellStyle.BackColor = #FFEBEE`).
- Anchored to the first remaining `equipment` row by FK. Skipped if none exist.

#### 23.6.3 Reset Application Data — Expanded Scope & FK-Safe Order

Previously deleted only `Allocations` + `Equipment`. Now deletes in FK-safe order:

1. Equipment Logs
2. Equipment Location Histories
3. Maintenance Records
4. Transactions (Check-In/Out)
5. Allocations
6. Equipment
7. Employees
8. Locations

Per-table `try/catch` and `SaveChanges`. Errors collected and shown in summary toast. **Users, roles, categories, and app_settings are NOT deleted.**

---

### 23.7 AuditService — New Constants

**Source:** `Services/AuditService.cs`

| Constant | Value |
|----------|-------|
| `ActionReturnUndone` | `"return_undone"` |
| `ActionDecommissionUndone` | `"decommission_undone"` |

---

### 23.8 Logs Form — New Action Types, Colors & Display

**Source:** `Forms/LogsForm.cs`

New entries fully mapped in `FormatAction` switch and `RefreshGrid` colour switch:

| `action_type` | Display Label | Badge Colors (Back / Fore) |
|---|---|---|
| `return_undone` | Returned to Active Allocation | `#FFE0B2` / `#EF6C00` (orange) |
| `decommission_undone` | Restored from Decommission | `#E0F7F4` / `#26A69A` (lighter teal) |
| `Allocation Request Deleted` | Allocation Request Deleted | `#FFAB91` / `#BF360C` (orange-red) |
| `settings_data_delete` | ⚠ Settings Data Deleted | `#FFCDD2` / `#B71C1C` (red badge + full row highlight `#FFEBEE`) |
| `allocation_overdue` | Allocation Overdue | `#FFE0B2` / `#FFA000` (amber) |

`"allocation_returned"` display label: **"Returned to Store"**.

`"Expected Return Date Updated"` (free string): now has an explicit case → yellow update badge (`#FFF9C4` / `#826400`).

---

### 23.9 Cross-Module Effects — 2026-09-13 Changes

| Action | Module | Tables Written | Audit Log? |
|--------|--------|----------------|------------|
| Restore from Decommission | Decommission | `equipment`, `equipment_logs` | ✅ `decommission_undone` |
| Delete Allocation (non-approved) | Allocation | `allocations` (hard delete), `equipment_logs` | ✅ `Allocation Request Deleted` |
| Undo Return | Returns | `allocations`, `equipment`, `equipment_logs` | ✅ `return_undone` |
| Clear All Logs | Settings | `equipment_logs` (all deleted), then `settings_data_delete` row re-inserted + `security_audit.log` | ✅ `settings_data_delete` |
| Reset Application Data | Settings | Logs → Location Histories → Maintenance Records → Transactions → Allocations → Equipment → Employees → Locations + `security_audit.log` + `settings_data_delete` row | ✅ `settings_data_delete` |
| Delete User | Settings | `user_roles`, `users` | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Change Password | Settings / AddEditUserForm | `users` | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Bulk Scan Export | QR Scanner | *(no DB writes)* | ℹ️ N/A (read-only file export to CSV/TXT) |
| View QR Code / View Picture | QR Scanner | *(no DB writes)* | ℹ️ N/A (read-only inspection dialogs) |
| Navigate to Check-In/Out from QR Scanner | QR Scanner | *(navigation only)* | ℹ️ N/A (cross-form navigational transition) |

---

## 24. New Features & Comprehensive Overhaul (2026-11-10)

This section summarizes all major design modernization, security hardening, pagination additions, and architecture updates implemented in the **2026-11-10** release.

---

### 24.1 Reports Screen — Complete UI Modernization

**Source:** `Forms/ReportsForm.cs`, `Forms/ReportsForm.Designer.cs`

The Reports screen has undergone a complete redesign to provide a high-density, modern executive analytics experience matching contemporary enterprise UI paradigms.

#### 24.1.1 Default Initial State & Developer Watermark
- **Zero-Load Default:** When navigating to the Reports screen, no report data or equipment records are loaded automatically.
- **KPI Dashes:** All four KPI cards display neutral placeholder dashes (`—`).
- **Hidden Pagination on Default:** The pagination controls (`Prev`, `Next`, per-page selector) are completely hidden on the default view.
- **Attribution Watermark:** In place of the pagination bar, the developer watermark is displayed across the bottom:
  `Designed and Developed by 🐙@shayan-qazmi/📷@shayanqazmi`
- **Dynamic Transition:** As soon as an operator clicks any of the 5 report buttons (e.g. Equipment, Employee, etc.), the database is queried, KPI metrics compute, the DataGrid renders, pagination controls appear (`Showing 1-25 of 385`), and the watermark transitions cleanly.

#### 24.1.2 Five Operational Report Tabs
- **Equipment ▾** (`#4F46E5`): Comprehensive equipment inventory register. Includes an integrated context menu category picker to filter by specific hardware categories without bloating the horizontal filter toolbar.
- **Employee** (`#7C3AED`): Holdings summary of active workforce members, tracking items currently held vs. total allocations.
- **Allocation** (`#EA580C`): Active allocations and pending requests with assignment dates, locations, and return deadlines.
- **Returns** (`#4338CA`): Custody return log and duration analysis. Highlights allocations held for more than 30 days.
- **Check In/Out** (`#0284C7`): Real-time borrow transactions ledger.

#### 24.1.3 Single-Row Filter Toolbar
- **Apply Button:** High-contrast emerald green button (`#10B981`) placed on the left edge.
- **Date Range Quick-Pills:** `Today`, `7 days`, `30 days`, `This year` (active default), `Custom...`.
- **Unclipped Custom Date Dialog:** The `Custom...` date range dialog was enlarged (`Width 340, Height 230`) to guarantee that `From Date`, `To Date`, and `Apply Range` button are fully visible and never clipped.
- **Dropdown Filters:** `All locations ▾` and `Any status ▾`.
- **Live Search:** Instant client-side substring filter (`Filter rows...`).
- **Reset Button:** Restores all filters to defaults, resets sorting, and restores any hidden columns.

#### 24.1.4 KPI Summary Cards & Status Badges
- **Total rows:** Indigo accent bar (`#7C3AED`).
- **Allocated:** Amber accent bar (`#F59E0B`).
- **In store:** Emerald green accent bar (`#10B981`).
- **Needs attention:** Red accent bar (`#EF4444`).
- **Under Repair Badge:** Formatted with friendly capitalization and no underscore (`under_repair` -> `Under Repair`) rendered in **light blue** (`#E0F2FE` background / `#0284C7` text).

---

### 24.2 QR Scanner & Locations Pagination Overhaul

#### 24.2.1 Locations Screen Pagination
**Source:** `Forms/LocationsForm.cs`
- Added standardized pagination (`PageSize = 50`).
- Bottom pagination controls with `Page X of Y` indicator, responsive `Prev` / `Next` buttons, and instant search integration.

#### 24.2.2 QR Scanner History Cap & Inspection Hub
**Source:** `Forms/QRScannerForm.cs`
- Standardized the equipment audit history grid to the **50 most recent records** (`Take(50)`).
- Dynamic header: `📜 Equipment History (showing 50 of {totalLogCount})`.
- Integrated camera scanning with AForge/ZXing, freeze/unfreeze controls, autofocus, and barcode recognition.
- Fast-action buttons: `Check Out`, `Check In`, `View QR`, `View Picture`, `View Details`, `Print Label`, `Allocate`.

---

### 24.3 Login Screen & Credential Security Hardening

**Source:** `Forms/LoginForm.cs`

#### 24.3.1 Password Recovery UI Layout & Text Padding Fix
- Fixed input text and placeholder clipping: Increased verification field horizontal position (`txtVerify.Left = 54`, `Width = contentWidth - 54 - 40`), creating an 18–22px clear gutter past the left icon (`✉` / `🔑`).
- Eliminates clipping of initial characters (`name@company.com`, `Enter personal PIN or Admin PIN`).

#### 24.3.2 Strict Input Restrictions
- **Recovery PIN Mode:**
  - Enforces **numerals/numbers only** (`char.IsDigit`).
  - KeyPress handler intercepts and cancels non-digits and spaces.
  - TextChanged cleanses pasted non-digits while maintaining cursor position.
- **Work Email Mode:**
  - Enforces **valid email format characters** (`[a-zA-Z0-9@._-+]`).
  - Intercepts spaces (`e.KeyChar == ' '`) and blocks multiple `@` symbols.
  - Cleanses pasted spaces and illegal symbols.
- **Tab State Cleaning:** Toggling between Work Email and Recovery PIN automatically clears the input field to prevent carrying mismatched formats across verification modes.

---

### 24.4 Security Audit Trail & Offline Recovery PIN Architecture

**Source:** `Helpers/RecoveryPinHelper.cs`, `Helpers/PasswordHelper.cs`, `Helpers/SecurityAuditLogger.cs`

#### 24.4.1 BCrypt Password Hashing
- Replaced plaintext password comparisons with **BCrypt.Net** hashing (`PasswordHelper.Hash` and `PasswordHelper.Verify`).
- Transparent backward compatibility: Legacy plaintext passwords authenticate seamlessly and are hashed with BCrypt upon the next password reset or administrative change.

#### 24.4.2 Offline Master PIN & User PIN Helper
- Default master recovery PIN updated to **`123456`** (100% numeric, compatible with the login PIN input validation).
- Organization-wide custom PIN stored at `%AppData%\AssetPilot\recovery_pin.txt`.
- Individual per-user recovery PINs persisted at `%AppData%\AssetPilot\user_pins.json`.
- Mandatory account recovery credential policy in `AddEditUserForm`: administrators must supply either a valid Email, a Recovery PIN, or both.

#### 24.4.3 Security Audit Log
- Append-only file log at `%AppData%\AssetPilot\security_audit.log` tracking:
  - User creation, deletion, status toggling, role assignment.
  - Self-service password resets and administrative password resets.
  - Organization PIN updates and resets.
  - Application data purge and logs clearance.

---

### 24.5 Cross-Module Effects — 2026-11-10 Changes

| Action | Module | Tables Written | Audit Log? |
|--------|--------|----------------|------------|
| Self-Service Password Reset (Email/PIN) | Login (`LoginForm`) | `users` | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Set / Reset Organization Master PIN | Settings (`SettingsForm`) | File (`recovery_pin.txt`) | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Save User Recovery PIN | Users (`AddEditUserForm`) | File (`user_pins.json`) | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Report Generation & Multi-Format Export | Reports (`ReportsForm`) | *(no DB writes)* | ℹ️ N/A (read-only export to PDF/Excel/CSV/Print) |
| Live Camera Scanning & 50 History Cap | QR Scanner (`QRScannerForm`) | *(no DB writes)* | ℹ️ N/A (read-only barcode detection & inspection) |
| Paginated Location Navigation | Locations (`LocationsForm`) | *(no DB writes)* | ℹ️ N/A (in-memory paged navigation) |
| Equipment Soft Delete Guard | Equipment (`EquipmentService`, `EquipmentForm`) | `equipment` (soft delete blocked if allocated/pending/checked out) | ✅ `equipment_deleted` logged when permitted; blocked attempts trigger warning toast without state change |
| Delete User | Settings (`SettingsForm`) | `user_roles`, `users` | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Change Password | Settings / `AddEditUserForm` | `users` | ⚠️ Security audit only (`security_audit.log`); excluded from DB `equipment_logs` by design |
| Bulk Scan Export | QR Scanner (`QRScannerForm`) | *(no DB writes)* | ℹ️ N/A (read-only file export to CSV/TXT) |
| View QR Code / View Picture | QR Scanner (`QRScannerForm`) | *(no DB writes)* | ℹ️ N/A (read-only visual modal viewer) |
| Navigate to Check-In/Out from QR Scanner | QR Scanner (`QRScannerForm`) | *(navigation only)* | ℹ️ N/A (cross-module UI navigation) |
| Full Operational Reset with Pre-Backup | Settings (`SettingsForm`) | All 8 operational tables inside `BeginTransaction()` | ✅ `settings_data_delete` + `security_audit.log` |

---

### 24.6 Tier 1 Critical Limitations Resolutions

#### 24.6.1 Dynamic User Attribution (`CreatedBy` / `ApprovedBy` / `AllocatedBy`)
- **Add / Edit Equipment:** `EquipmentService.Add` and `AddEditEquipmentForm` dynamically assign `CreatedByUserId = AuthService.CurrentUserId > 0 ? AuthService.CurrentUserId : 1`.
- **Allocation Service & Forms:** `AllocationService.Allocate`, `AllocationForm.cs`, and `TransferForm.cs` dynamically assign `AllocatedByUserId` and `ApprovedByUserId` to `AuthService.CurrentUserId`, capturing the authenticated operator on record creation, manual approvals, and asset transfers.

#### 24.6.2 Equipment Soft Delete Guard
- **Comprehensive Dependency Check:** Before flagging any asset with `IsDeleted = true`, both `EquipmentService` (`Delete` & `DeleteRange`) and `EquipmentForm` verify:
  1. Active approved allocations (`allocations.status == 'approved'`).
  2. Pending allocation requests (`allocations.status == 'pending'`).
  3. Active or overdue borrow transactions (`transactions.status IN ('Pending', 'Overdue')`).
  4. Active equipment status (`CurrentStatus == 'allocated'`).
- If any active or pending dependency exists, deletion is strictly blocked, displaying a descriptive warning to return or complete custody first.

#### 24.6.3 Atomic Reset Transaction & Dual-Mode Pre-Reset Safety Backup
- **Single Atomic Transaction:** `SettingsForm.BtnResetAppData_Click` wraps all 8 table deletions inside `using var tx = db.Database.BeginTransaction()`. Any error triggers an immediate `tx.Rollback()`, guaranteeing that an interrupted reset will never leave the database in an inconsistent or partially deleted state.
- **Dual-Mode Automatic Backup:** Before any reset commences, `PerformAutomaticPreResetBackup()` runs:
  1. Primary: Spawns `mysqldump` using secure temporary option files (`--defaults-extra-file="{cnfFile}"`), writing a complete SQL dump.
  2. Secondary Fallback: If `mysqldump` is unavailable on the workstation, automatically serializes all 8 tables via EF Core into a structured JSON snapshot (`pre_reset_backup_{timestamp}.json`).
- Archived safety backups are stored at `%AppData%\AssetPilot\Backups\` and recorded in `SecurityAuditLogger`.

---

### 24.7 Operational & Workflow Consistency Enhancements (Items #10 & #12)

#### 24.7.1 Category Deletion & Reassignment Architecture (Item #10)
**Source:** `Services/CategoryService.cs`, `Forms/SettingsForm.cs`
- **Orphan Prevention:** Deleting a category that is currently assigned to equipment no longer leaves orphaned strings on equipment records or phantom filter options in dropdowns.
- **Interactive Reassignment:** When an administrator attempts to delete an in-use category, the system calculates the active equipment count and presents an explicit choice:
  `"Category '{name}' is currently assigned to {count} equipment item(s). Would you like to reassign these {count} item(s) to 'Uncategorized' and delete '{name}'?"`
- **Automatic Migration:** Confirming reassignment automatically creates the `"Uncategorized"` category in `categories` (if not already present), updates all matching `Equipment.Category` records to `"Uncategorized"`, and safely removes the target category.
- **Cancellation Safety:** If the administrator declines reassignment, the category deletion is safely aborted without modifying any records.

#### 24.7.2 Decommission Eligibility Pre-Filtering & Active Custody Guard (Item #12)
**Source:** `Forms/DecommissionForm.cs`
- **Eligibility Pre-Filter:** The Decommission dialog dropdown and suggestion helper now strictly pre-filter the equipment list to exclude:
  1. Equipment with `CurrentStatus == "allocated"`.
  2. Equipment with active approved allocations (`allocations.Status == "approved"`).
  3. Equipment with pending allocation requests (`allocations.Status == "pending"`).
  4. Equipment with active or overdue borrow transactions (`transactions.Status IN ('Pending', 'Overdue')`).
- **Clear Operator Guidance:** If all active assets in the database are currently assigned or checked out, the system alerts the operator upfront:
  `"No equipment currently eligible to decommission. Active equipment is currently allocated to employees or checked out. Please process returns before decommissioning."`
- **Multi-Layer Guard:** Both `DecommissionDialog.BtnSave_Click` and `DecommissionForm.btnDecommission_Click` enforce real-time database checks before writing `CurrentStatus = "decommissioned"`, ensuring an item can never be decommissioned while under employee custody.

---

### 24.8 Logs Screen Advanced Filter Architecture (2026-11-10)

**Source:** `Forms/LogsForm.cs`, `Forms/LogsForm.Designer.cs`

#### 24.8.1 Action Category Pill Buttons
A dedicated second toolbar row (`flowFilterRow`) provides category-based quick filtering. The active pill state dynamically adopts the exact matching badge color from the DataGridView action cells:
- **`All`**: Charcoal (`#1F2937`) — resets action-type filtering.
- **`Created`**: Dark green (`#1B5E20`) — filters `equipment_created`, `employee_created`, `location_created`.
- **`Updated`**: Yellow (`#CA8A04`) — filters `equipment_updated`, `employee_updated`, `location_updated`, `Expected Return Date Updated`.
- **`Allocation`**: Orange (`#EA580C`) — filters `allocation_requested`, `allocation_approved`, `allocation_rejected`, `allocation_returned`, `allocation_overdue`, `return_undone`, `Allocation Request Deleted`.
- **`Check In/Out`**: Dark blue (`#01579B`) — enlarged button width (`110px`) to prevent text clipping; filters `checked_out`, `checked_in`.
- **`Transfers`**: Deep purple (`#4A148C`) — filters `transferred`.
- **`Decommission`**: Red (`#D32F2F`) — enlarged button width (`118px`) to prevent text clipping; filters `decommission_requested`, `decommission_approved`, `decommission_undone`, `decommission_rejected`.
- **`Deleted`**: Deep red (`#B71C1C`) — filters `equipment_deleted`, `employee_deleted`, `location_deleted`, `equipment_restored`, `settings_data_delete`.

#### 24.8.2 User & Date Range Filter Integration
- **User Filter (`cmbFilterUser`):** Dynamically populated with all database users (`Users` table) with an `"All users"` default option.
- **Default "All time" Date Range:** Date filter defaults to **`All time`** (`SelectedIndex = 2`), ensuring historical logs are immediately visible without an artificial 30-day window restriction. Supports `Last 7 days`, `Last 30 days`, `All time`, and `Custom...` with an inline popup date range picker.
- **Real-Time Live Status Indicator:** Displays `● Live – updated [time]` in Pakistan Standard Time (PKT), refreshed on every query.
- **Density Selector:** Standardized density options (`Compact`, `Comfortable`, `Cozy`).
- **Comprehensive Reset Action (`btnReset`):** Clears text search, resets category pills to `All`, resets user filter to `All users`, resets date range to `All time`, resets column sorting to `CreatedAt DESC`, and returns to page 1.
