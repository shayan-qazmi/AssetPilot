#                Q-Inventory 

<p align="center">
<img width="1916" height="1030" alt="image" src="https://github.com/user-attachments/assets/1271357e-fdae-41dc-890f-899432499851" />
</p>

<p align="center">
<em>A desktop application for tracking physical equipment — from purchase to allocation, transfer, repair, and retirement — built with C# .NET 8 (WinForms) and MariaDB.</em>
</p>

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
- [Installation & Setup](#installation--setup)
- [Database Setup](#database-setup)
- [Known Limitations](#known-limitations)
- [Credits](#credits)

---

## What This App Does

Q-Inventory tracks every physical item a company owns — laptops, furniture, accessories, tools, anything — through its entire life:

- What equipment exists, and what condition is it in?
- Where is it right now, and who has it?
- Who supplied it, what did it cost, and is it still under warranty?
- When was it handed out, and when is it due back?
- Is it out on a quick informal loan, or a formal tracked allocation?
- What's happened to it over its lifetime — every repair, transfer, return, and status change?
- Who made each change, and when?

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
| PDF Generation | QuestPDF |
| Charts | Custom GDI+ (no third-party charting library) |
| Platform | Windows 10/11 (64-bit) |

---

## User Roles

Q-Inventory has two roles, assigned per user account:

| Feature | Admin | Operator |
|---|:---:|:---:|
| Dashboard & Charts | ✅ | ✅ |
| Equipment — View / Add / Edit / Delete | ✅ | ✅ |
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
| Settings | ✅ | ❌ Hidden from sidebar |

The logged-in user's name and role are shown persistently at the bottom of the sidebar.

---

## Login

<p align="center">
<img width="982" height="664" alt="image" src="https://github.com/user-attachments/assets/cc307e41-7c53-4f78-b252-f9886c43b622" />

</p>

- Username + password sign-in with a show/hide password toggle.
- **"Remember Me"** toggle — pre-fills the username field on next launch. The password is never stored or remembered; it must always be entered manually.
- Pressing **Enter** on either field submits the form, same as clicking Sign In.
- Invalid credentials show a single generic error message (doesn't reveal whether the username or the password was wrong).

---

## Dashboard

<p align="center">
<img width="1916" height="1030" alt="image" src="https://github.com/user-attachments/assets/1271357e-fdae-41dc-890f-899432499851" />
</p>

The landing screen after login. Everything here is live, read-only summary data.

### Stat Cards
Animated cards across the top, each clickable to jump straight to the relevant screen:

| Card | Shows |
|---|---|
| Total Equipment | Count of all active (non-deleted) equipment |
| Allocated | Currently allocated / checked-out items |
| In Stock | Items available in storage |
| Under Repair | Items currently flagged for maintenance |
| Currently Overdue | Allocations past their expected return date |
| Decommissioned | Retired equipment |
| Total Employees | Active employee count |

<p align="center">
<img width="1716" height="265" alt="image" src="https://github.com/user-attachments/assets/622b7acc-306b-494c-812e-a0eb238f1982" />
</p>

### Charts
- **Equipment Status** — donut chart, breakdown by current status
- **Asset Status Breakdown** — vertical bar chart, same data as an alternate view
- **Equipment by Category** — horizontal bar chart, count per category

<p align="center">
<img width="1719" height="735" alt="image" src="https://github.com/user-attachments/assets/e22f6b0b-2c65-45ea-97c8-24b9772d97f1" />
</p>

### Sidebar Navigation
Every module in the app is reachable from the left sidebar, with an animated sliding indicator marking the active screen.

<p align="center">
<img width="199" height="1031" alt="image" src="https://github.com/user-attachments/assets/c39f6b85-2302-48f8-9036-b097a9bc5337" />

</p>

---

## Equipment

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/411133cd-7c03-4135-b5d2-cce898f2370f" />
</p>

The master list of every equipment item, with columns for Equipment Code, QR Code, Procurement Type, Name, Make, Model, Serial Number, Category, Status (color-coded), Condition, Location, and Purchase Date.

### Add Equipment

<p align="center">
<img width="1915" height="1030" alt="image" src="https://github.com/user-attachments/assets/72f818a0-18ba-4148-b110-12aa5d8f4726" />

</p>

- Equipment Code and QR Code are **auto-generated** — never typed manually, no risk of duplicates or typos.
- Fields: Name (required), Make, Model, Serial Number, Status, Purchase Date, Category, Vendor Name, Condition Grade, Warranty Expiry (optional toggle), Notes, and a free-text Procurement Type field that matches an existing type or creates a new one automatically.
- A picture can be staged and attached to the record.

### Edit Equipment

<p align="center">
<img width="1919" height="1026" alt="image" src="https://github.com/user-attachments/assets/0604a046-78ad-4ae1-8772-11521ef06b02" />

</p>

- Same fields as Add, pre-filled with the current values.
- Switching status **to "Under Repair"** opens a maintenance dialog to log the issue being reported.
- Switching status **away from "Under Repair"** opens a resolution dialog to record what was done.

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/3c97facb-56d2-4b64-9ee1-9e560442f726" />
</p>

### Equipment Detail View

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/d57c6314-e847-46e8-8961-3b6cd882fa0e" />

</p>

Double-clicking a row (or a dedicated view action) opens a read-only detail popup showing every field plus the item's full maintenance history.

### Delete Equipment
- Deleting an item is a **soft delete** — it's hidden from every list and dropdown in the app but not destroyed. It moves to the **Deleted Items** screen (Admin-only), where it can be restored or permanently erased.

### Show QR / QR Sheet

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/6c840801-f1bb-4c77-a610-03aa7e9ae281" />
</p>

- **Show QR** — displays a single item's QR code with its details, and a Print button for label printers.
- **QR Sheet** — select multiple items (via the Select ✔ / Select All dropdown or checkboxes) and generate a printable PDF sheet of QR codes, laid out in a grid with company name, code, name, and status on each label. Opens automatically in your PDF viewer, with an optional print step.

<p align="center">
<img width="1919" height="1034" alt="image" src="https://github.com/user-attachments/assets/51769bfd-cad8-48e5-99a4-4831b93c3162" />
</p>

### Import / Export
- **Import Excel** — bulk-add equipment from a `.xlsx` file.
- **Export** — export the current grid to Excel.

### Search & Filter
- Fuzzy search box (matches Name, Code, Serial Number, Make, Model, Category even with typos or partial text).
- Category filter dropdown ("All Categories" or a specific one).

---

## Employees

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/03621c9a-ba14-46c3-902b-535332eb4918" />
</p>

Manage the people equipment can be allocated to.

<p align="center">
<img width="1918" height="1029" alt="image" src="https://github.com/user-attachments/assets/34489a07-ec59-481b-9652-529bdce00329" />
</p>

- **Add / Edit** — Full Name (required), Employee Code (auto-generated if left blank), Department, Email (must be unique if provided), Phone, Default Location, Active/Inactive status.
- **Delete** — blocked if the employee currently has any active (approved) allocation.
- Grid shows Employee Code, Full Name, Department, Email, Phone, and Status.
- Fuzzy search and Excel export available.

---

## Locations

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/71551762-78f9-45de-b524-f89d84b2d437" />
</p>

Manage the physical places equipment can be assigned to (offices, stores, buildings, rooms).

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/c6209a40-5f8c-4e62-9f1a-99767ef0421c" />
</p>

- **Add / Edit** — Location Name, Building, Room Number, Room Name.
- **Delete** — removes the location record.
- Grid shows Location Name, Building, Room Number, Room Name.

---

## Allocation

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/0ab9859c-63dc-4d38-a806-373facd231e6" />
</p>

The formal, tracked workflow for assigning equipment to employees — with an approval step and full status history.

### Allocate

<p align="center">
<img width="1919" height="1033" alt="image" src="https://github.com/user-attachments/assets/ac87b4fd-0dfa-4c79-a86f-3680bcfcf28b" />
</p>

- Search-and-select equipment (only shows items that are available — not already allocated, under repair, or decommissioned).
- Choose an active employee and a location.
- Optionally set an expected return date.

### Approve
- Moves a **pending** allocation to **approved**, marking the equipment as `allocated`.

### Return

<p align="center">
<img width="1919" height="1030" alt="image" src="https://github.com/user-attachments/assets/70294e41-fafb-4c85-a1fc-e90079f2d566" />
</p>

- Marks the allocation as returned and the equipment status back to `in_store`.

### Edit Expected Return Date

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/9ecc3585-edb9-44d5-be09-039a438ea984" />
</p>

### Delete
- Removes an allocation record entirely (blocked if it's currently approved/active).

### Status & Filtering
- Statuses shown: `pending`, `approved`, `returned`, `rejected`, and a computed `Overdue` flag (approved + past expected return date, shown in red).
- Filter dropdown: All / Available / Allocated / Pending.
- Fuzzy search, Excel export, Select All for bulk actions.

---

## Returns

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/119710bd-d879-40f2-8211-bf1ecab99ae2" />
</p>

A dedicated history view of everything that's come back, combining both formal Allocation returns and Check-In/Out completions.

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/a8ec2158-6848-4c2e-8841-7f08a933259c" />
</p>

- **Process Return** — select an active allocation, set the return date and the item's condition on return.
- **Edit Return** — adjust the recorded return date or condition after the fact.
- **Undo Return** — reverts a processed return, putting the allocation back to active and the equipment back to `allocated`.
- Late returns (actual return date after the expected date) are visually flagged.

---

## Transfer

<p align="center">
<img width="1919" height="1033" alt="image" src="https://github.com/user-attachments/assets/845daa99-a00d-4228-b392-6536ab6c11ea" />
</p>

Move equipment directly from one employee/location to another, without a separate return-then-reallocate step.

<p align="center">
<img width="1919" height="1033" alt="image" src="https://github.com/user-attachments/assets/2d1efaf2-457d-45ac-a13c-7d5afc785759" />
</p>

- **New Transfer** — search-and-select the equipment (fuzzy search by code, name, or serial number), pick the new employee and new location.
- **Double-clicking a row** in the Transfer grid pre-selects that item and jumps straight into the transfer dialog.
- The equipment's status stays `allocated` throughout the transfer — only the assigned employee/location changes.
- Fuzzy search, pagination, and CSV export on the main grid.

---

## Decommission

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/bb01d0c6-c738-4a41-9f88-7dca3be647f2" />
</p>

*(Admin only)* — the permanent retirement workflow for equipment that's reached end-of-life, while keeping its full history.

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/fd08a416-d556-4272-8945-e5a7aeeb2ab4" />
</p>

- Select an item currently in stock or under repair.
- Choose a reason: Damaged, Obsolete, Beyond Repair, End of Life, Lost, or Other — with an optional notes field.
- Confirming sets the item's status to `decommissioned` permanently.
- The Decommission grid shows the reason and date for each retired item.

---

## Deleted Items

<p align="center">
<img width="1919" height="1030" alt="image" src="https://github.com/user-attachments/assets/d5e1e725-8a1e-413d-b778-8782414e0663" />
</p>

*(Admin only)* — a recycle bin for equipment removed via the Equipment screen's Delete button. Nothing is permanently lost until explicitly confirmed here.

<p align="center">
<img width="1919" height="1027" alt="image" src="https://github.com/user-attachments/assets/d339508a-9f85-4cfa-930d-6579344db5f6" />
</p>

- **Restore Selected** — brings item(s) back to the active Equipment list exactly as they were.
- **Delete Permanently** — erases the selected item(s) from the database. This step cannot be undone.
- **Select ✔ / Select All** dropdown for bulk operations, same pattern as Equipment.
- **Double-clicking a row** opens a small Restore / Delete / Cancel dialog for that single item.

---

## Logs (Audit Trail)

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/069134fa-7a5d-481f-9c8d-7e31e34c3a2a" />
</p>

A searchable, chronological record of key actions taken across the app.

- Columns: Equipment Code, QR Code, Action (color-coded by type), Changed By, Previous Value, New Value, Timestamp.
- Actions logged include: equipment creation, allocation requested/approved/returned, transfers, and decommissions.
- Fuzzy search across action type, user, and old/new values.
- Excel export.

---

## QR Scanner

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/c9745ca7-147b-429a-a9fb-7df550413a1a" />
</p>

A fast, read-only lookup tool designed for use with a USB barcode/QR scanner (keyboard-wedge mode, no drivers needed).

<p align="center">
<img width="1919" height="1029" alt="image" src="https://github.com/user-attachments/assets/e9d2cdd6-6f59-48ef-99dc-522994305c3a" />
</p>

- Scan or type a QR code, Equipment Code, or Serial Number — the app auto-searches as you type (with a short debounce) or on Enter.
- Fuzzy suggestion dropdown assists manual lookups.
- On match: full equipment detail card (status, current holder, current location) plus a complete change-history timeline for that item.

---

## Check In / Check Out

<p align="center">
<img width="1919" height="1029" alt="image" src="https://github.com/user-attachments/assets/560bf04b-b86a-4e3c-a812-098cf26950fc" />
</p>

A lightweight, no-approval loan tracker for short-term, informal borrows — separate from the formal Allocation workflow (e.g. borrowing a projector for a meeting).

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/8a1b1daf-6eba-4f23-8ed3-3e23a348b168" />
</p>

- Scan or search for the equipment — the screen automatically opens the correct tab (Check-In if it's currently out, Check-Out if it's available).
- **Check-Out** — records who's taking it, department, expected return, and condition before, then marks the item `allocated`.
- **Check-In** — records the return, condition after, and any remarks, then marks the item `in_store` again.
- A history panel shows the last 10 transactions for the selected item.

---

## Reports

<p align="center">
<img src="screenshots/reports-list.png" alt="Reports screen" width="900">
</p>

Four report types, each with a live on-screen preview before exporting:

| Report | Shows |
|---|---|
| **Equipment Inventory** | Full equipment list with status and current location; filterable by category |
| **Employee Allocation Summary** | Every active employee with items currently held and total ever allocated |
| **Active Allocations** | All equipment currently allocated or pending approval |
| **Returns History** | All completed returns with the duration each item was held, in days |

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/c6d569ab-b45f-4985-841b-ea216d317771" />
</p>

- **Export PDF** — generates a properly formatted PDF and opens it directly in your system's PDF viewer for preview, with an optional print step afterward.
- **Export Excel** — styled spreadsheet export with a company-name header row and colored headers.

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/7cd3ca90-0ed3-44a8-a5ad-67ad2fc38a95" />
</p>

---

## Settings

*(Admin only)*

<p align="center">
<img width="1918" height="1031" alt="image" src="https://github.com/user-attachments/assets/96d6dac3-b98d-4537-bd35-9fc65723aab0" />
</p>

### General
- Company Name (shown across reports, QR sheets, and printed labels).
- Label size configuration.
- System info: app version, last backup date, connected database name.

### Backup / Restore

<p align="center">
<img width="1718" height="1031" alt="image" src="https://github.com/user-attachments/assets/aeb5b271-726c-4367-bc7f-319143c79b9f" />
</p>

- **Backup** — exports the full database to a `.sql` file.
- **Restore** — imports a previously exported `.sql` file back into the database.

### Users

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/acf38f2c-408b-418b-a7db-87d71d03b6f9" />
</p>

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/a7d05b66-337e-491a-ace7-a60deabe71c5" />
</p>

- **Add / Edit User** — Username, Full Name, Email, Password, Role (Admin/Operator).
- **Toggle Active** — enable or disable a login without deleting the account.
- **Delete User** — removes the account (you cannot delete or deactivate your own account).

### Change Password

<p align="center">
<img width="1916" height="1034" alt="image" src="https://github.com/user-attachments/assets/a6c01b65-83b4-4967-9db5-cbf792546646" />
</p>

- Requires current password, a new password (4+ characters), and confirmation.

### Printing

<p align="center">
<img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/ff18928d-e371-4974-b184-a2964d86bf22" />
</p>

- Configure printer name and label dimensions (width/height in mm).
- **Test Print** — prints a sample QR label at the configured size.

### Categories

<p align="center">
<img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/f09ab3b2-7b20-48d9-ae71-ab1f71db17cd" />
</p>

- **Add / Delete** categories used throughout the Equipment and Reports screens.

### Danger Zone

<p align="center">
<img width="1003" height="522" alt="image" src="https://github.com/user-attachments/assets/1141ca61-ae6d-407f-9dc9-d67ac6ffb5c5" />
</p>

- **Clear all logs** — permanently wipes the audit trail.
- **Reset application data** — deletes all allocations and equipment records (requires typing "RESET" to confirm). Users, roles, categories, and settings are unaffected.

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
| Decommissioned | Permanently retired |

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
3. Update the connection string in `Models/InventoryDbContext.cs` to point to your server, database name, and credentials.
4. Seed the `roles` table with `Admin` and `Operator` entries, then create at least one Admin user via the `users` / `user_roles` tables (or the app's Add User screen once one Admin account exists).

---

## Known Limitations

- Passwords are stored and compared as plain text — not hashed. Recommended to add hashing before any real production deployment.
- Some actions (equipment edits, employee/location changes, check-in/out) do not currently write to the audit log.
- The connection string is set once in code rather than a separate config file.
- Several grids load their full dataset for search rather than paging server-side.

---

## Credits

Developed by **Syed Shayan Haider Kazmi**.

---

## License

This project is for internal organizational use.
All rights reserved © 2026.
