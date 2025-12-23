namespace Upload.gui
{
    partial class UiStoreSetting
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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btUpdate = new System.Windows.Forms.Button();
            this.treeFolder = new System.Windows.Forms.TreeView();
            this.numMinSession = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numMaxSession = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numCycleTime = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numMinSession)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSession)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCycleTime)).BeginInit();
            this.SuspendLayout();
            // 
            // btUpdate
            // 
            this.btUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btUpdate.Location = new System.Drawing.Point(127, 244);
            this.btUpdate.Name = "btUpdate";
            this.btUpdate.Size = new System.Drawing.Size(324, 24);
            this.btUpdate.TabIndex = 19;
            this.btUpdate.Text = "Update";
            this.btUpdate.UseVisualStyleBackColor = true;
            // 
            // treeFolder
            // 
            this.treeFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeFolder.Location = new System.Drawing.Point(12, 60);
            this.treeFolder.Name = "treeFolder";
            this.treeFolder.Size = new System.Drawing.Size(552, 178);
            this.treeFolder.TabIndex = 18;
            // 
            // numMinSession
            // 
            this.numMinSession.Location = new System.Drawing.Point(83, 8);
            this.numMinSession.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numMinSession.Name = "numMinSession";
            this.numMinSession.Size = new System.Drawing.Size(73, 20);
            this.numMinSession.TabIndex = 20;
            this.numMinSession.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 21;
            this.label1.Text = "Min session";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 23;
            this.label2.Text = "Max session";
            // 
            // numMaxSession
            // 
            this.numMaxSession.Location = new System.Drawing.Point(83, 34);
            this.numMaxSession.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numMaxSession.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numMaxSession.Name = "numMaxSession";
            this.numMaxSession.Size = new System.Drawing.Size(73, 20);
            this.numMaxSession.TabIndex = 22;
            this.numMaxSession.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numMaxSession.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(196, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(91, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Cycle update time";
            // 
            // numCycleTime
            // 
            this.numCycleTime.Location = new System.Drawing.Point(290, 8);
            this.numCycleTime.Name = "numCycleTime";
            this.numCycleTime.Size = new System.Drawing.Size(73, 20);
            this.numCycleTime.TabIndex = 24;
            this.numCycleTime.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numCycleTime.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // UiStoreSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 273);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.numCycleTime);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numMaxSession);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numMinSession);
            this.Controls.Add(this.btUpdate);
            this.Controls.Add(this.treeFolder);
            this.Name = "UiStoreSetting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "UiStore setting";
            this.Load += new System.EventHandler(this.UiStoreSetting_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numMinSession)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxSession)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numCycleTime)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btUpdate;
        private System.Windows.Forms.TreeView treeFolder;
        private System.Windows.Forms.NumericUpDown numMinSession;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numMaxSession;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numCycleTime;
    }
}