namespace UserProfileTransfer
{
    partial class UserProfileTransferControl
    {
        /// <summary> 
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary> 
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas 
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStripMenu = new System.Windows.Forms.ToolStrip();
            this.tsbClose = new System.Windows.Forms.ToolStripButton();
            this.tssSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnExportUserProfiles = new System.Windows.Forms.Button();
            this.btnImportUserProfiles = new System.Windows.Forms.Button();
            this.toolStripMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripMenu
            // 
            this.toolStripMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbClose,
            this.tssSeparator1});
            this.toolStripMenu.Location = new System.Drawing.Point(0, 0);
            this.toolStripMenu.Name = "toolStripMenu";
            this.toolStripMenu.Size = new System.Drawing.Size(559, 25);
            this.toolStripMenu.TabIndex = 4;
            this.toolStripMenu.Text = "toolStrip1";
            // 
            // tsbClose
            // 
            this.tsbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbClose.Name = "tsbClose";
            this.tsbClose.Size = new System.Drawing.Size(86, 22);
            this.tsbClose.Text = "Close this tool";
            this.tsbClose.Click += new System.EventHandler(this.tsbClose_Click);
            // 
            // tssSeparator1
            // 
            this.tssSeparator1.Name = "tssSeparator1";
            this.tssSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnExportUserProfiles
            // 
            this.btnExportUserProfiles.Location = new System.Drawing.Point(16, 45);
            this.btnExportUserProfiles.Name = "btnExportUserProfiles";
            this.btnExportUserProfiles.Size = new System.Drawing.Size(138, 30);
            this.btnExportUserProfiles.TabIndex = 5;
            this.btnExportUserProfiles.Text = "Export User Profiles";
            this.btnExportUserProfiles.UseVisualStyleBackColor = true;
            this.btnExportUserProfiles.Click += new System.EventHandler(this.btnExportUserProfiles_Click);
            // 
            // btnImportUserProfiles
            // 
            this.btnImportUserProfiles.Location = new System.Drawing.Point(16, 91);
            this.btnImportUserProfiles.Name = "btnImportUserProfiles";
            this.btnImportUserProfiles.Size = new System.Drawing.Size(138, 32);
            this.btnImportUserProfiles.TabIndex = 6;
            this.btnImportUserProfiles.Text = "Import User Profiles";
            this.btnImportUserProfiles.UseVisualStyleBackColor = true;
            this.btnImportUserProfiles.Click += new System.EventHandler(this.btnImportUserProfiles_Click);
            // 
            // UserProfileTransferControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnImportUserProfiles);
            this.Controls.Add(this.btnExportUserProfiles);
            this.Controls.Add(this.toolStripMenu);
            this.Name = "UserProfileTransferControl";
            this.Size = new System.Drawing.Size(559, 300);
            this.Load += new System.EventHandler(this.MyPluginControl_Load);
            this.toolStripMenu.ResumeLayout(false);
            this.toolStripMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStripMenu;
        private System.Windows.Forms.ToolStripButton tsbClose;
        private System.Windows.Forms.ToolStripSeparator tssSeparator1;
        private System.Windows.Forms.Button btnExportUserProfiles;
        private System.Windows.Forms.Button btnImportUserProfiles;
    }
}
