namespace Aerotech_Control
{
    partial class Form2
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form2));
            this.pwd_txtbx = new System.Windows.Forms.TextBox();
            this.lbl_Talisker_Connection = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lbl_Password_Prompt = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pwd_txtbx
            // 
            this.pwd_txtbx.Location = new System.Drawing.Point(249, 155);
            this.pwd_txtbx.Name = "pwd_txtbx";
            this.pwd_txtbx.Size = new System.Drawing.Size(115, 20);
            this.pwd_txtbx.TabIndex = 0;
            this.pwd_txtbx.KeyDown += new System.Windows.Forms.KeyEventHandler(this.pwd_txtbx_KeyDown);
            // 
            // lbl_Talisker_Connection
            // 
            this.lbl_Talisker_Connection.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Talisker_Connection.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Talisker_Connection.Location = new System.Drawing.Point(11, 188);
            this.lbl_Talisker_Connection.Name = "lbl_Talisker_Connection";
            this.lbl_Talisker_Connection.Size = new System.Drawing.Size(354, 19);
            this.lbl_Talisker_Connection.TabIndex = 7;
            this.lbl_Talisker_Connection.Text = "Connected To Talikser Laser";
            this.lbl_Talisker_Connection.Visible = false;
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panel1.Controls.Add(this.lbl_Password_Prompt);
            this.panel1.Controls.Add(this.lbl_Talisker_Connection);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1366, 768);
            this.panel1.TabIndex = 8;
            // 
            // lbl_Password_Prompt
            // 
            this.lbl_Password_Prompt.AutoSize = true;
            this.lbl_Password_Prompt.BackColor = System.Drawing.Color.Transparent;
            this.lbl_Password_Prompt.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Password_Prompt.Location = new System.Drawing.Point(12, 154);
            this.lbl_Password_Prompt.Name = "lbl_Password_Prompt";
            this.lbl_Password_Prompt.Size = new System.Drawing.Size(182, 19);
            this.lbl_Password_Prompt.TabIndex = 8;
            this.lbl_Password_Prompt.Text = "Please Type Password";
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1366, 768);
            this.ControlBox = false;
            this.Controls.Add(this.pwd_txtbx);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox pwd_txtbx;
        private System.Windows.Forms.Label lbl_Talisker_Connection;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lbl_Password_Prompt;
    }
}