#                AssetPilot Inventory Management System

<p align="center">
<img src="screenshots/000-app-overview.png" alt="Screenshot: Dashboard overview" width="900">
</p>

<p align="center">
<em>A desktop application for tracking physical equipment — from purchase to allocation, transfer, repair, and retirement — built with C# .NET 8 (WinForms), Entity Framework Core, and MariaDB.</em>
</p>

> 📸 **About the screenshots in this README:** every image below points to a file under `screenshots/` that doesn't exist yet — capture your own and drop it in with the matching filename to fill each slot in. Placeholders are numbered sequentially and grouped by screen/dialog/state so that **every distinct piece of functionality has its own dedicated image slot** — list views, every dialog, every popup, every button state, every filter, and every empty/error/confirmation state. Keep the numbering intact when you add captures so the doc stays easy to audit at a glance (a missing number = a missing screenshot).

---

## Table of Contents

- [What This App Does](#what-this-app-does)
- [Tech Stack](#tech-stack)
- [User Roles](#user-roles)
- [Login](#login)
- [Dashboard](#dashboard)
- [Equipment](#equipment)
- [Employees](#employees)
- [Locations](#locations)
- [Allocation](#allocation)
- [Returns](#returns)
- [Transfer](#transfer)
- [Decommission](#decommission)
- [Deleted Items](#deleted-items)
- [Logs (Audit Trail)](#logs-audit-trail)
- [QR Scanner](#qr-scanner)
- [Check In / Check Out](#check-in--check-out)
- [Reports](#reports)
- [Settings](#settings)
- [Equipment Status Lifecycle](#equipment-status-lifecycle)
- [Cross-Module Effects Reference](#cross-module-effects-reference)
- [Installation & Setup](#installation--setup)
- [Database Setup](#database-setup)
- [Known Limitations & Data-Safety Notes](#known-limitations--data-safety-notes)
- [Credits](#credits)

---

## What This App Does

Q-Inventory tracks every physical item a company owns — laptops, furniture, accessories, tools, anything — through its entire life:

- What equipment exists, and what condition is it in?
- Where is it right now, and who has it?
- Who supplied it, what did it cost, and is it still under warranty?
- When was it handed out, and when is it due back?
- Is it out on a quick informal loan (Check In/Out), or a formal tracked allocation?
- What's happened to it over its lifetime — every repair, transfer, return, and status change?
- Who made each change, and when?
- Routine confirmations (save, restore, delete) show as brief toast messages instead of interrupting popups — only destructive actions still require an explicit confirmation dialog.

It covers the full lifecycle: **procurement → storage → allocation → return / transfer → repair → decommission**, with QR-code identification and printable labels throughout.

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# .NET 8 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core |
| Database | MariaDB |
| QR Generation | QRCoder |
| Excel Import/Export | ClosedXML |
| PDF Generation / Label Printing | System.Drawing.Printing + PrintPreviewDialog |
| Charts | Custom GDI+ (no third-party charting library) |
| Shared Files | Network share (`\\192.168.18.240\QInventoryData`) for equipment, user, and employee pictures |
| Platform | Windows 10/11 (64-bit) |

---

## User Roles

Q-Inventory has two roles, assigned per user account and cached for the session via `AuthService`.

| Feature | Admin | Operator |
|---|:---:|:---:|
| Dashboard & Charts | ✅ | ✅ |
| Equipment — View / Add / Edit | ✅ | ✅ |
| Equipment — Restore | ✅ | ❌ Restore button hidden |
| Employees — Full management | ✅ | ✅ |
| Locations — Full management | ✅ | ✅ |
| Allocation — Allocate / Approve / Return | ✅ | ✅ |
| Transfer | ✅ | ✅ |
| Returns History | ✅ | ✅ |
| QR Scanner | ✅ | ✅ |
| Check In / Check Out | ✅ | ✅ |
| Reports — View & Export | ✅ | ✅ |
| Logs (Audit Trail) | ✅ | ✅ |
| Decommission | ✅ | ❌ Hidden from sidebar |
| Deleted Items (restore/permanent delete) | ✅ | ❌ Hidden from sidebar |
| Settings — General / Users / Change Password / Printing / Categories / Backup-Restore / System Configuration | ✅ | ❌ Hidden from sidebar |

The logged-in user's full name and role are shown persistently in the sidebar/session, and every save made anywhere in the app is attributed to that account and timestamped — this is what powers the audit trail (see [Logs](#logs-audit-trail)).

<p align="center">
<img src="screenshots/001-sidebar-admin-vs-operator.png" alt="Screenshot: Sidebar comparison — Admin view vs Operator view, side by side" width="900">
</p>

---

## Login

<p align="center">
<img src="screenshots/010-login-screen.png" alt="Screenshot: Login screen, empty state" width="900">
</p>

- Username + password sign-in with a show/hide password toggle (eye icon).
- **"Keep Me Logged In"** toggle — pre-fills the username field on next launch. The password is **never** stored or remembered; it must always be typed manually, every time, even with the toggle on.
- Pressing **Enter** in either field submits the form, same as clicking **Sign In**.
- Invalid credentials show a single generic error message — it deliberately does not reveal whether the username or the password was the one that was wrong.

<p align="center">
<img src="screenshots/011-login-password-visible.png" alt="Screenshot: Login screen with password revealed via eye icon" width="700">
</p>

<p align="center">
<img src="screenshots/012-login-remember-me-toggle.png" alt="Screenshot: Keep Me Logged In toggle, on state" width="700">
</p>

<p align="center">
<img src="screenshots/013-login-invalid-credentials-error.png" alt="Screenshot: Invalid username or password error message" width="700">
</p>

---

## Dashboard

<p align="center">
<img src="screenshots/020-dashboard-full-view.png" alt="Screenshot: Dashboard full view" width="900">
</p>

The landing screen after login. Everything here is live, read-only summary data — nothing on this screen can be edited directly.

### Stat Cards

<p align="center">
<img src="screenshots/021-dashboard-stat-cards.png" alt="Screenshot: Dashboard stat cards row, with entrance animation mid-way" width="900">
</p>

Animated cards across the top:

| Card | Shows |
|---|---|
| Total Equipment | Count of all active (non-deleted) equipment |
| In Store | Items available in storage |
| Allocated | Currently allocated / checked-out items |
| Under Repair | Items currently flagged for maintenance |
| Decommissioned | Retired equipment |
| Employees | Total employee count |
| Overdue | Active allocations past their expected return date (computed live, never stored) |

### Charts

<p align="center">
<img src="screenshots/022-dashboard-status-breakdown-donut.png" alt="Screenshot: Status Breakdown donut chart" width="500">
</p>

<p align="center">
<img src="screenshots/023-dashboard-category-breakdown-bar.png" alt="Screenshot: Category Breakdown horizontal bar chart" width="500">
</p>

<p align="center">
<img src="screenshots/024-dashboard-6month-trend-line.png" alt="Screenshot: 6-Month Allocation Trend line chart" width="900">
</p>

- **Status Breakdown** — donut chart, current equipment status.
- **Category Breakdown** — horizontal bar chart, count per category.
- **6-Month Allocation Trend** — line chart, allocations created per month over the last 6 months.

### Sidebar Navigation

<p align="center">
<img src="screenshots/025-dashboard-sidebar-expanded.png" alt="Screenshot: Sidebar navigation, expanded" width="300">
</p>

<p align="center">
<img src="screenshots/026-dashboard-sidebar-collapsed.png" alt="Screenshot: Sidebar navigation, collapsed" width="150">
</p>

<p align="center">
<img src="screenshots/027-dashboard-title-bar-animation.png" alt="Screenshot: Title bar label mid character-by-character animation when switching screens" width="900">
</p>

Every module in the app is reachable from the left sidebar. Admin-only entries (Decommission, Deleted Items, Settings) are automatically hidden for Operator accounts.

### Notifications

<p align="center">
<img src="screenshots/028-dashboard-notification-bell-badge.png" alt="Screenshot: Notification bell icon with live badge count, visible from any screen" width="300">
</p>

<p align="center">
<img src="screenshots/029-dashboard-notification-dropdown.png" alt="Screenshot: Notification bell dropdown open, listing overdue allocations and open maintenance items" width="500">
</p>

A bell icon in the top bar shows a live count badge, visible from every screen in the app — not just the Dashboard:
- **Overdue allocations** — approved allocations past their expected return date.
- **Open maintenance items** — equipment with a maintenance record that isn't yet resolved.

Clicking it opens a dropdown listing both categories with quick links straight to the relevant equipment record.

### Recent Activities Panel

<p align="center">
<img src="screenshots/030-dashboard-recent-activities.png" alt="Screenshot: Recent Activities live feed panel" width="900">
</p>

A live feed of the most recent allocation, return, and transfer events across the whole system, with relative timestamps (e.g. "7h ago") — sourced directly from the audit trail.

---

## Equipment

<p align="center">
<img src="screenshots/040-equipment-list-full.png" alt="Screenshot: Equipment list view, full grid" width="900">
</p>

The master list of every equipment item. Columns: Equipment Code, QR Code, Procurement Type, Name, Make, Model, Serial Number, Category, Status, Condition, Location, Purchase Date.

<p align="center">
<img src="screenshots/041-equipment-status-color-legend.png" alt="Screenshot: Equipment grid rows showing all four status colors together" width="900">
</p>

Rows are color-coded by status: green = In Store, blue = Allocated, orange = Under Repair, red = Decommissioned.

### Search & Filter

<p align="center">
<img src="screenshots/042-equipment-search-box.png" alt="Screenshot: Equipment search box with fuzzy suggestion results" width="700">
</p>

<p align="center">
<img src="screenshots/043-equipment-category-filter-dropdown.png" alt="Screenshot: Equipment category filter dropdown, open, listing saved and inferred categories" width="500">
</p>

- Fuzzy search box (matches Equipment Code / QR Code exactly first, then falls back to fuzzy matching on Name, Make, Model, Serial Number, Category).
- Category filter dropdown, built from both the Categories list in Settings and any category values already present on equipment records. Search and category filter combine.

### Toolbar Actions

<p align="center">
<img src="screenshots/044-equipment-toolbar-full.png" alt="Screenshot: Equipment toolbar — Add, Edit, Delete, Refresh, Export, Select buttons" width="900">
</p>

<p align="center">
<img src="screenshots/045-equipment-select-mode-checkboxes.png" alt="Screenshot: Equipment grid with Select mode checkboxes enabled" width="900">
</p>

<p align="center">
<img src="screenshots/046-equipment-select-all.png" alt="Screenshot: Equipment grid with Select All applied, all rows checked" width="900">
</p>

### Add Equipment

<p align="center">
<img src="screenshots/047-equipment-add-form-empty.png" alt="Screenshot: Add Equipment form, blank state" width="900">
</p>

- Equipment Code and QR Code are **auto-generated** — never typed manually, no risk of duplicates or typos.
- Fields: Name (required), Make, Model, Serial Number, Status, Purchase Date, Category, Vendor Name, Condition Grade, Warranty Expiry (optional toggle to enable the date picker), Description/Notes, Price, and a free-text Procurement Type field that matches an existing type or creates a new one automatically the first time it's used.
- A picture can be staged and attached to the record.

<p align="center">
<img src="screenshots/048-equipment-add-form-filled.png" alt="Screenshot: Add Equipment form, filled out with sample data" width="900">
</p>

<p align="center">
<img src="screenshots/049-equipment-add-warranty-toggle.png" alt="Screenshot: Warranty Expiry checkbox toggled on, date picker enabled" width="700">
</p>

<p align="center">
<img src="screenshots/050-equipment-add-picture-upload.png" alt="Screenshot: Picture upload control on the Add Equipment form, with a staged image" width="700">
</p>

<p align="center">
<img src="screenshots/051-equipment-add-saved-toast.png" alt="Screenshot: Toast confirmation after saving a new equipment record" width="500">
</p>

### Edit Equipment

<p align="center">
<img src="screenshots/052-equipment-edit-form.png" alt="Screenshot: Edit Equipment form, pre-filled with existing values" width="900">
</p>

- Same fields as Add, pre-filled with the current values.
- Switching status **to "Under Repair"** opens a maintenance dialog to log the issue being reported, opening a new maintenance record.
- Switching status **away from "Under Repair"** marks the open maintenance record as completed automatically.

<p align="center">
<img src="screenshots/053-equipment-status-dropdown-open.png" alt="Screenshot: Status dropdown open, showing In Store / Allocated / Under Repair / Decommissioned options" width="500">
</p>

<p align="center">
<img src="screenshots/054-equipment-maintenance-dialog-open.png" alt="Screenshot: Maintenance record dialog triggered by switching status to Under Repair" width="700">
</p>

<p align="center">
<img src="screenshots/055-equipment-maintenance-dialog-filled.png" alt="Screenshot: Maintenance dialog filled — issue description, servicer, estimated cost" width="700">
</p>

<p align="center">
<img src="screenshots/056-equipment-edit-diff-audit.png" alt="Screenshot: Logs entry showing only the changed fields after an equipment edit" width="900">
</p>

### Equipment Detail View

<p align="center">
<img src="screenshots/057-equipment-detail-popup-main.png" alt="Screenshot: Equipment detail popup, full card" width="900">
</p>

Double-clicking a row opens a read-only detail popup: every field, current allocation info (holder + location, if allocated), and the item's full maintenance history — plus a **View Picture** button if a picture is attached.

<p align="center">
<img src="screenshots/058-equipment-detail-view-picture.png" alt="Screenshot: View Picture button expanded, showing the equipment photo" width="700">
</p>

<p align="center">
<img src="screenshots/059-equipment-detail-maintenance-history.png" alt="Screenshot: Maintenance History section inside the detail popup — open, in progress, and resolved records" width="900">
</p>

<p align="center">
<img src="screenshots/060-equipment-detail-no-maintenance.png" alt="Screenshot: Detail popup for an item with no maintenance history — 'No maintenance history recorded' state" width="900">
</p>

### Delete Equipment

<p align="center">
<img src="screenshots/061-equipment-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Equipment screen" width="700">
</p>

- Deleting an item is a **soft delete** — it's hidden from every list and dropdown in the app but not destroyed. It moves to the **Deleted Items** screen (Admin-only), where it can be restored or permanently erased. This action is **not** written to the audit log, and there is no check that blocks deleting an item that is still allocated.

### Show QR / QR Sheet

<p align="center">
<img src="screenshots/062-equipment-show-qr-single.png" alt="Screenshot: Show QR dialog for a single item, with Print button" width="700">
</p>

- **Show QR** — displays a single item's QR code with its details, and a Print button for label printers.
- **QR Sheet** — select multiple items (via the Select ✔ / Select All dropdown or checkboxes) and generate a printable sheet of QR codes, laid out in a grid with company name, code, name, and status on each label. Opens automatically for preview, with an optional print step.

<p align="center">
<img src="screenshots/063-equipment-qr-sheet-preview.png" alt="Screenshot: QR Sheet multi-item print preview" width="900">
</p>

<p align="center">
<img src="screenshots/064-equipment-qr-sheet-print-dialog.png" alt="Screenshot: Print dialog launched from the QR Sheet preview" width="700">
</p>

### Import / Export

<p align="center">
<img src="screenshots/065-equipment-import-dialog.png" alt="Screenshot: Import Excel dialog, file picker step" width="700">
</p>

<p align="center">
<img src="screenshots/066-equipment-import-result-summary.png" alt="Screenshot: Import result summary — rows added, rows skipped/failed" width="700">
</p>

<p align="center">
<img src="screenshots/067-equipment-export-csv-toast.png" alt="Screenshot: CSV export confirmation toast" width="500">
</p>

- **Import Excel** — bulk-add equipment from a `.xlsx` file.
- **Export** — export the currently visible/filtered rows to CSV.

---

## Employees

<p align="center">
<img src="screenshots/080-employees-list.png" alt="Screenshot: Employees list view, full grid" width="900">
</p>

Manage the people equipment can be allocated to. Grid shows Employee Code, Full Name, Designation, Email, Phone, and Status.

<p align="center">
<img src="screenshots/081-employees-search.png" alt="Screenshot: Employees search box with results" width="700">
</p>

### Add / Edit Employee

<p align="center">
<img src="screenshots/082-employees-add-form-empty.png" alt="Screenshot: Add Employee form, blank state" width="900">
</p>

<p align="center">
<img src="screenshots/083-employees-edit-form-filled.png" alt="Screenshot: Edit Employee form, pre-filled" width="900">
</p>

<p align="center">
<img src="screenshots/084-employees-profile-picture-upload.png" alt="Screenshot: Profile picture Upload / Remove buttons on the Employee form" width="700">
</p>

<p align="center">
<img src="screenshots/085-employees-active-inactive-toggle.png" alt="Screenshot: Is Active checkbox toggled off, employee marked Inactive" width="500">
</p>

- **Add / Edit** — Full Name (required), Employee Code (auto-generated if left blank), Designation, Email (must be unique if provided), Phone, Is Active checkbox, and a profile picture (upload or remove — stored on the shared network folder, same as user and equipment pictures).

### Employee Detail Popup

<p align="center">
<img src="screenshots/086-employees-detail-popup.png" alt="Screenshot: Employee detail popup, avatar and status badge" width="700">
</p>

- Double-clicking a row opens a read-only detail card showing the employee's avatar (circular crop, or a default grey silhouette if no picture is set), Employee Code, Designation, Email, Phone, and a Status badge.

### Delete Employee

<p align="center">
<img src="screenshots/087-employees-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Employees screen" width="700">
</p>

<p align="center">
<img src="screenshots/088-employees-delete-blocked-active-allocation.png" alt="Screenshot: Delete blocked error — employee still has an active allocation" width="700">
</p>

- **Delete** is a **permanent, hard delete** — blocked if the employee currently has any active (approved) allocation. Historical (non-active) allocation records keep a reference to the deleted employee's ID and will show a broken/blank employee reference afterward.

### Import / Export

<p align="center">
<img src="screenshots/089-employees-import-dialog.png" alt="Screenshot: Employees bulk Import dialog" width="700">
</p>

<p align="center">
<img src="screenshots/090-employees-export-csv-toast.png" alt="Screenshot: Employees CSV export confirmation toast" width="500">
</p>

---

## Locations

<p align="center">
<img src="screenshots/100-locations-list.png" alt="Screenshot: Locations list view, full grid (no pagination)" width="900">
</p>

Manage the physical places equipment can be assigned to (offices, stores, buildings, rooms). Grid shows Location Name, Building, Room Number, Room Name. No search box and no pagination — the full list loads at once.

<p align="center">
<img src="screenshots/101-locations-add-form.png" alt="Screenshot: Add Location form" width="700">
</p>

<p align="center">
<img src="screenshots/102-locations-edit-form.png" alt="Screenshot: Edit Location form, pre-filled" width="700">
</p>

<p align="center">
<img src="screenshots/103-locations-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Locations screen" width="700">
</p>

<p align="center">
<img src="screenshots/104-locations-export-csv-toast.png" alt="Screenshot: Locations CSV export confirmation toast" width="500">
</p>

- **Add / Edit** — Location Name, Building, Room Number, Room Name.
- **Delete** — removes the location record. Add, Edit, and Delete are each written to the audit trail.

---

## Allocation

<p align="center">
<img src="screenshots/110-allocation-list-merged.png" alt="Screenshot: Allocation list view, merged: formal allocations + Check-In/Out loans + available equipment" width="900">
</p>

The formal, tracked workflow for assigning equipment to employees — with an approval step and full status history. The list merges three sources: active formal allocations, active Check-In/Out loans (labeled with a "Check-In/Out" source tag), and currently-available equipment.

<p align="center">
<img src="screenshots/111-allocation-equipment-filter-dropdown.png" alt="Screenshot: Equipment filter dropdown — All / Available / Allocated / Pending" width="500">
</p>

<p align="center">
<img src="screenshots/112-allocation-category-filter-dropdown.png" alt="Screenshot: Category filter dropdown on the Allocation screen" width="500">
</p>

<p align="center">
<img src="screenshots/113-allocation-overdue-highlight.png" alt="Screenshot: Allocation row(s) shown with the computed Overdue flag/highlight" width="900">
</p>

<p align="center">
<img src="screenshots/114-allocation-checkinout-source-row-disabled-buttons.png" alt="Screenshot: A Check-In/Out sourced row selected, with Approve/Return/Delete/Edit Return buttons greyed out" width="900">
</p>

### New Allocation

<p align="center">
<img src="screenshots/115-allocation-new-dialog-empty.png" alt="Screenshot: New Allocation dialog, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/116-allocation-new-equipment-dropdown.png" alt="Screenshot: Equipment dropdown in the New Allocation dialog, showing only available items" width="700">
</p>

<p align="center">
<img src="screenshots/117-allocation-new-employee-dropdown.png" alt="Screenshot: Employee dropdown in the New Allocation dialog, showing only active employees" width="700">
</p>

<p align="center">
<img src="screenshots/118-allocation-new-filled-saved.png" alt="Screenshot: New Allocation dialog filled out and saved, toast confirmation" width="700">
</p>

- Search-and-select equipment (only items not already tied to an active transaction/allocation, and not decommissioned or under repair).
- Choose an active employee and a location. Optionally set an expected return date.
- If Status is left blank, the allocation is auto-promoted straight to **Approved** rather than sitting in Pending.

### Approve

<p align="center">
<img src="screenshots/119-allocation-approve-action.png" alt="Screenshot: Approve button being used on a Pending allocation row" width="700">
</p>

<p align="center">
<img src="screenshots/120-allocation-approve-blocked-checkinout-conflict.png" alt="Screenshot: Approve blocked — equipment has an active Check-In/Out transaction" width="700">
</p>

- Moves a **pending** allocation to **approved**, marking the equipment `allocated`. Blocked if the equipment has an active Check-In/Out transaction pending elsewhere.

### Return (from Allocation screen)

<p align="center">
<img src="screenshots/121-allocation-return-confirmation.png" alt="Screenshot: Return confirmation dialog on the Allocation screen" width="700">
</p>

- Marks the allocation as returned and the equipment status back to `in_store`. Note: this path does **not** record an actual return date on the allocation record — use the Returns screen (below) for a dated return.

### Edit Expected Return Date

<p align="center">
<img src="screenshots/122-allocation-edit-return-date-dialog.png" alt="Screenshot: Edit Expected Return Date dialog" width="700">
</p>

### Delete Allocation

<p align="center">
<img src="screenshots/123-allocation-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Allocation screen" width="700">
</p>

<p align="center">
<img src="screenshots/124-allocation-delete-blocked-active.png" alt="Screenshot: Delete blocked error — allocation is currently Approved" width="700">
</p>

- Removes an allocation record entirely. Blocked while it's Approved (return or transfer first). Not written to the audit log.

### Allocation Detail Popup

<p align="center">
<img src="screenshots/125-allocation-detail-popup.png" alt="Screenshot: Allocation detail popup — photo, status badge, full field list" width="700">
</p>

---

## Returns

<p align="center">
<img src="screenshots/130-returns-list.png" alt="Screenshot: Returns history view — allocation returns + completed Check-In/Out, merged and sorted newest first" width="900">
</p>

A dedicated history view combining every returned allocation with every completed Check-In/Out transaction. Rows are flagged when the actual return date is later than the expected return date.

### Process Return

<p align="center">
<img src="screenshots/131-returns-process-dialog-empty.png" alt="Screenshot: Process Return dialog, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/132-returns-process-equipment-dropdown.png" alt="Screenshot: Process Return dialog — dropdown listing all currently Approved allocations" width="700">
</p>

<p align="center">
<img src="screenshots/133-returns-process-condition-picker.png" alt="Screenshot: Condition-on-return grade picker in the Process Return dialog" width="700">
</p>

- Select an active allocation, set the return date and the item's condition on return. Saving updates the allocation to Returned, sets the equipment to In Store, and stores the new condition grade directly on the equipment record.

### Edit Return

<p align="center">
<img src="screenshots/134-returns-edit-dialog.png" alt="Screenshot: Edit Return dialog — adjusting return date / condition after the fact" width="700">
</p>

- Available only for allocation-sourced rows (not Check-In/Out sourced rows). Not written to the audit log.

### Undo Return

<p align="center">
<img src="screenshots/135-returns-undo-confirmation.png" alt="Screenshot: Undo Return confirmation dialog" width="700">
</p>

- Reverts a processed return: the allocation goes back to Approved, its actual return date is cleared, and the equipment goes back to Allocated. Logged with the **same** audit action label as a genuine return.

<p align="center">
<img src="screenshots/136-returns-overdue-flag.png" alt="Screenshot: A return row visually flagged as late/overdue" width="900">
</p>

---

## Transfer

<p align="center">
<img src="screenshots/140-transfer-list.png" alt="Screenshot: Transfer list view — every currently Approved allocation" width="900">
</p>

Move equipment directly from one employee/location to another, without a separate return-then-reallocate step.

<p align="center">
<img src="screenshots/141-transfer-new-dialog-prefilled.png" alt="Screenshot: Transfer dialog opened by double-clicking a row, pre-filled with that allocation" width="700">
</p>

<p align="center">
<img src="screenshots/142-transfer-new-employee-picker.png" alt="Screenshot: New employee picker inside the Transfer dialog" width="700">
</p>

<p align="center">
<img src="screenshots/143-transfer-new-location-picker.png" alt="Screenshot: New location picker inside the Transfer dialog" width="700">
</p>

<p align="center">
<img src="screenshots/144-transfer-confirmation-toast.png" alt="Screenshot: Toast confirmation after a successful transfer" width="500">
</p>

- **New Transfer** — search-and-select the equipment, pick the new employee and/or new location.
- Double-clicking a row in the Transfer grid pre-selects that item and jumps straight into the transfer dialog.
- The equipment's status stays `allocated` throughout — only the assigned employee/location changes. Internally, the old allocation is closed out as **Returned** and a new **Approved** allocation is created, linked back to the one it replaced.

<p align="center">
<img src="screenshots/145-transfer-appears-in-returns-history.png" alt="Screenshot: The closed-out old allocation from a transfer, showing up in Returns history" width="900">
</p>

---

## Decommission

*(Admin only)*

<p align="center">
<img src="screenshots/150-decommission-list.png" alt="Screenshot: Decommission list view, showing decommissioned equipment with reason/date columns" width="900">
</p>

The permanent retirement workflow for equipment that's reached end-of-life, while keeping its full history.

<p align="center">
<img src="screenshots/151-decommission-dialog-empty.png" alt="Screenshot: Decommission dialog, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/152-decommission-equipment-dropdown.png" alt="Screenshot: Decommission dialog equipment dropdown — only In Store / Under Repair items offered" width="700">
</p>

<p align="center">
<img src="screenshots/153-decommission-reason-dropdown.png" alt="Screenshot: Decommission reason dropdown — Damaged, Obsolete, Beyond Repair, End of Life, Lost, Other" width="700">
</p>

<p align="center">
<img src="screenshots/154-decommission-notes-field.png" alt="Screenshot: Optional notes field on the Decommission dialog, filled in" width="700">
</p>

<p align="center">
<img src="screenshots/155-decommission-confirmation-toast.png" alt="Screenshot: Toast confirmation after decommissioning an item" width="500">
</p>

- Select an item currently In Store or Under Repair (display-only filter — no hard database check blocks other statuses via other paths).
- Choose a reason: Damaged, Obsolete, Beyond Repair, End of Life, Lost, or Other — with an optional notes field.
- Confirming sets the item's status to `decommissioned` — there is **no "un-decommission" button** anywhere in the app.
- The Decommission grid's Reason and Date columns are sourced from that item's audit log entry — clearing logs from Settings will blank these for existing entries.

---

## Deleted Items

*(Admin only)*

<p align="center">
<img src="screenshots/160-deleted-items-list.png" alt="Screenshot: Deleted Items list view — soft-deleted equipment, most recent first" width="900">
</p>

A recycle bin for equipment removed via the Equipment screen's Delete button. Nothing is permanently lost until explicitly confirmed here.

<p align="center">
<img src="screenshots/161-deleted-items-select-mode.png" alt="Screenshot: Select ✔ / Select All dropdown enabled on the Deleted Items grid" width="700">
</p>

<p align="center">
<img src="screenshots/162-deleted-items-restore-confirmation.png" alt="Screenshot: Restore confirmation dialog / toast" width="700">
</p>

<p align="center">
<img src="screenshots/163-deleted-items-permanent-delete-confirmation.png" alt="Screenshot: Permanent Delete confirmation dialog, with irreversible-action warning copy" width="700">
</p>

<p align="center">
<img src="screenshots/164-deleted-items-permanent-delete-blocked.png" alt="Screenshot: Permanent Delete failing because related records still exist elsewhere" width="700">
</p>

<p align="center">
<img src="screenshots/165-deleted-items-doubleclick-quick-dialog.png" alt="Screenshot: Small inline Restore / Delete / Cancel dialog from double-clicking a row" width="500">
</p>

- **Restore** — brings item(s) back to the active Equipment list exactly as they were. Not written to the audit log.
- **Permanent Delete** — erases the selected item(s) from the database entirely. Cannot be undone. Not written to the audit log. Will fail outright if the item still has related records (allocations, transactions, logs, maintenance records) elsewhere.
- **Select ✔ / Select All** dropdown for bulk operations, same pattern as Equipment.
- **Double-clicking a row** opens a small Restore / Delete / Cancel dialog for that single item.

---

## Logs (Audit Trail)

<p align="center">
<img src="screenshots/170-logs-list.png" alt="Screenshot: Logs / Audit Trail list view, color-coded by action type" width="900">
</p>

A searchable, chronological record of key actions taken across the app.

<p align="center">
<img src="screenshots/171-logs-search.png" alt="Screenshot: Logs search box with fuzzy-matched results" width="700">
</p>

<p align="center">
<img src="screenshots/172-logs-action-color-legend.png" alt="Screenshot: Logs grid showing several differently-colored action types together (green/blue/yellow/red/purple/orange)" width="900">
</p>

<p align="center">
<img src="screenshots/173-logs-click-to-sort-column.png" alt="Screenshot: Logs grid column header clicked to sort" width="900">
</p>

- Columns: Equipment Code, QR Code, Action (color-coded by type), Changed By, Previous Value, New Value, Timestamp.
- Actions logged include: equipment created/updated, maintenance opened/resolved, allocation requested/approved/returned, transfers, decommission approved, employee and location created/updated/deleted, and checked in/checked out.
- **Not logged:** equipment soft-delete, restore, permanent delete, allocation delete, edit return, all user-management actions in Settings, password changes, category add/delete, saving Settings, and overdue Check-In/Out status updates.
- ⚠️ Undoing a Return in the Returns screen logs the exact same action label as a genuine return — telling them apart requires opening the individual entry's before/after detail.

### Log Detail Popup

<p align="center">
<img src="screenshots/174-logs-detail-popup.png" alt="Screenshot: Log detail popup — Before (red) panel and After (green) panel, bulleted field changes" width="700">
</p>

### Export

<p align="center">
<img src="screenshots/175-logs-export-csv-toast.png" alt="Screenshot: Logs CSV export confirmation toast" width="500">
</p>

---

## QR Scanner

<p align="center">
<img src="screenshots/180-qrscanner-main-empty.png" alt="Screenshot: QR Scanner main screen, blank/ready state with code field auto-focused" width="900">
</p>

A fast, **entirely read-only** lookup tool designed for use with a USB barcode/QR scanner (keyboard-wedge mode, no drivers needed) or manual typing.

<p align="center">
<img src="screenshots/181-qrscanner-typing-suggestions.png" alt="Screenshot: Fuzzy suggestion dropdown appearing while typing a partial code" width="700">
</p>

<p align="center">
<img src="screenshots/182-qrscanner-match-result-detail.png" alt="Screenshot: QR Scanner full match result — detail card with status badge" width="900">
</p>

<p align="center">
<img src="screenshots/183-qrscanner-match-current-holder.png" alt="Screenshot: Match result showing current holder + location for an allocated item" width="900">
</p>

<p align="center">
<img src="screenshots/184-qrscanner-history-grid.png" alt="Screenshot: Full unpaginated change-history grid beneath a match result" width="900">
</p>

<p align="center">
<img src="screenshots/185-qrscanner-no-match-found.png" alt="Screenshot: No-match state after searching a code that doesn't exist" width="700">
</p>

- Scan or type a QR code, Equipment Code, or Serial Number — the app auto-searches ~300ms after you stop typing, or immediately on Enter.
- Matching is **exact**, not fuzzy — a partial code returns nothing; the suggestion dropdown only helps you complete the code.
- On match: full equipment detail card (status, current holder, current location) plus a complete, unpaginated change-history timeline for that item.

---

## Check In / Check Out

<p align="center">
<img src="screenshots/190-checkinout-main-empty.png" alt="Screenshot: Check In/Out main screen, blank/ready state" width="900">
</p>

A lightweight, no-approval loan tracker for short-term, informal borrows — kept in a separate `transactions` table from the formal Allocation workflow (e.g. borrowing a projector for a meeting).

<p align="center">
<img src="screenshots/191-checkinout-autotab-checkout.png" alt="Screenshot: Screen auto-opening the Check-Out tab for an available item" width="900">
</p>

<p align="center">
<img src="screenshots/192-checkinout-autotab-checkin.png" alt="Screenshot: Screen auto-opening the Check-In tab for a currently checked-out item" width="900">
</p>

### Check-Out

<p align="center">
<img src="screenshots/193-checkinout-checkout-form-empty.png" alt="Screenshot: Check-Out form, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/194-checkinout-checkout-form-filled.png" alt="Screenshot: Check-Out form filled — issued to, department, operator, expected return, condition, remarks" width="700">
</p>

<p align="center">
<img src="screenshots/195-checkinout-checkout-blocked-formal-allocation.png" alt="Screenshot: Check-Out blocked — item already has a pending/approved formal allocation" width="700">
</p>

- Records who it was issued to, their department, the operator processing it (pre-filled with your own name), expected return date, condition beforehand, and remarks. Marks the equipment `allocated` and the transaction `Pending`.
- Blocked if the item already has a pending/approved formal allocation.

### Check-In

<p align="center">
<img src="screenshots/196-checkinout-checkin-form-empty.png" alt="Screenshot: Check-In form, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/197-checkinout-checkin-form-filled.png" alt="Screenshot: Check-In form filled — condition after, remarks" width="700">
</p>

- Records the actual return date, marks the transaction `Completed`, and records condition-after plus remarks. Marks the equipment `in_store`. Note: this updates the transaction's own condition record, **not** the equipment's main Condition Grade field.

### Overdue Transactions

<p align="center">
<img src="screenshots/198-checkinout-overdue-flag.png" alt="Screenshot: A pending transaction shown flagged as Overdue" width="900">
</p>

### Transaction History Panel

<p align="center">
<img src="screenshots/199-checkinout-history-panel.png" alt="Screenshot: Last-10-transactions history panel for the currently loaded item" width="900">
</p>

---

## Reports

<p align="center">
<img src="screenshots/200-reports-main-screen.png" alt="Screenshot: Reports screen, report type selector" width="900">
</p>

Four report types, each with a live on-screen preview before exporting. All reports are read-only — nothing here writes to the database. Your organization's name (Settings → General) appears at the top of every export.

| Report | Shows |
|---|---|
| **Equipment Inventory** | Full equipment list with status and current location; filterable by category |
| **Employee Allocation Summary** | Every active employee with items currently held and total ever allocated (employees with zero allocations are omitted) |
| **Active Allocations** | All equipment currently allocated or pending approval |
| **Returns History** | All formal allocation returns with the duration each item was held, in days, flagged if over 30 days (Check-In/Out completions are **not** included in this report) |

<p align="center">
<img src="screenshots/201-reports-equipment-inventory-preview.png" alt="Screenshot: Equipment Inventory report preview" width="900">
</p>

<p align="center">
<img src="screenshots/202-reports-equipment-inventory-category-filter.png" alt="Screenshot: Category filter applied to the Equipment Inventory report" width="700">
</p>

<p align="center">
<img src="screenshots/203-reports-employee-summary-preview.png" alt="Screenshot: Employee Allocation Summary report preview" width="900">
</p>

<p align="center">
<img src="screenshots/204-reports-active-allocations-preview.png" alt="Screenshot: Active Allocations report preview" width="900">
</p>

<p align="center">
<img src="screenshots/205-reports-returns-history-preview.png" alt="Screenshot: Returns History report preview, with a >30-day duration flagged" width="900">
</p>

### Export

<p align="center">
<img src="screenshots/206-reports-export-pdf-print-preview.png" alt="Screenshot: PDF/print preview generated from a report, multi-page" width="900">
</p>

<p align="center">
<img src="screenshots/207-reports-export-pdf-printer-selection.png" alt="Screenshot: Printer selection dialog, choosing a PDF printer driver to save as PDF" width="700">
</p>

<p align="center">
<img src="screenshots/208-reports-export-excel-result.png" alt="Screenshot: Excel export result — company-name header row, bold green column headers" width="900">
</p>

- **Export PDF** — generates a formatted print preview you can print directly or save via a PDF printer driver (e.g. "Microsoft Print to PDF"). There is no direct "Save as PDF" button.
- **Export Excel** — styled spreadsheet export with a company-name header row, bold green column headers, and auto-sized columns.

---

## Settings

*(Admin only — this entire module is hidden from the sidebar for Operator accounts)*

<p align="center">
<img src="screenshots/210-settings-overview-tabs.png" alt="Screenshot: Settings screen overview, showing all left-sidebar tabs" width="900">
</p>

Settings has seven tabs down its own left sidebar: General, Backup / Restore, Users, Change Password, Printing, Categories, and System Configuration.

### General

<p align="center">
<img src="screenshots/211-settings-general-tab.png" alt="Screenshot: Settings — General tab, Company Name field and System info card" width="900">
</p>

<p align="center">
<img src="screenshots/212-settings-general-save-disabled.png" alt="Screenshot: Save button disabled state before the Company Name field is changed" width="700">
</p>

<p align="center">
<img src="screenshots/213-settings-general-save-confirmed.png" alt="Screenshot: Inline 'Saved' confirmation after saving General settings" width="700">
</p>

<p align="center">
<img src="screenshots/214-settings-general-system-info-card.png" alt="Screenshot: System info card — app version, last backup date, connected database name/server" width="700">
</p>

- Company Name (shown across reports, QR sheets, and printed labels). Save button stays disabled until the field actually changes.
- System info card: installed app version, date of last database backup, and currently connected database (name + server).

#### Danger Zone

<p align="center">
<img src="screenshots/215-settings-danger-zone-panel.png" alt="Screenshot: Danger Zone panel — Clear all logs and Reset application data buttons" width="700">
</p>

<p align="center">
<img src="screenshots/216-settings-danger-zone-clear-logs-confirmation.png" alt="Screenshot: Clear all logs confirmation prompt" width="700">
</p>

<p align="center">
<img src="screenshots/217-settings-danger-zone-reset-type-to-confirm.png" alt="Screenshot: Reset Application Data — 'type RESET to confirm' input field" width="700">
</p>

<p align="center">
<img src="screenshots/218-settings-danger-zone-reset-confirmed.png" alt="Screenshot: Reset Application Data confirmation dialog, confirm button now enabled" width="700">
</p>

- **Clear all logs** — permanently wipes the audit trail (no undo). Also blanks the reason/date shown on already-decommissioned items, since those are sourced from the log.
- **Reset application data** — deletes all allocations, then all equipment records, in two separate save steps (requires typing "RESET" to enable the confirm button). Users, roles, categories, and settings are unaffected. **No automatic backup is taken first** — take a manual backup before running this.

### Backup / Restore

<p align="center">
<img src="screenshots/219-settings-backup-restore-tab.png" alt="Screenshot: Settings — Backup/Restore tab overview" width="900">
</p>

<p align="center">
<img src="screenshots/220-settings-backup-credentials-prompt.png" alt="Screenshot: Backup — confirm database host/name/username/password prompt" width="700">
</p>

<p align="center">
<img src="screenshots/221-settings-backup-file-save-dialog.png" alt="Screenshot: Backup — choose save location file dialog" width="700">
</p>

<p align="center">
<img src="screenshots/222-settings-backup-success-toast.png" alt="Screenshot: Backup success toast notification" width="500">
</p>

<p align="center">
<img src="screenshots/223-settings-restore-file-picker.png" alt="Screenshot: Restore — choose backup file to restore from" width="700">
</p>

<p align="center">
<img src="screenshots/224-settings-restore-destructive-confirmation.png" alt="Screenshot: Restore confirmation dialog, warning that current data will be overwritten" width="700">
</p>

<p align="center">
<img src="screenshots/225-settings-restore-success-restart-notice.png" alt="Screenshot: Restore success message recommending an app restart" width="700">
</p>

- **Backup** — exports the full database to a file via the located MySQL backup tool. Timestamp is saved and shown on the General tab.
- **Restore** — imports a previously saved backup file, **overwriting current data entirely**. Restart the app afterward.

### Users

<p align="center">
<img src="screenshots/226-settings-users-list.png" alt="Screenshot: Settings — Users list, active/inactive status dots" width="900">
</p>

<p align="center">
<img src="screenshots/227-settings-users-add-form-empty.png" alt="Screenshot: Add User form, blank state" width="900">
</p>

<p align="center">
<img src="screenshots/228-settings-users-add-form-filled.png" alt="Screenshot: Add User form filled — username, full name, email, password, role, picture" width="900">
</p>

<p align="center">
<img src="screenshots/229-settings-users-edit-form-username-locked.png" alt="Screenshot: Edit User form — Username field shown as locked/non-editable" width="900">
</p>

<p align="center">
<img src="screenshots/230-settings-users-role-dropdown.png" alt="Screenshot: Role dropdown — Admin / Operator" width="500">
</p>

<p align="center">
<img src="screenshots/231-settings-users-profile-picture-upload.png" alt="Screenshot: Profile picture upload control on the User form" width="700">
</p>

<p align="center">
<img src="screenshots/232-settings-users-detail-popup.png" alt="Screenshot: User detail popup — avatar, username, full name, email, role, status" width="700">
</p>

<p align="center">
<img src="screenshots/233-settings-users-toggle-active.png" alt="Screenshot: Toggle Active/Inactive action on a user row" width="700">
</p>

<p align="center">
<img src="screenshots/234-settings-users-toggle-blocked-self.png" alt="Screenshot: Error — cannot deactivate the account you are currently logged in as" width="700">
</p>

<p align="center">
<img src="screenshots/235-settings-users-delete-confirmation.png" alt="Screenshot: Delete User confirmation dialog" width="700">
</p>

<p align="center">
<img src="screenshots/236-settings-users-delete-blocked-self.png" alt="Screenshot: Error — cannot delete the account you are currently logged in as" width="700">
</p>

- **Add / Edit User** — Username (required, unique, locked after creation), Full Name, Email (unique if provided), New/Confirm Password (required on create, leave blank on edit to keep unchanged), Role (Admin/Operator), Profile Picture (JPG/PNG/BMP/GIF), Is Active Account checkbox.
- **User Detail popup** — double-clicking a row opens a read-only card.
- **Toggle Active** — you cannot deactivate your own account.
- **Delete User** — permanent; you cannot delete your own account; fails if the account is still referenced elsewhere.
- ⚠️ None of the actions on this tab are written to the audit trail.

### Change Password

<p align="center">
<img src="screenshots/237-settings-change-password-tab.png" alt="Screenshot: Settings — Change Password tab, three password fields" width="900">
</p>

<p align="center">
<img src="screenshots/238-settings-change-password-strength-weak.png" alt="Screenshot: Live strength meter showing Weak" width="700">
</p>

<p align="center">
<img src="screenshots/239-settings-change-password-strength-strong.png" alt="Screenshot: Live strength meter showing Strong" width="700">
</p>

<p align="center">
<img src="screenshots/240-settings-change-password-mismatch-error.png" alt="Screenshot: Error — new password and confirmation don't match" width="700">
</p>

<p align="center">
<img src="screenshots/241-settings-change-password-wrong-current.png" alt="Screenshot: Error — current password is incorrect" width="700">
</p>

<p align="center">
<img src="screenshots/242-settings-change-password-security-tips-panel.png" alt="Screenshot: Security Tips panel alongside the form" width="700">
</p>

- Requires current password, a new password (**only enforced rule: 4+ characters**), and confirmation. The Weak/Medium/Strong meter is advisory only and does not block a weak password from being saved.

### Printing

<p align="center">
<img src="screenshots/243-settings-printing-tab.png" alt="Screenshot: Settings — Printing tab, printer name and label dimensions" width="900">
</p>

<p align="center">
<img src="screenshots/244-settings-printing-test-print-preview.png" alt="Screenshot: Test Print preview at configured label size" width="700">
</p>

- Configure printer name (blank = system default) and label dimensions (default 62mm × 29mm).
- **Test Print** — prints a sample QR label at the configured size.

### Categories

<p align="center">
<img src="screenshots/245-settings-categories-tab.png" alt="Screenshot: Settings — Categories tab, numbered list of saved categories" width="700">
</p>

<p align="center">
<img src="screenshots/246-settings-categories-add-new.png" alt="Screenshot: Typing a new category name and pressing Enter/Add" width="700">
</p>

<p align="center">
<img src="screenshots/247-settings-categories-delete-confirmation.png" alt="Screenshot: Delete confirmation for a category" width="700">
</p>

- **Add / Delete** categories used throughout the Equipment and Reports screens. Deleting a category does **not** retroactively update equipment records that already used it — those records keep the old category text, which will keep appearing as an orphaned filter option.

### System Configuration

*(Admin only — the single most privileged screen in the app)*

<p align="center">
<img src="screenshots/248-settings-system-config-tab.png" alt="Screenshot: Settings — System Configuration tab, database connection fields" width="900">
</p>

<p align="center">
<img src="screenshots/249-settings-system-config-password-hidden.png" alt="Screenshot: Database Password field, hidden with eye icon" width="700">
</p>

<p align="center">
<img src="screenshots/250-settings-system-config-test-connection-success.png" alt="Screenshot: Test Connection — green check success indicator" width="700">
</p>

<p align="center">
<img src="screenshots/251-settings-system-config-test-connection-fail.png" alt="Screenshot: Test Connection — red cross failure indicator" width="700">
</p>

<p align="center">
<img src="screenshots/252-settings-system-config-save-disabled-until-tested.png" alt="Screenshot: Save Connection button disabled until a successful test has run" width="700">
</p>

<p align="center">
<img src="screenshots/253-settings-system-config-save-confirmed-toast.png" alt="Screenshot: Save Connection success toast" width="500">
</p>

<p align="center">
<img src="screenshots/254-settings-system-config-status-card.png" alt="Screenshot: Connection Status card — live status indicator, active server/database, last successful connection timestamp" width="700">
</p>

- Manages the live database connection (Server/IP, Database Name, Username, Password) independently of the General tab.
- **Test Connection** must succeed before **Save Connection** becomes available.
- The saved password is encrypted in a way tied to the specific Windows account and machine it was saved from — it cannot be decrypted if the settings file is moved elsewhere.
- Changing these settings changes which database the **entire application** — every screen, every user — reads and writes to. Change with care.

---

## Equipment Status Lifecycle

```
in_store      -->  allocated       (Allocation approved OR Check-Out)
in_store      -->  under_repair    (Edit → status changed to Under Repair)
in_store      -->  decommissioned  (Decommission)
under_repair  -->  in_store        (Maintenance resolved)
under_repair  -->  decommissioned  (Decommission)
allocated     -->  in_store        (Return / Deallocate / Check-In)
allocated     -->  allocated       (Transfer — new allocation record, status unchanged)
```

| Status | Meaning |
|---|---|
| In Store | Available, not currently assigned |
| Allocated | Currently assigned to an employee |
| Under Repair | Flagged for maintenance |
| Decommissioned | Permanently retired (no arrow leads back out in normal use) |

<p align="center">
<img src="screenshots/260-lifecycle-diagram-in-app-if-any.png" alt="Screenshot: In-app visualization of an item's status history, if the UI surfaces one (dashboard timeline, detail popup, etc.)" width="900">
</p>

---

## Cross-Module Effects Reference

A quick reference for what each action also touches elsewhere, and whether it's audited — useful when investigating an unexpected change.

| Action | Also affects | Audited? |
|---|---|:---:|
| Add / Edit Equipment | Equipment, Logs, optionally Procurement Types | ✅ |
| Soft Delete / Restore / Permanent Delete Equipment | Equipment | ❌ |
| Add / Edit / Delete Employee | Employees, Logs | ✅ |
| Add / Edit / Delete Location | Locations, Logs | ✅ |
| Create / Approve / Return Allocation | Allocations, Equipment, Logs | ✅ |
| Delete Allocation | Allocations | ❌ |
| Edit Expected Return Date | Allocations, Logs (free-text entry) | ✅ |
| Process Return / Undo Return | Allocations, Equipment, Logs (undo shares the "return" label) | ✅ |
| Edit Return record | Allocations, Equipment | ❌ |
| Transfer | Allocations (2 rows), Logs | ✅ |
| Decommission | Equipment, Logs | ✅ |
| Check-Out / Check-In | Transactions, Equipment, Logs | ✅ |
| Add / Toggle / Delete User, Change Password | Users | ❌ |
| Add / Delete Category | Categories | ❌ |
| Save Settings | App Settings | ❌ |
| Clear All Logs | Logs (all rows removed) | n/a |
| Reset Application Data | Allocations (all), Equipment (all) | ❌ |

---

## Installation & Setup

1. Install **.NET 8 SDK/Runtime** and **Visual Studio 2022** (or newer) with the .NET desktop development workload.
2. Clone or copy the project folder.
3. Open the `.sln` file in Visual Studio.
4. Restore NuGet packages (Visual Studio does this automatically on first build).
5. Press **F5** to build and run.

---

## Database Setup

1. Set up a MariaDB (or MySQL-compatible) server, reachable from the machine(s) running the app.
2. Import the provided schema (`inventory_management.sql`) into a new database.
3. Update the connection string in `Models/InventoryDbContext.cs` to point to your server, database name, and credentials — or configure it from inside the app via **Settings → System Configuration** once you have at least one Admin account.
4. Seed the `roles` table with `Admin` and `Operator` entries, then create at least one Admin user via the `users` / `user_roles` tables (or the app's Add User screen once one Admin account exists).
5. Equipment, User, and Employee pictures are read from and written to a shared network folder (`\\<server-ip>\QInventoryData`, in subfolders `Equipment`, `Users`, and `Employees`) so every PC running the app sees the same pictures — make sure this share exists and is reachable before uploading pictures.

---

## Known Limitations & Data-Safety Notes

- **Passwords are stored and compared as plain text** — not hashed. Recommended to add hashing before any real production deployment. Treat direct database access to the `users` table as equivalent to having every account's password.
- **The System Configuration database password is encrypted in a way tied to a specific Windows user account + machine** — moving the settings file elsewhere requires reconfiguring from scratch.
- **The database password used for backups is passed as a plain command-line argument** to the backup tool, and could theoretically be visible in the process list on a shared/monitored machine at the exact moment a backup runs.
- **Audit log entries for some actions are attributed to a default/generic account** rather than always reflecting the precise session user for every code path — verify this against your build before relying on it for compliance.
- **Undoing a return in the Returns screen logs the same audit action type as a normal return**, so the two can't be told apart from the Logs list alone — you have to open the individual entry's before/after detail.
- **A Transfer causes the old allocation to also appear in Returns history**, since internally it's closed out the same way a real return is.
- **Deleting a Category doesn't update equipment records that already used it** — the old category text remains on those records and keeps appearing as an orphaned filter option.
- **"Reset application data" in Settings is not wrapped in a single transaction** — if it fails partway through, allocations may be deleted while equipment records remain. It also does **not** take an automatic backup first.
- **Equipment soft-delete has no check for active allocations** — it's possible to delete an item that is still allocated to someone.
- **The Decommission dialog's "In Store / Under Repair only" equipment list is a UI-level filter, not a hard database constraint** — always verify an item's real status before assuming decommission is safe.
- **The Returns History report only includes formal allocation returns** — completed Check-In/Out transactions do not appear there, even though they appear correctly on the Returns screen itself.
- The connection string / shared-picture-folder path can be set once in code or via System Configuration, but is not (yet) split across separate per-environment config files.
- Several grids (Locations, QR Scanner history, some report views) load their full dataset rather than paging server-side.

---

## Credits

Developed by **Syed Shayan Haider Kazmi**.

---

## License

This project is for internal organizational use.
All rights reserved © 2026.
