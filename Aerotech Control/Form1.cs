using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading;
using System.IO;

using Aerotech.A3200;
using Aerotech.A3200.Exceptions;
using Aerotech.A3200.Status;
using Aerotech.A3200.Variables;
using Aerotech.A3200.Tasks;
using Aerotech.A3200.Information;
using Aerotech.Common;
using Aerotech.Common.Collections;
using Aerotech.A3200.Status.Custom;
using Aerotech.A3200.Units;

using TIS.Imaging;

namespace Aerotech_Control
{
    enum sensorProperty { Range, Wavelength, Diffuser, Mode, Pulselength, Threshold, Filter, Trigger };

    public partial class Form1 : Form
    {
        //Ophir Device

        OphirLMMeasurementLib.CoLMMeasurement lm_Co1;

        // Camera 

        public TIS.Imaging.ICImagingControl ImagingControl;
        private string DeviceState;
        private const string NOT_AVAILABLE = "n\a";

        #region Global Variables Aerotech and Control

        private Controller myController;
        private int axisIndex;
        private int taskIndex;
        ControllerDiagPacket controllerDiagPacket;


        List<string> listA = new List<string>();
        List<string> listB = new List<string>();
        List<string> listC = new List<string>();
        List<string> zVal = new List<string>();

        //int list_Size = 0;

        double JogValue = 0.1;

        // Corner Coordinates

        double A_Xaxis = 0;
        double A_Yaxis = 0;
        double A_Zaxis = 0;
        double A_Daxis = 0;

        double B_Xaxis = 0;
        double B_Yaxis = 0;
        double B_Zaxis = 0;
        double B_Daxis = 0;

        double C_Xaxis = 0;
        double C_Yaxis = 0;
        double C_Zaxis = 0;
        double C_Daxis = 0;

        double D_Xaxis = 0;
        double D_Yaxis = 0;
        double D_Zaxis = 0;
        double D_Daxis = 0;

        int hold = 0;
        double theta_hold = 0;
        double phi_hold = 0;

        double[] Refined_Xaxis = new double[4];
        double[] Refined_Yaxis = new double[4];

        double StepIn = 0.25;
        double MarkLength = 0.25;

        double Z_Scan_length = 0.5;
        double Mark_Number = 10;
        double Line_Spacing = 0.25;

        double ablation_focus;
        double microscope_focus;

        double Offset_Xaxis;
        double Offset_Yaxis;

        double[] Correction_Xaxis = new double[4];
        double[] Correction_Yaxis = new double[4];

        double OffsetAccurate_Xaxis;
        double OffsetAccurate_Yaxis;

        double[] LaserFocus_Xaxis = new double[8];
        double[] LaserFocus_Yaxis = new double[8];
        double[] LaserFocus_Zaxis = new double[8];

        int command_delay = 1000;

        private static AutoResetEvent CornerAlignmentEvent = new AutoResetEvent(false);
        private static AutoResetEvent LaserAlignEvent = new AutoResetEvent(false);

        TextWriter CornersX = new StreamWriter("//CIM-UP/Share/Chris/TEST/Aerotech Control/Reference Values/CornersX.txt");
        TextWriter CornersY = new StreamWriter("//CIM-UP/Share/Chris/TEST/Aerotech Control/Reference Values/CornersY.txt");
        TextWriter CornersZ = new StreamWriter("//CIM-UP/Share/Chris/TEST/Aerotech Control/Reference Values/CornersZ.txt");

        #endregion

        private void SetTaskState(NewTaskStatesArrivedEventArgs e)
        {
            lbl_TaskState.Text = e.TaskStates[this.taskIndex].ToString();
        }

        public Form1()
        {
            InitializeComponent();
            backgroundWorker_AlignCorners.RunWorkerAsync(); // Background task for post intial tlit correction position
            backgroundWorker_LaserAlign.RunWorkerAsync(); // Background task for laser uScope Alignment
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btn_UpdateCoords.Enabled = false;

            // Set initial stage jog value

            lbl_JogValue.Text = JogValue.ToString() + " mm";

            ////Initialising Microscope Camera

            //if (!icImagingControl1.DeviceValid)
            //{
            //    icImagingControl1.ShowDeviceSettingsDialog();

            //    if (!icImagingControl1.DeviceValid)
            //    {
            //        MessageBox.Show("No device was selected.", "Grabbing an Image",
            //                         MessageBoxButtons.OK, MessageBoxIcon.Information);
            //        this.Close();
            //    }
            //}
            //else
            //{
            //    //if (ImagingControl.DeviceFlipHorizontalAvailable)
            //    //{
            //    //ImagingControl.DeviceFlipHorizontal = true;
            //    //}
            //    icImagingControl1.LiveStart();
            //}

            //// Used for background thread process

            //Control.CheckForIllegalCrossThreadCalls = false;

            //// Initialise Serial Control

            //// Talisker

            //TalikserLaser.PortName = "COM18";
            //TalikserLaser.BaudRate = 115200;

            //TalikserLaser.Close();
            //TalikserLaser.Open();

            //// Watt Pilot

            //WattPilot_1064.PortName = "COM6";
            //WattPilot_1064.BaudRate = 38400;

            //WattPilot_1064.Close();
            //WattPilot_1064.Open();

            // Ophir Device

            lm_Co1 = new OphirLMMeasurementLib.CoLMMeasurement();

            // Register delegates
            lm_Co1.DataReady += new OphirLMMeasurementLib._ICoLMMeasurementEvents_DataReadyEventHandler(this.DataReadyHandler);
            lm_Co1.PlugAndPlay += new OphirLMMeasurementLib._ICoLMMeasurementEvents_PlugAndPlayEventHandler(this.PlugAndPlayHandler);

        }

