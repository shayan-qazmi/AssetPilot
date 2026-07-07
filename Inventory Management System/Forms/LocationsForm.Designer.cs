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
            btnAdd       = new System.Windows.Forms.Button();
            btnEdit      = new System.Windows.Forms.Button();
            btnDelete    = new System.Windows.Forms.Button();
            btnRefresh   = new System.Windows.Forms.Button();
            btnExport    = new System.Windows.Forms.Button();
            txtSearch    = new System.Windows.Forms.TextBox();
            dgvLocations = new System.Windows.Forms.DataGridView();

            panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLocations).BeginInit();
            SuspendLayout();

            panelTop.Dock   = System.Windows.Forms.DockStyle.Top;
            panelTop.Height = 60;
            panelTop.Name   = "panelTop";
            panelTop.Controls.Add(btnAdd);
            panelTop.Controls.Add(btnEdit);
            panelTop.Controls.Add(btnDelete);
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(btnExport);
            panelTop.Controls.Add(txtSearch);

            btnAdd.Location = new System.Drawing.Point(12, 14);
            btnAdd.Size     = new System.Drawing.Size(90, 32);
            btnAdd.Text     = "Add";
            btnAdd.Name     = "btnAdd";
            btnAdd.Click   += btnAdd_Click;

            btnEdit.Location = new System.Drawing.Point(110, 14);
            btnEdit.Size     = new System.Drawing.Size(90, 32);
            btnEdit.Text     = "Edit";
            btnEdit.Name     = "btnEdit";
            btnEdit.Click   += btnEdit_Click;

            btnDelete.Location = new System.Drawing.Point(208, 14);
            btnDelete.Size     = new System.Drawing.Size(90, 32);
            btnDelete.Text     = "Delete";
            btnDelete.Name     = "btnDelete";
            btnDelete.Click   += btnDelete_Click;

            btnRefresh.Location = new System.Drawing.Point(306, 14);
            btnRefresh.Size     = new System.Drawing.Size(90, 32);
            btnRefresh.Text     = "Refresh";
            btnRefresh.Name     = "btnRefresh";
            btnRefresh.Click   += btnRefresh_Click;

            btnExport.Location = new System.Drawing.Point(404, 14);
            btnExport.Size     = new System.Drawing.Size(90, 32);
            btnExport.Text     = "Export";
            btnExport.Name     = "btnExport";
            btnExport.Click   += btnExport_Click;

            txtSearch.Location        = new System.Drawing.Point(600, 16);
            txtSearch.Size            = new System.Drawing.Size(260, 26);
            txtSearch.PlaceholderText = "Search Location…";
            txtSearch.Name            = "txtSearch";

            dgvLocations.Dock                = System.Windows.Forms.DockStyle.Fill;
            dgvLocations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvLocations.AllowUserToAddRows  = false;
            dgvLocations.ReadOnly            = true;
            dgvLocations.SelectionMode       = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvLocations.Name                = "dgvLocations";
            dgvLocations.Columns.Add("ID",              "Location ID");
            dgvLocations.Columns.Add("Name",            "Location Name");
            dgvLocations.Columns.Add("Building",        "Building");
            dgvLocations.Columns.Add("RoomNumber",      "Room Number");
            dgvLocations.Columns.Add("AllocatedPerson", "Allocated Person");

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(1200, 700);
            AutoScroll          = true;
            AutoScrollMinSize   = new System.Drawing.Size(1000, 300);
            Controls.Add(dgvLocations);
            Controls.Add(panelTop);
            Text = "Locations";
            Name = "LocationsForm";

            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvLocations).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel       panelTop;
        private System.Windows.Forms.Button      btnAdd;
        private System.Windows.Forms.Button      btnEdit;
        private System.Windows.Forms.Button      btnDelete;
        private System.Windows.Forms.Button      btnRefresh;
        private System.Windows.Forms.Button      btnExport;
        private System.Windows.Forms.TextBox     txtSearch;
        private System.Windows.Forms.DataGridView dgvLocations;
    }
}