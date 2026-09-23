#                AssetPilot Inventory Management System

<p align="center">
<img src="screenshots/000-app-overview.png" alt="Screenshot: Dashboard overview" width="900">
</p>

<p align="center">
<em>A desktop application for tracking physical equipment — from purchase to allocation, transfer, repair, and retirement — built with C# .NET 8 (WinForms), Entity Framework Core, and MariaDB / MySQL.</em>
</p>

> 📸 **About the screenshots in this README:** every image below points to a file under `screenshots/` that doesn't exist yet — capture your own and drop it in with the matching filename to fill each slot in. Placeholders are numbered sequentially in blocks per section so that **every distinct piece of functionality has its own dedicated image slot** — list views, every dialog, every popup, every button state, every filter, and every empty/error/confirmation state. Keep the numbering intact when you add captures so the doc stays easy to audit at a glance (a missing number = a missing screenshot).



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

AssetPilot tracks every physical item a company owns — laptops, furniture, accessories, tools, anything — through its entire life:

- What equipment exists, and what condition is it in?
- Where is it right now, and who has it?
- Who supplied it, what did it cost, and is it still under warranty?
- When was it handed out, and when is it due back?
- Is it out on a quick informal loan (Check In/Out), or a formal tracked allocation?
- What's happened to it over its lifetime — every repair, transfer, return, and status change?
- Who made each change, and when?
- Routine confirmations (save, restore, delete, approve, edit) show as brief toast messages color-matched to the action that triggered them, instead of interrupting popups — only destructive actions still require an explicit confirmation dialog.

