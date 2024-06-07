namespace RemoSharp.Utilities
{
    partial class LoginDialouge
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
            this.usernameBox = new System.Windows.Forms.TextBox();
            this.passwordBox = new System.Windows.Forms.TextBox();
            this.sessionIDBox = new System.Windows.Forms.TextBox();
            this.saveCheck = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.showPassCheck = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // usernameBox
            // 
            this.usernameBox.BackColor = System.Drawing.Color.White;
            this.usernameBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.usernameBox.Location = new System.Drawing.Point(72, 10);
            this.usernameBox.Margin = new System.Windows.Forms.Padding(1);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(181, 21);
            this.usernameBox.TabIndex = 0;
            // 
            // passwordBox
            // 
            this.passwordBox.BackColor = System.Drawing.Color.White;
            this.passwordBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.passwordBox.Location = new System.Drawing.Point(72, 33);
            this.passwordBox.Margin = new System.Windows.Forms.Padding(1);
            this.passwordBox.Name = "passwordBox";
            this.passwordBox.PasswordChar = '*';
            this.passwordBox.Size = new System.Drawing.Size(181, 21);
            this.passwordBox.TabIndex = 1;
            // 
            // sessionIDBox
            // 
            this.sessionIDBox.BackColor = System.Drawing.Color.White;
            this.sessionIDBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.sessionIDBox.Location = new System.Drawing.Point(72, 56);
            this.sessionIDBox.Margin = new System.Windows.Forms.Padding(1);
            this.sessionIDBox.Name = "sessionIDBox";
            this.sessionIDBox.Size = new System.Drawing.Size(181, 21);
            this.sessionIDBox.TabIndex = 2;
            // 
            // saveCheck
            // 
            this.saveCheck.AutoSize = true;
            this.saveCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.saveCheck.Location = new System.Drawing.Point(118, 83);
            this.saveCheck.Margin = new System.Windows.Forms.Padding(1);
            this.saveCheck.Name = "saveCheck";
            this.saveCheck.Size = new System.Drawing.Size(53, 19);
            this.saveCheck.TabIndex = 5;
            this.saveCheck.Text = "Save";
            this.saveCheck.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Username";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Password";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Session ID";
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(178, 79);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 3;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            this.okButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.okButton_KeyUp);
            // 
            // showPassCheck
            // 
            this.showPassCheck.AutoSize = true;
            this.showPassCheck.Location = new System.Drawing.Point(12, 85);
            this.showPassCheck.Name = "showPassCheck";
            this.showPassCheck.Size = new System.Drawing.Size(102, 17);
            this.showPassCheck.TabIndex = 4;
            this.showPassCheck.Text = "Show Password";
            this.showPassCheck.UseVisualStyleBackColor = true;
            this.showPassCheck.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            this.showPassCheck.CheckStateChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // LoginDialouge
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(263, 115);
            this.Controls.Add(this.showPassCheck);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.saveCheck);
            this.Controls.Add(this.sessionIDBox);
            this.Controls.Add(this.passwordBox);
            this.Controls.Add(this.usernameBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginDialouge";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Login";
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.RosyBrown;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox usernameBox;
        private System.Windows.Forms.TextBox passwordBox;
        private System.Windows.Forms.TextBox sessionIDBox;
        private System.Windows.Forms.CheckBox saveCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.CheckBox showPassCheck;
    }
}