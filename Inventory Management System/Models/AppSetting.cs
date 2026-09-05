// Models/AppSetting.cs
using System;

namespace Inventory_Management_System.Models;

public partial class AppSetting
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "AssetPilot";
    public string Theme { get; set; } = "Light";
    public string DbServer { get; set; } = "localhost";
    public string DbName { get; set; } = "inventory_management";
    public DateTime? UpdatedAt { get; set; }
    public string? PrinterName { get; set; }
    public int? LabelWidthMm { get; set; }
    public int? LabelHeightMm { get; set; }
}