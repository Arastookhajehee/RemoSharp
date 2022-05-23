
namespace RemoSharp
{
    partial class StreamIPSet
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StreamIPSet));
            this.Srv_Add_Label = new System.Windows.Forms.Label();
            this.Full_Address_Box = new System.Windows.Forms.TextBox();
            this.DialougeTitle = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.IP_Address_Box = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.Port_Box = new System.Windows.Forms.TextBox();
            this.Set_Full_Address = new System.Windows.Forms.Button();
            this.Set_IP_Address = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // Srv_Add_Label
            // 
            this.Srv_Add_Label.AutoSize = true;
            this.Srv_Add_Label.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.Srv_Add_Label.Location = new System.Drawing.Point(12, 79);
            this.Srv_Add_Label.Name = "Srv_Add_Label";
            this.Srv_Add_Label.Size = new System.Drawing.Size(85, 16);
            this.Srv_Add_Label.TabIndex = 0;
            this.Srv_Add_Label.Text = "Full Address:";
            // 
            // Full_Address_Box
            // 
            this.Full_Address_Box.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.Full_Address_Box.Location = new System.Drawing.Point(104, 76);
            this.Full_Address_Box.Name = "Full_Address_Box";
            this.Full_Address_Box.Size = new System.Drawing.Size(360, 22);
            this.Full_Address_Box.TabIndex = 4;
            // 
            // DialougeTitle
            // 
            this.DialougeTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.DialougeTitle.AutoSize = true;
            this.DialougeTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.DialougeTitle.Location = new System.Drawing.Point(105, 11);
            this.DialougeTitle.Name = "DialougeTitle";
            this.DialougeTitle.Size = new System.Drawing.Size(365, 17);
            this.DialougeTitle.TabIndex = 2;
            this.DialougeTitle.Text = "Please Set Your Geometry Broadcasting Server Address";
            this.DialougeTitle.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.label2.Location = new System.Drawing.Point(12, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 16);
            this.label2.TabIndex = 0;
            this.label2.Text = "IP Address:";
            // 
            // IP_Address_Box
            // 
            this.IP_Address_Box.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.IP_Address_Box.Location = new System.Drawing.Point(104, 44);
            this.IP_Address_Box.Name = "IP_Address_Box";
            this.IP_Address_Box.Size = new System.Drawing.Size(185, 22);
            this.IP_Address_Box.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.label3.Location = new System.Drawing.Point(295, 47);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Port: ";
            // 
            // Port_Box
            // 
            this.Port_Box.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.Port_Box.Location = new System.Drawing.Point(339, 44);
            this.Port_Box.Name = "Port_Box";
            this.Port_Box.Size = new System.Drawing.Size(125, 22);
            this.Port_Box.TabIndex = 2;
            // 
            // Set_Full_Address
            // 
            this.Set_Full_Address.AutoSize = true;
            this.Set_Full_Address.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.Set_Full_Address.Location = new System.Drawing.Point(470, 74);
            this.Set_Full_Address.Name = "Set_Full_Address";
            this.Set_Full_Address.Size = new System.Drawing.Size(92, 26);
            this.Set_Full_Address.TabIndex = 5;
            this.Set_Full_Address.Text = "Set Address";
            this.Set_Full_Address.UseVisualStyleBackColor = true;
            this.Set_Full_Address.Click += new System.EventHandler(this.Set_Full_Address_Click);
            // 
            // Set_IP_Address
            // 
            this.Set_IP_Address.AutoSize = true;
            this.Set_IP_Address.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.Set_IP_Address.Location = new System.Drawing.Point(470, 42);
            this.Set_IP_Address.Name = "Set_IP_Address";
            this.Set_IP_Address.Size = new System.Drawing.Size(91, 26);
            this.Set_IP_Address.TabIndex = 3;
            this.Set_IP_Address.Text = "Set IP : Port";
            this.Set_IP_Address.UseVisualStyleBackColor = true;
            this.Set_IP_Address.Click += new System.EventHandler(this.Set_IP_Address_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.button1.Location = new System.Drawing.Point(339, 106);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(222, 26);
            this.button1.TabIndex = 6;
            this.button1.Text = "Set Internet Based Public Address";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.label4.Location = new System.Drawing.Point(12, 111);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(168, 16);
            this.label4.TabIndex = 0;
            this.label4.Text = "Public Internet Server Index";
            // 
            // comboBox1
            // 
            this.comboBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F);
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "01",
            "02",
            "03",
            "04",
            "05",
            "06",
            "07",
            "08",
            "09",
            "10"});
            this.comboBox1.Location = new System.Drawing.Point(205, 108);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(111, 24);
            this.comboBox1.TabIndex = 7;
            // 
            // StreamIPSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(575, 149);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Set_IP_Address);
            this.Controls.Add(this.Set_Full_Address);
            this.Controls.Add(this.DialougeTitle);
            this.Controls.Add(this.Port_Box);
            this.Controls.Add(this.IP_Address_Box);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.Full_Address_Box);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Srv_Add_Label);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "StreamIPSet";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "RemoSharp WebSocket Address Settings";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label Srv_Add_Label;
        private System.Windows.Forms.TextBox Full_Address_Box;
        public System.Windows.Forms.Label DialougeTitle;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox IP_Address_Box;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox Port_Box;
        private System.Windows.Forms.Button Set_Full_Address;
        private System.Windows.Forms.Button Set_IP_Address;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBox1;
    }
}