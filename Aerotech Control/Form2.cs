using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace Aerotech_Control
{
    public partial class Form2 : Form
    {
        int command_delay = 1000;

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

                    // Connect to Talisker Laser

                    Talisker_SerialPortCommunicator.SerialPort.PortName = "COM18";
                    Talisker_SerialPortCommunicator.SerialPort.BaudRate = 115200;

                    Talisker_SerialPortCommunicator.SerialPort.Close();
                    Talisker_SerialPortCommunicator.SerialPort.Open();

                    lbl_Talisker_Connection.Text = "Connected to Talisker Laser";
                    lbl_Talisker_Connection.Visible = true;
                    this.Update();
                      
                    // Connect to Watt Pilot

                    WattPilot_SerialPortCommunicator.SerialPort.PortName = "COM6";
                    WattPilot_SerialPortCommunicator.SerialPort.BaudRate = 38400;

                    WattPilot_SerialPortCommunicator.SerialPort.Close();
                    WattPilot_SerialPortCommunicator.SerialPort.Open();

                    lbl_Talisker_Connection.Text = "Connected to Watt Pilot";
                    this.Update();

                    elight_SerialPortCommunicator.SerialPort.PortName = "COM17";
                    elight_SerialPortCommunicator.SerialPort.BaudRate = 9600;

                    elight_SerialPortCommunicator.SerialPort.Close();
                    elight_SerialPortCommunicator.SerialPort.Open();

                    lbl_Talisker_Connection.Text = "Connected to eLight";
                    this.Update();

                    uScope_zoom_SerialPortCommunicator.SerialPort.PortName = "COM3";
                    uScope_zoom_SerialPortCommunicator.SerialPort.BaudRate = 9600;

                    uScope_zoom_SerialPortCommunicator.SerialPort.Close();
                    uScope_zoom_SerialPortCommunicator.SerialPort.Open();

                    lbl_Talisker_Connection.Text = "Connected to uScope Zoom";
                    this.Update();

                    laserbtninitialisation();

                    lbl_Talisker_Connection.Text = "Connected to Aerotech Stage";
                    this.Update();

                    lbl_Talisker_Connection.Text = "Connected to Ophir Power Meter";
                    this.Update();

                    Form1 MainForm = new Form1();

                    MainForm.Show();
                    MainForm.Activate();
                                     
                    this.Hide();

                }
            }
        }
               
        #region Talisker and Watt Pilot Control Functions

        private void laserbtninitialisation()
        {
            lbl_Talisker_Connection.Text = "Closing Shutter";
            this.Update();

            shutter_closed();

            lbl_Talisker_Connection.Text = "Shutter Closed";
            this.Update();

            lbl_Talisker_Connection.Text = "Setting Watt Pilot ATT = 100";
            this.Update();

            watt_pilot_attenuation(100);

            lbl_Talisker_Connection.Text = "Watt Pilot ATT = 100";
            this.Update();

            lbl_Talisker_Connection.Text = "Setting Talisker ATT = 100";
            this.Update();

            talisker_attenuation(100);

            lbl_Talisker_Connection.Text = "Talisker ATT = 100";
            this.Update();

            lbl_Talisker_Connection.Text = "Setting AOM Mode = Continuous";
            this.Update();

            aommode_0();

            lbl_Talisker_Connection.Text = "AOM Mode = Continuous";
            this.Update();

            lbl_Talisker_Connection.Text = "Setting AOM GATE = High";
            this.Update();

            aomgate_high_trigger();

            lbl_Talisker_Connection.Text = "AOM GATE = High";
            this.Update();

            talisker_burst_pulses(1);

            lbl_Talisker_Connection.Text = "Divisor = 1";
            this.Update();

            lbl_Talisker_Connection.Text = "Setting Repetition Rate = 200 kHz";
            this.Update();

            talisker_rep_rate(200000);

            lbl_Talisker_Connection.Text = "Repetition Rate = 200 kHz";
            this.Update();

        }

        // Functions for serial controls 

        private void shutter_closed()
        {
            string shutter_closed = "s=0\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(shutter_closed);
            Thread.Sleep(command_delay);
            //btn_Shutter.Text = "Open Shutter";
            //lbl_ShutterStatus.Text = "Closed";
            //lbl_ShutterStatus.BackColor = Color.Lime;
        }

        private void shutter_open()
        {
            string shutter_open = "s=1\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(shutter_open);
            Thread.Sleep(command_delay);
            //btn_Shutter.Text = "Close Shutter";
            //lbl_ShutterStatus.Text = "Open";
            //lbl_ShutterStatus.BackColor = Color.Red;
        }

        private void aommode_0()
        {
            string aommode0 = "AOMMODE=0\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aommode0);
            Thread.Sleep(command_delay);
            //lbl_AOMMode.Text = "Continuous";
        }

        private void aommode_2()
        {
            string aommode0 = "AOMMODE=2\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aommode0);
            Thread.Sleep(command_delay);
            //lbl_AOMMode.Text = "Divided";
        }

        private void aommode_3()
        {
            string aommode0 = "AOMMODE=3\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aommode0);
            Thread.Sleep(command_delay);
            //lbl_AOMMode.Text = "Burst";
        }

        private void aomgate_low_trigger()
        {
            string aomgate_low_trigger = "AOMGATE=0\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aomgate_low_trigger);
            Thread.Sleep(command_delay);
            //btn_AOMGATE.Text = "AOMGate - High";
            //lbl_AOMGateStatus.Text = "Low";
        }

        private void aomgate_high_trigger()
        {
            string aomgate_high_trigger = "AOMGATE=1\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aomgate_high_trigger);
            Thread.Sleep(command_delay);
            //btn_AOMGATE.Text = "AOMGate - Low";
            //lbl_AOMGateStatus.Text = "High";
        }

        private void talisker_attenuation(int value)
        {
            string atten_command = "ATT=100\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(atten_command);
            Thread.Sleep(5000);
            atten_command = "ATT=" + value + "\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(atten_command);
            Thread.Sleep(command_delay);
            //lbl_TaliskerATT.Text = value.ToString("0");
        }

        private void watt_pilot_attenuation(double value)
        {
            // Offset for watt pilot 1064 = -443 532 = +1520 355 = +7870
            double offset = -967;
            double stepsPerUnit = 43.333;
            double resolution = 2;
            double ratio = (100 - value) / 100;
            double angle = ((Math.Acos(Math.Sqrt(ratio))) * 180.0) / (2.0 * Math.PI);
            double steps = (angle * stepsPerUnit * resolution) + offset;
            string command = "g " + steps + "\r";
            WattPilot_SerialPortCommunicator.SerialPort.Write(command); //WattPilot_1064.Write(command);
            Thread.Sleep(5000);
            //lbl_WPATT.Text = (value).ToString("0.0");
        }

        private void talisker_burst_pulses(int value)
        {
            string burst_pulses = "BURST=" + value + "\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(burst_pulses);
            Thread.Sleep(command_delay);
            //lbl_BurstPulses.Text = value.ToString("0");
        }

        private void talisker_rep_rate(int value)
        {
            double divisor = 200000 / value;
            if ((divisor % 1) > 0)
            {
                MessageBox.Show("Not a integer divisor");
            }
            else
            {
                string rep_rate = "AOMD=" + divisor + "\r\n";
                Talisker_SerialPortCommunicator.SerialPort.Write(rep_rate);
                Thread.Sleep(command_delay);
                //lbl_RepRate.Text = value.ToString("0");
            }
        }


        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            

            elight_SerialPortCommunicator.SerialPort.Write("&");
            elight_SerialPortCommunicator.SerialPort.Write("iF");
            elight_SerialPortCommunicator.SerialPort.Write("\r");

            uScope_zoom_SerialPortCommunicator.SerialPort.Write("XH\r");

            Thread.Sleep(10 * 1000);

            elight_SerialPortCommunicator.SerialPort.Write("&");
            elight_SerialPortCommunicator.SerialPort.Write("iF2");
            elight_SerialPortCommunicator.SerialPort.Write("\r");

            uScope_zoom_SerialPortCommunicator.SerialPort.Write("XL\r");

            Thread.Sleep(10 * 1000);            

            elight_SerialPortCommunicator.SerialPort.Close();
            uScope_zoom_SerialPortCommunicator.SerialPort.Close();

        }
    }

    #region Setup Serial Ports

    public static class WattPilot_SerialPortCommunicator
        {
            private static SerialPort _wp_serialPort = new SerialPort();

            public static SerialPort SerialPort
            {
                get { return _wp_serialPort; }
                set { _wp_serialPort = value; }
            }
        }

        public static class Talisker_SerialPortCommunicator
        {
            private static SerialPort _serialPort = new SerialPort();

            public static SerialPort SerialPort
            {
                get { return _serialPort; }
                set { _serialPort = value; }
            }
        }

    public static class elight_SerialPortCommunicator
    {
        private static SerialPort _elight_serialPort = new SerialPort();

        public static SerialPort SerialPort
        {
            get { return _elight_serialPort; }
            set { _elight_serialPort = value; }
        }
    }

    public static class uScope_zoom_SerialPortCommunicator
    {
        private static SerialPort _uScope_zoom_serialPort = new SerialPort();

        public static SerialPort SerialPort
        {
            get { return _uScope_zoom_serialPort; }
            set { _uScope_zoom_serialPort = value; }
        }
    }

    #endregion
}
