using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aerotech_Control
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            this.BringToFront();
            pwd_txtbx.Text = "";
            pwd_txtbx.PasswordChar = '*';
        }

        private void pwd_txtbx_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (pwd_txtbx.Text != "umbro")
                {
                    this.Close();
                    Environment.Exit(1);
                }
                else if (pwd_txtbx.Text == "umbro")
                  {
                    Form1 MainForm = new Form1();

                    MainForm.Show();
                    

                }
            }
        }
    }
}