        private void btn_ConnectController_Click(object sender, EventArgs e)
        {
            try
            {
                // Connect to A3200 controller.  
                this.myController = Controller.Connect();
                chkbx_ConnectedVal.Checked = true;
                //EnableControls(true);

                btn_ConnectController.Enabled = false;
                btn_DisconnectController.Enabled = true;

                // populate axis names
                foreach (AxisInfo axis in this.myController.Information.Axes)
                {
                    cmb_AxisNames.Items.Add(axis.Name);
                }
                this.axisIndex = 0;
                cmb_AxisNames.SelectedIndex = this.axisIndex;

                // populate task names
                foreach (Task task in this.myController.Tasks)
                {
                    if (task.State != TaskState.Inactive)
                    {
                        cmb_AxisNames.Items.Add(task.Name.ToString());
                    }
                }
                // Task 0 is reserved
                this.taskIndex = 1;
                cmb_AxisNames.SelectedIndex = this.taskIndex - 1;

                // register task state and diagPackect arrived events
                this.myController.ControlCenter.TaskStates.NewTaskStatesArrived += new EventHandler<NewTaskStatesArrivedEventArgs>(TaskStates_NewTaskStatesArrived);
                this.myController.ControlCenter.Diagnostics.NewDiagPacketArrived += new EventHandler<NewDiagPacketArrivedEventArgs>(Diagnostics_NewDiagPacketArrived);

            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        private void btn_DisconnectController_Click(object sender, EventArgs e)
        {
            try
            {
                // Disconnect the A3200 controller.
                Controller.Disconnect();
                chkbx_ConnectedVal.Checked = false;

                grpbx_AxControl.Enabled = false;
                btn_ConnectController.Enabled = true;
                btn_DisconnectController.Enabled = false;

                lbl_XStatus.Text = "Disabled";
                lbl_XStatus.BackColor = System.Drawing.SystemColors.Control;

                lbl_YStatus.Text = "Disabled";
                lbl_YStatus.BackColor = System.Drawing.SystemColors.Control;

                lbl_ZStatus.Text = "Disabled";
                lbl_ZStatus.BackColor = System.Drawing.SystemColors.Control;

                lbl_DStatus.Text = "Disabled";
                lbl_DStatus.BackColor = System.Drawing.SystemColors.Control;

                lbl_AStatus.Text = "Disabled";
                lbl_AStatus.BackColor = System.Drawing.SystemColors.Control;

                lbl_BStatus.Text = "Disabled";
                lbl_BStatus.BackColor = System.Drawing.SystemColors.Control;
            }
            catch (A3200Exception exception)
            {
                // lbl_ErrorMsg.Text = exception.Message;
            }
        }

        #region Position Updates

        private void SetAxisState(NewDiagPacketArrivedEventArgs e)
        {
            /*lbl_EnabledState.Text = e.Data[this.axisIndex].DriveStatus.Enabled.ToString();
            lbl_HomeState.Text = e.Data[this.axisIndex].AxisStatus.Homed.ToString();
            lbl_FaultState.Text = (!e.Data[this.axisIndex].AxisFault.None).ToString();
            lbl_PositionState.Text = e.Data[this.axisIndex].PositionFeedback.ToString();
            lbl_SpeedState.Text = e.Data[this.axisIndex].VelocityFeedback.ToString();*/

            // Axis position update
            //lbl_XPos.Text = String.Format("{0:#,0.000}", e.Data[0].PositionFeedback);
            //lbl_XPos.Text = e.Data[this.axisIndex].PositionFeedback.ToString();

            lbl_XPos.Text = String.Format("{0:#,0.000}", e.Data["X"].PositionFeedback);
            lbl_YPos.Text = String.Format("{0:#,0.000}", e.Data["Y"].PositionFeedback);
            lbl_ZPos.Text = String.Format("{0:#,0.000}", e.Data["Z"].PositionFeedback);
            lbl_DPos.Text = String.Format("{0:#,0.000}", e.Data["D"].PositionFeedback);
            lbl_APos.Text = String.Format("{0:#,0.000}", e.Data["A"].PositionFeedback);
            lbl_BPos.Text = String.Format("{0:#,0.000}", e.Data["B"].PositionFeedback);

            lbl_XPosUnit.Text = e.Data["X"].PositionUnitName;
            lbl_YPosUnit.Text = e.Data["Y"].PositionUnitName;
            lbl_ZPosUnit.Text = e.Data["Z"].PositionUnitName;
            lbl_DPosUnit.Text = e.Data["D"].PositionUnitName;
            lbl_APosUnit.Text = e.Data["A"].PositionUnitName;
            lbl_BPosUnit.Text = e.Data["B"].PositionUnitName;

            // Axis velocity update
            lbl_XVel.Text = String.Format("{0:#,0.000}", e.Data["X"].VelocityFeedback);
            lbl_YVel.Text = String.Format("{0:#,0.000}", e.Data["Y"].VelocityFeedback);
            lbl_ZVel.Text = String.Format("{0:#,0.000}", e.Data["Z"].VelocityFeedback);
            lbl_DVel.Text = String.Format("{0:#,0.000}", e.Data["D"].VelocityFeedback);
            lbl_AVel.Text = String.Format("{0:#,0.000}", e.Data["A"].VelocityFeedback);
            lbl_BVel.Text = String.Format("{0:#,0.000}", e.Data["B"].VelocityFeedback);

            lbl_XVelUnit.Text = e.Data["X"].VelocityUnitName;
            lbl_YVelUnit.Text = e.Data["Y"].VelocityUnitName;
            lbl_ZVelUnit.Text = e.Data["Z"].VelocityUnitName;
            lbl_DVelUnit.Text = e.Data["D"].VelocityUnitName;
            lbl_AVelUnit.Text = e.Data["A"].VelocityUnitName;
            lbl_BVelUnit.Text = e.Data["B"].VelocityUnitName;


            // X axis status update
            if ((!e.Data[this.axisIndex].AxisFault.None))
            {
                lbl_XStatus.Text = "Fault";
                lbl_XStatus.BackColor = System.Drawing.Color.Red;
            }
            else if (e.Data["X"].AxisStatus.Homed == true & e.Data["X"].DriveStatus.Enabled == true)
            {
                lbl_XStatus.Text = "Homed";
                lbl_XStatus.BackColor = System.Drawing.ColorTranslator.FromHtml("#85F055");
            }
            else if (e.Data["X"].DriveStatus.Enabled == true)
            {
                lbl_XStatus.Text = "Enabled";
                lbl_XStatus.BackColor = System.Drawing.Color.Yellow;
            }
            else if (e.Data["X"].DriveStatus.Enabled == false)
            {
                lbl_XStatus.Text = "Disabled";
                lbl_XStatus.BackColor = System.Drawing.SystemColors.Control;
            }

            // Y axis status update
            if (e.Data["Y"].AxisStatus.Homed == true & e.Data["Y"].DriveStatus.Enabled == true)
            {
                lbl_YStatus.Text = "Homed";
                lbl_YStatus.BackColor = System.Drawing.ColorTranslator.FromHtml("#85F055");
            }
            else if (e.Data["Y"].DriveStatus.Enabled == true)
            {
                lbl_YStatus.Text = "Enabled";
                lbl_YStatus.BackColor = System.Drawing.Color.Yellow;
            }
            else if (e.Data["Y"].DriveStatus.Enabled == false)
            {
                lbl_YStatus.Text = "Disabled";
                lbl_YStatus.BackColor = System.Drawing.SystemColors.Control;
            }

            // Z axis status update
            if (e.Data["Z"].AxisStatus.Homed == true & e.Data["Z"].DriveStatus.Enabled == true)
            {
                lbl_ZStatus.Text = "Homed";
                lbl_ZStatus.BackColor = System.Drawing.ColorTranslator.FromHtml("#85F055");
            }
            else if (e.Data["Z"].DriveStatus.Enabled == true)
            {
                lbl_ZStatus.Text = "Enabled";
                lbl_ZStatus.BackColor = System.Drawing.Color.Yellow;
            }
            else if (e.Data["Z"].DriveStatus.Enabled == false)
            {
                lbl_ZStatus.Text = "Disabled";
                lbl_ZStatus.BackColor = System.Drawing.SystemColors.Control;
            }

            //D axis status update
            if (e.Data["D"].AxisStatus.Homed == true & e.Data["D"].DriveStatus.Enabled == true)
            {
                lbl_DStatus.Text = "Homed";
                lbl_DStatus.BackColor = System.Drawing.ColorTranslator.FromHtml("#85F055");
            }
            else if (e.Data["D"].DriveStatus.Enabled == true)
            {
                lbl_DStatus.Text = "Enabled";
                lbl_DStatus.BackColor = System.Drawing.Color.Yellow;
            }
            else if (e.Data["D"].DriveStatus.Enabled == false)
            {
                lbl_DStatus.Text = "Disabled";
                lbl_DStatus.BackColor = System.Drawing.SystemColors.Control;
            }

            //A axis status update

            if (e.Data["A"].AxisStatus.Homed == true & e.Data["A"].DriveStatus.Enabled == true)
            {
                lbl_AStatus.Text = "Homed";
                lbl_AStatus.BackColor = System.Drawing.ColorTranslator.FromHtml("#85F055");
            }
            else if (e.Data["A"].DriveStatus.Enabled == true)
            {
                lbl_AStatus.Text = "Enabled";
                lbl_AStatus.BackColor = System.Drawing.Color.Yellow;
            }
            else if (e.Data["A"].DriveStatus.Enabled == false)
            {
                lbl_AStatus.Text = "Disabled";
                lbl_AStatus.BackColor = System.Drawing.SystemColors.Control;
            }

            //B axis status update
            if (e.Data["B"].AxisStatus.Homed == true & e.Data["B"].DriveStatus.Enabled == true)
            {
                lbl_BStatus.Text = "Homed";
                lbl_BStatus.BackColor = System.Drawing.ColorTranslator.FromHtml("#85F055");
            }
            else if (e.Data["B"].DriveStatus.Enabled == true)
            {
                lbl_BStatus.Text = "Enabled";
                lbl_BStatus.BackColor = System.Drawing.Color.Yellow;
            }
            else if (e.Data["B"].DriveStatus.Enabled == false)
            {
                lbl_BStatus.Text = "Disabled";
                lbl_BStatus.BackColor = System.Drawing.SystemColors.Control;
            }

            //cont_XPos = e.Data[0].PositionFeedback;
            //cont_YPos = e.Data[1].PositionFeedback;
            //cont_ZPos = e.Data[2].PositionFeedback;
        }

        #endregion


        #region ControllerEvents

        /// <summary>
        /// Handle task state arrived event. Invoke SetTaskState to process data
        /// </summary>
        private void TaskStates_NewTaskStatesArrived(object sender, NewTaskStatesArrivedEventArgs e)
        {
            try
            {
                //URL: http://msdn.microsoft.com/en-us/library/ms171728.aspx
                //How to: Make Thread-Safe Calls to Windows Forms Controls
                this.Invoke(new Action<NewTaskStatesArrivedEventArgs>(SetTaskState), e);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Handle DiagPacket (axis state in it) arrived event. Invoke SetAxisState to process data
        /// </summary>
        private void Diagnostics_NewDiagPacketArrived(object sender, NewDiagPacketArrivedEventArgs e)
        {
            try
            {
                //URL: http://msdn.microsoft.com/en-us/library/ms171728.aspx
                //How to: Make Thread-Safe Calls to Windows Forms Controls
                this.Invoke(new Action<NewDiagPacketArrivedEventArgs>(SetAxisState), e);
            }
            catch
            {
            }
        }

        #endregion ControllerEvents

        #region Enable Axes Buttons

        private void btn_XOnOff_Click_1(object sender, EventArgs e)
        {
            controllerDiagPacket = myController.DataCollection.RetrieveDiagnostics();

            if (controllerDiagPacket["X"].DriveStatus.Enabled)
            {
                try
                {
                    myController.Commands.Axes["X"].Motion.Disable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
            else
            {
                try
                {
                    myController.Commands.Axes["X"].Motion.Enable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
        }

        private void btn_YOnOff_Click(object sender, EventArgs e)
        {
            controllerDiagPacket = myController.DataCollection.RetrieveDiagnostics();

            if (controllerDiagPacket["Y"].DriveStatus.Enabled)
            {
                try
                {
                    myController.Commands.Axes["Y"].Motion.Disable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
            else
            {
                try
                {
                    myController.Commands.Axes["Y"].Motion.Enable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
        }

        private void btn_ZOnOff_Click(object sender, EventArgs e)
        {
            controllerDiagPacket = myController.DataCollection.RetrieveDiagnostics();

            if (controllerDiagPacket["Z"].DriveStatus.Enabled)
            {
                try
                {
                    myController.Commands.Axes["Z"].Motion.Disable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
            else
            {
                try
                {
                    myController.Commands.Axes["Z"].Motion.Enable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
        }

        private void btn_DOnOff_Click(object sender, EventArgs e)
        {
            controllerDiagPacket = myController.DataCollection.RetrieveDiagnostics();

            if (controllerDiagPacket["D"].DriveStatus.Enabled)
            {
                try
                {
                    myController.Commands.Axes["D"].Motion.Disable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
            else
            {
                try
                {
                    myController.Commands.Axes["D"].Motion.Enable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
        }

        private void btn_AOnOff_Click(object sender, EventArgs e)
        {
            controllerDiagPacket = myController.DataCollection.RetrieveDiagnostics();

            if (controllerDiagPacket["A"].DriveStatus.Enabled)
            {
                try
                {
                    myController.Commands.Axes["A"].Motion.Disable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
            else
            {
                try
                {
                    myController.Commands.Axes["A"].Motion.Enable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
        }

        private void btn_BOnOff_Click(object sender, EventArgs e)
        {
            controllerDiagPacket = myController.DataCollection.RetrieveDiagnostics();

            if (controllerDiagPacket["B"].DriveStatus.Enabled)
            {
                try
                {
                    myController.Commands.Axes["B"].Motion.Disable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
            else
            {
                try
                {
                    myController.Commands.Axes["B"].Motion.Enable();
                }
                catch (A3200Exception exception)
                {
                    //lbl_ErrorMsg.Text = exception.Message;
                }
            }
        }

        #endregion

        #region Home Axes Buttons

        private void btn_HomeProcedure_Click(object sender, EventArgs e)
        {
            myController.Commands.AcknowledgeAll();

            myController.Commands.Axes["X", "D"].Motion.Enable();

            myController.Commands.Axes["D"].Motion.Home();

            myController.Commands.Motion.Linear("X", -5, 5);

            myController.Commands.Axes["Y", "Z", "A", "B"].Motion.Enable();

            myController.Commands.Motion.Linear("X", 5, 5);

            myController.Commands.Axes["Z"].Motion.Home();

            myController.Commands.Axes["Y"].Motion.Home();

            myController.Commands.Axes["X"].Motion.Home();

            myController.Commands.Motion.Setup.Absolute();

            myController.Commands.Motion.Linear("Z", -3, 1);

            myController.Commands.Motion.Linear("X", -5, 1);

            myController.Commands.Motion.Linear("Y", 2.25, 1);

            myController.Commands.Axes["A"].Motion.Home();

            myController.Commands.Axes["B"].Motion.Home();
        }

        private void btn_EnableAir_Click(object sender, EventArgs e)
        {
            myController.Commands.IO.DigitalOutputBit(0, "X", 1);
        }

        private void btn_XHome_Click(object sender, EventArgs e)
        {
            try
            {
                myController.Commands.Axes["X"].Motion.Home();
            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        private void btn_YHome_Click(object sender, EventArgs e)
        {
            try
            {
                myController.Commands.Axes["Y"].Motion.Home();
            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        private void btn_ZHome_Click(object sender, EventArgs e)
        {
            try
            {
                myController.Commands.Axes["Z"].Motion.Home();
            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        private void btn_DHome_Click(object sender, EventArgs e)
        {
            try
            {
                myController.Commands.Axes["D"].Motion.Home();
            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        private void btn_AHome_Click(object sender, EventArgs e)
        {
            try
            {
                myController.Commands.Axes["A"].Motion.Home();
            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        private void btn_BHome_Click(object sender, EventArgs e)
        {
            try
            {
                myController.Commands.Axes["B"].Motion.Home();
            }
            catch (A3200Exception exception)
            {
                //lbl_ErrorMsg.Text = exception.Message;
            }
        }

        #endregion

        #region Changing Jog Variable Value

        private void btn_jog00001_Click(object sender, EventArgs e)
        {
            JogValue = 0.0001;
            lbl_JogValue.Text = JogValue.ToString() + " mm";
        }

        private void btn_jog0001_Click(object sender, EventArgs e)
        {
            JogValue = 0.001;
            lbl_JogValue.Text = JogValue.ToString() + " mm";
        }

        private void btn_jog001_Click(object sender, EventArgs e)
        {
            JogValue = 0.01;
            lbl_JogValue.Text = JogValue.ToString() + " mm";
        }

        private void btn_jog01_Click(object sender, EventArgs e)
        {
            JogValue = 0.1;
            lbl_JogValue.Text = JogValue.ToString() + " mm";
        }

        private void btn_jog1_Click(object sender, EventArgs e)
        {
            JogValue = 1;
            lbl_JogValue.Text = JogValue.ToString() + " mm";
        }

        #endregion

        #region Jog Movements

        private void btn_PosJogX_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("X", JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_NegJogX_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("X", -JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_PosJogY_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Y", JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_NegJogY_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Y", -JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_PosJogZ_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Z", JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_NegJogZ_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Z", -JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_PosJogD_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("D", JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_NegJogD_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("D", -JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        private void btn_PosJogA_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("A", -JogValue, 1);
            myController.Commands.Motion.Setup.Absolute();
        }

        #endregion

        private void btn_StartAlignment_Click(object sender, EventArgs e)
        {

            ablation_focus = Convert.ToDouble(File.ReadAllText("C:/Users/User/Desktop/Share/Chris/TEST/Aerotech Control/Reference Values/ABLATION_FOCUS.txt", Encoding.UTF8)); /// SPECIFY PATH
            microscope_focus = Convert.ToDouble(File.ReadAllText("C:/Users/User/Desktop/Share/Chris/TEST/Aerotech Control/Reference Values/MICROSCOPE.txt", Encoding.UTF8)); /// SPECIFY PATH

            double Start_Xaxis = -36;
            double Start_Yaxis = -21.5;

            myController.Commands.Motion.Setup.Absolute();
            myController.Commands.Axes["X", "Y", "Z", "D", "A", "B"].Motion.Linear(new double[] { Start_Xaxis, Start_Yaxis, ablation_focus, microscope_focus, 0, 0 }, 2.5);

        }

        #region Collect Initial Corner Positions

        private void btn_cornerA_Click(object sender, EventArgs e)
        {
            A_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            A_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            A_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            A_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            CornersX.WriteLine(A_Xaxis);
            CornersY.WriteLine(A_Yaxis);
            CornersZ.WriteLine(A_Zaxis);

            btn_cornerA.Enabled = false;
        }

        private void btn_cornerB_Click(object sender, EventArgs e)
        {
            B_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            B_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            B_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            B_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            CornersX.WriteLine(B_Xaxis);
            CornersY.WriteLine(B_Yaxis);
            CornersZ.WriteLine(B_Zaxis);

            btn_cornerB.Enabled = false;
        }

        private void btn_cornerC_Click(object sender, EventArgs e)
        {
            C_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            C_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            C_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            C_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            CornersX.WriteLine(B_Xaxis);
            CornersY.WriteLine(B_Yaxis);
            CornersZ.WriteLine(B_Zaxis);

            btn_cornerC.Enabled = false;
        }

        private void btn_cornerD_Click(object sender, EventArgs e)
        {
            D_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            D_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            D_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            D_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            CornersX.WriteLine(D_Xaxis);
            CornersY.WriteLine(D_Yaxis);
            CornersZ.WriteLine(D_Zaxis);

            CornersX.Close();
            CornersY.Close();
            CornersZ.Close();

            btn_cornerD.Enabled = false;
        }

        #endregion

        #region uScope Tilt Correction

        private void btn_TiltCorrection_Click(object sender, EventArgs e)
        {

            PlaneFit();

            MessageBox.Show("Realign and refocus corner");

            btn_UpdateCoords.Enabled = true;

            CornerAlignmentEvent.Set();

        }

        private void PlaneFit()

        {
            // Set enviroment for matlab instance
            var activationContext = Type.GetTypeFromProgID("matlab.application.single");
            var matlab = (MLApp.MLApp)Activator.CreateInstance(activationContext);
            string tilt_path = "cd('C:/Users/User/Desktop/Share/Chris/TEST/Aerotech Control/Matlab Code')"; // change to path on UP PC
            matlab.Execute(tilt_path);

            // Send corner coordinates to Matlab

            matlab.PutWorkspaceData("X", "base", new[] { A_Xaxis, B_Xaxis, C_Xaxis, D_Xaxis });
            matlab.PutWorkspaceData("Y", "base", new[] { A_Yaxis, B_Yaxis, C_Yaxis, D_Yaxis });
            matlab.PutWorkspaceData("Z", "base", new[] { A_Zaxis, B_Zaxis, C_Zaxis, D_Zaxis });

            // Calculate rotation angles

            matlab.Execute("[theta,phi] = rot_angles(X,Y,Z)");

            // Get rotation angles

            double theta = matlab.GetVariable("theta", "base");
            double phi = matlab.GetVariable("phi", "base");

            theta_hold = theta_hold + theta;
            phi_hold = phi_hold + phi;

            // Apply rotation angles

            myController.Commands.Motion.Setup.Absolute();
            MessageBox.Show(theta_hold.ToString());
            myController.Commands.Motion.Linear("A", theta_hold, 1);
            MessageBox.Show(phi_hold.ToString());
            myController.Commands.Motion.Linear("B", phi_hold, 1);
        }

        private void backgroundWorkerAlignCorners_DoWork(object sender, DoWorkEventArgs e)
        {
            Update_Corners();
        }

        private void Update_Corners()
        {
            for (int i = 0; i < 5; i++)
            {
                CornerAlignmentEvent.WaitOne();

                if (i == 0)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { A_Xaxis, A_Yaxis }, 1);
                    btn_UpdateCoords.Text = "Realign Corner A";

                    hold = 0;
                }
                else if (i == 1)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { B_Xaxis, B_Yaxis }, 1);
                    btn_UpdateCoords.Text = "Realign Corner B";
                    hold = 1;
                }
                else if (i == 2)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { C_Xaxis, C_Yaxis }, 1);
                    btn_UpdateCoords.Text = "Realign Corner C";
                    hold = 2;
                }
                else if (i == 3)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { D_Xaxis, D_Yaxis }, 1);
                    btn_UpdateCoords.Text = "Realign Corner D";
                    hold = 3;
                }
                else if (i == 4)
                {
                    btn_UpdateCoords.Text = "Corners Reaquired";
                    btn_UpdateCoords.Enabled = false;
                    MessageBox.Show("Calculating new tilt");
                    PlaneFit();
                }
            }
            hold = 0;
        }

        private void btn_UpdateCoords_Click_1(object sender, EventArgs e)
        {
            if (hold == 0)
            {
                //MessageBox.Show("Aquiring Axes Pos A");
                A_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                A_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                A_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                A_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            }
            else if (hold == 1)
            {
                //MessageBox.Show("Aquiring Axes Pos B");
                B_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                B_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                B_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                B_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            }
            else if (hold == 2)
            {
                //MessageBox.Show("Aquiring Axes Pos C");
                C_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                C_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                C_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                C_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            }
            else if (hold == 3)
            {
                //MessageBox.Show("Aquiring Axes Pos D");
                D_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                D_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                D_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                D_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
                microscope_focus = (A_Daxis + B_Daxis + C_Daxis + D_Daxis) / 4;
            }

            CornerAlignmentEvent.Set();
        }

        #endregion

        #region Align Laser and uScope

        private void btn_AlignLaseruScope_Click(object sender, EventArgs e)
        {
            // Find X dimensions

            if (A_Xaxis > B_Xaxis)
            {
                Refined_Xaxis[0] = A_Xaxis + StepIn;
                Refined_Xaxis[1] = A_Xaxis + StepIn;
            }
            else
            {
                Refined_Xaxis[0] = B_Xaxis + StepIn;
                Refined_Xaxis[1] = B_Xaxis + StepIn;
            }

            if (D_Xaxis > C_Xaxis)
            {
                Refined_Xaxis[2] = C_Xaxis - StepIn;
                Refined_Xaxis[3] = C_Xaxis - StepIn;
            }
            else
            {
                Refined_Xaxis[2] = D_Xaxis - StepIn;
                Refined_Xaxis[3] = D_Xaxis - StepIn;
            }

            // Find Y dimensions

            if (A_Yaxis > D_Yaxis)
            {
                Refined_Yaxis[0] = D_Yaxis - StepIn;
                Refined_Yaxis[3] = D_Yaxis - StepIn;
            }
            else
            {
                Refined_Yaxis[0] = A_Yaxis - StepIn;
                Refined_Yaxis[3] = A_Yaxis - StepIn;
            }

            if (B_Yaxis > C_Xaxis)
            {
                Refined_Yaxis[1] = B_Yaxis - StepIn;
                Refined_Yaxis[2] = B_Yaxis - StepIn;
            }
            else
            {
                Refined_Yaxis[1] = C_Yaxis + StepIn;
                Refined_Yaxis[2] = C_Yaxis + StepIn;
            }

            // Find previous offset 

            Offset_Xaxis = Convert.ToDouble(File.ReadAllText("C:/Users/User/Desktop/Share/Chris/TEST/Aerotech Control/Reference Values/OFFSETX.txt", Encoding.UTF8));
            Offset_Yaxis = Convert.ToDouble(File.ReadAllText("C:/Users/User/Desktop/Share/Chris/TEST/Aerotech Control/Reference Values/OFFSETY.txt", Encoding.UTF8));

            // Set laser parameters

            myController.Commands.Motion.Linear("D", 0, 2);
            myController.Commands.Motion.Linear("Z", ablation_focus, 1);

            shutter_open();
            aommode_0();
            aomgate_high_trigger();
            talisker_attenuation(80);
            watt_pilot_attenuation(100);

            // Mark Fiducial Markers

            myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[0] - Offset_Xaxis, Refined_Yaxis[0] - Offset_Yaxis }, 5);
            MarkCross();
            myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[1] - Offset_Xaxis, Refined_Yaxis[1] - Offset_Yaxis }, 5);
            MarkCross();
            myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[2] - Offset_Xaxis, Refined_Yaxis[2] - Offset_Yaxis }, 5);
            MarkCross();
            myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[3] - Offset_Xaxis, Refined_Yaxis[3] - Offset_Yaxis }, 5);
            MarkCross();

            // Check Alignment

            LaserAlignEvent.Set();
        }

        private void backgroundWorker_LaserAlign_DoWork(object sender, DoWorkEventArgs e)
        {
            CollectMarkerPos();
        }

        private void CollectMarkerPos()
        {
            for (int i = 0; i < 4; i++)
            {
                LaserAlignEvent.WaitOne();

                hold = i;

                myController.Commands.Motion.Setup.Absolute();
                myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[i], Refined_Yaxis[i] }, 1);
                myController.Commands.Axes["D"].Motion.Linear(new double[] { microscope_focus });

                if (i == 3)
                {
                    MessageBox.Show("Calculating Accurate offset");
                    OffsetAccurate_Xaxis = Offset_Xaxis + Correction_Xaxis.Sum();
                    OffsetAccurate_Yaxis = Offset_Yaxis + Correction_Xaxis.Sum();
                    btn_MarkerAligned.Enabled = false;
                }
            }
        }

        private void btn_MarkerAligned_Click(object sender, EventArgs e)
        {
            Correction_Xaxis[hold] = (myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback) - Refined_Xaxis[hold]);
            Correction_Yaxis[hold] = (myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback) - Refined_Yaxis[hold]);
            LaserAlignEvent.Set();
        }

        private void MarkCross()
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("X", -MarkLength / 2, 1);
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            myController.Commands.Motion.Linear("X", MarkLength, 1);
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            myController.Commands.Motion.Linear("X", -MarkLength / 2, 1);
            myController.Commands.Motion.Linear("Y", -MarkLength / 2, 1);
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            myController.Commands.Motion.Linear("Y", MarkLength, 1);
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            myController.Commands.Motion.Setup.Absolute();
        }

        #endregion

        #region Laser Focus and Tilt Correction

        private void btn_FindFocus_Click(object sender, EventArgs e)
        {
            FindFocus_Xaxis();
            FindFocus_Yaxis();
        }

        private void FindFocus_Xaxis()
        {
            double Current_X = 0;
            double Current_Y = 0;

            myController.Commands.Motion.Linear("D", 0, 1);

            shutter_open();
            aommode_0();
            aomgate_high_trigger();
            talisker_attenuation(90);
            watt_pilot_attenuation(100);

            for (int hold = 0; hold < 4; hold++)
            {
                myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] + MarkLength / 2 - OffsetAccurate_Xaxis, 1);

                if (hold == 0)
                {
                    myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] - 0.5 - OffsetAccurate_Yaxis, 1);
                }
                else if (hold == 1)
                {
                    myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5 - OffsetAccurate_Yaxis, 1);
                }
                else if (hold == 2)
                {
                    myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5 - OffsetAccurate_Yaxis, 1);
                }
                else if (hold == 3)
                {
                    myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] - 0.5 - OffsetAccurate_Yaxis, 1);
                }
                // X axis find focus 
                for (int i = 0; i < Mark_Number; i++)
                {
                    // Change Z height
                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("Z", -Z_Scan_length / 2 + Z_Scan_length / Mark_Number * i, 1);
                    myController.Commands.Motion.Linear("Y", -(Line_Spacing * hold), 1);
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
                    myController.Commands.Motion.Linear("X", MarkLength, 1);
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
                    myController.Commands.Motion.Linear("X", -MarkLength, 1);
                    myController.Commands.Motion.Setup.Absolute();
                }
            }

            // Inspect Lines and record minimum line thickness

            for (int hold = 0; hold < 4; hold++)
            {
                Current_X = Refined_Xaxis[hold] + MarkLength / 2;

                myController.Commands.Motion.Linear("X", Current_X, 1);

                if (hold == 0)
                {
                    Current_Y = Refined_Yaxis[hold] - 0.5;
                }
                else if (hold == 1)
                {
                    Current_Y = Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5;
                }
                else if (hold == 2)
                {
                    Current_Y = Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5;
                }
                else if (hold == 3)
                {
                    Current_Y = Refined_Yaxis[hold] - 0.5;
                }

                myController.Commands.Motion.Linear("Y", Current_Y, 1);

                // Bring microscope into focus

                myController.Commands.Motion.Linear("D", microscope_focus, 1);

                int pos_count = 0; // Variable for recording position

                for (int i = 0; i < Mark_Number; i++)
                {
                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("X", -MarkLength / 2, 1);
                    myController.Commands.Motion.Linear("Y", -(Line_Spacing * hold), 1);
                    DialogResult Focus_Result = MessageBox.Show("Is this result larger than the previous result?", "Important Question", MessageBoxButtons.YesNo);
                    if (Focus_Result == DialogResult.Yes)
                    {
                        // Recording position of focus point
                        LaserFocus_Xaxis[pos_count] = Current_X - MarkLength / 2;
                        LaserFocus_Yaxis[pos_count] = Current_Y - (Line_Spacing * (hold - 1));
                        LaserFocus_Zaxis[pos_count] = ablation_focus - Z_Scan_length / 2 + Z_Scan_length / Mark_Number * (i - 1);
                        pos_count++;
                    }
                    myController.Commands.Motion.Setup.Absolute();
                }
            }
            shutter_closed();
        }

        // Y axis find focus 

        private void FindFocus_Yaxis()
        {
            double Current_X = 0;
            double Current_Y = 0;

            myController.Commands.Motion.Linear("D", 0, 1);

            shutter_open();
            aommode_0();
            aomgate_high_trigger();
            talisker_attenuation(90);
            watt_pilot_attenuation(100);

            for (int hold = 0; hold < 4; hold++)
            {
                myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] + MarkLength / 2 - OffsetAccurate_Yaxis, 1);

                if (hold == 0)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5 - OffsetAccurate_Xaxis, 1);
                }
                else if (hold == 1)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5 - OffsetAccurate_Xaxis, 1);
                }
                else if (hold == 2)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] + 0.5 - OffsetAccurate_Xaxis, 1);
                }
                else if (hold == 3)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] + 0.5 - OffsetAccurate_Xaxis, 1);
                }