It covers the full lifecycle: **procurement → storage → allocation → return / transfer → repair → decommission (with a way back)**, with QR-code identification throughout, a full audit trail, and a separate file-based security log for the most sensitive administrative actions.

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# .NET 8 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core |
| Database | MariaDB / MySQL |
| QR Generation | QRCoder |
| Excel Export | ClosedXML |
| PDF Generation | System.Drawing.Printing + PrintPreviewDialog |
| Charts | Custom GDI+ (no third-party charting library) |
| Picture Storage | Network share (`\\{server}\AssetPilotData\`) for equipment, user, and employee pictures, with an automatic local fallback (`%AppData%\AssetPilot\`) when the share isn't reachable — e.g. a local/XAMPP deployment |
| Platform | Windows 10/11 (64-bit) |

---

## User Roles

AssetPilot has two roles, assigned per user account and cached for the session via `AuthService`.

| Feature | Admin | Operator |
|---|:---:|:---:|
| Dashboard & Charts | ✅ | ✅ |
| Equipment — View / Add / Edit | ✅ | ✅ |
| Equipment — Delete | ✅ | ❌ Restore button hidden |
| Employees — Full management | ✅ | ✅ |
| Locations — Full management | ✅ | ✅ |
| Allocation — Allocate / Approve / Return | ✅ | ✅ |
| Transfer | ✅ | ✅ |
| Returns History | ✅ | ✅ |
| QR Scanner | ✅ | ✅ |
| Check In / Check Out | ✅ | ✅ |
| Reports — View & Export | ✅ | ✅ |
| Logs (Audit Trail) | ✅ | ✅ |
| Decommission (incl. Restore) | ✅ | ❌ Not Accessible from sidebar |
| Deleted Items (restore/permanent delete) | ✅ | ❌ Not Accessible from sidebar |
| Settings — General / Users / Change Password / Printing / Categories / Backup-Restore / System Configuration | ✅ | ❌ Not Accessible from sidebar |

The logged-in user's full name and role are shown persistently in the sidebar/session, and every save made anywhere in the app is attributed to that account and timestamped — this is what powers the audit trail (see [Logs](#logs-audit-trail)). Attempting to open a restricted screen directly is blocked with a warning toast rather than a silent failure.

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

> ⚠️ Passwords are currently stored and compared as **plain text**, not hashed — see [Known Limitations](#known-limitations--data-safety-notes).

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
| Currently Overdue | Active allocations past their expected return date (computed live, never stored). Shown in amber; clicking the card jumps straight to the Allocation screen pre-filtered to overdue items |

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

### Top Employees by Holdings

<p align="center">
<img src="screenshots/025-dashboard-top-employees-card.png" alt="Screenshot: Top Employees by Holdings card, ranked progress bars" width="900">
</p>

<p align="center">
<img src="screenshots/026-dashboard-top-employees-hover-tooltip.png" alt="Screenshot: Hover tooltip over an employee's bar, showing a full allocation breakdown" width="700">
</p>

Ranks the employees currently holding the most equipment, using multi-colored progress bars sized to each person's allocation volume, with the allocation count shown on the left of each bar. Hovering a name or bar shows a tooltip with the full breakdown of what they currently hold.

### Sidebar Navigation & Collapse

<p align="center">
<img src="screenshots/027-dashboard-sidebar-expanded.png" alt="Screenshot: Sidebar navigation, expanded" width="300">
</p>

<p align="center">
<img src="screenshots/028-dashboard-sidebar-collapsed.png" alt="Screenshot: Sidebar navigation, collapsed to icon-only view" width="150">
</p>

<p align="center">
<img src="screenshots/029-dashboard-title-bar-animation.png" alt="Screenshot: Title bar label mid character-by-character animation when switching screens" width="900">
</p>

Every module in the app is reachable from the left sidebar. Admin-only entries (Decommission, Deleted Items, Settings) are automatically hidden for Operator accounts. A toggle button at the top of the sidebar smoothly collapses it to an icon-only strip and back, without affecting any screen's functionality.

### Global Search (Ctrl + K)

<p align="center">
<img src="screenshots/030-dashboard-global-search-open.png" alt="Screenshot: Global Search dialog open via Ctrl+K, showing fuzzy results across modules" width="700">
</p>

Press **Ctrl + K** anywhere in the app to open a real-time fuzzy search across Equipment, Allocations, Employees, and Locations at once. Selecting a result jumps directly to that record or screen.

### Light / Dark Mode Toggle

<p align="center">
<img src="screenshots/031-dashboard-theme-toggle-light.png" alt="Screenshot: Theme toggle switch in light mode position" width="300">
</p>

<p align="center">
<img src="screenshots/032-dashboard-theme-toggle-dark.png" alt="Screenshot: Theme toggle switch in dark mode position, whole app repainted in dark palette" width="900">
</p>

A pill-shaped switch in the top bar, next to the notification bell, toggles the entire app between a light and dark color palette instantly, with an animated sliding thumb. The theme is remembered per workstation (not per user account) and restored automatically the next time AssetPilot is launched — there's no separate save step.

### Notifications

<p align="center">
<img src="screenshots/033-dashboard-notification-bell-badge.png" alt="Screenshot: Notification bell icon with live badge count, visible from any screen" width="300">
</p>

<p align="center">
<img src="screenshots/034-dashboard-notification-dropdown.png" alt="Screenshot: Notification bell dropdown open, listing overdue allocations and open maintenance items" width="500">
</p>

A bell icon in the top bar shows a live count badge, visible from every screen in the app — not just the Dashboard:
- **Overdue allocations** — approved allocations past their expected return date.
- **Open maintenance items** — equipment with a maintenance record that isn't yet resolved.
- **Low-stock items**, where applicable.

Clicking it opens a dropdown listing these alerts with quick links straight to the relevant equipment record.

### Recent Activities Panel

<p align="center">
<img src="screenshots/035-dashboard-recent-activities.png" alt="Screenshot: Recent Activities live feed panel" width="900">
</p>

A live feed of the most recent allocation, return, and transfer events across the whole system, with relative timestamps (e.g. "7h ago") — sourced directly from the audit trail.

---

## Equipment

<p align="center">
<img src="screenshots/040-equipment-list-full.png" alt="Screenshot: Equipment list view, full grid" width="900">
</p>

The master list of every equipment item. Columns: Equipment Code, QR Code, Procurement Type, Name, Make, Model, Serial Number, Category, Status, Condition, Location, Purchase Date, Price.

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
<img src="screenshots/044-equipment-toolbar-full.png" alt="Screenshot: Equipment toolbar — Add, Edit, Delete, Refresh, Export, Import, Select buttons" width="900">
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
- A picture can be staged and attached to the record — stored on the shared network folder when reachable, or a local folder on this machine otherwise.

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

> ⚠️ The record's internal "created by" field is always stamped with a fixed system account rather than the signed-in user, even though the "Equipment Created" audit log entry itself is correctly attributed to whoever actually saved it.

### Edit Equipment

<p align="center">
<img src="screenshots/052-equipment-edit-form.png" alt="Screenshot: Edit Equipment form, pre-filled with existing values" width="900">
</p>

- Same fields as Add, pre-filled with the current values.
- Switching status **to "Under Repair"** opens a maintenance dialog to log the issue being reported, opening a new maintenance record and writing a "Maintenance Opened" audit entry.
- Switching status **away from "Under Repair"** marks the open maintenance record as completed automatically and writes a "Maintenance Resolved" audit entry.
- Saving an edit compares old and new values field-by-field and logs only the fields that actually changed.

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
<img src="screenshots/057-equipment-detail-popup-main.png" alt="Screenshot: Equipment detail popup, full two-column card" width="900">
</p>

Double-clicking a row opens a read-only, two-column detail popup: every field on the left, current allocation info (holder + location, if allocated) and a scrollable maintenance history on the right — plus a **View Picture** button if a picture is attached.

<p align="center">
<img src="screenshots/058-equipment-detail-view-picture.png" alt="Screenshot: View Picture button expanded, showing the equipment photo" width="700">
</p>

<p align="center">
<img src="screenshots/059-equipment-detail-maintenance-history.png" alt="Screenshot: Maintenance History section inside the detail popup — open, in progress, and resolved records" width="900">
</p>

<p align="center">
<img src="screenshots/060-equipment-detail-no-maintenance.png" alt="Screenshot: Detail popup for an item with no maintenance history — 'No maintenance history recorded' state" width="900">
</p>

<p align="center">
<img src="screenshots/061-equipment-detail-view-logs-button.png" alt="Screenshot: View Logs button at the bottom of the detail popup's left column" width="700">
</p>

<p align="center">
<img src="screenshots/062-equipment-detail-clear-history-button.png" alt="Screenshot: Clear History button, active state next to View Logs" width="700">
</p>

<p align="center">
<img src="screenshots/063-equipment-detail-clear-history-confirmation.png" alt="Screenshot: Clear History confirmation prompt" width="700">
</p>

<p align="center">
<img src="screenshots/064-equipment-detail-clear-history-disabled.png" alt="Screenshot: Clear History button greyed out — no maintenance records exist" width="700">
</p>

- **View Logs** — closes the popup and opens the Logs screen pre-filtered to this item's equipment code.
- **Clear History** — permanently deletes every maintenance record for this item (with confirmation). Disabled if there are none. This is written to the audit trail as "Maintenance History Cleared," even though the records themselves are gone for good.

### Delete Equipment

<p align="center">
<img src="screenshots/065-equipment-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Equipment screen" width="700">
</p>

- Deleting an item is a **soft delete** — it's hidden from every list and dropdown in the app but not destroyed. It moves to the **Deleted Items** screen (Admin-only), where it can be restored or permanently erased. This action is **not** written to the audit log, and there is no check that blocks deleting an item that is still allocated.

### Import / Export

<p align="center">
<img src="screenshots/066-equipment-import-tooltip.png" alt="Screenshot: Import button hover tooltip reminding to pre-create categories in Settings" width="700">
</p>

<p align="center">
<img src="screenshots/067-equipment-import-file-picker.png" alt="Screenshot: Import CSV file picker" width="700">
</p>

<p align="center">
<img src="screenshots/068-equipment-export-csv-toast.png" alt="Screenshot: CSV export confirmation toast" width="500">
</p>

- **Import** — bulk-add equipment from a CSV file. Hovering the button shows a reminder to pre-create any needed categories in Settings → Categories first, so imported rows don't create orphaned free-text category values.
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

- **Add / Edit** — Full Name (required), Employee Code (auto-generated if left blank), Designation, Email (must be unique if provided), Phone, Is Active checkbox, and a profile picture (upload or remove — stored on the shared network folder when reachable, with an automatic local fallback otherwise, same as equipment and user pictures).

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

Manage the physical places equipment can be assigned to (offices, stores, buildings, rooms). Grid shows Location Name, Building, Room Number, Room Name. No search box and no pagination — the full list loads at once, so keep this list to a manageable number of meaningful places.

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
- Locations defined here also drive the linked Location/Room dropdowns on the Check In/Out screen's Check-Out tab (see [Check In / Check Out](#check-in--check-out)).

---

## Allocation

<p align="center">
<img src="screenshots/110-allocation-list-merged.png" alt="Screenshot: Allocation list view, merged: formal allocations + Check-In/Out loans + available equipment" width="900">
</p>

The formal, tracked workflow for assigning equipment to employees — with an approval step and full status history. The list merges three sources: active formal allocations, active Check-In/Out loans (labeled with a "Check-In/Out" source tag), and currently-available equipment. Status and Source values are shown in clean, human-readable text (e.g. "In Store", not "in_store").

### Filters

<p align="center">
<img src="screenshots/111-allocation-filters-button-menu.png" alt="Screenshot: Combined Filters button open, showing Status and Category submenus" width="500">
</p>

<p align="center">
<img src="screenshots/112-allocation-filters-active-label.png" alt="Screenshot: Filters button label updated to show active filters, e.g. 'Filters (Overdue, Laptops)'" width="500">
</p>

<p align="center">
<img src="screenshots/113-allocation-overdue-highlight.png" alt="Screenshot: Allocation row(s) shown with the computed Overdue flag/amber highlight" width="900">
</p>

<p align="center">
<img src="screenshots/114-allocation-checkinout-source-row-disabled-buttons.png" alt="Screenshot: A Check-In/Out sourced row selected, with Approve/Return/Delete/Edit Return buttons greyed out" width="900">
</p>

A single **Filters** button combines Status (All / In Store / Allocated / Pending / Overdue) and Category into one dropdown menu, with the button's own label updating to show what's currently active.

### Selecting, Sorting & Pagination

<p align="center">
<img src="screenshots/115-allocation-checkbox-select-header.png" alt="Screenshot: Gmail-style checkbox select header with attached All/None menu" width="700">
</p>

<p align="center">
<img src="screenshots/116-allocation-sortable-column-header.png" alt="Screenshot: Allocation grid column header clicked to sort, sort indicator shown" width="900">
</p>

<p align="center">
<img src="screenshots/117-allocation-pagination-controls.png" alt="Screenshot: Pagination controls — page size selector, Prev/Next, page indicator" width="700">
</p>

### New Allocation

<p align="center">
<img src="screenshots/118-allocation-new-dialog-empty.png" alt="Screenshot: New Allocation dialog, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/119-allocation-new-equipment-dropdown.png" alt="Screenshot: Equipment dropdown in the New Allocation dialog, showing only available items" width="700">
</p>

<p align="center">
<img src="screenshots/120-allocation-new-employee-dropdown.png" alt="Screenshot: Employee dropdown in the New Allocation dialog, showing only active employees" width="700">
</p>

<p align="center">
<img src="screenshots/121-allocation-new-filled-saved.png" alt="Screenshot: New Allocation dialog filled out and saved, toast confirmation" width="700">
</p>

- Search-and-select equipment (only items not already tied to an active transaction/approved/pending allocation, and not decommissioned or under repair — equipment with only historical returned allocations is offered normally).
- Choose an active employee and a location. Optionally set an expected return date.
- If Status is left blank, the allocation is auto-promoted straight to **Approved** rather than sitting in Pending.

### Approve

<p align="center">
<img src="screenshots/122-allocation-approve-action.png" alt="Screenshot: Approve button being used on a Pending allocation row" width="700">
</p>

<p align="center">
<img src="screenshots/123-allocation-approve-blocked-checkinout-conflict.png" alt="Screenshot: Approve blocked — equipment has an active Check-In/Out transaction" width="700">
</p>

- Moves a **pending** allocation to **approved**, marking the equipment `allocated`. Blocked if the equipment has an active Check-In/Out transaction pending elsewhere.
- ⚠️ The "approved by" field stored on the record is always a fixed system account, not the real approver — check the Logs screen's "Changed By" column instead.

### Return (from Allocation screen)

<p align="center">
<img src="screenshots/124-allocation-return-confirmation.png" alt="Screenshot: Return confirmation dialog on the Allocation screen" width="700">
</p>

- Marks the allocation as returned and the equipment status back to `in_store`. Note: this path does **not** record an actual return date on the allocation record — use the Returns screen (below) for a dated return.

### Edit Expected Return Date

<p align="center">
<img src="screenshots/125-allocation-edit-return-date-dialog.png" alt="Screenshot: Edit Expected Return Date dialog" width="700">
</p>

- Logged with a distinct "Expected Return Date Updated" entry, shown with a yellow badge in Logs.

### Delete Allocation

<p align="center">
<img src="screenshots/126-allocation-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Allocation screen" width="700">
</p>

<p align="center">
<img src="screenshots/127-allocation-delete-blocked-active.png" alt="Screenshot: Delete blocked error — allocation is currently Approved" width="700">
</p>

- Removes an allocation record entirely. Blocked while it's Approved (return or transfer first). **Now written to the audit trail** as "Allocation Request Deleted" (orange-red badge) — this is a change from earlier versions, where this action was silent.

### Allocation Detail Popup

<p align="center">
<img src="screenshots/128-allocation-detail-popup.png" alt="Screenshot: Allocation detail popup — photo, status badge, full field list, Allocated By" width="700">
</p>

<p align="center">
<img src="screenshots/129-allocation-detail-view-picture-logs-buttons.png" alt="Screenshot: View Picture and View Logs buttons stacked in the detail popup" width="700">
</p>

Includes **View Picture** (disabled if no picture attached) and **View Logs** (always active, jumps to the pre-filtered Logs screen) buttons.

---

## Returns

<p align="center">
<img src="screenshots/130-returns-list.png" alt="Screenshot: Returns history view — allocation returns + completed Check-In/Out, merged and sorted newest first" width="900">
</p>

A dedicated history view combining every returned allocation with every completed Check-In/Out transaction. Rows are flagged when the actual return date is later than the expected return date. A Timestamp column (Pakistan Standard Time) records exactly when each return was processed.

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

- Available only for allocation-sourced rows (not Check-In/Out sourced rows, which are guarded with a warning). Not written to the audit log.

### Undo Return

<p align="center">
<img src="screenshots/135-returns-undo-confirmation.png" alt="Screenshot: Undo Return confirmation dialog" width="700">
</p>

<p align="center">
<img src="screenshots/136-returns-undo-blocked-active-allocation.png" alt="Screenshot: Undo blocked — equipment already has a different active allocation" width="700">
</p>

- Reverts a processed return: the allocation goes back to Approved, its actual return date is cleared, and the equipment goes back to Allocated.
- Before undoing, AssetPilot checks whether the equipment already has a different active allocation (e.g. it was reassigned in the meantime) — if so, the undo is blocked with a warning naming the current holder.
- Now logged as its own distinct **"Returned to Active Allocation"** entry (orange badge) — no longer sharing the same audit label as a genuine return, so the Logs screen alone is enough to tell them apart.

### Return Detail Popup

<p align="center">
<img src="screenshots/137-returns-detail-popup.png" alt="Screenshot: Return Detail popup, double-clicked from a row — full metadata, Source, Overdue flag" width="700">
</p>

<p align="center">
<img src="screenshots/138-returns-overdue-flag.png" alt="Screenshot: A return row visually flagged as late/overdue" width="900">
</p>

Double-clicking any row opens a detail popup with the full return record, including its Source (Allocation or Check-In/Out) and whether it's flagged Overdue.

---

## Transfer

<p align="center">
<img src="screenshots/150-transfer-list.png" alt="Screenshot: Transfer list view — every currently Approved allocation" width="900">
</p>

Move equipment directly from one employee/location to another, without a separate return-then-reallocate step.

<p align="center">
<img src="screenshots/151-transfer-new-dialog-prefilled.png" alt="Screenshot: Transfer dialog opened by double-clicking a row, pre-filled with that allocation" width="700">
</p>

<p align="center">
<img src="screenshots/152-transfer-new-employee-picker.png" alt="Screenshot: New employee picker inside the Transfer dialog" width="700">
</p>

<p align="center">
<img src="screenshots/153-transfer-new-location-picker.png" alt="Screenshot: New location picker inside the Transfer dialog" width="700">
</p>

<p align="center">
<img src="screenshots/154-transfer-confirmation-toast.png" alt="Screenshot: Toast confirmation after a successful transfer" width="500">
</p>

- **New Transfer** — search-and-select the equipment, pick the new employee and/or new location.
- Double-clicking a row in the Transfer grid pre-selects that item and jumps straight into the transfer dialog.
- The equipment's status stays `allocated` throughout — only the assigned employee/location changes. Internally, the old allocation is closed out as **Returned** and a new **Approved** allocation is created, linked back to the one it replaced.
- ⚠️ Both the "allocated by" and "approved by" fields on the new allocation are always the same fixed system account, never the person who actually performed the transfer — check the Logs screen's "Changed By" column instead.

<p align="center">
<img src="screenshots/155-transfer-appears-in-returns-history.png" alt="Screenshot: The closed-out old allocation from a transfer, showing up in Returns history" width="900">
</p>

---

## Decommission

*(Admin only)*

<p align="center">
<img src="screenshots/160-decommission-list.png" alt="Screenshot: Decommission list view, showing decommissioned equipment with reason/date columns" width="900">
</p>

The permanent-retirement workflow for equipment that's reached end-of-life — now reversible.

<p align="center">
<img src="screenshots/161-decommission-dialog-empty.png" alt="Screenshot: Decommission dialog, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/162-decommission-equipment-dropdown-all-statuses.png" alt="Screenshot: Decommission dialog equipment dropdown, now including an Allocated item alongside In Store / Under Repair" width="700">
</p>

<p align="center">
<img src="screenshots/163-decommission-reason-dropdown.png" alt="Screenshot: Decommission reason dropdown — Damaged, Obsolete, Beyond Repair, End of Life, Lost, Other" width="700">
</p>

<p align="center">
<img src="screenshots/164-decommission-notes-field.png" alt="Screenshot: Optional notes field on the Decommission dialog, filled in" width="700">
</p>

<p align="center">
<img src="screenshots/165-decommission-confirmation-toast.png" alt="Screenshot: Toast confirmation after decommissioning an item" width="500">
</p>

- The dialog now offers **every non-decommissioned item, including equipment that is currently Allocated** — there is no automatic check that first returns or unassigns an allocated item, so confirm status yourself before decommissioning if that matters to your process.
- Choose a reason: Damaged, Obsolete, Beyond Repair, End of Life, Lost, or Other — with an optional notes field.
- Confirming sets the item's status to `decommissioned` and records the reason in the audit trail.
- The Decommission grid's Reason and Date columns are sourced from that item's audit log entry — clearing logs from Settings will blank these for existing entries.

### Restore

<p align="center">
<img src="screenshots/166-decommission-restore-button.png" alt="Screenshot: Restore button in the Decommission toolbar" width="700">
</p>

<p align="center">
<img src="screenshots/167-decommission-restore-confirmation.png" alt="Screenshot: Restore confirmation prompt — 'set back to In Store?'" width="700">
</p>

<p align="center">
<img src="screenshots/168-decommission-restore-logs-entry.png" alt="Screenshot: 'Restored from Decommission' teal-badged entry in the Logs screen" width="900">
</p>

- **Restore** sets a decommissioned item's status directly back to In Store, after a confirmation prompt, and writes a **"Restored from Decommission"** entry to the audit trail (teal badge). This is a genuine capability change: earlier versions had no built-in way back from Decommissioned other than manually editing the equipment record.

---

## Deleted Items

*(Admin only)*

<p align="center">
<img src="screenshots/180-deleted-items-list.png" alt="Screenshot: Deleted Items list view — soft-deleted equipment, most recent first" width="900">
</p>

A recycle bin for equipment removed via the Equipment screen's Delete button. Nothing is permanently lost until explicitly confirmed here.

<p align="center">
<img src="screenshots/181-deleted-items-checkbox-select-header.png" alt="Screenshot: Gmail-style checkbox select header with attached All/None menu" width="700">
</p>

<p align="center">
<img src="screenshots/182-deleted-items-pagination.png" alt="Screenshot: Pagination controls — page size selector, Prev/Next, page indicator" width="700">
</p>

<p align="center">
<img src="screenshots/183-deleted-items-restore-confirmation.png" alt="Screenshot: Restore confirmation dialog / toast" width="700">
</p>

<p align="center">
<img src="screenshots/184-deleted-items-permanent-delete-confirmation.png" alt="Screenshot: Permanent Delete confirmation dialog, with irreversible-action warning copy" width="700">
</p>

<p align="center">
<img src="screenshots/185-deleted-items-permanent-delete-blocked.png" alt="Screenshot: Permanent Delete failing because related records still exist elsewhere" width="700">
</p>

<p align="center">
<img src="screenshots/186-deleted-items-doubleclick-quick-dialog.png" alt="Screenshot: Small inline Restore / Delete / Cancel dialog from double-clicking a row" width="500">
</p>

- **Restore** — brings item(s) back to the active Equipment list exactly as they were. Not written to the audit log.
- **Permanent Delete** — erases the selected item(s) from the database entirely. Cannot be undone. Not written to the audit log. Will fail outright if the item still has related records (allocations, transactions, logs, maintenance records) elsewhere.
- A checkbox-selection header (with an attached All/None menu) supports bulk operations, the same pattern used on the Allocation screen.
- The grid is fully paginated (25 / 50 / 100 / 250 / 500 / 1000 rows per page).
- **Double-clicking a row** opens a small Restore / Delete / Cancel dialog for that single item.

---

## Logs (Audit Trail)

<p align="center">
<img src="screenshots/190-logs-list.png" alt="Screenshot: Logs / Audit Trail list view, color-coded by action type" width="900">
</p>

A searchable, sortable, chronological record of key actions taken across the app.

<p align="center">
<img src="screenshots/191-logs-search.png" alt="Screenshot: Logs search box with fuzzy-matched results" width="700">
</p>

<p align="center">
<img src="screenshots/192-logs-action-color-legend.png" alt="Screenshot: Logs grid showing several differently-colored action types together (green/blue/yellow/red/purple/teal/orange)" width="900">
</p>

<p align="center">
<img src="screenshots/193-logs-click-to-sort-column.png" alt="Screenshot: Logs grid column header clicked to sort, sort indicator and page reset to 1" width="900">
</p>

- Columns: Equipment Code, QR Code, Action (color-coded by type), Changed By, Previous Value, New Value, Timestamp.
- **"Changed By" now correctly reflects the real signed-in user** for every entry written to the audit trail, rather than a single fixed account as in earlier versions. (A small number of record-level fields elsewhere in the app — an allocation's own "approved by"/"allocated by", an equipment item's own "created by" — still store a fixed system account; trust this column over those fields.)
- Actions logged include: equipment created/updated, maintenance opened/resolved, maintenance history cleared, allocation requested/approved/returned/deleted, undo return, expected return date updated, transfers, decommission approved, restored from decommission, employee and location created/updated/deleted, and checked in/checked out.
- **Not logged:** equipment restore/permanent delete from Deleted Items, editing a return record, everyday user-management actions in Settings (add/edit/toggle/delete user), password changes, category add/delete, saving general Settings, and overdue Check-In/Out status updates. (User deletion and password changes are, however, captured in the separate Security Audit Log — see below.)
- ✅ Undoing a Return now logs its own distinct action ("Returned to Active Allocation") rather than sharing a label with a genuine return.
- Clearing all logs or resetting application data re-inserts a single "⚠ Settings Data Deleted" marker entry, highlighted red across the whole row.

### Security Audit Log (file-based)

<p align="center">
<img src="screenshots/194-logs-security-audit-file-example.png" alt="Screenshot: Text editor showing an excerpt of the security_audit.log file" width="900">
</p>

A second, append-only log — separate from the database — is written to `%AppData%\AssetPilot\security_audit.log`. It specifically covers: Clear All Logs, Reset Application Data, Delete User, Change Own Password, and Admin Changes Another User's Password. It survives both Clear All Logs and Reset Application Data, since there is no in-app way to delete it.

### Log Detail Popup

<p align="center">
<img src="screenshots/195-logs-detail-popup.png" alt="Screenshot: Log detail popup — Before (red) panel and After (green) panel, bulleted field changes" width="700">
</p>

<p align="center">
<img src="screenshots/196-logs-detail-popup-dark-mode.png" alt="Screenshot: Log detail popup rendered in dark mode" width="700">
</p>

### Export

<p align="center">
<img src="screenshots/197-logs-export-csv-toast.png" alt="Screenshot: Logs CSV export confirmation toast" width="500">
</p>

---

## QR Scanner

<p align="center">
<img src="screenshots/200-qrscanner-main-empty.png" alt="Screenshot: QR Scanner main screen, blank/ready state with code field auto-focused" width="900">
</p>

An equipment lookup and inspection hub, designed for use with a USB barcode/QR scanner (keyboard-wedge mode, no drivers needed) or manual typing. It reads equipment and log data and navigates to other screens, but does not itself change any equipment or allocation record.

<p align="center">
<img src="screenshots/201-qrscanner-typing-suggestions.png" alt="Screenshot: Fuzzy suggestion dropdown appearing while typing a partial code" width="700">
</p>

<p align="center">
<img src="screenshots/202-qrscanner-match-result-hero-photo.png" alt="Screenshot: QR Scanner match result — hero photo panel plus detail card with status badge" width="900">
</p>

<p align="center">
<img src="screenshots/203-qrscanner-match-current-holder.png" alt="Screenshot: Match result showing current holder + location for an allocated item" width="900">
</p>

<p align="center">
<img src="screenshots/204-qrscanner-warranty-expired.png" alt="Screenshot: Warranty Expiry shown in red with '(Expired)' label" width="700">
</p>

<p align="center">
<img src="screenshots/205-qrscanner-warranty-expiring-soon.png" alt="Screenshot: Warranty Expiry shown in amber with '(Expires in Nd)' label" width="700">
</p>

<p align="center">
<img src="screenshots/206-qrscanner-history-grid.png" alt="Screenshot: Full unpaginated change-history grid beneath a match result" width="900">
</p>

<p align="center">
<img src="screenshots/207-qrscanner-no-match-did-you-mean.png" alt="Screenshot: No exact match — 'Did you mean:' chip suggestions shown" width="700">
</p>

- Scan or type a QR code, Equipment Code, or Serial Number — the app auto-searches ~300ms after you stop typing, or immediately on Enter.
- Matching is **exact**, not fuzzy — a partial code returns nothing. If nothing matches, up to 3 "Did you mean?" chip suggestions are offered based on a fuzzy match against codes and names.
- On match: a hero photo panel, a full equipment detail card (status, current holder, current location), a live Warranty Expiry status indicator (Expired / Expiring soon / Valid), and a complete, unpaginated change-history timeline for that item.

### Recent Scans & Bulk Scan

<p align="center">
<img src="screenshots/208-qrscanner-recent-scans-strip.png" alt="Screenshot: Recent Scans chip strip below the search bar, last 8 items" width="900">
</p>

<p align="center">
<img src="screenshots/209-qrscanner-bulk-scan-checkbox.png" alt="Screenshot: Bulk Scan checkbox enabled, running count toast after a scan" width="700">
</p>

<p align="center">
<img src="screenshots/210-qrscanner-export-bulk-button.png" alt="Screenshot: Export Bulk button, visible once Bulk Scan mode is on" width="700">
</p>

- **Recent Scans** — a strip of clickable chips remembering your last 8 distinct successful scans.
- **Bulk Scan** — toggle on to build a running session list of everything scanned, with a toast confirming the count after each scan; **Export Bulk** saves the list (Equipment Code, Equipment Name, Scanned At) to CSV. Turning bulk mode off clears the session list.

### Quick-Action Buttons

<p align="center">
<img src="screenshots/211-qrscanner-view-qr-button.png" alt="Screenshot: View QR button opening the QR code image dialog" width="700">
</p>

<p align="center">
<img src="screenshots/212-qrscanner-view-qr-print-label-stub.png" alt="Screenshot: Print Label button showing the 'not wired up yet' notice" width="700">
</p>

<p align="center">
<img src="screenshots/213-qrscanner-view-picture-button.png" alt="Screenshot: View Picture button, full-screen zoomed photo" width="700">
</p>

<p align="center">
<img src="screenshots/214-qrscanner-checkinout-nav-button.png" alt="Screenshot: Check-In/Out navigation button, jumping to that screen with the item pre-loaded" width="700">
</p>

<p align="center">
<img src="screenshots/215-qrscanner-view-logs-button.png" alt="Screenshot: View Logs button, jumping to the pre-filtered Logs screen" width="700">
</p>

<p align="center">
<img src="screenshots/216-qrscanner-export-history-button.png" alt="Screenshot: Export History button, saving the item's log grid to CSV" width="700">
</p>

- **View QR** — shows the item's QR code as a scannable image with a Print Label button (label printing isn't wired up yet in this release — it shows a notice rather than printing).
- **View Picture** — full-screen photo with zoom; relabeled "No Picture" and disabled if none is attached.
- **Check-In/Out** — jumps to the Check In/Out screen with this item pre-searched.
- **View Logs** — jumps to the Logs screen pre-filtered to this item.
- **Export History** — saves this item's log grid to CSV.

<p align="center">
<img src="screenshots/217-qrscanner-history-doubleclick-log-popup.png" alt="Screenshot: Double-clicking a history row opens the Log Detail popup" width="700">
</p>

- Double-clicking any row in the history grid opens the same Log Detail popup used on the Logs screen.
- Because the QR Scanner performs no writes to equipment or allocation data itself, it remains safe to hand to any staff member — including those without login access to other modules — purely for identifying an item and checking its status.

---

## Check In / Check Out

<p align="center">
<img src="screenshots/230-checkinout-main-empty.png" alt="Screenshot: Check In/Out main screen, blank/ready state" width="900">
</p>

A lightweight, no-approval loan tracker for short-term, informal borrows — kept in a separate `transactions` table from the formal Allocation workflow (e.g. borrowing a projector for a meeting).

<p align="center">
<img src="screenshots/231-checkinout-autotab-checkout.png" alt="Screenshot: Screen auto-opening the Check-Out tab for an available item" width="900">
</p>

<p align="center">
<img src="screenshots/232-checkinout-autotab-checkin.png" alt="Screenshot: Screen auto-opening the Check-In tab for a currently checked-out item" width="900">
</p>

### Check-Out

<p align="center">
<img src="screenshots/233-checkinout-checkout-form-empty.png" alt="Screenshot: Check-Out form, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/234-checkinout-checkout-location-dropdown.png" alt="Screenshot: Location dropdown on the Check-Out tab" width="700">
</p>

<p align="center">
<img src="screenshots/235-checkinout-checkout-room-dropdown-cascaded.png" alt="Screenshot: Room dropdown cascading from the selected Location" width="700">
</p>

<p align="center">
<img src="screenshots/236-checkinout-checkout-department-autofilled.png" alt="Screenshot: Department field auto-filled from the Location + Room selection" width="700">
</p>

<p align="center">
<img src="screenshots/237-checkinout-checkout-location-required-block.png" alt="Screenshot: Save blocked — Location must be selected before confirming a Check-Out" width="700">
</p>

<p align="center">
<img src="screenshots/238-checkinout-checkout-form-filled.png" alt="Screenshot: Check-Out form filled — issued to, department, operator, expected return, condition, remarks" width="700">
</p>

<p align="center">
<img src="screenshots/239-checkinout-checkout-blocked-formal-allocation.png" alt="Screenshot: Check-Out blocked — item already has a pending/approved formal allocation" width="700">
</p>

- Linked **Location** and **Room** dropdowns: Location lists every location defined in the Locations module; Room cascades from the chosen Location (disabled if it has no rooms). Department is auto-filled from the combination — you never type it yourself.
- A Location must be selected before Check-Out can be confirmed — this is enforced and blocks the save if left blank.
- Also records who it was issued to, the operator processing it (pre-filled with your own name), expected return date, condition beforehand, and remarks. Marks the equipment `allocated` and the transaction `Pending`.
- Blocked if the item already has a pending/approved formal allocation.

### Check-In

<p align="center">
<img src="screenshots/240-checkinout-checkin-form-empty.png" alt="Screenshot: Check-In form, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/241-checkinout-checkin-form-filled.png" alt="Screenshot: Check-In form filled — condition after, remarks" width="700">
</p>

- Records the actual return date, marks the transaction `Completed`, and records condition-after plus remarks. Marks the equipment `in_store`. Note: this updates the transaction's own condition record, **not** the equipment's main Condition Grade field.

### Overdue Transactions

<p align="center">
<img src="screenshots/242-checkinout-overdue-flag.png" alt="Screenshot: A pending transaction shown flagged as Overdue" width="900">
</p>

### Transaction History Panel & Full History Screen

<p align="center">
<img src="screenshots/243-checkinout-history-panel-last10.png" alt="Screenshot: Last-10-transactions history panel for the currently loaded item" width="900">
</p>

<p align="center">
<img src="screenshots/244-checkinout-history-button-toolbar.png" alt="Screenshot: History button in the Check In/Out toolbar" width="700">
</p>

<p align="center">
<img src="screenshots/245-checkinout-history-fullscreen-panel.png" alt="Screenshot: Full-screen Check-In/Out History panel, all equipment" width="900">
</p>

<p align="center">
<img src="screenshots/246-checkinout-history-search.png" alt="Screenshot: Real-time search on the History panel" width="700">
</p>

<p align="center">
<img src="screenshots/247-checkinout-history-pagination.png" alt="Screenshot: Page size selector and Prev/Next controls on the History panel" width="700">
</p>

<p align="center">
<img src="screenshots/248-checkinout-history-export-csv.png" alt="Screenshot: Export button on the History panel, saving current page to CSV" width="700">
</p>

- The last ten transactions for the currently loaded item appear on the scan screen itself (display-only).
- The **History** button (🕘) opens a dedicated, full-screen, searchable, paginated, exportable transaction history covering every piece of equipment — each transaction expands into up to two rows (a blue Check-Out entry and, once returned, a green Check-In entry). Click **Back** to return to the scan view.

---

## Reports

<p align="center">
<img src="screenshots/250-reports-main-screen.png" alt="Screenshot: Reports screen, report type selector" width="900">
</p>

Four report types, each with a live on-screen preview before exporting. All reports are read-only — nothing here writes to the database. Your organization's name (Settings → General) appears at the top of every export.

| Report | Shows |
|---|---|
| **Equipment Inventory** | Full equipment list with status and current location; filterable by category |
| **Employee Allocation Summary** | Every active employee with items currently held and total ever allocated (employees with zero allocations are omitted) |
| **Active Allocations** | All equipment currently allocated or pending approval |
| **Returns History** | All formal allocation returns with the duration each item was held, in days, flagged if over 30 days (Check-In/Out completions are **not** included in this report) |

<p align="center">
<img src="screenshots/251-reports-equipment-inventory-preview.png" alt="Screenshot: Equipment Inventory report preview" width="900">
</p>

<p align="center">
<img src="screenshots/252-reports-equipment-inventory-category-filter.png" alt="Screenshot: Category filter applied to the Equipment Inventory report" width="700">
</p>

<p align="center">
<img src="screenshots/253-reports-employee-summary-preview.png" alt="Screenshot: Employee Allocation Summary report preview" width="900">
</p>

<p align="center">
<img src="screenshots/254-reports-active-allocations-preview.png" alt="Screenshot: Active Allocations report preview" width="900">
</p>

<p align="center">
<img src="screenshots/255-reports-returns-history-preview.png" alt="Screenshot: Returns History report preview, with a >30-day duration flagged" width="900">
</p>

### Export

<p align="center">
<img src="screenshots/256-reports-export-pdf-print-preview.png" alt="Screenshot: PDF/print preview generated from a report, multi-page" width="900">
</p>

<p align="center">
<img src="screenshots/257-reports-export-pdf-printer-selection.png" alt="Screenshot: Printer selection dialog, choosing a PDF printer driver to save as PDF" width="700">
</p>

<p align="center">
<img src="screenshots/258-reports-export-excel-result.png" alt="Screenshot: Excel export result — company-name header row, bold green column headers" width="900">
</p>

- **Export PDF** — generates a formatted print preview you can print directly or save via a PDF printer driver (e.g. "Microsoft Print to PDF"). There is no direct "Save as PDF" button.
- **Export Excel** — styled spreadsheet export with a company-name header row, bold green column headers, and auto-sized columns.

---

## Settings

*(Admin only — this entire module is hidden from the sidebar for Operator accounts)*

<p align="center">
<img src="screenshots/260-settings-overview-tabs.png" alt="Screenshot: Settings screen overview, showing all left-sidebar tabs" width="900">
</p>

Settings has seven tabs down its own left sidebar: General, Backup / Restore, Users, Change Password, Printing, Categories, and System Configuration.

### General

<p align="center">
<img src="screenshots/261-settings-general-tab.png" alt="Screenshot: Settings — General tab, Company Name field and System info card" width="900">
</p>

<p align="center">
<img src="screenshots/262-settings-general-save-disabled.png" alt="Screenshot: Save button disabled state before the Company Name field is changed" width="700">
</p>

<p align="center">
<img src="screenshots/263-settings-general-save-confirmed.png" alt="Screenshot: Inline 'Saved' confirmation after saving General settings" width="700">
</p>

<p align="center">
<img src="screenshots/264-settings-general-system-info-card.png" alt="Screenshot: System info card — app version, last backup date, connected database name/server" width="700">
</p>

- Company Name (shown across reports and printed labels). Save button stays disabled until the field actually changes.
- System info card: installed app version, date of last database backup, and currently connected database (name + server).

#### Danger Zone

<p align="center">
<img src="screenshots/265-settings-danger-zone-panel.png" alt="Screenshot: Danger Zone panel — Clear all logs and Reset application data buttons" width="700">
</p>

<p align="center">
<img src="screenshots/266-settings-danger-zone-clear-logs-confirmation.png" alt="Screenshot: Clear all logs confirmation prompt" width="700">
</p>

<p align="center">
<img src="screenshots/267-settings-danger-zone-reset-type-to-confirm.png" alt="Screenshot: Reset Application Data — 'type RESET to confirm' input field" width="700">
</p>

<p align="center">
<img src="screenshots/268-settings-danger-zone-reset-confirmed.png" alt="Screenshot: Reset Application Data confirmation dialog, confirm button now enabled" width="700">
</p>

<p align="center">
<img src="screenshots/269-settings-danger-zone-settings-data-deleted-marker.png" alt="Screenshot: '⚠ Settings Data Deleted' red-highlighted marker row in the Logs grid, written after a clear/reset" width="900">
</p>

- **Clear all logs** — permanently wipes the audit trail (no undo). A single "⚠ Settings Data Deleted" marker entry is re-inserted afterward (full row highlighted red in Logs), and the action is separately recorded in the Security Audit Log. Also blanks the reason/date shown on already-decommissioned items, since those are sourced from the log.
- **Reset application data** — requires typing "RESET" to enable the confirm button. Deletes, in foreign-key-safe order and in separate save steps: Equipment Logs, Equipment Location Histories, Maintenance Records, Transactions (Check In/Out), Allocations, Equipment, Employees, and Locations. Users, roles, categories, and general settings are unaffected. **No automatic backup is taken first** — take a manual backup before running this. Because each table is deleted in its own step, an interruption partway through can leave some tables cleared and others intact.

### Backup / Restore

<p align="center">
<img src="screenshots/270-settings-backup-restore-tab.png" alt="Screenshot: Settings — Backup/Restore tab overview" width="900">
</p>

<p align="center">
<img src="screenshots/271-settings-backup-credentials-prompt.png" alt="Screenshot: Backup — confirm database host/name/username/password prompt" width="700">
</p>

<p align="center">
<img src="screenshots/272-settings-backup-file-save-dialog.png" alt="Screenshot: Backup — choose save location file dialog" width="700">
</p>

<p align="center">
<img src="screenshots/273-settings-backup-success-toast.png" alt="Screenshot: Backup success toast notification" width="500">
</p>

<p align="center">
<img src="screenshots/274-settings-restore-file-picker.png" alt="Screenshot: Restore — choose backup file to restore from" width="700">
</p>

<p align="center">
<img src="screenshots/275-settings-restore-destructive-confirmation.png" alt="Screenshot: Restore confirmation dialog, warning that current data will be overwritten" width="700">
</p>

<p align="center">
<img src="screenshots/276-settings-restore-success-restart-notice.png" alt="Screenshot: Restore success message recommending an app restart" width="700">
</p>

- **Backup** — exports the full database to a file via the located MySQL backup tool. Timestamp is saved and shown on the General tab.
- **Restore** — imports a previously saved backup file, **overwriting current data entirely**. Restart the app afterward.

### Users

<p align="center">
<img src="screenshots/277-settings-users-list.png" alt="Screenshot: Settings — Users list, active/inactive status dots" width="900">
</p>

<p align="center">
<img src="screenshots/278-settings-users-add-form-empty.png" alt="Screenshot: Add User form, blank state" width="900">
</p>

<p align="center">
<img src="screenshots/279-settings-users-add-form-filled.png" alt="Screenshot: Add User form filled — username, full name, email, password, role, picture" width="900">
</p>

<p align="center">
<img src="screenshots/280-settings-users-edit-form-username-locked.png" alt="Screenshot: Edit User form — Username field shown as locked/non-editable" width="900">
</p>

<p align="center">
<img src="screenshots/281-settings-users-role-dropdown.png" alt="Screenshot: Role dropdown — Admin / Operator" width="500">
</p>

<p align="center">
<img src="screenshots/282-settings-users-profile-picture-upload.png" alt="Screenshot: Profile picture upload control on the User form" width="700">
</p>

<p align="center">
<img src="screenshots/283-settings-users-detail-popup.png" alt="Screenshot: User detail popup — avatar, username, full name, email, role, status" width="700">
</p>

<p align="center">
<img src="screenshots/284-settings-users-toggle-active.png" alt="Screenshot: Toggle Active/Inactive action on a user row" width="700">
</p>

<p align="center">
<img src="screenshots/285-settings-users-toggle-blocked-self.png" alt="Screenshot: Error — cannot deactivate the account you are currently logged in as" width="700">
</p>

<p align="center">
<img src="screenshots/286-settings-users-delete-confirmation.png" alt="Screenshot: Delete User confirmation dialog" width="700">
</p>

<p align="center">
<img src="screenshots/287-settings-users-delete-blocked-self.png" alt="Screenshot: Error — cannot delete the account you are currently logged in as" width="700">
</p>

- **Add / Edit User** — Username (required, unique, locked after creation), Full Name, Email (unique if provided), New/Confirm Password (required on create, leave blank on edit to keep unchanged), Role (Admin/Operator), Profile Picture (JPG/PNG/BMP/GIF), Is Active Account checkbox.
- **User Detail popup** — double-clicking a row opens a read-only card.
- **Toggle Active** — you cannot deactivate your own account.
- **Delete User** — permanent; you cannot delete your own account; fails if the account is still referenced elsewhere.
- ⚠️ None of the actions on this tab are written to the on-screen audit trail — but Delete User and any password change (your own or another user's) **are** recorded in the separate Security Audit Log (see [Logs](#logs-audit-trail)).

### Change Password

<p align="center">
<img src="screenshots/288-settings-change-password-tab.png" alt="Screenshot: Settings — Change Password tab, three password fields" width="900">
</p>

<p align="center">
<img src="screenshots/289-settings-change-password-strength-weak.png" alt="Screenshot: Live strength meter showing Weak" width="700">
</p>

<p align="center">
<img src="screenshots/290-settings-change-password-strength-strong.png" alt="Screenshot: Live strength meter showing Strong" width="700">
</p>

<p align="center">
<img src="screenshots/291-settings-change-password-mismatch-error.png" alt="Screenshot: Error — new password and confirmation don't match" width="700">
</p>

<p align="center">
<img src="screenshots/292-settings-change-password-wrong-current.png" alt="Screenshot: Error — current password is incorrect" width="700">
</p>

<p align="center">
<img src="screenshots/293-settings-change-password-security-tips-panel.png" alt="Screenshot: Security Tips panel alongside the form" width="700">
</p>

- Requires current password, a new password (**only enforced rule: 4+ characters**), and confirmation. The Weak/Medium/Strong meter is advisory only and does not block a weak password from being saved.

### Printing

<p align="center">
<img src="screenshots/294-settings-printing-tab.png" alt="Screenshot: Settings — Printing tab, printer name and label dimensions" width="900">
</p>

<p align="center">
<img src="screenshots/295-settings-printing-test-print-preview.png" alt="Screenshot: Test Print preview at configured label size" width="700">
</p>

- Configure printer name (blank = system default) and label dimensions (default 62mm × 29mm).
- **Test Print** — prints a sample QR label at the configured size.

### Categories

<p align="center">
<img src="screenshots/296-settings-categories-tab.png" alt="Screenshot: Settings — Categories tab, numbered list of saved categories" width="700">
</p>

<p align="center">
<img src="screenshots/297-settings-categories-add-new.png" alt="Screenshot: Typing a new category name and pressing Enter/Add" width="700">
</p>

<p align="center">
<img src="screenshots/298-settings-categories-delete-confirmation.png" alt="Screenshot: Delete confirmation for a category" width="700">
</p>

- **Add / Delete** categories used throughout the Equipment and Reports screens. Deleting a category does **not** retroactively update equipment records that already used it — those records keep the old category text, which will keep appearing as an orphaned filter option.

### System Configuration

*(Admin only — the single most privileged screen in the app)*

<p align="center">
<img src="screenshots/299-settings-system-config-tab.png" alt="Screenshot: Settings — System Configuration tab, database connection fields" width="900">
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
allocated     -->  decommissioned  (Decommission — no return required first)
decommissioned -->  in_store       (Restore, from the Decommission screen)
```

| Status | Meaning |
|---|---|
| In Store | Available, not currently assigned |
| Allocated | Currently assigned to an employee |
| Under Repair | Flagged for maintenance |
| Decommissioned | Permanently retired, unless explicitly Restored (see [Decommission](#decommission)) |

---

## Cross-Module Effects Reference

A quick reference for what each action also touches elsewhere, and whether it's audited — useful when investigating an unexpected change. A dagger (†) means the action is additionally recorded in the file-based Security Audit Log.

| Action | Also affects | Audited? |
|---|---|:---:|
| Add / Edit Equipment | Equipment, Logs, optionally Procurement Types | ✅ |
| Soft Delete / Restore / Permanent Delete Equipment | Equipment | ❌ |
| Clear Maintenance History | Maintenance Records, Logs | ✅ |
| Add / Edit / Delete Employee | Employees, Logs | ✅ |
| Add / Edit / Delete Location | Locations, Logs | ✅ |
| Create / Approve / Return Allocation | Allocations, Equipment, Logs | ✅ |
| Delete Allocation (Pending/Available) | Allocations (row removed), Logs | ✅ |
| Delete Allocation (Approved) | Blocked | — |
| Edit Expected Return Date | Allocations, Logs (free-text entry) | ✅ |
| Process Return / Undo Return | Allocations, Equipment, Logs (each with its own distinct label) | ✅ |
| Edit Return record | Allocations, Equipment | ❌ |
| Transfer | Allocations (2 rows), Logs | ✅ |
| Decommission | Equipment, Logs | ✅ |
| Restore from Decommission | Equipment, Logs | ✅ |
| Check-Out / Check-In | Transactions, Equipment, Logs | ✅ |
| Add / Toggle Active User | Users | ❌ |
| Delete User | Users | ❌ Logs; † Security Audit Log |
| Change Password (own or another user's) | Users | ❌ Logs; † Security Audit Log |
| Add / Delete Category | Categories | ❌ |
| Save Settings / Save DB Connection | App Settings / local connection file | ❌ |
| Clear All Logs | Logs (all removed, marker re-inserted) | ✅; † Security Audit Log |
| Reset Application Data | Logs, Location Histories, Maintenance Records, Transactions, Allocations, Equipment, Employees, Locations | ✅; † Security Audit Log |

---

## Installation & Setup

1. Install **.NET 8 SDK/Runtime** and **Visual Studio 2022** (or newer) with the .NET desktop development workload.
2. Clone or copy the project folder.
3. Open the `.sln` file in Visual Studio.
4. Restore NuGet packages (Visual Studio does this automatically on first build).
5. Press **F5** to build and run.

---

## Database Setup

1. Set up a MariaDB (or MySQL-compatible) server, reachable from the machine(s) running the app — or run entirely on `localhost` (e.g. XAMPP) for a single-machine deployment.
2. Import the provided schema (`inventory_management.sql`) into a new database.
3. Update the connection string in `Models/InventoryDbContext.cs` to point to your server, database name, and credentials — or configure it from inside the app via **Settings → System Configuration** once you have at least one Admin account.
4. Seed the `roles` table with `Admin` and `Operator` entries, then create at least one Admin user via the `users` / `user_roles` tables (or the app's Add User screen once one Admin account exists).
5. Equipment, User, and Employee pictures are read from and written to a shared network folder (`\\<server-ip>\AssetPilotData`, in subfolders `equipment_pictures`, `user_pictures`, and `employee_pictures`) when reachable, so every PC running the app sees the same pictures. If the share can't be reached — for example, a local/XAMPP deployment — the app automatically falls back to a local folder under `%AppData%\AssetPilot\` on that machine instead.

---

## Known Limitations & Data-Safety Notes

- **Passwords are stored and compared as plain text** — not hashed. Recommended to add hashing before any real production deployment. Treat direct database access to the `users` table as equivalent to having every account's password.
- **The System Configuration database password is encrypted in a way tied to a specific Windows user account + machine** — moving the settings file elsewhere requires reconfiguring from scratch.
- **The database password used for backups is passed as a plain command-line argument** to the backup tool, and could theoretically be visible in the process list on a shared/monitored machine at the exact moment a backup runs.
- **Record-level "by" fields can be misleading.** An equipment item's own "created by" field, and an allocation's own "approved by" / "allocated by" fields, always store a fixed system account rather than the real acting user — even though the corresponding entry in the Logs screen (and, for the most sensitive admin actions, the Security Audit Log) is correctly attributed. Always trust the Logs screen's "Changed By" column over a record's own "by" field.
- **A Transfer causes the old allocation to also appear in Returns history**, since internally it's closed out the same way a real return is.
- **Deleting a Category doesn't update equipment records that already used it** — the old category text remains on those records and keeps appearing as an orphaned filter option.
- **"Reset application data" in Settings is not wrapped in a single transaction** — each affected table is deleted in its own step, so an interruption partway through can leave some tables cleared and others intact. It also does **not** take an automatic backup first.
- **Equipment soft-delete has no check for active allocations** — it's possible to delete an item that is still allocated to someone.
- **The Decommission dialog now allows Allocated equipment to be decommissioned directly** — there is no automatic check that first returns or unassigns the item.
- **The Returns History report only includes formal allocation returns** — completed Check-In/Out transactions do not appear there, even though they appear correctly on the Returns screen itself.
- **QR label printing from the QR Scanner's View QR dialog is not yet wired up** — the Print Label button currently shows a notice rather than printing.
- Several grids (Locations, QR Scanner history) load their full dataset rather than paging server-side.

---

## Credits

Developed by **Syed Shayan Haider Kazmi**.

---

## License

This project is for internal organizational use.
All rights reserved © 2026.
