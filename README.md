#                AssetPilot Inventory Management System

<p align="center">
<img src="screenshots/000-app-overview.png" alt="Screenshot: Dashboard overview" width="900">
</p>

<p align="center">
<em>A desktop application for tracking physical equipment — from purchase to allocation, transfer, repair, and retirement — built with C# .NET 8 (WinForms), Entity Framework Core, and MariaDB / MySQL.</em>
</p>

> 📸 **About the screenshots in this README:** every image below points to a file under `screenshots/` that you drop in yourself, using the exact filename shown in the image tag. Placeholders are numbered in blocks per section so that **every distinct piece of functionality has its own dedicated image slot** — list views, every dialog, every popup, every button state, every filter, and every empty/error/confirmation state. Numbers `000–299` are the original slots; numbers `300+` are the slots added for newer features (Forgot Password, Reports overhaul, Recovery PINs, pagination, camera scanning, and so on). A full ordered list with tick-boxes is in [`screenshots/SCREENSHOT_CHECKLIST.md`](screenshots/SCREENSHOT_CHECKLIST.md) — a missing number there means a missing screenshot.

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
- [Cross-Cutting Behaviour](#cross-cutting-behaviour)
- [Equipment Status Lifecycle](#equipment-status-lifecycle)
- [Audit Action Reference](#audit-action-reference)
- [Cross-Module Effects Reference](#cross-module-effects-reference)
- [Data Model Reference](#data-model-reference)
- [Files & Folders Used by the App](#files--folders-used-by-the-app)
- [Installation & Setup](#installation--setup)
- [Database Setup](#database-setup)
- [Known Limitations & Data-Safety Notes](#known-limitations--data-safety-notes)
- [Release History](#release-history)
- [Credits](#credits)
- [License](#license)

---

## What This App Does

AssetPilot tracks every physical item a company owns — laptops, furniture, accessories, tools, anything — through its entire life:

- What equipment exists, and what condition is it in?
- Where is it right now, and who has it?
- Who supplied it, what did it cost, and is it still under warranty?
- When was it handed out, and when is it due back — and is it overdue?
- Is it out on a quick informal loan (Check In/Out), or a formal tracked allocation?
- What's happened to it over its lifetime — every repair, transfer, return, and status change?
- Who made each change, and when?
- Can I scan it with a USB scanner or the PC's webcam and see everything about it instantly?
- Can I get a report on any of this, and export it to Print, PDF, Excel, or CSV?

It covers the full lifecycle: **procurement → storage → allocation → return / transfer → repair → decommission (with a way back)**, with QR-code identification throughout, a full database audit trail, and a separate append-only, file-based security log for the most sensitive administrative actions.

Routine success confirmations (save, restore, delete, approve, edit) appear as brief, auto-dismissing **toast notifications** colour-matched to the action that triggered them, instead of blocking popups. Only destructive actions ("Are you sure?") still use an explicit confirmation dialog. See [Cross-Cutting Behaviour](#cross-cutting-behaviour) for the full colour map.

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# .NET 8 |
| UI Framework | Windows Forms (WinForms) |
| ORM | Entity Framework Core |
| Database | MariaDB / MySQL (default database `inventory_management` on `localhost`, or a server IP) |
| Password Hashing | BCrypt (`BCrypt.Net`) with transparent legacy-password fallback |
| Credential Protection | Windows DPAPI (`ProtectedData`, current-user scope) for the saved database password |
| QR Generation | QRCoder |
| Camera Scanning | AForge (webcam capture) + ZXing (barcode / QR decoding) |
| Excel Export | ClosedXML |
| PDF / Print | `System.Drawing.Printing` (`PrintDocument`) + `PrintPreviewDialog` |
| CSV Export | RFC 4180-compliant quoting and escaping |
| Charts | Custom GDI+ (no third-party charting library) |
| Backup / Restore | `mysqldump.exe` / `mysql.exe`, driven with secure temporary option files |
| Picture Storage | Network share (`\\{server}\AssetPilotData\`) for equipment, user, and employee pictures, with an automatic local fallback (`%AppData%\AssetPilot\`) when the share isn't reachable — e.g. a local/XAMPP deployment |
| Time Zone Display | Pakistan Standard Time (PKT) for log and return timestamps |
| Platform | Windows 10/11 (64-bit) |

---

## User Roles

AssetPilot has two roles, stored in the `roles` table, linked to users through `user_roles`, and cached for the session in `AuthService`.

| Feature | Admin | Operator |
|---|:---:|:---:|
| Dashboard, Charts, Global Search, Notifications | ✅ | ✅ |
| Equipment — View / Add / Edit / Import / Export | ✅ | ✅ |
| Equipment — Delete | ✅ | ❌ Restore button hidden |
| Employees — Full management | ✅ | ✅ |
| Locations — Full management | ✅ | ✅ |
| Allocation — Allocate / Approve / Return / Edit date / Delete | ✅ | ✅ |
| Transfer | ✅ | ✅ |
| Returns (Process / Edit / Undo) | ✅ | ✅ |
| QR Scanner (incl. camera scanning, Bulk Scan) | ✅ | ✅ |
| Check In / Check Out and its History screen | ✅ | ✅ |
| Reports — View & Export | ✅ | ✅ |
| Logs (Audit Trail) | ✅ | ✅ |
| Decommission (incl. Restore) | ✅ | ❌ Not accessible from sidebar |
| Deleted Items (restore / permanent delete) | ✅ | ❌ Not accessible from sidebar |
| Settings — General / Backup-Restore / Users / Change Password / Recovery PIN / Printing / Categories / System Configuration | ✅ | ❌ Not accessible from sidebar |

The three admin-only sidebar entries — **Decommission, Deleted Items, Settings** — are hidden automatically for Operator accounts (`ApplyRolePermissions()` on the Dashboard). If a non-admin somehow attempts to open one of these screens, access is denied and a **warning toast** is shown instead of a silent failure.

Every save made anywhere in the app is attributed to the signed-in account and timestamped — this is what powers the audit trail (see [Logs](#logs-audit-trail)).

<p align="center">
<img src="screenshots/001-sidebar-admin-vs-operator.png" alt="Screenshot: Sidebar comparison — Admin view vs Operator view, side by side" width="900">
</p>

<p align="center">
<img src="screenshots/300-roles-operator-access-denied-toast.png" alt="Screenshot: Warning toast shown when a non-admin attempts to open a restricted screen" width="500">
</p>

---

## Login

<p align="center">
<img src="screenshots/010-login-screen.png" alt="Screenshot: Login screen, empty state" width="900">
</p>

- Username + password sign-in with a show/hide password toggle (eye icon). Both fields are required — leaving either blank shows an error.
- **Animated login screen** — the sign-in screen runs a background animation (stopped as soon as you sign in), and shows the AssetPilot branding.
- Only **active** accounts can sign in. Invalid credentials show one generic message (`Invalid username or password.`) — it deliberately does not reveal whether the username or the password was wrong, so usernames can't be enumerated. The input containers also play a short horizontal **shake animation** with a red border flash.
- Passwords are verified with **BCrypt** hashing. Older accounts that still hold a legacy plain-text password continue to sign in seamlessly and are upgraded to BCrypt the next time their password is changed or reset.
- **"Keep Me Logged In"** toggle — a custom-painted pill switch. When on, the username is saved to `remember_me.txt` (in the application's startup folder) and pre-filled on the next launch, with focus moved straight to the password box. The password is **never** stored or remembered; it must always be typed manually, every time, even with the toggle on. Turning the toggle off deletes the file.
- Pressing **Enter** in either field submits the form, same as clicking **Sign In**.

<p align="center">
<img src="screenshots/011-login-password-visible.png" alt="Screenshot: Login screen with password revealed via eye icon" width="700">
</p>

<p align="center">
<img src="screenshots/012-login-remember-me-toggle.png" alt="Screenshot: Keep Me Logged In toggle, on state" width="700">
</p>

<p align="center">
<img src="screenshots/301-login-username-prefilled-focus-password.png" alt="Screenshot: Next launch with Keep Me Logged In on — username pre-filled, cursor in the password field" width="700">
</p>

<p align="center">
<img src="screenshots/302-login-empty-fields-error.png" alt="Screenshot: Error shown when username or password is left blank" width="700">
</p>

<p align="center">
<img src="screenshots/013-login-invalid-credentials-error.png" alt="Screenshot: Invalid username or password error message" width="700">
</p>

<p align="center">
<img src="screenshots/303-login-shake-animation-red-border.png" alt="Screenshot: Wrong credentials — input containers mid-shake with the red border flash" width="700">
</p>

<p align="center">
<img src="screenshots/304-login-animated-background.png" alt="Screenshot: Login screen with its background animation running" width="900">
</p>

### Forgot Password (offline self-service reset)

The login screen includes a fully offline password-reset panel (`pnlForgot`) — no mail server is needed. The user proves who they are with one of two methods, chosen from a tab selector:

| Tab | What you must enter |
|---|---|
| ✉ **Work Email** | The work email registered on the account (must match the user's `Email`). |
| 🔑 **Recovery PIN** | Either the user's **personal recovery PIN** or the **organization master PIN**. |

<p align="center">
<img src="screenshots/305-login-forgot-password-link.png" alt="Screenshot: The Forgot Password link on the login screen" width="700">
</p>

<p align="center">
<img src="screenshots/306-login-forgot-panel-email-tab.png" alt="Screenshot: Forgot Password panel — Work Email tab selected, placeholder name@company.com visible" width="700">
</p>

<p align="center">
<img src="screenshots/307-login-forgot-panel-pin-tab.png" alt="Screenshot: Forgot Password panel — Recovery PIN tab selected, placeholder 'Enter personal PIN or Admin PIN' visible" width="700">
</p>

- **Clear text gutter** — the verification box sits to the right of its icon (✉ / 🔑) with a clean gutter, so the first characters of the placeholder and of what you type are never clipped.
- **Strict input rules**, enforced on every keystroke *and* on paste:
  - *Recovery PIN mode* — **digits only**. Letters, spaces and symbols are rejected, and anything non-numeric that's pasted is stripped automatically.
  - *Work Email mode* — spaces are rejected, only valid email characters are accepted (`a–z`, `A–Z`, `0–9`, `@`, `.`, `_`, `-`, `+`), a second `@` can't be typed, and whitespace is stripped on paste.
- **Tab switching clears the field**, so a half-typed email never carries over into PIN mode (or vice-versa).
- **Live verification** — the check runs as you type. The moment it matches: a green ✓ appears, the box border turns emerald (`#10B981`), a hint confirms the match, and the **New Password**, **Confirm New Password** and **Reset Password** controls become enabled. Until then they stay locked.
- **On reset** — the new password is hashed with BCrypt, `PasswordChangedAt` / `UpdatedAt` are updated, the event is written to the [Security Audit Log](#security-audit-log-file-based), a success toast appears, and you're returned to the login screen with your username pre-filled.

<p align="center">
<img src="screenshots/308-login-forgot-email-rejects-spaces-extra-at.png" alt="Screenshot: Work Email mode rejecting a space and a second @ sign" width="700">
</p>

<p align="center">
<img src="screenshots/309-login-forgot-pin-rejects-letters.png" alt="Screenshot: Recovery PIN mode rejecting letters and symbols (digits only)" width="700">
</p>

<p align="center">
<img src="screenshots/310-login-forgot-paste-stripped.png" alt="Screenshot: Non-numeric characters pasted into the PIN box automatically stripped" width="700">
</p>

<p align="center">
<img src="screenshots/311-login-forgot-verified-green-check.png" alt="Screenshot: Verification matched — green check, emerald border, matched hint" width="700">
</p>

<p align="center">
<img src="screenshots/312-login-forgot-new-password-fields-enabled.png" alt="Screenshot: New Password / Confirm / Reset Password controls enabled after verification" width="700">
</p>

<p align="center">
<img src="screenshots/313-login-forgot-fields-locked-before-verify.png" alt="Screenshot: New Password fields disabled while verification hasn't matched yet" width="700">
</p>

<p align="center">
<img src="screenshots/314-login-forgot-reset-success-toast.png" alt="Screenshot: Success toast after resetting the password, back at login with username pre-filled" width="700">
</p>

---

## Dashboard

<p align="center">
<img src="screenshots/020-dashboard-full-view.png" alt="Screenshot: Dashboard full view" width="900">
</p>

The landing screen after login. Everything here is live, read-only summary data — nothing on this screen can be edited directly. The screen loads its numbers on construction and again whenever it is shown.

### Stat Cards

<p align="center">
<img src="screenshots/021-dashboard-stat-cards.png" alt="Screenshot: Dashboard stat cards row, with entrance animation mid-way" width="900">
</p>

Animated cards across the top (they enter with a bounce animation):

| Card | Shows |
|---|---|
| Total Equipment | Count of all active (non-deleted) equipment |
| In Store | Items available in storage |
| Allocated | Currently allocated / checked-out items |
| Under Repair | Items currently flagged for maintenance |
| Decommissioned | Retired equipment |
| Employees | Total employee count |
| Currently Overdue | Approved allocations past their expected return date (computed live, never stored). Shown in dark amber (`#FFA000`); clicking the card jumps straight to the Allocation screen pre-filtered to overdue items |

<p align="center">
<img src="screenshots/320-dashboard-overdue-card-amber.png" alt="Screenshot: The Currently Overdue card in amber" width="500">
</p>

<p align="center">
<img src="screenshots/321-dashboard-overdue-card-click-prefiltered-allocation.png" alt="Screenshot: Allocation screen opened by clicking the Overdue card, pre-filtered to overdue items" width="900">
</p>

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

- **Status Breakdown** — donut chart of In Store / Allocated / Under Repair / Decommissioned.
- **Category Breakdown** — horizontal bar chart, count per category.
- **6-Month Allocation Trend** — line chart, allocations created per month over the last 6 months.

### Top Employees by Holdings

<p align="center">
<img src="screenshots/025-dashboard-top-employees-card.png" alt="Screenshot: Top Employees by Holdings card, ranked progress bars" width="900">
</p>

<p align="center">
<img src="screenshots/026-dashboard-top-employees-hover-tooltip.png" alt="Screenshot: Hover tooltip over an employee's bar, showing a full allocation breakdown" width="700">
</p>

Ranks the employees currently holding the most equipment, using rank-based multi-coloured progress bars sized to each person's allocation volume, with the allocation count shown on the left of each bar. Hovering a name or bar shows a tooltip with the employee's details and a complete breakdown of what they currently hold.

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

<p align="center">
<img src="screenshots/322-dashboard-sidebar-toggle-button.png" alt="Screenshot: The sidebar collapse toggle button at the top of the sidebar, fully visible" width="300">
</p>

<p align="center">
<img src="screenshots/323-dashboard-sidebar-operator-hidden-items.png" alt="Screenshot: Sidebar as seen by an Operator — Decommission, Deleted Items and Settings hidden" width="300">
</p>

Every module is reachable from the left sidebar; each button docks its screen into the content area and animates the title-bar label character by character. A toggle button at the top of the sidebar smoothly slides it between full and icon-only view without affecting any screen's functionality. Admin-only entries (Decommission, Deleted Items, Settings) are hidden for Operator accounts.

### Global Search (Ctrl + K)

<p align="center">
<img src="screenshots/030-dashboard-global-search-open.png" alt="Screenshot: Global Search dialog open via Ctrl+K, showing fuzzy results across modules" width="700">
</p>

<p align="center">
<img src="screenshots/324-dashboard-global-search-select-result.png" alt="Screenshot: Selecting a Global Search result and jumping to that record or screen" width="900">
</p>

Press **Ctrl + K** anywhere in the app to open a real-time fuzzy search across **Equipment, Allocations, Employees, and Locations** at once. Selecting a result jumps directly to that record or screen.

### Light / Dark Mode Toggle

<p align="center">
<img src="screenshots/031-dashboard-theme-toggle-light.png" alt="Screenshot: Theme toggle switch in light mode position" width="300">
</p>

<p align="center">
<img src="screenshots/032-dashboard-theme-toggle-dark.png" alt="Screenshot: Theme toggle switch in dark mode position, whole app repainted in dark palette" width="900">
</p>

<p align="center">
<img src="screenshots/325-dashboard-theme-toggle-hover.png" alt="Screenshot: Hover highlight over the sun/moon icon of the theme toggle" width="300">
</p>

<p align="center">
<img src="screenshots/326-dashboard-dark-mode-equipment-grid.png" alt="Screenshot: A child screen (Equipment) opened after switching to dark mode" width="900">
</p>

A pill-shaped switch in the top bar, immediately left of the notification bell, toggles the entire app between a light and dark colour palette instantly:

- ☀️ amber sun on the light side, 🌙 slate-blue moon on the dark side; a white thumb slides smoothly between them (track: muted green in light mode, near-black in dark mode). Hovering the icon shows a visual highlight.
- The dashboard, sidebar, top bar, stat cards, charts and every child form opened afterwards repaint in the chosen palette.
- The choice is saved to `theme.json` in the app folder and restored automatically on the next launch — it is remembered **per workstation**, not per user account, and has no separate save step.
- Dark palette key colours: dashboard background `RGB(18,20,24)`, cards `RGB(36,39,46)`, sidebar `RGB(14,16,19)`, top bar `RGB(45,48,55)`, primary text `RGB(220,224,232)`, secondary text `RGB(140,148,160)`.

### Notifications

<p align="center">
<img src="screenshots/033-dashboard-notification-bell-badge.png" alt="Screenshot: Notification bell icon with live badge count, visible from any screen" width="300">
</p>

<p align="center">
<img src="screenshots/034-dashboard-notification-dropdown.png" alt="Screenshot: Notification bell dropdown open, listing overdue allocations and open maintenance items" width="500">
</p>

<p align="center">
<img src="screenshots/327-dashboard-notification-bell-hover.png" alt="Screenshot: Hover highlight on the notification bell" width="300">
</p>

<p align="center">
<img src="screenshots/328-dashboard-notification-click-navigation.png" alt="Screenshot: Clicking an alert in the dropdown navigating straight to the related record" width="900">
</p>

A bell icon in the top bar shows a live count badge, visible from every screen in the app — not just the Dashboard:
- **Overdue allocations** — approved allocations past their expected return date.
- **Open maintenance items** — maintenance records that aren't yet resolved.
- **Low-stock items**, where applicable.

Clicking it opens a dropdown listing all alerts, each with direct click-through navigation to the relevant record.

### Recent Activities Panel

<p align="center">
<img src="screenshots/035-dashboard-recent-activities.png" alt="Screenshot: Recent Activities live feed panel" width="900">
</p>

A live feed of the most recent allocation, return, and transfer events across the whole system, with relative timestamps (e.g. "7h ago"), equipment name and action description — sourced directly from the audit trail (`equipment_logs`, newest first).

---

## Equipment

<p align="center">
<img src="screenshots/040-equipment-list-full.png" alt="Screenshot: Equipment list view, full grid" width="900">
</p>

The master list of every (non-deleted) equipment item, loaded one page at a time (server-side paging, ordered by Equipment Code). Columns: Equipment Code, QR Code, Procurement Type, Name, Make, Model, Serial Number, Category, Status, Condition, Location (with the room name after a hyphen), Purchase Date, Price.

<p align="center">
<img src="screenshots/340-equipment-pagination-controls.png" alt="Screenshot: Equipment grid pagination — Prev / Next and page indicator" width="700">
</p>

<p align="center">
<img src="screenshots/041-equipment-status-color-legend.png" alt="Screenshot: Equipment grid rows showing all four status colors together" width="900">
</p>

Rows are colour-coded by status: green = In Store, blue = Allocated, orange = Under Repair, red = Decommissioned.

### Search & Filter

<p align="center">
<img src="screenshots/042-equipment-search-box.png" alt="Screenshot: Equipment search box with fuzzy suggestion results" width="700">
</p>

<p align="center">
<img src="screenshots/043-equipment-category-filter-dropdown.png" alt="Screenshot: Equipment category filter dropdown, open, listing saved and inferred categories" width="500">
</p>

<p align="center">
<img src="screenshots/341-equipment-search-exact-code-match.png" alt="Screenshot: Searching a full Equipment Code or QR Code returns the exact match first" width="700">
</p>

<p align="center">
<img src="screenshots/342-equipment-search-category-combined.png" alt="Screenshot: Search text and category filter applied together" width="900">
</p>

- Search matches **Equipment Code / QR Code first** (exact `Contains`), then falls back to **fuzzy matching** on Name, Make, Model, Serial Number and Category, ranked by fuzzy score. (While a search is active, the full non-deleted table is searched rather than a single page.)
- The category filter dropdown is built from the Categories list in Settings **plus** any category values already present on equipment records (case-insensitive, alphabetical). Search and category filter combine.

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

<p align="center">
<img src="screenshots/343-equipment-refresh-resets-search-page.png" alt="Screenshot: Refresh clearing the search box and returning to page 1" width="700">
</p>

| Button | Behaviour |
|---|---|
| Add | Opens the Add Equipment form. On save, the grid reloads. |
| Edit | Opens the Edit form for the selected item. |
| Delete | Soft-deletes the selected item(s) — guarded, see [Delete Equipment](#delete-equipment). *(Hidden for Operators.)* |
| Refresh | Clears the search, resets to page 1, reloads. |
| Export | Exports the currently visible rows to CSV. |
| Import | Bulk-adds equipment from a CSV file. |
| Select ✔ / Select All | Shows a per-row checkbox column / ticks every row, for bulk operations. |

### Add Equipment

<p align="center">
<img src="screenshots/047-equipment-add-form-empty.png" alt="Screenshot: Add Equipment form, blank state" width="900">
</p>

- Equipment Code (`EQ###`) and QR Code (`QR###`) are **auto-generated** — the next free number is found and checked for uniqueness, so there's never a typo or duplicate.
- The record is stamped with the **signed-in user** as its creator.

| Field | Notes |
|---|---|
| Name | **Required** |
| Make / Model / Serial Number | Free text |
| Status | In Store / Allocated / Under Repair / Decommissioned |
| Purchase Date, Price | Date picker and amount |
| Category | Pick from the Categories list |
| Vendor Name | Free text |
| Condition Grade | Dropdown |
| Warranty Expiry | Optional — tick the checkbox to enable the date picker; stored empty if unticked |
| Description / Notes | Free text |
| Procurement Type | Free-text box: matches an existing type (case-insensitive) or **creates a new one automatically** the first time it's used |
| Picture | Optional staged image, see below |

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
<img src="screenshots/344-equipment-add-procurement-type-new.png" alt="Screenshot: Typing a brand-new Procurement Type that gets created on save" width="700">
</p>

<p align="center">
<img src="screenshots/345-equipment-add-name-required-error.png" alt="Screenshot: Validation error when Name is left blank" width="700">
</p>

<p align="center">
<img src="screenshots/346-equipment-add-cancel-staged-picture-discarded.png" alt="Screenshot: Cancelling the form — the staged picture is discarded" width="700">
</p>

<p align="center">
<img src="screenshots/051-equipment-add-saved-toast.png" alt="Screenshot: Toast confirmation after saving a new equipment record" width="500">
</p>

A picture can be staged and attached to the record — stored on the shared network folder when reachable, or in a local folder on this machine otherwise (see [Cross-Cutting Behaviour](#cross-cutting-behaviour)). On save, any previous picture file is replaced; on cancel, the staged file is deleted.

### Edit Equipment

<p align="center">
<img src="screenshots/052-equipment-edit-form.png" alt="Screenshot: Edit Equipment form, pre-filled with existing values" width="900">
</p>

<p align="center">
<img src="screenshots/053-equipment-status-dropdown-open.png" alt="Screenshot: Status dropdown open, showing In Store / Allocated / Under Repair / Decommissioned options" width="500">
</p>

- Same fields as Add, pre-filled with the current values.
- Switching status **to "Under Repair"** opens the **Maintenance Record dialog** to log the issue (issue description, serviced by, estimated cost). Saving opens a new maintenance record and writes a **Maintenance Opened** audit entry.
- Switching status **away from "Under Repair"** closes the open maintenance record automatically (resolved, with the end/resolved date stamped) and writes a **Maintenance Resolved** audit entry.
- Saving an edit compares the old and new values **field by field** and logs only what actually changed — Name, Make, Model, Serial Number, Status, Category, Purchase Date, Price, Description, Vendor, Condition, Warranty Expiry, Notes and Picture (added / removed / replaced).

<p align="center">
<img src="screenshots/054-equipment-maintenance-dialog-open.png" alt="Screenshot: Maintenance record dialog triggered by switching status to Under Repair" width="700">
</p>

<p align="center">
<img src="screenshots/055-equipment-maintenance-dialog-filled.png" alt="Screenshot: Maintenance dialog filled — issue description, servicer, estimated cost" width="700">
</p>

<p align="center">
<img src="screenshots/347-equipment-maintenance-dialog-cancel.png" alt="Screenshot: Cancelling the Maintenance dialog so the status change doesn't go ahead" width="700">
</p>

<p align="center">
<img src="screenshots/348-equipment-maintenance-resolved-on-status-change.png" alt="Screenshot: Status switched back from Under Repair, open maintenance record closed" width="700">
</p>

<p align="center">
<img src="screenshots/056-equipment-edit-diff-audit.png" alt="Screenshot: Logs entry showing only the changed fields after an equipment edit" width="900">
</p>

### Maintenance Records

Behind the dialog is the maintenance service, which keeps a repair history for every item:

- **Open** — requires an issue description; stamps the reported date as today, sets status `open`, and audits it with the issue, who serviced it and the estimated cost.
- **Resolve** — marks the record `resolved`, stamps the resolved date, optionally updates the final cost, and audits it with before/after details.
- Records are shown newest first; the most recent `open` / `in_progress` record is the "current" one for an item.
- Maintenance status badges: `open` red/orange (`#DC5528`), `in_progress` amber (`#D97706`), `resolved` green (`#228B22`).

<p align="center">
<img src="screenshots/349-equipment-maintenance-logs-opened-resolved.png" alt="Screenshot: Logs screen showing Maintenance Opened and Maintenance Resolved entries with their detailed old/new text" width="900">
</p>

### Equipment Detail View

<p align="center">
<img src="screenshots/057-equipment-detail-popup-main.png" alt="Screenshot: Equipment detail popup, full two-column card" width="900">
</p>

Double-clicking a row opens a read-only, two-column detail popup that auto-sizes and scrolls if it's taller than the screen:

- **Left column — Equipment Information:** Equipment Code, QR Code, Name, Make, Model, Serial Number, Category, Status (colour-coded badge), Condition, Purchase Date, Purchase Price, Procurement Type, Vendor Name, Warranty Expiry, Description, Notes, and a **View Picture** button.
- **Right column — Current Allocation:** Current Location, Currently Assigned To, Allocation Date, Expected Return Date. The employee's name is a **clickable link** that jumps to the Employees screen pre-filtered to that employee.
- **Right column — Maintenance History:** a scrollable panel listing every record with its number and reported date, status badge, issue, serviced by, cost (₨) and resolved date — or "No maintenance history recorded."

<p align="center">
<img src="screenshots/058-equipment-detail-view-picture.png" alt="Screenshot: View Picture button expanded, showing the equipment photo" width="700">
</p>

<p align="center">
<img src="screenshots/350-equipment-detail-employee-link-navigates.png" alt="Screenshot: Clicking the Currently Assigned To link opening Employees pre-filtered to that person" width="900">
</p>

<p align="center">
<img src="screenshots/059-equipment-detail-maintenance-history.png" alt="Screenshot: Maintenance History section inside the detail popup — open, in progress, and resolved records" width="900">
</p>

<p align="center">
<img src="screenshots/060-equipment-detail-no-maintenance.png" alt="Screenshot: Detail popup for an item with no maintenance history — 'No maintenance history recorded' state" width="900">
</p>

<p align="center">
<img src="screenshots/351-equipment-detail-no-picture-state.png" alt="Screenshot: View Picture button when no picture is attached" width="700">
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

<p align="center">
<img src="screenshots/352-equipment-detail-clear-history-refreshed-in-place.png" alt="Screenshot: The popup rebuilt in place after Clear History — empty maintenance panel, button greyed" width="900">
</p>

- **View Logs** — closes the popup and opens the Logs screen pre-filtered to this item's equipment code.
- **Clear History** — permanently deletes every maintenance record for this item (with confirmation), then refreshes the card in place. Red-tinted when records exist; greyed out and disabled when there are none. The action is written to the audit trail as "Maintenance History Cleared" with the record count, even though the records themselves are gone for good.

### Delete Equipment

<p align="center">
<img src="screenshots/065-equipment-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Equipment screen" width="700">
</p>

<p align="center">
<img src="screenshots/353-equipment-delete-blocked-allocated.png" alt="Screenshot: Delete blocked — item is allocated, with a warning to return it first" width="700">
</p>

<p align="center">
<img src="screenshots/354-equipment-delete-blocked-pending-or-checked-out.png" alt="Screenshot: Delete blocked — item has a pending allocation or an active/overdue Check-Out" width="700">
</p>

<p align="center">
<img src="screenshots/355-equipment-delete-bulk-blocked.png" alt="Screenshot: Bulk delete of several selected rows blocked because one is still in use" width="700">
</p>

<p align="center">
<img src="screenshots/356-equipment-delete-success-toast.png" alt="Screenshot: Toast after a permitted soft delete" width="500">
</p>

- Deleting an item is a **soft delete** — it's hidden from every list and dropdown but not destroyed. It moves to the **Deleted Items** screen (Admin-only), where it can be restored or permanently erased.
- Deletion is **guarded**. Before an item is flagged deleted (single or multi-select), AssetPilot checks for: an active approved allocation, a pending allocation request, an active or overdue Check-Out transaction, and a current status of `allocated`. If any exist, the delete is blocked with a warning telling you to return or complete custody first.
- A permitted delete is written to the audit trail as `equipment_deleted`.

### Import / Export

<p align="center">
<img src="screenshots/066-equipment-import-tooltip.png" alt="Screenshot: Import button hover tooltip reminding to pre-create categories in Settings" width="700">
</p>

<p align="center">
<img src="screenshots/067-equipment-import-file-picker.png" alt="Screenshot: Import CSV file picker" width="700">
</p>

<p align="center">
<img src="screenshots/357-equipment-import-result-toast.png" alt="Screenshot: Result shown after a successful equipment import" width="500">
</p>

<p align="center">
<img src="screenshots/068-equipment-export-csv-toast.png" alt="Screenshot: CSV export confirmation toast" width="500">
</p>

- **Import** — bulk-add equipment from a CSV file. Hovering the button shows: *"Make sure the category of the equipment you are about to import is already added in the Category screen in Settings."* The import maps each row's category text directly onto the record, so pre-create categories first to avoid stray free-text categories.
- **Export** — exports the currently visible/filtered grid rows to CSV.

---

## Employees

<p align="center">
<img src="screenshots/080-employees-list.png" alt="Screenshot: Employees list view, full grid" width="900">
</p>

Manage the people equipment can be allocated to. Grid columns: Employee Code, Full Name, Designation, Email, Phone, and Status (Active / Inactive). The list is paginated server-side (ordered by Full Name).

<p align="center">
<img src="screenshots/380-employees-pagination-controls.png" alt="Screenshot: Employees grid pagination — Prev / Next and page indicator" width="700">
</p>

<p align="center">
<img src="screenshots/081-employees-search.png" alt="Screenshot: Employees search box with results" width="700">
</p>

<p align="center">
<img src="screenshots/381-employees-search-by-code-first.png" alt="Screenshot: Searching an Employee Code, matched before the fuzzy fallback" width="700">
</p>

Search tries a **code match first**, then falls back to fuzzy matching across Full Name, Department, Email and Phone.

| Button | Behaviour |
|---|---|
| Add | Opens the Add Employee form. |
| Edit | Opens the Edit form for the selected employee. |
| Delete | Confirms, then permanently deletes — see [Delete Employee](#delete-employee). |
| Export | Exports the current page of the grid to CSV. |
| Import | Bulk-imports employees from a CSV file. |

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

<p align="center">
<img src="screenshots/382-employees-add-name-required-error.png" alt="Screenshot: Validation error when Full Name is left blank" width="700">
</p>

<p align="center">
<img src="screenshots/383-employees-add-duplicate-email-error.png" alt="Screenshot: Error when the email is already used by another employee" width="700">
</p>

<p align="center">
<img src="screenshots/384-employees-add-auto-code-generated.png" alt="Screenshot: New employee saved with an auto-generated EMP### code" width="700">
</p>

<p align="center">
<img src="screenshots/385-employees-edit-saved-toast.png" alt="Screenshot: Toast after saving an employee edit" width="500">
</p>

- **Fields** — Full Name (required), Employee Code (auto-generated as `EMP###` if left blank; must be unique), Designation, Email (must be unique if provided — checked against everyone else on edit), Phone, Is Active checkbox, and an optional profile picture (Upload / Remove).
- **Profile pictures** are copied to the employee-pictures folder (network share when reachable, local fallback otherwise) as `{EmployeeCode}_{GUID}{ext}`. On cancel/close, any staged picture is discarded.
- Add, Edit and Delete are each written to the audit trail (`employee_created`, `employee_updated`, `employee_deleted`).

### Employee Detail Popup

<p align="center">
<img src="screenshots/086-employees-detail-popup.png" alt="Screenshot: Employee detail popup, avatar and status badge" width="700">
</p>

<p align="center">
<img src="screenshots/386-employees-detail-popup-default-avatar.png" alt="Screenshot: Employee detail popup with the default grey silhouette (no picture set)" width="700">
</p>

- Double-clicking a row opens a read-only detail card showing the employee's avatar (circular crop, or a default grey silhouette if no picture is set), Employee Code, Designation, Email, Phone, and a Status badge.

### Delete Employee

<p align="center">
<img src="screenshots/087-employees-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Employees screen" width="700">
</p>

<p align="center">
<img src="screenshots/088-employees-delete-blocked-active-allocation.png" alt="Screenshot: Delete blocked error — employee still has an active allocation" width="700">
</p>

- **Delete** is a **permanent, hard delete** — blocked if the employee currently has any active (approved) allocation. Historical (non-approved) allocation records keep a reference to the deleted employee's ID and will show a broken/blank employee reference afterward. To retire someone without losing history, untick **Is Active** instead (inactive employees stop appearing in allocation pickers).

### Import / Export

<p align="center">
<img src="screenshots/089-employees-import-dialog.png" alt="Screenshot: Employees bulk Import dialog" width="700">
</p>

<p align="center">
<img src="screenshots/387-employees-import-result-toast.png" alt="Screenshot: Result shown after a successful employees import" width="500">
</p>

<p align="center">
<img src="screenshots/090-employees-export-csv-toast.png" alt="Screenshot: Employees CSV export confirmation toast" width="500">
</p>

---

## Locations

<p align="center">
<img src="screenshots/100-locations-list.png" alt="Screenshot: Locations list view, full grid" width="900">
</p>

Manage the physical places equipment can be assigned to (offices, stores, buildings, rooms). Grid shows Location Name, Building, Room Number, Room Name (the internal ID is hidden). The list is **paginated at 50 records per page** with a `Page X of Y` indicator and Prev / Next buttons, and has a **live search** box.

<p align="center">
<img src="screenshots/400-locations-pagination-controls.png" alt="Screenshot: Locations pagination — Page X of Y, Prev / Next" width="700">
</p>

<p align="center">
<img src="screenshots/401-locations-live-search.png" alt="Screenshot: Live search filtering the Locations grid as you type" width="700">
</p>

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

<p align="center">
<img src="screenshots/402-locations-save-toast.png" alt="Screenshot: Toast after saving a location" width="500">
</p>

- **Add / Edit** — Location Name, Building, Room Number, Room Name.
- **Delete** — removes the location record. Add, Edit and Delete are each written to the audit trail (`location_created`, `location_updated`, `location_deleted`).
- **Export** — exports the Locations list to CSV.
- Locations defined here also drive the linked Location/Room dropdowns on the Check In/Out screen's Check-Out tab (see [Check In / Check Out](#check-in--check-out)) and the Location column everywhere else.

---

## Allocation

<p align="center">
<img src="screenshots/110-allocation-list-merged.png" alt="Screenshot: Allocation list view, merged: formal allocations + Check-In/Out loans + available equipment" width="900">
</p>

The formal, tracked workflow for assigning equipment to employees — with an approval step and full status history. The list merges three sources:

1. **Formal allocations** that aren't yet returned (pending, approved, rejected).
2. **Active Check-In/Out loans** (Pending or Overdue Check-Outs of items currently allocated), labelled with a *Check-In/Out* source tag.
3. **Available equipment** — items that aren't decommissioned, under repair, or tied up in any active allocation or loan, shown as synthetic "Available" rows so you can allocate straight from the list.

Columns: QR Code, Equipment Code, Equipment Name, Make, Employee, Location (with room name after a hyphen), Date Allocated, Expected Return, Status, Source, Allocated By. Status and Source are shown in clean, human-readable text (e.g. "In Store", not `in_store`).

| Status | Meaning |
|---|---|
| Pending | Created, awaiting approval |
| Approved | Active; the equipment is `allocated` |
| Returned | Handed back; equipment is `in_store` (moves to the Returns screen) |
| Rejected | Rejected; equipment status unchanged |
| **Overdue** | *Computed in the UI only* — an Approved allocation whose expected return date has passed. Never stored in the database |
| **Available** | *Computed in the UI only* — equipment not in any active allocation. Never stored in the database |

### Filters

<p align="center">
<img src="screenshots/111-allocation-filters-button-menu.png" alt="Screenshot: Combined Filters button open, showing Status and Category submenus" width="500">
</p>

<p align="center">
<img src="screenshots/112-allocation-filters-active-label.png" alt="Screenshot: Filters button label updated to show active filters, e.g. 'Filters (Overdue, Laptops)'" width="500">
</p>

<p align="center">
<img src="screenshots/430-allocation-filter-status-submenu.png" alt="Screenshot: The Status submenu — All / In Store / Allocated / Pending / Overdue" width="500">
</p>

<p align="center">
<img src="screenshots/431-allocation-filter-category-submenu.png" alt="Screenshot: The Category submenu — All Categories plus each category" width="500">
</p>

<p align="center">
<img src="screenshots/432-allocation-filter-menu-auto-closes.png" alt="Screenshot: The filter menu closing itself when the cursor leaves the button and menu" width="500">
</p>

<p align="center">
<img src="screenshots/113-allocation-overdue-highlight.png" alt="Screenshot: Allocation row(s) shown with the computed Overdue flag/amber highlight" width="900">
</p>

<p align="center">
<img src="screenshots/114-allocation-checkinout-source-row-disabled-buttons.png" alt="Screenshot: A Check-In/Out sourced row selected, with Approve/Return/Delete/Edit Return buttons greyed out" width="900">
</p>

A single **🔽 Filters** button combines **Status** (All / In Store / Allocated / Pending / Overdue) and **Category** (All Categories plus every category from Settings and from equipment records) into one two-level menu that opens on hover or click and closes when the cursor leaves it. The button's own label updates to show what's active, e.g. `Filters (Overdue, Laptops)`.

- **Overdue rows** are highlighted in dark amber — the same amber as the Dashboard's *Currently Overdue* card.
- When a **Check-In/Out sourced** row is selected, **Approve, Return, Delete and Edit Return are all disabled** — those loans are managed on the Check In/Out screen.

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

<p align="center">
<img src="screenshots/433-allocation-checkbox-select-all-none-menu.png" alt="Screenshot: The ▾ menu beside the header checkbox offering All and None" width="500">
</p>

<p align="center">
<img src="screenshots/434-allocation-header-checkbox-partial-state.png" alt="Screenshot: Header checkbox in its partial state when only some rows are ticked" width="500">
</p>

- A **Gmail-style `[☐ ▾]` select control** in the toolbar: the checkbox toggles every visible row, the ▾ opens an **All / None** menu, and the header checkbox automatically reflects all / some / none selected.
- Click any column header to **sort** (▲ / ▼ indicator); the page resets to 1 on every sort change.
- Page sizes **10 / 25 / 50 / 100 / 250 / 500 / 1000** (default 25), Prev / Next buttons and a `Page N of M` label.

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
<img src="screenshots/435-allocation-new-equipment-search-suggest.png" alt="Screenshot: Fuzzy search-and-select while typing in the equipment box" width="700">
</p>

<p align="center">
<img src="screenshots/436-allocation-new-location-dropdown.png" alt="Screenshot: Location dropdown in the New Allocation dialog" width="700">
</p>

<p align="center">
<img src="screenshots/437-allocation-new-expected-return-date.png" alt="Screenshot: Optional expected return date picker in the New Allocation dialog" width="700">
</p>

<p align="center">
<img src="screenshots/438-allocation-new-blocked-equipment-unavailable.png" alt="Screenshot: Allocation refused because the item already has an approved/pending allocation or isn't in store" width="700">
</p>

<p align="center">
<img src="screenshots/121-allocation-new-filled-saved.png" alt="Screenshot: New Allocation dialog filled out and saved, toast confirmation" width="700">
</p>

- Search-and-select equipment — only items that are **not** tied to an active transaction or an approved/pending allocation, and **not** decommissioned or under repair. Equipment with only historical *returned* allocations is offered normally.
- Choose an **active** employee and a location, and optionally an expected return date.
- The service requires the item to be `in_store` or `allocated`, and refuses if an approved or pending allocation already exists.
- If Status is left blank, the allocation is auto-promoted straight to **Approved** (the pending queue is bypassed) and the equipment becomes `allocated`.
- Audited as `allocation_requested`.

### Approve

<p align="center">
<img src="screenshots/122-allocation-approve-action.png" alt="Screenshot: Approve button being used on a Pending allocation row" width="700">
</p>

<p align="center">
<img src="screenshots/123-allocation-approve-blocked-checkinout-conflict.png" alt="Screenshot: Approve blocked — equipment has an active Check-In/Out transaction" width="700">
</p>

- Moves a **pending** allocation to **approved**, stamps the approval date and the **approving user** (the signed-in account), and marks the equipment `allocated`. Blocked if the equipment has an active Check-In/Out transaction. Audited as `allocation_approved`; a green toast confirms.

### Return (from Allocation screen)

<p align="center">
<img src="screenshots/124-allocation-return-confirmation.png" alt="Screenshot: Return confirmation dialog on the Allocation screen" width="700">
</p>

- Marks the allocation as returned and the equipment back to `in_store`, after a confirmation. Audited as `allocation_returned`. Note: this path does **not** record an actual return date on the allocation — use the Returns screen for a dated return with condition.

### Edit Expected Return Date

<p align="center">
<img src="screenshots/125-allocation-edit-return-date-dialog.png" alt="Screenshot: Edit Expected Return Date dialog" width="700">
</p>

<p align="center">
<img src="screenshots/439-allocation-edit-return-date-logs-entry.png" alt="Screenshot: The yellow 'Expected Return Date Updated' entry in Logs" width="900">
</p>

- Opens a small dialog to change the due date. Logged with a distinct "Expected Return Date Updated" entry (old date → new date), shown with a yellow badge in Logs.

### Delete Allocation

<p align="center">
<img src="screenshots/126-allocation-delete-confirmation.png" alt="Screenshot: Delete confirmation dialog on the Allocation screen" width="700">
</p>

<p align="center">
<img src="screenshots/127-allocation-delete-blocked-active.png" alt="Screenshot: Delete blocked error — allocation is currently Approved" width="700">
</p>

<p align="center">
<img src="screenshots/440-allocation-delete-logs-entry-orange.png" alt="Screenshot: The orange-red 'Allocation Request Deleted' entry in Logs, with its Source Screen tag" width="900">
</p>

- Removes a non-approved allocation record entirely. **Blocked while it's Approved** (return or transfer first). Written to the audit trail as **"Allocation Request Deleted"** (orange-red badge) with a *Source Screen: Allocation Screen* tag.

### Allocation Detail Popup

<p align="center">
<img src="screenshots/128-allocation-detail-popup.png" alt="Screenshot: Allocation detail popup — photo, status badge, full field list, Allocated By" width="700">
</p>

<p align="center">
<img src="screenshots/129-allocation-detail-view-picture-logs-buttons.png" alt="Screenshot: View Picture and View Logs buttons stacked in the detail popup" width="700">
</p>

<p align="center">
<img src="screenshots/441-allocation-detail-popup-checkinout-source.png" alt="Screenshot: Detail popup for a Check-In/Out sourced row (Source: Check-In/Out)" width="700">
</p>

<p align="center">
<img src="screenshots/442-allocation-detail-view-picture-opened.png" alt="Screenshot: The equipment picture opened in the zoomable black viewer" width="700">
</p>

<p align="center">
<img src="screenshots/443-allocation-detail-no-picture-disabled.png" alt="Screenshot: Detail popup with View Picture greyed out because no picture exists" width="700">
</p>

<p align="center">
<img src="screenshots/444-allocation-detail-view-logs-result.png" alt="Screenshot: Logs screen reached from View Logs, pre-filtered to that equipment" width="900">
</p>

Double-clicking a row shows the equipment photo (or a gear placeholder) with a status badge on the left, and on the right: QR Code / Equipment Code, Equipment Name, Serial / Make / Model, Employee, Location, Date Allocated, Expected Return, Source (*Allocation* or *Check-In/Out*) and **Allocated By**.

Two buttons sit under the picture: **🖼 View Picture** (greyed out and disabled if no picture is attached; otherwise opens a zoom viewer) and **📜 View Logs** (always active; closes the popup and opens the Logs screen filtered to that equipment).

---

## Returns

<p align="center">
<img src="screenshots/130-returns-list.png" alt="Screenshot: Returns history view — allocation returns + completed Check-In/Out, merged and sorted newest first" width="900">
</p>

A dedicated history view combining every **returned allocation** with every **completed Check-In/Out transaction**, merged and sorted newest first. Rows are flagged **Overdue** when the actual return date is later than the expected return date. A **Timestamp** column shows exactly when each return was processed, in **Pakistan Standard Time** (`dd MMM yyyy, hh:mm tt`).

<p align="center">
<img src="screenshots/450-returns-timestamp-pkt-column.png" alt="Screenshot: The Timestamp column in Pakistan Standard Time" width="900">
</p>

<p align="center">
<img src="screenshots/451-returns-checkinout-source-row.png" alt="Screenshot: A completed Check-In/Out row appearing in Returns history" width="900">
</p>

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

<p align="center">
<img src="screenshots/452-returns-process-return-date-picker.png" alt="Screenshot: Return date picker in the Process Return dialog" width="700">
</p>

<p align="center">
<img src="screenshots/453-returns-process-success-toast.png" alt="Screenshot: Toast after a successful return" width="500">
</p>

- Select an active (approved) allocation, set the return date and the item's condition on return. Saving sets the allocation to Returned with the actual return date, sets the equipment to **In Store**, writes the new condition grade directly on the equipment record, and audits `allocation_returned`.

### Edit Return

<p align="center">
<img src="screenshots/134-returns-edit-dialog.png" alt="Screenshot: Edit Return dialog — adjusting return date / condition after the fact" width="700">
</p>

<p align="center">
<img src="screenshots/454-returns-edit-checkinout-row-blocked.png" alt="Screenshot: Edit blocked with a warning toast on a Check-In/Out row" width="700">
</p>

- Available only for allocation-sourced rows; updates the actual return date and condition grade. Check-In/Out rows (internally negative IDs) are guarded with a warning toast. Not written to the audit log.

### Undo Return

<p align="center">
<img src="screenshots/135-returns-undo-confirmation.png" alt="Screenshot: Undo Return confirmation dialog" width="700">
</p>

<p align="center">
<img src="screenshots/136-returns-undo-blocked-active-allocation.png" alt="Screenshot: Undo blocked — equipment already has a different active allocation" width="700">
</p>

<p align="center">
<img src="screenshots/455-returns-undo-checkinout-row-blocked.png" alt="Screenshot: Undo blocked with a warning toast on a Check-In/Out row" width="700">
</p>

<p align="center">
<img src="screenshots/456-returns-undo-logs-entry-orange.png" alt="Screenshot: The orange 'Returned to Active Allocation' entry in Logs" width="900">
</p>

<p align="center">
<img src="screenshots/457-returns-undo-success-purple-toast.png" alt="Screenshot: Purple Undo toast after a successful undo" width="500">
</p>

- Reverts a processed return: the allocation goes back to **Approved**, its actual return date and return-processed timestamp are cleared, and the equipment goes back to **Allocated**.
- Before undoing, AssetPilot checks whether the equipment already has a *different* approved allocation (e.g. it was reassigned in the meantime) — if so the undo is aborted with a warning naming the current holder: *"Can't undo — '{code}' is currently allocated to {employee}."*
- Logged as its own **"Returned to Active Allocation"** entry (`return_undone`, orange badge) so it can never be mistaken for a genuine return.

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

Move equipment directly from one employee/location to another, without a separate return-then-reallocate step. The list shows every currently **Approved** allocation.

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
<img src="screenshots/460-transfer-new-transfer-button-dialog-empty.png" alt="Screenshot: Transfer dialog opened from the toolbar, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/154-transfer-confirmation-toast.png" alt="Screenshot: Toast confirmation after a successful transfer" width="500">
</p>

<p align="center">
<img src="screenshots/461-transfer-logs-entry-employee-old-new.png" alt="Screenshot: The 'Transferred' entry in Logs showing 'Employee old' → 'Employee new'" width="900">
</p>

- Search-and-select the equipment, pick the new employee and/or new location. Double-clicking a row pre-selects that allocation and jumps straight into the dialog.
- The equipment's status stays `allocated` throughout. Internally the old allocation is closed out as **Returned** (with today's date as its actual return date) and a new **Approved** allocation is created for the new employee/location, linked back to the one it replaced.
- The new allocation's **allocated-by and approved-by** fields are the signed-in user who performed the transfer. Audited as `transferred`.

<p align="center">
<img src="screenshots/155-transfer-appears-in-returns-history.png" alt="Screenshot: The closed-out old allocation from a transfer, showing up in Returns history" width="900">
</p>

> ℹ️ Because the old allocation is closed as *returned*, it also **appears in Returns history** (and the Returns report) without a separate "transferred" flag — see [Known Limitations](#known-limitations--data-safety-notes).

---

## Decommission

*(Admin only)*

<p align="center">
<img src="screenshots/160-decommission-list.png" alt="Screenshot: Decommission list view, showing decommissioned equipment with reason/date columns" width="900">
</p>

The permanent-retirement workflow for equipment that's reached end-of-life — and it's reversible. The grid lists every equipment item whose status is `decommissioned`. Its **Reason** and **Date** columns come from that item's most recent decommission audit-log entry — so clearing the logs from Settings will blank them for existing entries.

<p align="center">
<img src="screenshots/161-decommission-dialog-empty.png" alt="Screenshot: Decommission dialog, blank state" width="700">
</p>

### Eligibility & Safeguards

Only assets that are genuinely free can be decommissioned. The dialog's equipment list (and its suggestion helper) is **pre-filtered to exclude**:

1. equipment with status `allocated`,
2. equipment with an approved allocation,
3. equipment with a pending allocation request,
4. equipment with an active or overdue Check-Out transaction.

Only items currently **In Store** or **Under Repair** are offered. If nothing qualifies, you're told upfront: *"No equipment currently eligible to decommission. Active equipment is currently allocated to employees or checked out. Please process returns before decommissioning."*

The same checks run again against the database when you save — both in the dialog and in the screen itself — so an item can never be decommissioned while under someone's custody, even if its state changed while the dialog was open. Process the return first, then decommission.

<p align="center">
<img src="screenshots/470-decommission-equipment-dropdown-eligible-only.png" alt="Screenshot: Decommission dialog equipment dropdown listing only eligible items (In Store / Under Repair)" width="700">
</p>

<p align="center">
<img src="screenshots/471-decommission-no-eligible-equipment-message.png" alt="Screenshot: The 'No equipment currently eligible to decommission' message" width="700">
</p>

<p align="center">
<img src="screenshots/472-decommission-blocked-save-guard.png" alt="Screenshot: Save-time guard blocking an item that became allocated or checked out while the dialog was open" width="700">
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

<p align="center">
<img src="screenshots/473-decommission-logs-entry-reason-notes.png" alt="Screenshot: The 'Decommission Approved' entry in Logs with reason and notes in the new value" width="900">
</p>

- Choose a **reason**: Damaged, Obsolete, Beyond Repair, End of Life, Lost, or Other — with an optional notes field.
- Confirming sets the item's status to `decommissioned` and audits `decommission_approved` (old status → *reason — notes*). A red toast confirms.

### Restore

<p align="center">
<img src="screenshots/166-decommission-restore-button.png" alt="Screenshot: Restore button in the Decommission toolbar" width="700">
</p>

<p align="center">
<img src="screenshots/167-decommission-restore-confirmation.png" alt="Screenshot: Restore confirmation prompt — 'set back to In Store?'" width="700">
</p>

<p align="center">
<img src="screenshots/474-decommission-restore-no-selection-toast.png" alt="Screenshot: Teal info toast when Restore is clicked with no row selected" width="500">
</p>

<p align="center">
<img src="screenshots/475-decommission-restore-success-toast.png" alt="Screenshot: Teal success toast after restoring, grid reloaded" width="500">
</p>

<p align="center">
<img src="screenshots/168-decommission-restore-logs-entry.png" alt="Screenshot: 'Restored from Decommission' teal-badged entry in the Logs screen" width="900">
</p>

- The teal **↩ Restore** button requires a selected row (otherwise a teal info toast), then asks *"Are you sure you want to set '{name}' ({code}) back to In Store?"*.
- On confirm the item's status goes straight back to **In Store** and a **"Restored from Decommission"** entry (`decommission_undone`, lighter-teal badge) is written with a *Source Screen: Decommission Screen* tag.

---

## Deleted Items

*(Admin only)*

<p align="center">
<img src="screenshots/180-deleted-items-list.png" alt="Screenshot: Deleted Items list view — soft-deleted equipment, most recent first" width="900">
</p>

A recycle bin for equipment removed via the Equipment screen's Delete button — listed most recently deleted first. Columns: Equipment Code, QR Code, Procurement Type, Name, Make, Model, Serial Number, Category, Purchase Date. Nothing is permanently lost until explicitly confirmed here.

<p align="center">
<img src="screenshots/181-deleted-items-checkbox-select-header.png" alt="Screenshot: Gmail-style checkbox select header with attached All/None menu" width="700">
</p>

<p align="center">
<img src="screenshots/490-deleted-items-select-all-none-menu.png" alt="Screenshot: The ▾ menu beside the header checkbox offering All and None" width="500">
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

<p align="center">
<img src="screenshots/491-deleted-items-restored-item-back-in-equipment.png" alt="Screenshot: A restored item reappearing in the active Equipment list" width="900">
</p>

<p align="center">
<img src="screenshots/492-deleted-items-empty-state.png" alt="Screenshot: The Deleted Items screen when nothing has been deleted" width="900">
</p>

- **Restore** — brings item(s) back to the active Equipment list exactly as they were (flag and deletion date cleared; purple toast). Not written to the audit log.
- **Permanent Delete** — erases the selected item(s) from the database entirely. Cannot be undone. Not written to the audit log. Fails outright if the item still has related records (allocations, transactions, logs, maintenance records) without cascade rules.
- A checkbox-selection header (with an attached All/None menu) supports bulk operations — the same pattern as the Allocation screen.
- The grid is fully paginated (**25 / 50 / 100 / 250 / 500 / 1000** rows per page, default 25) with Prev / Next and a row-count label.
- **Double-clicking a row** opens a small Restore / Delete / Cancel dialog for that single item.

---

## Logs (Audit Trail)

<p align="center">
<img src="screenshots/190-logs-list.png" alt="Screenshot: Logs / Audit Trail list view, color-coded by action type" width="900">
</p>

A searchable, sortable, filterable, chronological record of key actions taken across the app. Filtering, sorting and paging all run **server-side** before the page is fetched.

### Toolbar & Header Controls

<p align="center">
<img src="screenshots/500-logs-toolbar-controls.png" alt="Screenshot: The Logs toolbar — Refresh, Export, search bar, Live indicator, Density, Reset" width="900">
</p>

<p align="center">
<img src="screenshots/191-logs-search.png" alt="Screenshot: Logs search box with fuzzy-matched results" width="700">
</p>

<p align="center">
<img src="screenshots/501-logs-search-autocomplete-suggestions.png" alt="Screenshot: Debounced search autocomplete suggestions in the Logs search bar" width="700">
</p>

<p align="center">
<img src="screenshots/502-logs-live-indicator-pkt.png" alt="Screenshot: The '● Live – updated [time]' status indicator in Pakistan Time" width="500">
</p>

<p align="center">
<img src="screenshots/503-logs-density-selector-options.png" alt="Screenshot: The Density selector — Compact / Comfortable / Cozy" width="500">
</p>

<p align="center">
<img src="screenshots/504-logs-density-compact-vs-cozy.png" alt="Screenshot: The same Logs grid at Compact vs Cozy density" width="900">
</p>

<p align="center">
<img src="screenshots/505-logs-refresh-button.png" alt="Screenshot: Refresh clearing the search and returning to page 1" width="500">
</p>

<p align="center">
<img src="screenshots/506-logs-reset-button-all-filters-cleared.png" alt="Screenshot: Reset restoring All, All users, All time and default sort" width="900">
</p>

- **Refresh** — clears the search, returns to page 1, and refreshes both the data and the autocomplete suggestions.
- **Search bar** — rounded, debounced, real-time search with autocomplete. Matches fuzzily or exactly on equipment code, QR code, user name and the old/new values.
- **● Live – updated [time]** — shows when the query last ran, in Pakistan Time (PKT).
- **Density** — Compact, Comfortable, or Cozy row spacing.
- **Reset** — clears the search, puts the category pills back to *All*, the user filter to *All users*, the date range to *All time*, the sort to newest-first, and returns to page 1.
- **Export** — see [Export](#export).

### Action Category Filter Pills

<p align="center">
<img src="screenshots/507-logs-category-pills-row.png" alt="Screenshot: The category pill row — All, Created, Updated, Allocation, Check In/Out, Transfers, Decommission, Deleted" width="900">
</p>

<p align="center">
<img src="screenshots/508-logs-pill-created-active.png" alt="Screenshot: The Created pill active (dark green), grid filtered" width="900">
</p>

<p align="center">
<img src="screenshots/509-logs-pill-updated-active.png" alt="Screenshot: The Updated pill active (yellow), grid filtered" width="900">
</p>

<p align="center">
<img src="screenshots/510-logs-pill-allocation-active.png" alt="Screenshot: The Allocation pill active (orange), grid filtered" width="900">
</p>

<p align="center">
<img src="screenshots/511-logs-pill-checkinout-active.png" alt="Screenshot: The Check In/Out pill active (dark blue, full label visible), grid filtered" width="900">
</p>

<p align="center">
<img src="screenshots/512-logs-pill-transfers-active.png" alt="Screenshot: The Transfers pill active (deep purple), grid filtered" width="900">
</p>

<p align="center">
<img src="screenshots/513-logs-pill-decommission-active.png" alt="Screenshot: The Decommission pill active (red, full label visible), grid filtered" width="900">
</p>

<p align="center">
<img src="screenshots/514-logs-pill-deleted-active.png" alt="Screenshot: The Deleted pill active (deep red), grid filtered" width="900">
</p>

A second toolbar row of quick-filter pills; the active pill takes on the same colour as the matching badges in the grid.

| Pill | Colour | Filters |
|---|---|---|
| **All** | Charcoal `#1F2937` | Clears action-type filtering |
| **Created** | Dark green `#1B5E20` | `equipment_created`, `employee_created`, `location_created` |
| **Updated** | Yellow `#CA8A04` | `equipment_updated`, `employee_updated`, `location_updated`, Expected Return Date Updated |
| **Allocation** | Orange `#EA580C` | `allocation_requested`, `_approved`, `_rejected`, `_returned`, `_overdue`, `return_undone`, Allocation Request Deleted |
| **Check In/Out** | Dark blue `#01579B` | `checked_out`, `checked_in` |
| **Transfers** | Deep purple `#4A148C` | `transferred` |
| **Decommission** | Red `#D32F2F` | `decommission_requested`, `_approved`, `_undone`, `_rejected` |
| **Deleted** | Deep red `#B71C1C` | `equipment_deleted`, `employee_deleted`, `location_deleted`, `equipment_restored`, `settings_data_delete` |

### User & Date Range Filters

<p align="center">
<img src="screenshots/515-logs-user-filter-dropdown.png" alt="Screenshot: The All users dropdown, listing every system user" width="500">
</p>

<p align="center">
<img src="screenshots/516-logs-date-range-dropdown.png" alt="Screenshot: The Date Range dropdown — Last 7 days / Last 30 days / All time / Custom…" width="500">
</p>

<p align="center">
<img src="screenshots/517-logs-date-range-custom-popup.png" alt="Screenshot: The inline custom date-range picker popup" width="500">
</p>

<p align="center">
<img src="screenshots/518-logs-combined-filters-active.png" alt="Screenshot: A pill, a user and a date range all applied together" width="900">
</p>

- **All users** — populated with every user in the system.
- **Date Range** — defaults to **All time**, so historical logs are immediately visible. Also offers Last 7 days, Last 30 days, and **Custom…** with an inline date-range picker.

### The Grid

<p align="center">
<img src="screenshots/192-logs-action-color-legend.png" alt="Screenshot: Logs grid showing several differently-colored action types together (green/blue/yellow/red/purple/teal/orange)" width="900">
</p>

<p align="center">
<img src="screenshots/193-logs-click-to-sort-column.png" alt="Screenshot: Logs grid column header clicked to sort, sort indicator and page reset to 1" width="900">
</p>

<p align="center">
<img src="screenshots/519-logs-pagination-controls.png" alt="Screenshot: Logs grid pagination controls" width="700">
</p>

<p align="center">
<img src="screenshots/520-logs-settings-data-deleted-red-row.png" alt="Screenshot: A '⚠ Settings Data Deleted' entry highlighted red across the whole row" width="900">
</p>

- Columns: Equipment Code, QR Code, Action (colour-coded badge), Changed By, Previous Value, New Value, Timestamp.
- **Sortable columns** — click any header to toggle ascending/descending. Sort state is preserved across page turns and search/filter changes, and the page resets to 1 on a sort change.
- **"Changed By" is the real signed-in user** for every entry written to the audit trail.
- Every action type has its own label and badge colour — see the [Audit Action Reference](#audit-action-reference).
- Some entries carry a **"Source Screen"** tag in their new value (e.g. *Allocation Screen*). The Logs screen and QR Scanner strip it from the value and show it beside the action, e.g. `Allocation Requested (via Allocation Screen)`.
- **Not logged:** equipment restore and permanent delete from Deleted Items, editing a return record, everyday user-management actions in Settings (add / edit / toggle / delete user), password changes, category add/delete, saving general Settings, and overdue Check-In/Out status updates. (User deletion, password changes and admin purges are, however, captured in the separate Security Audit Log — see below.)
- Clearing all logs or resetting application data re-inserts a single **"⚠ Settings Data Deleted"** marker entry, highlighted red across the whole row.
- The audit write is best-effort: if it ever fails, the failure is logged internally and the business action still completes.

### Security Audit Log (file-based)

<p align="center">
<img src="screenshots/194-logs-security-audit-file-example.png" alt="Screenshot: Text editor showing an excerpt of the security_audit.log file" width="900">
</p>

A second, append-only log — separate from the database — is written to `%AppData%\AssetPilot\security_audit.log`. Each line reads `yyyy-MM-dd HH:mm:ss | {Full Name} (@{username}) | {action} | {details}`. It survives both Clear All Logs and Reset Application Data, since there is no in-app way to delete it. It records:

- Clear All Logs, and Reset Application Data (including where the pre-reset backup was saved)
- User creation, deletion, status toggling and role assignment
- Self-service password resets (Forgot Password) and administrative password resets
- Your own password changes
- Organization master PIN updates and resets

### Log Detail Popup

<p align="center">
<img src="screenshots/195-logs-detail-popup.png" alt="Screenshot: Log detail popup — Before (red) panel and After (green) panel, bulleted field changes" width="700">
</p>

<p align="center">
<img src="screenshots/196-logs-detail-popup-dark-mode.png" alt="Screenshot: Log detail popup rendered in dark mode" width="700">
</p>

<p align="center">
<img src="screenshots/521-logs-detail-popup-header-fields.png" alt="Screenshot: The popup header and identity rows — Action, Changed By, When (PKT), Equipment Code / QR, Name / Make / Model" width="700">
</p>

<p align="center">
<img src="screenshots/522-logs-detail-popup-long-values-wrap.png" alt="Screenshot: A long diff string wrapping fully inside the popup with no clipping" width="700">
</p>

Double-click any row to open a detail card: a green **📋 Log Details** header strip; Action, Changed By, When (PKT), Equipment Code | QR Code and Name, Make, Model; then a **Before** panel (old value split on ` | `, one bullet per field, light red `#FFF5F5`) and an **After** panel (new value, light green `#F0FDF4`). Panels auto-size with word-wrapping — nothing is clipped — and adapt to dark mode (Before `#321E1E` / After `#19281E`).

### Export

<p align="center">
<img src="screenshots/197-logs-export-csv-toast.png" alt="Screenshot: Logs CSV export confirmation toast" width="500">
</p>

<p align="center">
<img src="screenshots/523-logs-export-csv-file-opened.png" alt="Screenshot: The exported Logs CSV opened in a spreadsheet" width="900">
</p>

- **Export** saves the active view to CSV with full audit details (current page of the filtered result).

---

## QR Scanner

<p align="center">
<img src="screenshots/200-qrscanner-main-empty.png" alt="Screenshot: QR Scanner main screen, blank/ready state with code field auto-focused" width="900">
</p>

An equipment lookup, inspection and fast-action hub. It works with a **USB barcode/QR scanner** (keyboard-wedge mode, no drivers), **manual typing**, or the PC's **webcam**. It reads equipment and log data and navigates to other screens, but does not itself change any equipment or allocation record — so it is safe to hand to any staff member purely for identifying an item and checking its status.

### Finding an Item

<p align="center">
<img src="screenshots/201-qrscanner-typing-suggestions.png" alt="Screenshot: Fuzzy suggestion dropdown appearing while typing a partial code" width="700">
</p>

<p align="center">
<img src="screenshots/530-qrscanner-code-field-autofocus.png" alt="Screenshot: The code field focused and ready as soon as the screen loads" width="700">
</p>

<p align="center">
<img src="screenshots/531-qrscanner-auto-search-debounce.png" alt="Screenshot: The search firing ~300 ms after typing stops, without pressing Enter" width="700">
</p>

- The code field is focused on load. Searching starts automatically **~300 ms after you stop typing**, or immediately on **Enter**. A fuzzy auto-suggestion dropdown helps with quick keyboard matching.
- Matching is **exact** on **QR Code, Equipment Code or Serial Number** — a partial code returns nothing.
- **No match?** Up to **3 "Did you mean:" chip suggestions** (fuzzy match against codes and names) are shown; clicking one fills the box and searches immediately.

<p align="center">
<img src="screenshots/207-qrscanner-no-match-did-you-mean.png" alt="Screenshot: No exact match — 'Did you mean:' chip suggestions shown" width="700">
</p>

<p align="center">
<img src="screenshots/532-qrscanner-did-you-mean-click-result.png" alt="Screenshot: Clicking a 'Did you mean' chip and the search running at once" width="700">
</p>

### Camera Scanning

<p align="center">
<img src="screenshots/533-qrscanner-camera-feed-live.png" alt="Screenshot: Live webcam feed with a QR code being decoded" width="900">
</p>

<p align="center">
<img src="screenshots/534-qrscanner-camera-device-selection.png" alt="Screenshot: The camera device selector with several cameras listed" width="500">
</p>

<p align="center">
<img src="screenshots/535-qrscanner-camera-freeze-unfreeze.png" alt="Screenshot: Freeze / Unfreeze frame control on the camera feed" width="700">
</p>

An integrated live webcam feed (AForge + ZXing) with **camera device selection**, **freeze / unfreeze frame** control, autofocus and continuous barcode/QR decoding. A successful decode runs the same search as typing the code.

### The Match View

<p align="center">
<img src="screenshots/202-qrscanner-match-result-hero-photo.png" alt="Screenshot: QR Scanner match result — hero photo panel plus detail card with status badge" width="900">
</p>

<p align="center">
<img src="screenshots/203-qrscanner-match-current-holder.png" alt="Screenshot: Match result showing current holder + location for an allocated item" width="900">
</p>

On a match you get:

- a **hero photo panel** — the equipment picture as a rounded-corner image, or a 🖥 placeholder on an indigo background when there isn't one;
- a **detail card** with every equipment field, current allocation (employee and location), procurement type, purchase date and a colour-coded status badge;
- a live **Warranty Expiry indicator**;
- the item's **change-history grid** (below).

| Warranty condition | Display | Colour |
|---|---|---|
| Expired | `yyyy-MM-dd  (Expired)` | Red |
| Expiring within 30 days | `yyyy-MM-dd  (Expires in Nd)` | Amber |
| Valid | `yyyy-MM-dd` | Normal text |

<p align="center">
<img src="screenshots/204-qrscanner-warranty-expired.png" alt="Screenshot: Warranty Expiry shown in red with '(Expired)' label" width="700">
</p>

<p align="center">
<img src="screenshots/205-qrscanner-warranty-expiring-soon.png" alt="Screenshot: Warranty Expiry shown in amber with '(Expires in Nd)' label" width="700">
</p>

<p align="center">
<img src="screenshots/536-qrscanner-warranty-valid-normal.png" alt="Screenshot: Warranty Expiry in normal text when still valid" width="700">
</p>

<p align="center">
<img src="screenshots/537-qrscanner-hero-photo-placeholder.png" alt="Screenshot: The 🖥 placeholder hero panel when no picture is attached" width="700">
</p>

### Equipment History

<p align="center">
<img src="screenshots/206-qrscanner-history-grid.png" alt="Screenshot: Change-history grid beneath a match result, capped at the 50 most recent entries" width="900">
</p>

<p align="center">
<img src="screenshots/538-qrscanner-history-header-showing-50-of-n.png" alt="Screenshot: The history header 'showing 50 of N' when an item has more than 50 log entries" width="900">
</p>

<p align="center">
<img src="screenshots/539-qrscanner-history-header-count-only.png" alt="Screenshot: The history header showing just the count when 50 or fewer entries exist" width="900">
</p>

<p align="center">
<img src="screenshots/540-qrscanner-history-source-screen-in-action.png" alt="Screenshot: A history row showing 'Allocation Requested (via Allocation Screen)' with a clean New Value" width="900">
</p>

<p align="center">
<img src="screenshots/217-qrscanner-history-doubleclick-log-popup.png" alt="Screenshot: Double-clicking a history row opens the Log Detail popup" width="700">
</p>

<p align="center">
<img src="screenshots/216-qrscanner-export-history-button.png" alt="Screenshot: Export History button, saving the item's log grid to CSV" width="700">
</p>

- The grid shows the item's **50 most recent** audit entries, with the title `📜 Equipment History (showing 50 of {total})` — or just `({count})` when there are 50 or fewer. Action badges use the same labels and colours as the Logs screen.
- The "Source Screen" tag is split out of the New Value and shown beside the action.
- Double-clicking a row opens the same **Log Detail popup** as the Logs screen.
- **Export History** saves the grid to `{EquipmentCode}_History_{yyyyMMdd_HHmm}.csv` (Action, Previous Value, New Value, Changed By, Timestamp), with a toast on success or failure.

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

<p align="center">
<img src="screenshots/541-qrscanner-recent-scans-click-chip.png" alt="Screenshot: Clicking a recent-scan chip re-running that search" width="700">
</p>

<p align="center">
<img src="screenshots/542-qrscanner-bulk-export-csv-opened.png" alt="Screenshot: The BulkScan CSV opened in a spreadsheet" width="900">
</p>

- **Recent Scans** — a strip of clickable chips remembering your **last 8** distinct successful scans (newest first, case-insensitive de-duplication, oldest evicted at 8).
- **Bulk Scan** — tick the checkbox and every successful scan is added to a session list; a toast confirms the running count (*"Added to bulk list (N scanned)"*). **📤 Export Bulk** saves it to `BulkScan_{yyyyMMdd_HHmm}.csv` (Equipment Code, Equipment Name, Scanned At). Turning bulk mode off clears the session list.

### Quick-Action Buttons

<p align="center">
<img src="screenshots/543-qrscanner-action-hub-buttons.png" alt="Screenshot: The fast-action hub — Check Out, Check In, View QR, View Picture, View Details, Print Label, Allocate" width="900">
</p>

<p align="center">
<img src="screenshots/211-qrscanner-view-qr-button.png" alt="Screenshot: View QR button opening the QR code image dialog" width="700">
</p>

<p align="center">
<img src="screenshots/544-qrscanner-view-qr-no-code-toast.png" alt="Screenshot: Info toast when the item has no QR code assigned" width="500">
</p>

<p align="center">
<img src="screenshots/212-qrscanner-view-qr-print-label-stub.png" alt="Screenshot: Print Label button showing the 'not wired up yet' notice" width="700">
</p>

<p align="center">
<img src="screenshots/213-qrscanner-view-picture-button.png" alt="Screenshot: View Picture button, full-screen zoomed photo" width="700">
</p>

<p align="center">
<img src="screenshots/545-qrscanner-view-picture-none-disabled.png" alt="Screenshot: The button relabelled '🖼 No Picture' and greyed out" width="500">
</p>

<p align="center">
<img src="screenshots/214-qrscanner-checkinout-nav-button.png" alt="Screenshot: Check-In/Out navigation button, jumping to that screen with the item pre-loaded" width="700">
</p>

<p align="center">
<img src="screenshots/546-qrscanner-no-equipment-loaded-warning-toast.png" alt="Screenshot: Warning toast when an action button is used before an item is scanned" width="500">
</p>

<p align="center">
<img src="screenshots/215-qrscanner-view-logs-button.png" alt="Screenshot: View Logs button, jumping to the pre-filtered Logs screen" width="700">
</p>

- **Check Out / Check In / Allocate** — shortcut buttons into those transactional operations.
- **🔍 View QR** — opens a 340×400 dialog showing the item's QR code as a scannable image plus its QR content as text, with a **Print Label** button. (Label printing from this dialog isn't wired up yet in this release — it shows a notice instead of printing. If no QR code is assigned you get an info toast.)
- **🖼 View Picture** — opens the photo in a zoomable black-background viewer; relabelled "No Picture" and disabled if none is attached.
- **🔄 Check-In/Out** — jumps to the Check In/Out screen with this item pre-searched.
- **📜 View Logs** — jumps to the Logs screen pre-filtered to this item.
- **View Details** — opens the item's details.
- Any of these used before an item is loaded shows a warning toast.

---

## Check In / Check Out

<p align="center">
<img src="screenshots/230-checkinout-main-empty.png" alt="Screenshot: Check In/Out main screen, blank/ready state" width="900">
</p>

A lightweight, no-approval loan tracker for short-term, informal borrows — kept in a separate `transactions` table from the formal Allocation workflow (e.g. borrowing a projector for a meeting).

### Finding the Item

<p align="center">
<img src="screenshots/570-checkinout-search-suggestions.png" alt="Screenshot: Fuzzy suggestion dropdown while typing on the Check In/Out screen" width="700">
</p>

<p align="center">
<img src="screenshots/231-checkinout-autotab-checkout.png" alt="Screenshot: Screen auto-opening the Check-Out tab for an available item" width="900">
</p>

<p align="center">
<img src="screenshots/232-checkinout-autotab-checkin.png" alt="Screenshot: Screen auto-opening the Check-In tab for a currently checked-out item" width="900">
</p>

Search works like the QR Scanner: ~300 ms debounce, fuzzy suggestions, and **exact** matching on QR Code, Equipment Code or Serial Number. The screen then **auto-selects the right tab** — an `allocated` item opens the **Check-In** tab; anything else opens **Check-Out**. The item's active (Pending or Overdue) transaction, if any, is loaded for check-in.

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

<p align="center">
<img src="screenshots/571-checkinout-checkout-blocked-not-in-store.png" alt="Screenshot: Check-Out blocked because the item's status isn't In Store" width="700">
</p>

<p align="center">
<img src="screenshots/572-checkinout-checkout-issued-to-required.png" alt="Screenshot: Check-Out blocked because 'Issued To' is empty" width="700">
</p>

<p align="center">
<img src="screenshots/573-checkinout-checkout-blocked-active-transaction.png" alt="Screenshot: Check-Out refused because the item already has a Pending/Overdue transaction" width="700">
</p>

<p align="center">
<img src="screenshots/574-checkinout-checkout-success-blue-toast.png" alt="Screenshot: Blue toast after a successful Check-Out" width="500">
</p>

<p align="center">
<img src="screenshots/575-checkinout-checkout-logs-entry.png" alt="Screenshot: The 'Checked Out' entry in Logs — 'in_store' → 'Issued to: …'" width="900">
</p>

- Linked **Location** and **Room** dropdowns: Location lists the distinct location names from the Locations module (alphabetical); Room cascades from the chosen Location (disabled if it has none). **Department is auto-filled** as `{Location} - Room {number} ({name})` — you never type it.
- **Validation order:** item status must be `in_store` → no pending/approved formal allocation → *Issued To* filled → a **Location** selected. The service then also refuses if the item already has a Pending or Overdue transaction.
- Records who it was issued to, the operator processing it (pre-filled with your own name), expected return date, condition beforehand, and remarks. Marks the equipment `allocated` and the transaction `Pending`; audits `checked_out` (in_store → *Issued to: {name}*).

### Check-In

<p align="center">
<img src="screenshots/240-checkinout-checkin-form-empty.png" alt="Screenshot: Check-In form, blank state" width="700">
</p>

<p align="center">
<img src="screenshots/241-checkinout-checkin-form-filled.png" alt="Screenshot: Check-In form filled — condition after, remarks" width="700">
</p>

<p align="center">
<img src="screenshots/576-checkinout-checkin-success-toast.png" alt="Screenshot: Blue toast after a successful Check-In" width="500">
</p>

<p align="center">
<img src="screenshots/577-checkinout-checkin-logs-entry.png" alt="Screenshot: The 'Checked In' entry in Logs — 'allocated' → 'Condition: …'" width="900">
</p>

- Stamps today as the actual return date, marks the transaction `Completed`, and records condition-after plus remarks. Marks the equipment `in_store` and audits `checked_in`. Note: this updates the **transaction's** condition record, **not** the equipment's main Condition Grade field.

### Overdue Transactions

<p align="center">
<img src="screenshots/242-checkinout-overdue-flag.png" alt="Screenshot: A pending transaction shown flagged as Overdue" width="900">
</p>

Pending transactions whose expected return date has passed are marked **Overdue** by a status update (`TransactionService.UpdateStatus`). Overdue loans keep showing in the Allocation list's Check-In/Out rows and in the history with a red Overdue status, and are checked by the Decommission and Delete safeguards. The status change itself is not audited.

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

<p align="center">
<img src="screenshots/578-checkinout-history-checkout-checkin-rows-colours.png" alt="Screenshot: A Check-Out row (blue) and its matching Check-In row (green) in the history grid" width="900">
</p>

<p align="center">
<img src="screenshots/579-checkinout-history-status-colours.png" alt="Screenshot: Status colours — Pending amber, Completed green, Overdue red" width="900">
</p>

<p align="center">
<img src="screenshots/580-checkinout-history-back-button.png" alt="Screenshot: The ◀ Back button returning to the scan view" width="700">
</p>

- The last ten transactions for the loaded item appear on the scan screen itself (display-only).
- The **🕘 History** button opens a dedicated full-screen history of **every** transaction for every item. Each transaction expands into up to two rows — a blue **Check-Out** entry and, once returned, a green **Check-In** entry — re-sorted newest-first by effective time.
- Columns: Equipment Code, Equipment Name, Type, Issued To, Department, Operator, Expected Return, Status (Pending amber / Completed green / Overdue red), Condition Before, Condition After, Date, Timestamp.
- **Search** (300 ms debounce): code/QR substring match first, then fuzzy match on Equipment Name, Issued To, Department, Operator and Status, re-ranked by score.
- **Pagination:** page size 25 / 50 / 100 / 250 / 500 / 1000, Prev / Next, `Page N of M` and `Showing X of Y records`.
- **📤 Export** saves the current page to `CheckInOutHistory_{yyyyMMdd_HHmm}.csv`. **◀ Back** returns to the scan view.

---

## Reports

<p align="center">
<img src="screenshots/250-reports-main-screen.png" alt="Screenshot: Reports screen, default empty state with developer watermark" width="900">
</p>

A modern analytics and reporting screen: real-time KPI cards, five operational report tabs, a single-row filter toolbar, column and sort controls, pagination, and four export formats. Everything here is read-only — nothing writes to the database. Your organization's name (Settings → General) appears at the top of exports.

### Default State

<p align="center">
<img src="screenshots/600-reports-default-kpi-dashes.png" alt="Screenshot: The four KPI cards showing neutral '—' placeholders on first open" width="900">
</p>

Opening Reports loads **no data**. The KPI cards show `—`, the grid is empty, pagination is **hidden**, and the bottom bar shows the developer watermark *"Designed and Developed by 🐙@shayan-qazmi/📷@shayanqazmi"*. The moment you pick a report tab, the database is queried, the KPIs compute, the grid fills, and the watermark gives way to the pagination bar (e.g. `Showing 1-25 of 385`).

<p align="center">
<img src="screenshots/601-reports-first-report-loaded-pagination-appears.png" alt="Screenshot: After choosing a tab — KPIs populated, grid filled, pagination bar replacing the watermark" width="900">
</p>

### The Five Report Tabs

| Tab | Colour | What it shows |
|---|---|---|
| **Equipment ▾** | Indigo `#4F46E5` | Full equipment register — code, name, category, make/model, assignment, location, status. A split-button category picker (`All Categories` or one category) lives in the tab so the filter bar stays on one row |
| **Employee** | Purple `#7C3AED` | Active workforce holdings: total items ever allocated vs. items currently held, per employee |
| **Allocation** | Amber/Orange `#EA580C` | Active and pending allocations — employee, location, dates, expected returns |
| **Returns** | Royal Blue `#4338CA` | Return log and custody-duration analysis (flags holds over 30 days); unifies allocation returns and completed Check-In/Out borrows, with source badges *Allocation*, *Transferred*, *Check In/Out* |
| **Check In/Out** | Sky Blue `#0284C7` | Check-out / check-in borrow transaction ledger |

<p align="center">
<img src="screenshots/251-reports-equipment-inventory-preview.png" alt="Screenshot: Equipment report tab loaded" width="900">
</p>

<p align="center">
<img src="screenshots/252-reports-equipment-inventory-category-filter.png" alt="Screenshot: Category picker on the Equipment tab applied to the report" width="700">
</p>

<p align="center">
<img src="screenshots/253-reports-employee-summary-preview.png" alt="Screenshot: Employee report tab loaded" width="900">
</p>

<p align="center">
<img src="screenshots/254-reports-active-allocations-preview.png" alt="Screenshot: Allocation report tab loaded" width="900">
</p>

<p align="center">
<img src="screenshots/255-reports-returns-history-preview.png" alt="Screenshot: Returns report tab loaded, with a >30-day duration flagged" width="900">
</p>

<p align="center">
<img src="screenshots/602-reports-checkinout-tab-loaded.png" alt="Screenshot: Check In/Out report tab loaded" width="900">
</p>

<p align="center">
<img src="screenshots/603-reports-equipment-category-split-button-menu.png" alt="Screenshot: The Equipment ▾ split-button menu listing All Categories and each category" width="500">
</p>

<p align="center">
<img src="screenshots/604-reports-returns-source-badges.png" alt="Screenshot: Returns tab showing the Allocation / Transferred / Check In/Out source badges" width="900">
</p>

### Single-Row Filter Toolbar

<p align="center">
<img src="screenshots/605-reports-filter-toolbar-full.png" alt="Screenshot: The whole single-row filter toolbar" width="900">
</p>

<p align="center">
<img src="screenshots/606-reports-apply-button-emerald.png" alt="Screenshot: The emerald Apply button at the far left" width="300">
</p>

<p align="center">
<img src="screenshots/607-reports-date-pills.png" alt="Screenshot: Date quick-filter pills — Today, 7 days, 30 days, This year, Custom…" width="700">
</p>

<p align="center">
<img src="screenshots/608-reports-custom-date-dialog.png" alt="Screenshot: The Custom date-range dialog with From, To and an unclipped Apply Range button" width="500">
</p>

<p align="center">
<img src="screenshots/609-reports-location-dropdown.png" alt="Screenshot: The All locations dropdown" width="500">
</p>

<p align="center">
<img src="screenshots/610-reports-status-dropdown.png" alt="Screenshot: The Any status dropdown — allocated, in store, under repair, decommissioned, pending" width="500">
</p>

<p align="center">
<img src="screenshots/611-reports-live-search-filter-rows.png" alt="Screenshot: Live 'Filter rows…' search narrowing the grid" width="700">
</p>

<p align="center">
<img src="screenshots/612-reports-reset-button.png" alt="Screenshot: Reset restoring default filters, columns and sort" width="700">
</p>

All controls fit on **one row**:

1. **Apply** — high-contrast emerald button (`#10B981`) at the far left.
2. **Date quick-filter pills** — `Today`, `7 days`, `30 days`, `This year`, `Custom…`. The filter applies to each report's own date (purchase date, allocation date, actual return date, or transaction date). **Custom…** opens a dialog (340 × 230) with *From Date*, *To Date* and a fully visible *Apply Range* button.
3. A vertical divider.
4. **All locations ▾** — populated from the active locations in the database.
5. **Any status ▾** — `allocated`, `in_store`, `under_repair`, `decommissioned`, or `pending`.
6. **Filter rows…** — instant client-side text filter across all visible columns.
7. **Reset** — restores the default filters (date range, all locations, any status, empty search), unhides all columns and clears grid sorting.

### KPI Cards & Status Badges

<p align="center">
<img src="screenshots/613-reports-kpi-cards-populated.png" alt="Screenshot: The four populated KPI cards with their accent bars" width="900">
</p>

<p align="center">
<img src="screenshots/614-reports-status-badges.png" alt="Screenshot: In Store (green), Allocated (amber), Decommissioned (red) and Under Repair (light blue) badges in the grid" width="700">
</p>

| Card | Accent | Counts |
|---|---|---|
| **Total rows** | Indigo `#7C3AED` | Records matching the current filters |
| **Allocated** | Amber `#F59E0B` | Assets currently assigned or checked out |
| **In store** | Emerald `#10B981` | Assets available in inventory |
| **Needs attention** | Red `#EF4444` | Assets that are `under_repair`, `decommissioned`, or overdue |

Status badges: **Under Repair** (shown with friendly text, no underscore) — light blue `#E0F2FE` / `#0284C7`; **In Store** — `#DCFCE7` / `#15803D`; **Allocated** — `#FEF3C7` / `#B45309`; **Decommissioned** — `#FEE2E2` / `#B91C1C`.

### Columns, Sorting & Pagination

<p align="center">
<img src="screenshots/615-reports-columns-menu-checkboxes.png" alt="Screenshot: The Columns menu with a checkbox per column" width="500">
</p>

<p align="center">
<img src="screenshots/616-reports-column-hidden.png" alt="Screenshot: A report with a column hidden via the Columns menu" width="900">
</p>

<p align="center">
<img src="screenshots/617-reports-sort-menu.png" alt="Screenshot: The Sort ▾ menu — Ascending / Descending" width="500">
</p>

<p align="center">
<img src="screenshots/618-reports-column-header-sort.png" alt="Screenshot: A column header clicked to sort the report" width="900">
</p>

<p align="center">
<img src="screenshots/619-reports-page-size-selector.png" alt="Screenshot: The 25 / 50 / 100 per-page selector with Prev / Next and the position summary" width="700">
</p>

- **Columns** — a checkbox dropdown to show or hide any column.
- **Sort ▾** and column-header clicks — ascending / descending.
- **Pagination** — 25 / 50 / 100 per page, Prev / Next, and a record-position summary.

### Export

<p align="center">
<img src="screenshots/620-reports-export-buttons.png" alt="Screenshot: The four export buttons — Print, Export PDF, Export Excel, Export CSV" width="700">
</p>

<p align="center">
<img src="screenshots/256-reports-export-pdf-print-preview.png" alt="Screenshot: PDF/print preview generated from a report, multi-page" width="900">
</p>

<p align="center">
<img src="screenshots/257-reports-export-pdf-printer-selection.png" alt="Screenshot: Printer selection dialog, choosing a PDF printer driver to save as PDF" width="700">
</p>

<p align="center">
<img src="screenshots/258-reports-export-excel-result.png" alt="Screenshot: Excel export result — company-name header row, bold green column headers" width="900">
</p>

<p align="center">
<img src="screenshots/621-reports-export-csv-opened.png" alt="Screenshot: The exported Reports CSV opened in a spreadsheet" width="900">
</p>

<p align="center">
<img src="screenshots/622-reports-print-dialog.png" alt="Screenshot: The native Print dialog launched from the Print button" width="700">
</p>

- **Print** — multi-page GDI+ printing through `PrintDocument` with the native print preview.
- **Export PDF** — uses the same print subsystem: you get a formatted preview and save it with a PDF printer driver (e.g. "Microsoft Print to PDF").
- **Export Excel** — a styled `.xlsx` (ClosedXML) with a bold company header, colour-coded table headers and auto-fitted column widths.
- **Export CSV** — standard comma-separated values with RFC 4180 quoting and character escaping.

---

## Settings

*(Admin only — this entire module is hidden from the sidebar for Operator accounts)*

<p align="center">
<img src="screenshots/260-settings-overview-tabs.png" alt="Screenshot: Settings screen overview, showing all left-sidebar tabs" width="900">
</p>

<p align="center">
<img src="screenshots/640-settings-sidebar-slide-in-animation.png" alt="Screenshot: The Settings sidebar sliding in on load" width="500">
</p>

<p align="center">
<img src="screenshots/641-settings-sidebar-hover-fade.png" alt="Screenshot: A sidebar item fading smoothly between inactive, hover and active colours" width="500">
</p>

The Settings screen is built entirely in code. Its own left sidebar slides in on load, fades smoothly between inactive / hover / active colours, and shows the active tab as a green rounded pill. Tabs: **General, Backup / Restore, Users, Change Password, Printing, Categories, System Configuration** — plus the offline [Recovery PIN](#offline-recovery-pin-configuration) dialog.

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

- **Company Name** — shown across reports and printed labels. The Save button stays disabled until the field actually differs from the loaded value, and a small inline **✓ Saved** pill confirms the save. A blank company name is rejected.
- On first run with no settings row, defaults are created: company name *AssetPilot*, label size 62 × 29 mm.
- **System info card** — the installed app version, the date of the last database backup, and the connected database (`name · server`).
- The dark/light theme is toggled from the Dashboard and stored per workstation — it is separate from these settings.

#### Danger Zone

<p align="center">
<img src="screenshots/265-settings-danger-zone-panel.png" alt="Screenshot: Danger Zone panel — Clear all logs and Reset application data buttons" width="700">
</p>

<p align="center">
<img src="screenshots/266-settings-danger-zone-clear-logs-confirmation.png" alt="Screenshot: Clear all logs confirmation prompt" width="700">
</p>

<p align="center">
<img src="screenshots/642-settings-clear-logs-success-toast-count.png" alt="Screenshot: Toast reporting how many log rows were deleted" width="500">
</p>

<p align="center">
<img src="screenshots/267-settings-danger-zone-reset-type-to-confirm.png" alt="Screenshot: Reset Application Data — 'type RESET to confirm' input field" width="700">
</p>

<p align="center">
<img src="screenshots/268-settings-danger-zone-reset-confirmed.png" alt="Screenshot: Reset Application Data confirmation dialog, confirm button now enabled" width="700">
</p>

<p align="center">
<img src="screenshots/643-settings-reset-pre-backup-running.png" alt="Screenshot: The automatic pre-reset safety backup running before anything is deleted" width="700">
</p>

<p align="center">
<img src="screenshots/644-settings-reset-backup-folder.png" alt="Screenshot: The Backups folder holding the pre-reset .sql / .json snapshot" width="700">
</p>

<p align="center">
<img src="screenshots/645-settings-reset-success-summary-toast.png" alt="Screenshot: Summary toast after a completed reset" width="500">
</p>

<p align="center">
<img src="screenshots/646-settings-reset-rollback-error.png" alt="Screenshot: An error mid-reset triggering a full rollback with nothing deleted" width="700">
</p>

<p align="center">
<img src="screenshots/269-settings-danger-zone-settings-data-deleted-marker.png" alt="Screenshot: '⚠ Settings Data Deleted' red-highlighted marker row in the Logs grid, written after a clear/reset" width="900">
</p>

<p align="center">
<img src="screenshots/647-settings-security-audit-log-after-reset.png" alt="Screenshot: The security_audit.log entries written for a clear/reset" width="900">
</p>

- **Clear all logs** — after a confirmation, permanently wipes the audit trail (no undo) and shows a toast with the deleted count. A single "⚠ Settings Data Deleted" marker entry is re-inserted afterward (full row highlighted red in Logs) and the action is separately recorded in the Security Audit Log. It also blanks the reason/date shown on already-decommissioned items, since those are read from the log.
- **Reset application data** — you must type **RESET** to enable the confirm button. Then:
  1. An **automatic pre-reset safety backup** is taken — a full `.sql` dump through `mysqldump` (secure option file), or, if `mysqldump` isn't available on the workstation, a structured **JSON snapshot** of all eight tables (`pre_reset_backup_{timestamp}.json`). Backups are archived in `%AppData%\AssetPilot\Backups\` and noted in the Security Audit Log.
  2. All eight tables are deleted inside **one atomic database transaction**, in foreign-key-safe order: Equipment Logs → Equipment Location Histories → Maintenance Records → Transactions (Check In/Out) → Allocations → Equipment → Employees → Locations. Any error rolls everything back, so a reset can never leave the database half-deleted.
  3. The "⚠ Settings Data Deleted" marker and a Security Audit Log entry are written.
  Users, roles, categories and general settings are **not** touched.

### Backup / Restore

<p align="center">
<img src="screenshots/270-settings-backup-restore-tab.png" alt="Screenshot: Settings — Backup/Restore tab overview" width="900">
</p>

<p align="center">
<img src="screenshots/271-settings-backup-credentials-prompt.png" alt="Screenshot: Backup — confirm database host/name/username/password prompt" width="700">
</p>

<p align="center">
<img src="screenshots/648-settings-backup-locate-mysqldump-dialog.png" alt="Screenshot: The file dialog asking you to locate mysqldump.exe when it can't be found automatically" width="700">
</p>

<p align="center">
<img src="screenshots/272-settings-backup-file-save-dialog.png" alt="Screenshot: Backup — choose save location file dialog" width="700">
</p>

<p align="center">
<img src="screenshots/273-settings-backup-success-toast.png" alt="Screenshot: Backup success toast notification" width="500">
</p>

<p align="center">
<img src="screenshots/649-settings-backup-failure-toast.png" alt="Screenshot: Backup failure toast" width="500">
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

<p align="center">
<img src="screenshots/650-settings-restore-failure-toast.png" alt="Screenshot: Restore failure toast" width="500">
</p>

- **Backup** — prompts for the database Host, Database, Username and Password, then runs `mysqldump` and saves a `.sql` file. The password is passed through a **secure temporary option file** (`--defaults-extra-file`), never as a command-line argument, so it can't be read from the process list. The timestamp is saved to `lastbackup.txt` and shown on the General tab. `mysqldump.exe` is found via a cached path (`mysqltools.txt`), then common install folders (XAMPP, MySQL 8.0 / 8.4, WAMP64), then an *Open File* prompt.
- **Restore** — pick a `.sql` file, confirm the destructive overwrite, and the file is piped into `mysql.exe`. **Current data is overwritten entirely.** Restart the app afterward.
- Success and failure are shown as toasts.

### Users

<p align="center">
<img src="screenshots/277-settings-users-list.png" alt="Screenshot: Settings — Users list, active/inactive status dots" width="900">
</p>

The Users grid shows ID, Username, Full Name, Email, Role and Status (🟢 Active / 🔴 Inactive, colour-coded; inactive rows are muted grey). Double-clicking a row opens a **User detail popup**.

<p align="center">
<img src="screenshots/283-settings-users-detail-popup.png" alt="Screenshot: User detail popup — avatar, username, full name, email, role, status" width="700">
</p>

#### Add / Edit User

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
<img src="screenshots/651-settings-users-recovery-banner-warning.png" alt="Screenshot: The '⚠️ Enter either PIN or Email or both (Mandatory for recovery)' banner" width="700">
</p>

<p align="center">
<img src="screenshots/652-settings-users-recovery-pin-field.png" alt="Screenshot: The Recovery PIN field (4–8 digits) next to Work Email" width="700">
</p>

<p align="center">
<img src="screenshots/653-settings-users-no-recovery-credential-blocked.png" alt="Screenshot: Save blocked because neither a Work Email nor a Recovery PIN was entered" width="700">
</p>

<p align="center">
<img src="screenshots/654-settings-users-duplicate-username-or-email-error.png" alt="Screenshot: Error for a duplicate username or email" width="700">
</p>

<p align="center">
<img src="screenshots/655-settings-users-password-required-add-mode.png" alt="Screenshot: Password required in Add mode / left blank to keep unchanged in Edit mode" width="700">
</p>

| Field | Rules |
|---|---|
| Username | Required, unique, **locked** after creation |
| Full Name | Required |
| Work Email | Valid email format, unique across users |
| Recovery PIN | 4–8 digit numeric PIN for offline password resets (stored securely, per user) |
| New / Confirm Password | Required when adding; blank = keep the existing password when editing. Hashed with BCrypt |
| Role | Required — Admin or Operator, from the roles table |
| Profile Picture | Optional (JPG / PNG / BMP / GIF), stored in the user-pictures folder |
| Is Active Account | Checkbox |

**Mandatory account-recovery credential policy:** every account must have **at least one** of Work Email or Recovery PIN (both is fine); saving is blocked if both are empty. This is what makes the [Forgot Password](#forgot-password-offline-self-service-reset) screen work for everyone.

#### User Actions

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

<p align="center">
<img src="screenshots/656-settings-users-add-success-toast.png" alt="Screenshot: Toast after adding a user" width="500">
</p>

<p align="center">
<img src="screenshots/657-settings-users-edit-yellow-toast.png" alt="Screenshot: Yellow Edit toast after editing a user" width="500">
</p>

<p align="center">
<img src="screenshots/658-settings-users-delete-red-toast.png" alt="Screenshot: Red Delete toast after deleting a user" width="500">
</p>

<p align="center">
<img src="screenshots/659-settings-users-inactive-user-cannot-login.png" alt="Screenshot: An inactive account being refused at login" width="700">
</p>

- **Toggle Active** — flips the account's active flag after a confirmation; you cannot deactivate your own account. Inactive users can't sign in.
- **Delete User** — removes the user's role links, then the user; permanent; you cannot delete your own account; shown with a toast.
- ⚠️ None of the actions on this tab are written to the on-screen (database) audit trail — but **every one of them is recorded in the Security Audit Log** file.

### Change Password

<p align="center">
<img src="screenshots/288-settings-change-password-tab.png" alt="Screenshot: Settings — Change Password tab, three password fields" width="900">
</p>

<p align="center">
<img src="screenshots/289-settings-change-password-strength-weak.png" alt="Screenshot: Live strength meter showing Weak" width="700">
</p>

<p align="center">
<img src="screenshots/660-settings-change-password-strength-medium.png" alt="Screenshot: Live strength meter showing Medium" width="700">
</p>

<p align="center">
<img src="screenshots/290-settings-change-password-strength-strong.png" alt="Screenshot: Live strength meter showing Strong" width="700">
</p>

<p align="center">
<img src="screenshots/661-settings-change-password-eye-reveal.png" alt="Screenshot: The eye icon revealing the password in a field" width="700">
</p>

<p align="center">
<img src="screenshots/291-settings-change-password-mismatch-error.png" alt="Screenshot: Error — new password and confirmation don't match" width="700">
</p>

<p align="center">
<img src="screenshots/662-settings-change-password-too-short-error.png" alt="Screenshot: Error — new password shorter than 6 characters" width="700">
</p>

<p align="center">
<img src="screenshots/663-settings-change-password-empty-fields-error.png" alt="Screenshot: Error — a field left empty" width="700">
</p>

<p align="center">
<img src="screenshots/292-settings-change-password-wrong-current.png" alt="Screenshot: Error — current password is incorrect" width="700">
</p>

<p align="center">
<img src="screenshots/664-settings-change-password-success-toast.png" alt="Screenshot: Success toast after changing the password" width="500">
</p>

<p align="center">
<img src="screenshots/293-settings-change-password-security-tips-panel.png" alt="Screenshot: Security Tips panel alongside the form" width="700">
</p>

- Requires the **current password**, a **new password (at least 6 characters)**, and a matching confirmation. The current password is verified with BCrypt (with legacy fallback); the new one is hashed with BCrypt and `PasswordChangedAt` / `UpdatedAt` are stamped.
- All three fields are rounded panels with an **eye (👁) icon** to reveal/hide.
- A **live strength meter** (coloured bar + *Weak / Medium / Strong*) appears under the new-password box as you type. Scoring: length ≥ 4 (+1), ≥ 8 (+1), mixed case (+1), a digit (+1), a symbol (+1). The meter is advisory.
- A **Security Tips** card on the right lists best-practice guidance. The change is recorded in the Security Audit Log.

### Offline Recovery PIN Configuration

<p align="center">
<img src="screenshots/665-settings-recovery-pin-dialog.png" alt="Screenshot: The Recovery PIN dialog showing the organization master PIN" width="700">
</p>

<p align="center">
<img src="screenshots/666-settings-recovery-pin-custom-saved.png" alt="Screenshot: Saving a custom organization master PIN" width="700">
</p>

<p align="center">
<img src="screenshots/667-settings-recovery-pin-reset-default-button.png" alt="Screenshot: The Reset to Default button" width="700">
</p>

<p align="center">
<img src="screenshots/668-settings-recovery-pin-security-log-entry.png" alt="Screenshot: The Security Audit Log entry for a master-PIN change or reset" width="900">
</p>

Admins can view and configure the **organization-wide master recovery PIN** that works for any account on the Forgot Password screen:

- **Default master PIN:** `123456` (numeric, so it satisfies the login screen's digits-only rule). **Change it before going live.**
- **Custom PIN:** stored at `%AppData%\AssetPilot\recovery_pin.txt`. A **Reset to Default** button restores `123456`.
- **Per-user recovery PINs** are kept in `%AppData%\AssetPilot\user_pins.json` (thread-safe file serialization) and are set from the Add / Edit User form.
- Every PIN change or reset is recorded in the Security Audit Log.

### Printing

<p align="center">
<img src="screenshots/294-settings-printing-tab.png" alt="Screenshot: Settings — Printing tab, printer name and label dimensions" width="900">
</p>

<p align="center">
<img src="screenshots/295-settings-printing-test-print-preview.png" alt="Screenshot: Test Print preview at configured label size" width="700">
</p>

<p align="center">
<img src="screenshots/669-settings-printing-invalid-size-error.png" alt="Screenshot: Error for a zero or negative label width/height" width="700">
</p>

<p align="center">
<img src="screenshots/670-settings-printing-saved-message.png" alt="Screenshot: Confirmation after saving printing settings" width="500">
</p>

- Configure the **printer name** (blank = system default) and **label dimensions** (default 62 mm × 29 mm). Width and height must be positive whole numbers.
- **Test Print** — generates a QR code for `TEST-LABEL-001` and shows it in a Print Preview at the configured label size.

### Categories

<p align="center">
<img src="screenshots/296-settings-categories-tab.png" alt="Screenshot: Settings — Categories tab, numbered list of saved categories" width="700">
</p>

<p align="center">
<img src="screenshots/297-settings-categories-add-new.png" alt="Screenshot: Typing a new category name and pressing Enter/Add" width="700">
</p>

<p align="center">
<img src="screenshots/671-settings-categories-duplicate-or-empty-error.png" alt="Screenshot: Error when adding an empty or duplicate category name" width="700">
</p>

<p align="center">
<img src="screenshots/298-settings-categories-delete-confirmation.png" alt="Screenshot: Delete confirmation for a category" width="700">
</p>

<p align="center">
<img src="screenshots/672-settings-categories-delete-reassign-prompt.png" alt="Screenshot: The prompt offering to reassign N equipment items to 'Uncategorized' before deleting the category" width="700">
</p>

<p align="center">
<img src="screenshots/673-settings-categories-reassign-result.png" alt="Screenshot: Affected equipment now showing 'Uncategorized' and the category removed" width="900">
</p>

<p align="center">
<img src="screenshots/674-settings-categories-delete-declined-cancelled.png" alt="Screenshot: Declining the reassignment — nothing changed, category kept" width="700">
</p>

- The list is a custom owner-drawn, numbered list. **Add** validates a non-empty, unique name; pressing **Enter** in the text box adds it too.
- **Delete** is safe: if no equipment uses the category it's simply removed. If equipment does, you're asked: *"Category '{name}' is currently assigned to {count} equipment item(s). Would you like to reassign these {count} item(s) to 'Uncategorized' and delete '{name}'?"* Confirming creates the **Uncategorized** category if needed, moves every affected item to it, and deletes the category. Declining cancels the delete with nothing changed — so there are no orphaned category strings left behind.

### System Configuration

*(Admin only — the single most privileged screen in the app)*

<p align="center">
<img src="screenshots/299-settings-system-config-tab.png" alt="Screenshot: Settings — System Configuration tab, database connection fields" width="900">
</p>

<p align="center">
<img src="screenshots/675-settings-system-config-password-eye-toggle.png" alt="Screenshot: The database password field with its eye-icon toggle" width="700">
</p>

<p align="center">
<img src="screenshots/676-settings-system-config-test-success.png" alt="Screenshot: ✅ Test Connection succeeded, Save Connection now enabled" width="700">
</p>

<p align="center">
<img src="screenshots/677-settings-system-config-test-failure.png" alt="Screenshot: ❌ Test Connection failed, Save Connection still disabled" width="700">
</p>

<p align="center">
<img src="screenshots/678-settings-system-config-save-toast.png" alt="Screenshot: Toast after saving the connection" width="500">
</p>

<p align="center">
<img src="screenshots/679-settings-system-config-connection-status-card.png" alt="Screenshot: The Connection Status card — status, active server, last successful connection" width="700">
</p>

- Manages the live database connection — **Database Server (IP), Database Name, Database Username, Database Password** — independently of the General tab.
- **✔ Test Connection** builds a temporary connection from the form values and checks it; the result shows as ✅ or ❌ inline. **💾 Save Connection** only becomes available after a successful test.
- The saved password is **DPAPI-encrypted** for the current Windows user and written to `%AppData%\AssetPilot\dbconnection.json` — it can't be decrypted if the file is moved to another machine or user account.
- The **Connection Status** card shows *Current Status* (🟢 Connected / 🔴 Disconnected), *Active Server* (database · host) and *Last Successful Connection*, refreshed whenever the panel loads.
- On upgrade from an older version, if no settings file exists it is seeded once from the previous built-in connection so existing installs keep working.
- Changing these settings changes which database the **entire application** — every screen, every user — reads and writes to. Change with care.

---

## Cross-Cutting Behaviour

Things that behave the same way on every screen.

### Toast Notifications

Routine confirmations are auto-dismissing toasts, styled to match the button that triggered them. Destructive confirmations ("Are you sure?") still use a modal confirmation dialog.

| Action | Toast colour |
|---|---|
| Approve / Save / Success | Green |
| Edit / Warning / Info | Yellow / Amber |
| Delete / Decommission / Error | Red |
| Restore / Undo | Purple |
| Check-In / Check-Out | Blue |
| Decommission → Restore | Teal |

Examples: the Decommission toast matches the red Decommission button; on Deleted Items, Restore is purple and Delete is red; on Users, Edit is yellow; on Allocation, Approve is green.

<p align="center">
<img src="screenshots/740-toast-green-success.png" alt="Screenshot: A green success toast" width="500">
</p>

<p align="center">
<img src="screenshots/741-toast-yellow-warning-edit.png" alt="Screenshot: A yellow Edit / Warning toast" width="500">
</p>

<p align="center">
<img src="screenshots/742-toast-red-delete-error.png" alt="Screenshot: A red Delete / Error toast" width="500">
</p>

<p align="center">
<img src="screenshots/743-toast-purple-restore-undo.png" alt="Screenshot: A purple Restore / Undo toast" width="500">
</p>

<p align="center">
<img src="screenshots/744-toast-blue-checkinout.png" alt="Screenshot: A blue Check-In/Out toast" width="500">
</p>

<p align="center">
<img src="screenshots/745-toast-teal-decommission-restore.png" alt="Screenshot: A teal Decommission-Restore toast" width="500">
</p>

<p align="center">
<img src="screenshots/746-confirmation-messagebox-destructive.png" alt="Screenshot: A modal 'Are you sure?' confirmation used for a destructive action" width="500">
</p>

### Picture Storage

Equipment, employee and user pictures are saved to a network share when it is reachable, and to a local folder otherwise — so a single-PC XAMPP deployment works with no share at all. Saving tries the share first; if it can't be reached (or isn't configured), the app silently continues with the local path.

| Picture type | Network share | Local fallback |
|---|---|---|
| Equipment | `\\{server}\AssetPilotData\equipment_pictures\` | `%AppData%\AssetPilot\equipment_pictures\` |
| Employee | `\\{server}\AssetPilotData\employee_pictures\` | `%AppData%\AssetPilot\employee_pictures\` |
| User | `\\{server}\AssetPilotData\user_pictures\` | `%AppData%\AssetPilot\user_pictures\` |

Uploads accept JPG / PNG / BMP / GIF and are stored as `{code or username}_{GUID}{ext}`.

<p align="center">
<img src="screenshots/747-pictures-local-fallback-folder.png" alt="Screenshot: The local %AppData%\AssetPilot picture folders on a localhost deployment" width="700">
</p>

### Search, Time and Theme

- **Fuzzy search** (code match first, then fuzzy on names) is used across Equipment, Employees, Allocation, QR Scanner, Check In/Out, Logs, Reports and Global Search.
- Timestamps in **Logs** and **Returns** are shown in **Pakistan Standard Time**.
- **Light / dark theme** is remembered per workstation and applied to every screen opened afterwards (see [Dashboard](#light--dark-mode-toggle)).
- All grids with large datasets are paginated, either server-side (Equipment, Employees, Logs, Deleted Items, Locations) or in-page (Allocation, Reports, Check-In/Out History).

---

## Equipment Status Lifecycle

```
in_store       -->  allocated       (Allocation approved OR Check-Out)
in_store       -->  under_repair    (Edit → status changed to Under Repair)
in_store       -->  decommissioned  (Decommission)
under_repair   -->  in_store        (Maintenance resolved)
under_repair   -->  decommissioned  (Decommission)
allocated      -->  in_store        (Return / Deallocate / Check-In)
allocated      -->  allocated       (Transfer — new allocation record, status unchanged)
decommissioned -->  in_store        (Restore, from the Decommission screen)
```

`allocated` items cannot be decommissioned or deleted — return or check them in first.

| Status | Meaning | UI display |
|---|---|---|
| `in_store` | Available, not currently assigned | In Store |
| `allocated` | Currently assigned or checked out | Allocated |
| `under_repair` | Flagged for maintenance | Under Repair |
| `decommissioned` | Retired, unless explicitly Restored | Decommissioned |

---

## Audit Action Reference

Every row in the Logs screen has an action type. Entries are written by `AuditService`; the badge colour in Logs matches the quick-filter pills.

| Action type (stored) | Written by | Logs label / badge |
|---|---|---|
| `equipment_created` | Add Equipment | Created |
| `equipment_updated` | Edit Equipment (field-by-field diff) | Updated |
| `equipment_deleted` | Equipment soft delete (when permitted) | Deleted |
| `equipment_restored` | *(defined; used by the Deleted group)* | Deleted group, teal |
| `location_changed` | *(defined)* | — |
| `allocation_requested` | New Allocation | Allocation |
| `allocation_approved` | Approve | Allocation |
| `allocation_rejected` | *(defined)* | Allocation |
| `allocation_returned` | Return (Allocation screen / Process Return) | **Returned to Store** |
| `return_undone` | Undo Return | **Returned to Active Allocation** — orange `#FFE0B2` / `#EF6C00` |
| `allocation_overdue` | *(defined)* | **Allocation Overdue** — amber `#FFE0B2` / `#FFA000` |
| `Allocation Request Deleted` *(free text)* | Delete Allocation | Orange-red `#FFAB91` / `#BF360C` |
| `Expected Return Date Updated` *(free text)* | Edit Expected Return Date | Yellow `#FFF9C4` / `#826400` |
| `transferred` | Transfer | Transfers (deep purple) |
| `checked_out` | Check-Out | Check In/Out (dark blue) |
| `checked_in` | Check-In | Check In/Out (dark blue) |
| `decommission_requested` / `_approved` / `_rejected` | Decommission | Decommission (red) |
| `decommission_undone` | Decommission → Restore | **Restored from Decommission** — lighter teal `#E0F7F4` / `#26A69A` |
| `employee_created` / `_updated` / `_deleted` | Employees | Created / Updated / Deleted |
| `location_created` / `_updated` / `_deleted` | Locations | Created / Updated / Deleted |
| `maintenance_opened` | Status → Under Repair | **Maintenance Opened** — `#FFE082` / `#825A00` |
| `maintenance_resolved` | Status away from Under Repair | **Maintenance Resolved** — `#C5E1A5` / `#33691E` |
| `Maintenance History Cleared` *(free text)* | Clear History button | Shown as-is, no colour badge |
| `settings_data_delete` | Clear All Logs / Reset Application Data | **⚠ Settings Data Deleted** — red `#FFCDD2` / `#B71C1C`, full row `#FFEBEE` |

Employee and Location entries have no associated equipment, so they are written with an equipment id of `0`.

---

## Cross-Module Effects Reference

A quick reference for what each action also touches elsewhere, and whether it's audited — useful when investigating an unexpected change. **†** = additionally recorded in the file-based Security Audit Log.

| Action | Tables / files touched | Audited in Logs? |
|---|---|:---:|
| Add Equipment | `equipment`, `equipment_logs`, optionally `procurement_types` | ✅ |
| Edit Equipment | `equipment`, `equipment_logs`, optionally `maintenance_records`, `procurement_types` | ✅ |
| Soft Delete Equipment (guarded) | `equipment` | ✅ when permitted; blocked attempts show a warning only |
| Restore / Permanent Delete Equipment | `equipment` | ❌ |
| Clear Maintenance History | `maintenance_records`, `equipment_logs` | ✅ |
| Add / Edit / Delete Employee | `employees`, `equipment_logs` | ✅ |
| Add / Edit / Delete Location | `locations`, `equipment_logs` | ✅ |
| Create Allocation | `allocations`, `equipment`, `equipment_logs` | ✅ |
| Approve Allocation | `allocations`, `equipment`, `equipment_logs` | ✅ |
| Return (Allocation screen) | `allocations`, `equipment`, `equipment_logs` | ✅ |
| Delete Allocation (pending / available) | `allocations` (row removed), `equipment_logs` | ✅ |
| Delete Allocation (approved) | Blocked | — |
| Edit Expected Return Date | `allocations`, `equipment_logs` | ✅ |
| Process Return | `allocations`, `equipment`, `equipment_logs` | ✅ |
| Edit Return record | `allocations`, `equipment` | ❌ |
| Undo Return | `allocations`, `equipment`, `equipment_logs` | ✅ `return_undone` |
| Transfer | `allocations` (×2), `equipment_logs` | ✅ |
| Decommission | `equipment`, `equipment_logs` | ✅ |
| Restore from Decommission | `equipment`, `equipment_logs` | ✅ `decommission_undone` |
| Check-Out / Check-In | `transactions`, `equipment`, `equipment_logs` | ✅ |
| Overdue update (Check In/Out) | `transactions` | ❌ |
| Add / Edit / Toggle Active User | `users`, `user_roles` | ❌ (†) |
| Delete User | `user_roles`, `users` | ❌ (†) |
| Change Password (own or another user's) | `users` | ❌ (†) |
| Self-service Password Reset (Login) | `users` | ❌ (†) |
| Set / Reset Organization Master PIN | `recovery_pin.txt` | ❌ (†) |
| Save a User Recovery PIN | `user_pins.json` | ❌ (†) |
| Upload Equipment / Employee Picture | `equipment` / `employees` (`picture_path`) | ✅ via the edit entry |
| Upload User Picture | `users` (`picture_path`) | ❌ |
| Add / Delete Category | `categories` (delete may reassign `equipment.category`) | ❌ |
| Save Settings / Save DB Connection | `app_settings` / `dbconnection.json` | ❌ |
| Clear All Logs | `equipment_logs` (all rows), then marker re-inserted | ✅ `settings_data_delete` (†) |
| Reset Application Data | Logs, Location Histories, Maintenance Records, Transactions, Allocations, Equipment, Employees, Locations — in one transaction after an automatic backup | ✅ `settings_data_delete` (†) |
| Reports, exports, QR Scanner lookups, camera scanning, Bulk Scan, pagination | No writes | N/A |

---

## Data Model Reference

| Entity | Table | Notes |
|---|---|---|
| `Equipment` | `equipment` | `current_status` is a string; `is_deleted` / `deleted_at` implement soft delete |
| `Allocations` | `allocations` | Equipment → Employee → Location; `PreviousAllocationId` links a transfer to the allocation it replaced |
| `Employees` | `employees` | |
| `Locations` | `locations` | |
| `EquipmentLogs` | `equipment_logs` | Audit trail |
| `Transactions` | `transactions` | Check In/Out records |
| `MaintenanceRecords` | `maintenance_records` | Repair history |
| `ProcurementTypes` | `procurement_types` | Lookup, created on demand |
| `Roles` | `roles` | Admin, Operator |
| `Users` | `users` | `password_hash` holds BCrypt hashes (legacy plain text upgraded on next password change) |
| `UserRoles` | `user_roles` | UserId + RoleId junction |
| `AppSettings` | `app_settings` | Single row expected |
| `Categories` | `categories` | |

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
|---|---|
| Delete Equipment (soft) | Blocked if allocated, has an approved or pending allocation, or has an active/overdue Check-Out |
| Permanent Delete Equipment | Fails at the database if related rows exist without cascade rules |
| Delete Employee | Blocked if any approved allocation exists |
| Delete Location | Handled by the location service |
| Delete User | Not allowed on your own account; fails at the database if still referenced |
| Decommission | Multi-layer: UI pre-filter plus database checks in both the dialog and the screen |
| Delete Category | Reassignment prompt to *Uncategorized* when in use |
| Delete Allocation | Blocked while Approved |

---

## Files & Folders Used by the App

| Path | Purpose |
|---|---|
| `theme.json` (app folder) | Remembered light/dark choice for this workstation |
| `remember_me.txt` (app startup folder) | Remembered username when *Keep Me Logged In* is on (never the password) |
| `%AppData%\AssetPilot\dbconnection.json` | Saved database connection (DPAPI-encrypted password) |
| `%AppData%\AssetPilot\lastbackup.txt` | Timestamp of the last backup shown on Settings → General |
| `%AppData%\AssetPilot\mysqltools.txt` | Cached location of `mysqldump.exe` / `mysql.exe` |
| `%AppData%\AssetPilot\recovery_pin.txt` | Custom organization master recovery PIN |
| `%AppData%\AssetPilot\user_pins.json` | Per-user recovery PINs |
| `%AppData%\AssetPilot\security_audit.log` | Append-only Security Audit Log |
| `%AppData%\AssetPilot\Backups\` | Automatic pre-reset safety backups (`.sql` or `pre_reset_backup_{timestamp}.json`) |
| `%AppData%\AssetPilot\equipment_pictures\`, `employee_pictures\`, `user_pictures\` | Local picture fallback folders |
| `BulkScan_{yyyyMMdd_HHmm}.csv` | QR Scanner bulk-scan export |
| `{EquipmentCode}_History_{yyyyMMdd_HHmm}.csv` | QR Scanner history export |
| `CheckInOutHistory_{yyyyMMdd_HHmm}.csv` | Check In/Out History export |

---

## Installation & Setup

1. Install **.NET 8 SDK/Runtime** and **Visual Studio 2022** (or newer) with the .NET desktop development workload.
2. Clone or copy the project folder.
3. Open the `.sln` file in Visual Studio.
4. Restore NuGet packages (Visual Studio does this automatically on first build).
5. Press **F5** to build and run.

Optional extras: a **webcam** for camera scanning in the QR Scanner, a label/QR **printer** for Settings → Printing, and **`mysqldump.exe` / `mysql.exe`** (bundled with XAMPP, MySQL or WAMP) for Backup, Restore and the pre-reset safety backup (without them the reset falls back to a JSON snapshot).

---

## Database Setup

1. Set up a MariaDB (or MySQL-compatible) server reachable from the machine(s) running the app — or run entirely on `localhost` (e.g. XAMPP) for a single-machine deployment. The default database name is `inventory_management`.
2. Import the provided schema (`inventory_management.sql`) into a new database.
3. Point the app at your server: either update the connection string in `Models/InventoryDbContext.cs`, or — once you have an Admin account — use **Settings → System Configuration**. On first run after an upgrade, a missing connection file is seeded once from the old built-in connection so existing installs keep working.
4. Seed the `roles` table with `Admin` and `Operator`, then create at least one Admin user via the `users` / `user_roles` tables (or the Add User screen once one Admin exists). Give each account a Work Email and/or Recovery PIN so Forgot Password works.
5. Equipment, User and Employee pictures use the shared network folder (`\\<server-ip>\AssetPilotData`, subfolders `equipment_pictures`, `user_pictures`, `employee_pictures`) when reachable, so every PC sees the same pictures; otherwise they fall back to `%AppData%\AssetPilot\` on that machine.
6. **Change the default organization master recovery PIN** (`123456`) in Settings before going live.

---

## Known Limitations & Data-Safety Notes

### Still Open

- **A Transfer causes the old allocation to appear in Returns history.** It is closed as *returned*, so it shows in the Returns screen without a "transferred" flag. (The Reports → Returns tab labels such rows with a *Transferred* source badge.)
- **New allocations with a blank status are auto-approved**, bypassing the *pending → approved* queue.
- **"Overdue" is a display state only.** Overdue allocations stay `approved` in the database; the flag is computed when screens load.
- **Decommission reasons live in the audit log.** Clearing all logs blanks the Reason / Date columns on the Decommission screen.
- **Deleting an employee is a hard delete.** Their historical (non-approved) allocation records keep a dangling reference. Prefer unticking *Is Active*.
- **Audit writes are best-effort.** If the audit insert fails it is logged internally and the business action still completes.
- **Two audit actions are free-text strings** (*Allocation Request Deleted*, *Expected Return Date Updated*) rather than defined constants — they still display and filter correctly.
- **The saved database password is bound to one Windows user + machine** (DPAPI). Moving `dbconnection.json` elsewhere means re-entering it.
- **QR label printing from the View QR dialog isn't wired up yet** — *Print Label* shows a notice. Test Print in Settings → Printing works.
- **The Security Audit Log is a local file** under each Windows user's `%AppData%` — on a multi-PC deployment each workstation keeps its own copy.
- **Default master recovery PIN is `123456`** until an admin changes it.
- **Permanent Delete and Clear Maintenance History cannot be undone.** Take a backup first.
- **Internal tidy-up:** `SettingsForm.BuildSystemConfigPanel` contains an unreachable duplicate `return outer`.

### Resolved in Current Version

| Former limitation | Now |
|---|---|
| Plain-text passwords | ✅ BCrypt hashing everywhere (Login, Settings, Users, Password Reset), with legacy fallback |
| Audit "Changed By" always user 1 | ✅ Uses the signed-in user |
| Undo Return logged as a normal return | ✅ Logs its own `return_undone` action |
| Record-level created/approved/allocated-by hard-coded to 1 | ✅ Stamped with the signed-in user (fallback `1` only for unauthenticated seeds) |
| Category delete orphaned equipment | ✅ Reassignment prompt to *Uncategorized* |
| Reset not atomic, no backup | ✅ Single transaction with rollback + automatic pre-reset backup |
| Backup password visible in process list | ✅ Secure temporary option files |
| Returns report excluded Check-In/Out completions | ✅ Unified Returns report |
| Soft delete had no allocation guard | ✅ Guarded against allocated / pending / checked-out items |
| Locations had no pagination | ✅ 50 per page with search |
| Decommission accepted allocated equipment | ✅ Pre-filtered and double-checked at save |

### Remaining Roadmap

| Tier | Item |
|---|---|
| 2 — Workflow consistency | Distinguish *Transferred* from *Returned* allocations; enforce an explicit *pending → approved* workflow; sanitise historical references on employee deletion; show a user-facing warning when an audit write fails |
| 3 — Polish | Persist the decommission reason on the equipment record; promote free-text audit strings to constants; remove the dead code in System Configuration; offer a cross-machine credential-encryption option |

---

## Release History

| Date | Highlights |
|---|---|
| **2026-11-10** | Reports overhaul (five tabs, KPIs, single-row filters, Print / PDF / Excel / CSV, developer watermark); Locations pagination; QR Scanner history cap + camera scanning; Forgot Password with Work Email / Recovery PIN and strict input rules; BCrypt hashing; offline master PIN and per-user PINs; mandatory recovery credential; expanded Security Audit Log; dynamic user attribution; soft-delete guard; atomic reset with dual-mode pre-reset backup; category reassignment on delete; decommission eligibility pre-filter and guards; advanced Logs filters (category pills, user, date range, density, live indicator, Reset) |
| **2026-09-13** | QR Scanner overhaul (Recent Scans, Bulk Scan, View QR, Check-In/Out and View Logs buttons, warranty indicator, "Did you mean?", hero photo, history export, Source Screen tag); Decommission Restore; Allocation Filters button, AllocatedBy column, Gmail-style select header, sorting, pagination, audited delete; Returns undo guard, PKT timestamp and detail popup; Deleted Items pagination; SecurityAuditLogger and `settings_data_delete` marker; expanded reset scope |
| **2026-09-05** | Rebrand to **AssetPilot**; local picture storage fallback; toast colour system; readable Allocation status text; amber overdue highlighting in Allocation and Logs; Check-Out and Allocate bug fixes; Equipment import tooltip; sidebar toggle and hover polish |
| **2026-08-17** | Light / dark mode toggle; Check-In/Out History screen; maintenance history in the Equipment detail popup; Clear Maintenance History; View Logs buttons; Log Detail popup improvements; Location + Room dropdowns on Check-Out; sortable Logs columns and maintenance action colours |
| **2026-08-10** | System Configuration panel; profile pictures for users and employees; detail popups on double-click; maintenance history; session-aware audit and field-level diffs; Allocation filtering and detail popup; Settings UX polish; Log Detail popup; Recent Activities panel |

---

## Credits

Developed by **Syed Shayan Haider Kazmi**.

---

## License

This project is for internal organizational use.
All rights reserved © 2026.