                // Mark lines

                for (int i = 0; i < Mark_Number; i++)
                {
                    // Change Z height
                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("Z", ablation_focus - Z_Scan_length / 2 + Z_Scan_length / Mark_Number * i, 1);
                    myController.Commands.Motion.Linear("X", (Line_Spacing * hold), 1);
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
                    myController.Commands.Motion.Linear("Y", -MarkLength, 1);
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
                    myController.Commands.Motion.Linear("Y", MarkLength, 1);
                    myController.Commands.Motion.Setup.Absolute();
                }
            }

            // Inspect Lines and record minimum line thickness

            for (int hold = 0; hold < 4; hold++)
            {

                Current_Y = Refined_Yaxis[hold] + MarkLength / 2;

                myController.Commands.Motion.Linear("Y", Current_Y, 1);

                if (hold == 0)
                {
                    Current_X = Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5;
                }
                else if (hold == 1)
                {
                    Current_X = Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5;
                }
                else if (hold == 2)
                {
                    Current_X = Refined_Xaxis[hold] + 0.5;
                }
                else if (hold == 3)
                {
                    Current_X = Refined_Xaxis[hold] + 0.5;
                }

                myController.Commands.Motion.Linear("X", Current_X, 1);

                // Bring microscope into focus

                myController.Commands.Motion.Linear("D", microscope_focus, 1);

                int pos_count = 4; // Variable for recording position

                // Y axis find focus 
                for (int i = 0; i < Mark_Number; i++)
                {
                    // Change Z height
                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("X", (Line_Spacing * hold), 1);
                    myController.Commands.Motion.Linear("Y", -MarkLength / 2, 1);
                    DialogResult Focus_Result = MessageBox.Show("Is this result larger than the previous result?", "Important Question", MessageBoxButtons.YesNo);
                    if (Focus_Result == DialogResult.Yes)
                    {
                        // Recording position of focus point
                        LaserFocus_Xaxis[pos_count] = Current_X + (Line_Spacing * (hold - 1));
                        LaserFocus_Yaxis[pos_count] = Current_Y - MarkLength / 2;
                        LaserFocus_Zaxis[pos_count] = ablation_focus - Z_Scan_length / 2 + Z_Scan_length / Mark_Number * (i - 1);
                        pos_count++;
                    }
                    myController.Commands.Motion.Setup.Absolute();
                }
            }
            shutter_closed();
        }

        #endregion

        #region Finding Centre of Rotation

        private void Rotation_Centre()
        {
            double CentreTestRotation = 10;

            // Move to rough marker position 

            myController.Commands.Motion.Setup.Absolute();
            myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[1], Refined_Yaxis[1] }, 1);
            myController.Commands.Motion.Linear("D", 15, 1);

            // Apply +10 degree rotation 

            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("B", CentreTestRotation, 1);

            // Centre marker and find focus



            // Apply -10 degree rotation

            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("B", -2 * CentreTestRotation, 1);

            // Calculate centre of rotation 

            double Gradient = Math.Tan(CentreTestRotation * Math.PI / 180);

            //double Y_Centre = 
        }

        #endregion

        #region Talisker and Watt Pilot Control Functions

        // Functions for serial controls 

        private void shutter_closed()
        {
            string shutter_closed = "s=0\r\n";
            TalikserLaser.Write(shutter_closed);
            Thread.Sleep(command_delay);
            btn_Shutter.Text = "Open Shutter";
            lbl_ShutterStatus.Text = "Closed";
            lbl_ShutterStatus.BackColor = Color.Lime;
        }

        private void shutter_open()
        {
            string shutter_open = "s=1\r\n";
            TalikserLaser.Write(shutter_open);
            Thread.Sleep(command_delay);
            btn_Shutter.Text = "Close Shutter";
            lbl_ShutterStatus.Text = "Open";
            lbl_ShutterStatus.BackColor = Color.Red;
        }

        private void aommode_0()
        {
            string aommode0 = "AOMMODE=0\r\n";
            TalikserLaser.Write(aommode0);
            Thread.Sleep(command_delay);
            lbl_AOMMode.Text = "Continuous";
        }

        private void aommode_2()
        {
            string aommode0 = "AOMMODE=2\r\n";
            TalikserLaser.Write(aommode0);
            Thread.Sleep(command_delay);
            lbl_AOMMode.Text = "Divided";
        }

        private void aommode_3()
        {
            string aommode0 = "AOMMODE=3\r\n";
            TalikserLaser.Write(aommode0);
            Thread.Sleep(command_delay);
            lbl_AOMMode.Text = "Burst";
        }

        private void aomgate_low_trigger()
        {
            string aomgate_low_trigger = "AOMGATE=0\r\n";
            TalikserLaser.Write(aomgate_low_trigger);
            Thread.Sleep(command_delay);
            btn_AOMGATE.Text = "AOMGate - High";
            lbl_AOMGateStatus.Text = "Low";
        }

        private void aomgate_high_trigger()
        {
            string aomgate_high_trigger = "AOMGATE=1\r\n";
            TalikserLaser.Write(aomgate_high_trigger);
            Thread.Sleep(command_delay);
            btn_AOMGATE.Text = "AOMGate - Low";
            lbl_AOMGateStatus.Text = "High";
        }

        private void talisker_attenuation(int value)
        {
            string atten_command = "ATT=" + value + "\r\n";
            TalikserLaser.Write(atten_command);
            Thread.Sleep(command_delay);
            lbl_TaliskerATT.Text = value.ToString("0");
        }

        private void watt_pilot_attenuation(double value)
        {
            // Offset for watt pilot 1064 = -443 532 = +1520 355 = +7870
            double offset = -443;
            double stepsPerUnit = 43.333;
            double resolution = 2;
            double ratio = (100 - value) / 100;
            double angle = ((Math.Acos(Math.Sqrt(ratio))) * 180.0) / (2.0 * Math.PI);
            double steps = (angle * stepsPerUnit * resolution) + offset;
            string command = "g " + steps + "\r";
            WattPilot_1064.Write(command);
            Thread.Sleep(5000);
            lbl_WPATT.Text = (100 - value).ToString("0.0");
        }



        #endregion

        #region Ophir Device Integration

        /*
         * General functions to get/set sensor properties.
         */

        private void getProperty(object sender, sensorProperty prop)
        {
            try
            {
                //displayNoError();

                int nHandle = getCurrentDeviceHandle();
                if (nHandle == 0)
                    return;

                // Tag property of Get button is set to the channel number
                // using the Property Editor.
                int nChannel = Convert.ToInt32(((Button)sender).Tag);

                int index;
                Object options;

                switch (prop)
                {
                    case sensorProperty.Range:
                        lm_Co1.GetRanges(nHandle, nChannel, out index, out options);
                        //fillComboBox(RangeCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Wavelength:
                        lm_Co1.GetWavelengths(nHandle, nChannel, out index, out options);
                        //fillComboBox(WavelengthCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Diffuser:
                        lm_Co1.GetDiffuser(nHandle, nChannel, out index, out options);
                        //fillComboBox(DiffuserCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Mode:
                        lm_Co1.GetMeasurementMode(nHandle, nChannel, out index, out options);
                        //fillComboBox(ModeCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Pulselength:
                        lm_Co1.GetPulseLengths(nHandle, nChannel, out index, out options);
                        //fillComboBox(PulseLengthCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Threshold:
                        lm_Co1.GetThreshold(nHandle, nChannel, out index, out options);
                        //fillComboBox(ThresholdCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Filter:
                        lm_Co1.GetFilter(nHandle, nChannel, out index, out options);
                        //fillComboBox(FilterCombos[nChannel], options, index);
                        break;

                    case sensorProperty.Trigger:
                        lm_Co1.GetExtTrigOnOff(nHandle, nChannel, out index, out options);
                        //fillComboBox(TriggerCombos[nChannel], options, index);
                        break;

                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                //displayError(ex);
            }

        }

        /*
         * Data Ready event handler.
         */
        void DataReadyHandler(int hDevice, int channel)
        // Get the measured data from the OphirCOM object and display it
        {
            try
            {
                object dataArray;
                object timeStampArray;
                object statusArray;

                // Get the measured data from the OphirCOM object
                lm_Co1.GetData(hDevice, channel, out dataArray, out timeStampArray, out statusArray);

                if (Convert.ToInt32(HandleComboBox.SelectedItem) != hDevice)
                    return;

                // Extract the data from the arrays 
                if (((double[])dataArray).Length > 0)
                {
                    double[] dataArr = (double[])dataArray;
                    double[] tsArr = (double[])timeStampArray;
                    int[] statusArr = (int[])statusArray;

                    // Initialize measured data from the current displayed data
                    string timestampStr = LabelTime0.Text;
                    string measurementStr = LabelMeasurement0.Text;
                    string statusStr = LabelStatus0.Text;
                    //string xPositionStr = XPositionLabels[channel].Text;
                    //string yPositionStr = YPositionLabels[channel].Text;
                    //string sizeStr = SizeLabels[channel].Text;

                    // Values of the possible measurements types
                    int powerEnergyMeasurementType = 0;
                    //int xPositionMeasurementType = 1;
                    //int yPositionMeasurementType = 2;
                    //int sizeMeasurementType = 3;
                    //int eventMeasurementType = 4;

                    // Values of the possible statuses
                    int okStatus = 0;
                    int accuracyWarningStatus = 2;

                    for (int ind = 0; ind < dataArr.Length; ind++)
                    {
                        timestampStr = tsArr[ind].ToString();

                        // Each int type element in statusArr[] holds in its two high
                        // bytes the measurement type and in the two low bytes the status.
                        // Extract these two values.
                        int measurementType = statusArr[ind] / 0x10000;// high bytes 
                        int status = statusArr[ind] % 0x10000;// low bytes

                        // Power or energy measurement
                        if (measurementType == powerEnergyMeasurementType)
                        {
                            measurementStr = dataArr[ind].ToString();
                            //statusStr = GetStatus(statusArr[ind]);
                        }
                        // BeamTrack measurements
                        //else if (measurementType == xPositionMeasurementType)   // X Position
                        //{
                        //    if (status == okStatus)
                        //        xPositionStr = dataArr[ind].ToString();
                        //    else
                        //        xPositionStr = GetStatus(statusArr[ind]);
                        //}
                        //else if (measurementType == yPositionMeasurementType)  // Y Position
                        //{
                        //    if (status == okStatus)
                        //        yPositionStr = dataArr[ind].ToString();
                        //    else
                        //        yPositionStr = GetStatus(statusArr[ind]);
                        //}
                        //else if (measurementType == sizeMeasurementType) // Size
                        //{
                        //    if (status == okStatus || status == accuracyWarningStatus)
                        //        sizeStr = dataArr[ind].ToString();
                        //    else
                        //        sizeStr = GetStatus(statusArr[ind]);
                        //}
                        //else if (measurementType == eventMeasurementType)
                        //{
                        //    measurementStr = dataArr[ind].ToString();
                        //    statusStr = GetStatus(statusArr[ind]);
                        //}
                    }//for (int ind = 0;

                    // Display last measured data
                    LabelTime0.Text = timestampStr;
                    LabelMeasurement0.Text = measurementStr;
                    LabelStatus0.Text = statusStr;
                    //XPositionLabels[channel].Text = xPositionStr;
                    //YPositionLabels[channel].Text = yPositionStr;
                    //SizeLabels[channel].Text = sizeStr;
                }//if (((double[])dataArray).Length > 0)
            }//try
            catch (Exception ex)
            {
                //displayError(ex);
            }            
        }

        //private string GetStatus(int index)
        //{
        //    if (statusText.ContainsKey(index))  // if unknown status - ignore it, else - get it
        //        return statusText[index];
        //    else
        //        return "";
        //}

        /*
         * Plug and Play event handler.
         */
        void PlugAndPlayHandler()
        {
            MessageBox.Show("Device has been removed from the USB.");
        }

        // Auxiliary functions
        private int getCurrentDeviceHandle()
        {
            int h;

            if (HandleComboBox.Items.Count < 1)
            {
                MessageBox.Show("Open device and then choose a device handle from the combo box.");
                return 0;
            }

            h = Convert.ToInt32(HandleComboBox.SelectedItem);
            if (h == 0)
                MessageBox.Show("Choose a device handle from the combo box.");

            return h;
        }

        private void btn_ScanUSB_Click(object sender, EventArgs e)
        {
            try
            {
                //displayNoError();
                Cursor.Current = Cursors.WaitCursor;
                Object serialNumbers;
                lm_Co1.ScanUSB(out serialNumbers);
                Cursor.Current = Cursors.Default;
                DeviceListBox.Items.Clear();
                DeviceListBox.Items.AddRange((Object[])serialNumbers);
                if (DeviceListBox.Items.Count > 0)
                    DeviceListBox.SetSelected(0, true);
            }
            catch (Exception ex)
            {
                //displayError(ex);
            }
        }

        private void btn_OpenDevice_Click(object sender, EventArgs e)
        {
            try
            {
                //displayNoError();
                string snStr = DeviceListBox.SelectedItem.ToString();
                if (snStr == "") return;

                int hDevice;
                lm_Co1.OpenUSBDevice(snStr, out hDevice);
                HandleComboBox.Items.Add(hDevice.ToString());
                HandleComboBox.SelectedItem = hDevice.ToString();
            }
            catch (Exception ex)
            {
                //displayError(ex);
            }
        }
             
        private void btn_StartStream_Click(object sender, EventArgs e)
        {
            bool exists;

            int nHandle = getCurrentDeviceHandle();
            if (nHandle == 0)
                return;

            //displayNoError();

            //ClearMeasurementsData();

            for (int nChannel = 0; nChannel < 4; nChannel++)
            {

                try
                {
                    lm_Co1.IsSensorExists(nHandle, nChannel, out exists);
                    if (!exists)
                        continue;

                    lm_Co1.StartStream(nHandle, nChannel);
                }
                catch (Exception ex)
                {
                    //displayError(ex);
                }
            }
        }

        #endregion

        #region Laser Buttons and Control

        private void btn_Shutter_Click(object sender, EventArgs e)
        {
            if (lbl_ShutterStatus.Text == "Closed")
            {
                shutter_open();                
            }
            else if (lbl_ShutterStatus.Text == "Open")
            {
                shutter_closed();                
            }
        }

        private void txtbx_RequestedWPATT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                watt_pilot_attenuation(Convert.ToDouble(txtbx_RequestedTaliskerATT));
            }
        }

        private void txtbx_RequestedTaliskerATT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                talisker_attenuation(Convert.ToInt32(txtbx_RequestedTaliskerATT));
            }
        }

        private void btn_aommode0_Click(object sender, EventArgs e)
        {
            aommode_0();
        }

        private void btn_aommode2_Click(object sender, EventArgs e)
        {
            aommode_2();
        }

        private void btn_aommode3_Click(object sender, EventArgs e)
        {
            aommode_3();
        }

        #endregion

        private void btn_AOMGATE_Click(object sender, EventArgs e)
        {
            if (lbl_AOMGateStatus.Text == "High")
            {
                aomgate_low_trigger();
            }
            else if (lbl_AOMGateStatus.Text == "Low")
            {
                aomgate_high_trigger();
            }
        }
    }





}

