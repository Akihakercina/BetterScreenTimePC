namespace BetterScreenTimePC
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabBlockedApps = new System.Windows.Forms.TabPage();
            this.btnAddCustomApp = new System.Windows.Forms.Button();
            this.btnSaveApps = new System.Windows.Forms.Button();
            this.clbApps = new System.Windows.Forms.CheckedListBox();
            this.tabBlockedWebsites = new System.Windows.Forms.TabPage();
            this.btnSaveWebsites = new System.Windows.Forms.Button();
            this.btnRemoveWebsite = new System.Windows.Forms.Button();
            this.lbWebsites = new System.Windows.Forms.ListBox();
            this.btnAddWebsite = new System.Windows.Forms.Button();
            this.tbWebsite = new System.Windows.Forms.TextBox();
            this.tabFocusTime = new System.Windows.Forms.TabPage();
            this.btnSaveFocus = new System.Windows.Forms.Button();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.tabConcentration = new System.Windows.Forms.TabPage();
            this.lblConcentrationRemaining = new System.Windows.Forms.Label();
            this.btnSaveConcentration = new System.Windows.Forms.Button();
            this.btnStartConcentration = new System.Windows.Forms.Button();
            this.btnRemoveAllowed = new System.Windows.Forms.Button();
            this.lbAllowed = new System.Windows.Forms.ListBox();
            this.btnAddAllowed = new System.Windows.Forms.Button();
            this.tbAllowedSite = new System.Windows.Forms.TextBox();
            this.nudDuration = new System.Windows.Forms.NumericUpDown();
            this.labelDuration = new System.Windows.Forms.Label();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.systemOfEquationsRB = new System.Windows.Forms.RadioButton();
            this.textRetypeRB = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnSaveOptions = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabBlockedApps.SuspendLayout();
            this.tabBlockedWebsites.SuspendLayout();
            this.tabFocusTime.SuspendLayout();
            this.tabConcentration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).BeginInit();
            this.tabPage1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabBlockedApps);
            this.tabControl1.Controls.Add(this.tabBlockedWebsites);
            this.tabControl1.Controls.Add(this.tabFocusTime);
            this.tabControl1.Controls.Add(this.tabConcentration);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(760, 537);
            this.tabControl1.TabIndex = 0;
            // 
            // tabBlockedApps
            // 
            this.tabBlockedApps.Controls.Add(this.btnAddCustomApp);
            this.tabBlockedApps.Controls.Add(this.btnSaveApps);
            this.tabBlockedApps.Controls.Add(this.clbApps);
            this.tabBlockedApps.Location = new System.Drawing.Point(4, 22);
            this.tabBlockedApps.Name = "tabBlockedApps";
            this.tabBlockedApps.Padding = new System.Windows.Forms.Padding(3);
            this.tabBlockedApps.Size = new System.Drawing.Size(752, 511);
            this.tabBlockedApps.TabIndex = 0;
            this.tabBlockedApps.Text = "Blocked apps";
            this.tabBlockedApps.UseVisualStyleBackColor = true;
            // 
            // btnAddCustomApp
            // 
            this.btnAddCustomApp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddCustomApp.Location = new System.Drawing.Point(6, 443);
            this.btnAddCustomApp.Name = "btnAddCustomApp";
            this.btnAddCustomApp.Size = new System.Drawing.Size(740, 23);
            this.btnAddCustomApp.TabIndex = 2;
            this.btnAddCustomApp.Text = "Add apps";
            this.btnAddCustomApp.UseVisualStyleBackColor = true;
            // 
            // btnSaveApps
            // 
            this.btnSaveApps.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveApps.Location = new System.Drawing.Point(6, 482);
            this.btnSaveApps.Name = "btnSaveApps";
            this.btnSaveApps.Size = new System.Drawing.Size(740, 23);
            this.btnSaveApps.TabIndex = 1;
            this.btnSaveApps.Text = "Save";
            this.btnSaveApps.UseVisualStyleBackColor = true;
            // 
            // clbApps
            // 
            this.clbApps.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clbApps.FormattingEnabled = true;
            this.clbApps.Location = new System.Drawing.Point(6, 6);
            this.clbApps.Name = "clbApps";
            this.clbApps.Size = new System.Drawing.Size(740, 409);
            this.clbApps.TabIndex = 0;
            // 
            // tabBlockedWebsites
            // 
            this.tabBlockedWebsites.Controls.Add(this.btnSaveWebsites);
            this.tabBlockedWebsites.Controls.Add(this.btnRemoveWebsite);
            this.tabBlockedWebsites.Controls.Add(this.lbWebsites);
            this.tabBlockedWebsites.Controls.Add(this.btnAddWebsite);
            this.tabBlockedWebsites.Controls.Add(this.tbWebsite);
            this.tabBlockedWebsites.Location = new System.Drawing.Point(4, 22);
            this.tabBlockedWebsites.Name = "tabBlockedWebsites";
            this.tabBlockedWebsites.Padding = new System.Windows.Forms.Padding(3);
            this.tabBlockedWebsites.Size = new System.Drawing.Size(752, 511);
            this.tabBlockedWebsites.TabIndex = 1;
            this.tabBlockedWebsites.Text = "Blocked websites";
            this.tabBlockedWebsites.UseVisualStyleBackColor = true;
            // 
            // btnSaveWebsites
            // 
            this.btnSaveWebsites.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveWebsites.Location = new System.Drawing.Point(6, 482);
            this.btnSaveWebsites.Name = "btnSaveWebsites";
            this.btnSaveWebsites.Size = new System.Drawing.Size(740, 23);
            this.btnSaveWebsites.TabIndex = 4;
            this.btnSaveWebsites.Text = "Save";
            this.btnSaveWebsites.UseVisualStyleBackColor = true;
            // 
            // btnRemoveWebsite
            // 
            this.btnRemoveWebsite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveWebsite.Location = new System.Drawing.Point(671, 32);
            this.btnRemoveWebsite.Name = "btnRemoveWebsite";
            this.btnRemoveWebsite.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveWebsite.TabIndex = 3;
            this.btnRemoveWebsite.Text = "Remove";
            this.btnRemoveWebsite.UseVisualStyleBackColor = true;
            // 
            // lbWebsites
            // 
            this.lbWebsites.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbWebsites.FormattingEnabled = true;
            this.lbWebsites.Location = new System.Drawing.Point(6, 61);
            this.lbWebsites.Name = "lbWebsites";
            this.lbWebsites.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbWebsites.Size = new System.Drawing.Size(740, 407);
            this.lbWebsites.TabIndex = 2;
            // 
            // btnAddWebsite
            // 
            this.btnAddWebsite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddWebsite.Location = new System.Drawing.Point(590, 32);
            this.btnAddWebsite.Name = "btnAddWebsite";
            this.btnAddWebsite.Size = new System.Drawing.Size(75, 23);
            this.btnAddWebsite.TabIndex = 1;
            this.btnAddWebsite.Text = "Add";
            this.btnAddWebsite.UseVisualStyleBackColor = true;
            // 
            // tbWebsite
            // 
            this.tbWebsite.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbWebsite.Location = new System.Drawing.Point(6, 6);
            this.tbWebsite.Name = "tbWebsite";
            this.tbWebsite.Size = new System.Drawing.Size(740, 20);
            this.tbWebsite.TabIndex = 0;
            // 
            // tabFocusTime
            // 
            this.tabFocusTime.Controls.Add(this.btnSaveFocus);
            this.tabFocusTime.Controls.Add(this.dtpEnd);
            this.tabFocusTime.Controls.Add(this.dtpStart);
            this.tabFocusTime.Location = new System.Drawing.Point(4, 22);
            this.tabFocusTime.Name = "tabFocusTime";
            this.tabFocusTime.Padding = new System.Windows.Forms.Padding(3);
            this.tabFocusTime.Size = new System.Drawing.Size(752, 511);
            this.tabFocusTime.TabIndex = 2;
            this.tabFocusTime.Text = "Focus time";
            this.tabFocusTime.UseVisualStyleBackColor = true;
            // 
            // btnSaveFocus
            // 
            this.btnSaveFocus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveFocus.Location = new System.Drawing.Point(6, 482);
            this.btnSaveFocus.Name = "btnSaveFocus";
            this.btnSaveFocus.Size = new System.Drawing.Size(740, 23);
            this.btnSaveFocus.TabIndex = 2;
            this.btnSaveFocus.Text = "Save";
            this.btnSaveFocus.UseVisualStyleBackColor = true;
            // 
            // dtpEnd
            // 
            this.dtpEnd.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpEnd.Location = new System.Drawing.Point(6, 39);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.ShowUpDown = true;
            this.dtpEnd.Size = new System.Drawing.Size(200, 20);
            this.dtpEnd.TabIndex = 1;
            // 
            // dtpStart
            // 
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpStart.Location = new System.Drawing.Point(6, 6);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.ShowUpDown = true;
            this.dtpStart.Size = new System.Drawing.Size(200, 20);
            this.dtpStart.TabIndex = 0;
            // 
            // tabConcentration
            // 
            this.tabConcentration.Controls.Add(this.lblConcentrationRemaining);
            this.tabConcentration.Controls.Add(this.btnSaveConcentration);
            this.tabConcentration.Controls.Add(this.btnStartConcentration);
            this.tabConcentration.Controls.Add(this.btnRemoveAllowed);
            this.tabConcentration.Controls.Add(this.lbAllowed);
            this.tabConcentration.Controls.Add(this.btnAddAllowed);
            this.tabConcentration.Controls.Add(this.tbAllowedSite);
            this.tabConcentration.Controls.Add(this.nudDuration);
            this.tabConcentration.Controls.Add(this.labelDuration);
            this.tabConcentration.Location = new System.Drawing.Point(4, 22);
            this.tabConcentration.Name = "tabConcentration";
            this.tabConcentration.Padding = new System.Windows.Forms.Padding(3);
            this.tabConcentration.Size = new System.Drawing.Size(752, 511);
            this.tabConcentration.TabIndex = 3;
            this.tabConcentration.Text = "Concentration mode";
            this.tabConcentration.UseVisualStyleBackColor = true;
            // 
            // lblConcentrationRemaining
            // 
            this.lblConcentrationRemaining.Location = new System.Drawing.Point(389, 6);
            this.lblConcentrationRemaining.Name = "lblConcentrationRemaining";
            this.lblConcentrationRemaining.Size = new System.Drawing.Size(126, 20);
            this.lblConcentrationRemaining.TabIndex = 8;
            this.lblConcentrationRemaining.Text = "label1";
            this.lblConcentrationRemaining.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnSaveConcentration
            // 
            this.btnSaveConcentration.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveConcentration.Location = new System.Drawing.Point(6, 482);
            this.btnSaveConcentration.Name = "btnSaveConcentration";
            this.btnSaveConcentration.Size = new System.Drawing.Size(740, 23);
            this.btnSaveConcentration.TabIndex = 7;
            this.btnSaveConcentration.Text = "Save";
            this.btnSaveConcentration.UseVisualStyleBackColor = true;
            // 
            // btnStartConcentration
            // 
            this.btnStartConcentration.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartConcentration.Location = new System.Drawing.Point(671, 6);
            this.btnStartConcentration.Name = "btnStartConcentration";
            this.btnStartConcentration.Size = new System.Drawing.Size(75, 23);
            this.btnStartConcentration.TabIndex = 6;
            this.btnStartConcentration.Text = "Start";
            this.btnStartConcentration.UseVisualStyleBackColor = true;
            // 
            // btnRemoveAllowed
            // 
            this.btnRemoveAllowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveAllowed.Location = new System.Drawing.Point(590, 32);
            this.btnRemoveAllowed.Name = "btnRemoveAllowed";
            this.btnRemoveAllowed.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveAllowed.TabIndex = 5;
            this.btnRemoveAllowed.Text = "Remove";
            this.btnRemoveAllowed.UseVisualStyleBackColor = true;
            // 
            // lbAllowed
            // 
            this.lbAllowed.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbAllowed.FormattingEnabled = true;
            this.lbAllowed.Location = new System.Drawing.Point(6, 61);
            this.lbAllowed.Name = "lbAllowed";
            this.lbAllowed.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbAllowed.Size = new System.Drawing.Size(740, 407);
            this.lbAllowed.TabIndex = 4;
            // 
            // btnAddAllowed
            // 
            this.btnAddAllowed.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddAllowed.Location = new System.Drawing.Point(509, 32);
            this.btnAddAllowed.Name = "btnAddAllowed";
            this.btnAddAllowed.Size = new System.Drawing.Size(75, 23);
            this.btnAddAllowed.TabIndex = 3;
            this.btnAddAllowed.Text = "Add";
            this.btnAddAllowed.UseVisualStyleBackColor = true;
            // 
            // tbAllowedSite
            // 
            this.tbAllowedSite.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbAllowedSite.Location = new System.Drawing.Point(6, 34);
            this.tbAllowedSite.Name = "tbAllowedSite";
            this.tbAllowedSite.Size = new System.Drawing.Size(497, 20);
            this.tbAllowedSite.TabIndex = 2;
            // 
            // nudDuration
            // 
            this.nudDuration.Location = new System.Drawing.Point(90, 6);
            this.nudDuration.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.nudDuration.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDuration.Name = "nudDuration";
            this.nudDuration.Size = new System.Drawing.Size(120, 20);
            this.nudDuration.TabIndex = 1;
            this.nudDuration.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // labelDuration
            // 
            this.labelDuration.AutoSize = true;
            this.labelDuration.Location = new System.Drawing.Point(6, 8);
            this.labelDuration.Name = "labelDuration";
            this.labelDuration.Size = new System.Drawing.Size(75, 13);
            this.labelDuration.TabIndex = 0;
            this.labelDuration.Text = "Duration (min):";
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnSaveOptions);
            this.tabPage1.Controls.Add(this.flowLayoutPanel1);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(752, 511);
            this.tabPage1.TabIndex = 4;
            this.tabPage1.Text = "Settings";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.systemOfEquationsRB);
            this.flowLayoutPanel1.Controls.Add(this.textRetypeRB);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(19, 40);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(155, 79);
            this.flowLayoutPanel1.TabIndex = 1;
            // 
            // systemOfEquationsRB
            // 
            this.systemOfEquationsRB.AutoSize = true;
            this.systemOfEquationsRB.Checked = true;
            this.systemOfEquationsRB.Location = new System.Drawing.Point(3, 3);
            this.systemOfEquationsRB.Name = "systemOfEquationsRB";
            this.systemOfEquationsRB.Size = new System.Drawing.Size(120, 17);
            this.systemOfEquationsRB.TabIndex = 0;
            this.systemOfEquationsRB.TabStop = true;
            this.systemOfEquationsRB.Text = "System of equations";
            this.systemOfEquationsRB.UseVisualStyleBackColor = true;
            // 
            // textRetypeRB
            // 
            this.textRetypeRB.AutoSize = true;
            this.textRetypeRB.Location = new System.Drawing.Point(3, 26);
            this.textRetypeRB.Name = "textRetypeRB";
            this.textRetypeRB.Size = new System.Drawing.Size(78, 17);
            this.textRetypeRB.TabIndex = 1;
            this.textRetypeRB.Text = "Text retype";
            this.textRetypeRB.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Task type:";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // btnSaveOptions
            // 
            this.btnSaveOptions.Location = new System.Drawing.Point(19, 468);
            this.btnSaveOptions.Name = "btnSaveOptions";
            this.btnSaveOptions.Size = new System.Drawing.Size(715, 26);
            this.btnSaveOptions.TabIndex = 2;
            this.btnSaveOptions.Text = "Save";
            this.btnSaveOptions.UseVisualStyleBackColor = true;
            this.btnSaveOptions.Click += new System.EventHandler(this.btnSaveOptions_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "BetterScreenTIme";
            this.tabControl1.ResumeLayout(false);
            this.tabBlockedApps.ResumeLayout(false);
            this.tabBlockedWebsites.ResumeLayout(false);
            this.tabBlockedWebsites.PerformLayout();
            this.tabFocusTime.ResumeLayout(false);
            this.tabConcentration.ResumeLayout(false);
            this.tabConcentration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).EndInit();
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabBlockedApps;
        private System.Windows.Forms.TabPage tabBlockedWebsites;
        private System.Windows.Forms.TabPage tabFocusTime;
        private System.Windows.Forms.TabPage tabConcentration;
        private System.Windows.Forms.CheckedListBox clbApps;
        private System.Windows.Forms.Button btnSaveApps;
        private System.Windows.Forms.TextBox tbWebsite;
        private System.Windows.Forms.Button btnAddWebsite;
        private System.Windows.Forms.ListBox lbWebsites;
        private System.Windows.Forms.Button btnRemoveWebsite;
        private System.Windows.Forms.Button btnSaveWebsites;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Button btnSaveFocus;
        private System.Windows.Forms.Label labelDuration;
        private System.Windows.Forms.NumericUpDown nudDuration;
        private System.Windows.Forms.TextBox tbAllowedSite;
        private System.Windows.Forms.Button btnAddAllowed;
        private System.Windows.Forms.ListBox lbAllowed;
        private System.Windows.Forms.Button btnRemoveAllowed;
        private System.Windows.Forms.Button btnStartConcentration;
        private System.Windows.Forms.Button btnSaveConcentration;
        private System.Windows.Forms.Button btnAddCustomApp;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label lblConcentrationRemaining;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton systemOfEquationsRB;
        private System.Windows.Forms.RadioButton textRetypeRB;
        private System.Windows.Forms.Button btnSaveOptions;
    }
}
