// Forms/LocationsForm.Designer.cs
namespace Inventory_Management_System.Forms
{
    partial class LocationsForm
    {
        private System.ComponentModel.IContainer components = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelTop     = new System.Windows.Forms.Panel();
            flowToolbar  = new System.Windows.Forms.FlowLayoutPanel();
            btnAdd       = new System.Windows.Forms.Button();
            btnEdit      = new System.Windows.Forms.Button();
            btnDelete    = new System.Windows.Forms.Button();
            btnRefresh   = new System.Windows.Forms.Button();
            btnExport    = new System.Windows.Forms.Button();
            btnImport = new System.Windows.Forms.Button();
            dgvLocations = new System.Windows.Forms.DataGridView();

            panelTop.SuspendLayout();
            flowToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLocations).BeginInit();
            SuspendLayout();

            panelTop.Dock         = System.Windows.Forms.DockStyle.Top;
            panelTop.Height       = 60;
            panelTop.Name         = "panelTop";
            panelTop.Controls.Add(flowToolbar);

            flowToolbar.Dock          = System.Windows.Forms.DockStyle.Top;
            flowToolbar.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            flowToolbar.WrapContents  = true;
            flowToolbar.AutoSize      = true;
            flowToolbar.AutoSizeMode  = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowToolbar.Padding       = new System.Windows.Forms.Padding(8, 8, 8, 8);
            flowToolbar.Name          = "flowToolbar";
            flowToolbar.Controls.Add(btnAdd);
            flowToolbar.Controls.Add(btnEdit);
            flowToolbar.Controls.Add(btnDelete);
            flowToolbar.Controls.Add(btnRefresh);
            flowToolbar.Controls.Add(btnExport);
            flowToolbar.Controls.Add(btnImport);

            btnAdd.Size   = new System.Drawing.Size(90, 32);
            btnAdd.Text   = "➕ Add";
            btnAdd.Name   = "btnAdd";
            btnAdd.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnAdd.Click += btnAdd_Click;

            btnEdit.Size   = new System.Drawing.Size(90, 32);
            btnEdit.Text   = "✏️ Edit";
            btnEdit.Name   = "btnEdit";
            btnEdit.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnEdit.Click += btnEdit_Click;

            btnDelete.Size   = new System.Drawing.Size(90, 32);
            btnDelete.Text   = "🗑️ Delete";
            btnDelete.Name   = "btnDelete";
            btnDelete.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnDelete.Click += btnDelete_Click;

            btnRefresh.Size   = new System.Drawing.Size(90, 32);
            btnRefresh.Text   = "🔄 Refresh";
            btnRefresh.Name   = "btnRefresh";
            btnRefresh.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnRefresh.Click += btnRefresh_Click;

            btnExport.Size   = new System.Drawing.Size(90, 32);
            btnExport.Text   = "📥 Export";
            btnExport.Name   = "btnExport";
            btnExport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnExport.Click += btnExport_Click;

            btnImport.Size = new System.Drawing.Size(90, 32);
            btnImport.Text = "📤 Import";
            btnImport.Name = "btnImport";
            btnImport.Margin = new System.Windows.Forms.Padding(4, 4, 0, 0);
            btnImport.Click += btnImport_Click;

            dgvLocations.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvLocations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvLocations.AllowUserToAddRows  = false;
            dgvLocations.ReadOnly            = true;
            dgvLocations.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvLocations.Name                = "dgvLocations";
            dgvLocations.Columns.Add("ID",              "Location ID");
            dgvLocations.Columns.Add("Name",            "Location Name");
            dgvLocations.Columns.Add("Building", "Address/Building");
            dgvLocations.Columns.Add("RoomNumber",      "Room Number");
            dgvLocations.Columns.Add("RoomName", "Room Name");

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
            Controls.Add(dgvLocations);
            Controls.Add(panelTop);
            Text = "Locations";
            Name = "LocationsForm";

            flowToolbar.ResumeLayout(false);
            flowToolbar.PerformLayout();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLocations).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel       panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowToolbar;
        private System.Windows.Forms.Button      btnAdd;
        private System.Windows.Forms.Button      btnEdit;
        private System.Windows.Forms.Button      btnDelete;
        private System.Windows.Forms.Button      btnRefresh;
        private System.Windows.Forms.Button      btnExport;
        private System.Windows.Forms.DataGridView dgvLocations;
        private System.Windows.Forms.Button btnImport;
    }
}