namespace RemoSharp.Utilities
{
    partial class RemoLibraryInterface
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
            this.loadButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.purposeList = new System.Windows.Forms.ListBox();
            this.latestNicknames = new System.Windows.Forms.ListBox();
            this.inputBox = new System.Windows.Forms.TextBox();
            this.loginButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // loadButton
            // 
            this.loadButton.Enabled = false;
            this.loadButton.Location = new System.Drawing.Point(93, 412);
            this.loadButton.Name = "loadButton";
            this.loadButton.Size = new System.Drawing.Size(75, 23);
            this.loadButton.TabIndex = 0;
            this.loadButton.Text = "load";
            this.loadButton.UseVisualStyleBackColor = true;
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Enabled = false;
            this.saveButton.Location = new System.Drawing.Point(174, 412);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 1;
            this.saveButton.Text = "save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // deleteButton
            // 
            this.deleteButton.Enabled = false;
            this.deleteButton.Location = new System.Drawing.Point(255, 412);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(75, 23);
            this.deleteButton.TabIndex = 2;
            this.deleteButton.Text = "delete";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // purposeList
            // 
            this.purposeList.Enabled = false;
            this.purposeList.FormattingEnabled = true;
            this.purposeList.Location = new System.Drawing.Point(12, 12);
            this.purposeList.Name = "purposeList";
            this.purposeList.Size = new System.Drawing.Size(318, 368);
            this.purposeList.TabIndex = 4;
            this.purposeList.DoubleClick += new System.EventHandler(this.loadButton_Click);
            // 
            // latestNicknames
            // 
            this.latestNicknames.Enabled = false;
            this.latestNicknames.FormattingEnabled = true;
            this.latestNicknames.Location = new System.Drawing.Point(338, 12);
            this.latestNicknames.Name = "latestNicknames";
            this.latestNicknames.Size = new System.Drawing.Size(152, 69);
            this.latestNicknames.TabIndex = 5;
            // 
            // inputBox
            // 
            this.inputBox.Location = new System.Drawing.Point(12, 386);
            this.inputBox.Name = "inputBox";
            this.inputBox.Size = new System.Drawing.Size(318, 20);
            this.inputBox.TabIndex = 6;
            this.inputBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.inputBox_KeyUp);
            this.inputBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.inputBox_MouseUp);
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(12, 412);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(75, 23);
            this.loginButton.TabIndex = 7;
            this.loginButton.Text = "login";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new System.EventHandler(this.loginButton_Click);
            // 
            // RemoLibraryInterface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(503, 450);
            this.Controls.Add(this.loginButton);
            this.Controls.Add(this.inputBox);
            this.Controls.Add(this.latestNicknames);
            this.Controls.Add(this.purposeList);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.loadButton);
            this.Name = "RemoLibraryInterface";
            this.Text = "RemoLibraryInterface";
            this.Load += new System.EventHandler(this.RemoLibraryInterface_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button loadButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.ListBox purposeList;
        private System.Windows.Forms.ListBox latestNicknames;
        private System.Windows.Forms.TextBox inputBox;
        private System.Windows.Forms.Button loginButton;
    }
}