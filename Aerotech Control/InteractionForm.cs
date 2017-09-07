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

using Spiricon.BeamGage.Automation;
using Spiricon.Interfaces.ConsoleService;
using Spiricon.TreePattern;

namespace Aerotech_Control
{
    enum sensorProperty { Range, Wavelength, Diffuser, Mode, Pulselength, Threshold, Filter, Trigger };

    public partial class InteractionForm : Form
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

        double[] A_coords = { 0, 0, 0 }; // X Y Z
        double[] B_coords = { 0, 0, 0 }; // X Y Z
        double[] C_coords = { 0, 0, 0 }; // X Y Z
        double[] D_coords = { 0, 0, 0 }; // X Y Z

        //double A_Xaxis = 0;
        //double A_Yaxis = 0;
        //double A_Zaxis = 0;
        //double A_Daxis = 0;

        //double B_Xaxis = 0;
        //double B_Yaxis = 0;
        //double B_Zaxis = 0;
        //double B_Daxis = 0;

        //double C_Xaxis = 0;
        //double C_Yaxis = 0;
        //double C_Zaxis = 0;
        //double C_Daxis = 0;

        //double D_Xaxis = 0;
        //double D_Yaxis = 0;
        //double D_Zaxis = 0;
        //double D_Daxis = 0;

        double Point1_Xaxis;
        double Point1_Yaxis;
        double Point1_Zaxis;
        double Point1_Daxis;
        
        int hold = 0;
        double theta_hold = 0;
        double phi_hold = 0;
        double B_hold = 0;

        double[] Refined_Xaxis = new double[4];
        double[] Refined_Yaxis = new double[4];

        double StepIn = 0.25;
        
        double ablation_focus;
        double microscope_focus;

        // Z stage position for uScope
        double z_stage_position = 37.410445144916;

        double min_zoom_focus_d_btn;
        double min_zoom_focus_x_btn;
        double min_zoom_focus_y_btn;
        double microscope_zoom_x_correction = 0;
        double microscope_zoom_y_correction = 0;
        double zoom_offset = 0.123;
        

        double Offset_Xaxis = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/OFFSETX.txt", Encoding.UTF8));
        double Offset_Yaxis = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/OFFSETY.txt", Encoding.UTF8));

        double[] Correction_Xaxis = new double[4];
        double[] Correction_Yaxis = new double[4];

        //double OffsetAccurate_Xaxis;
        //double OffsetAccurate_Yaxis;

        double[] LaserFocus_Xaxis = new double[8];
        double[] LaserFocus_Yaxis = new double[8];
        double[] LaserFocus_Zaxis = new double[8];

        //double ablation_focus_accurate;

        double Z_Offset;
        double Y_Offset;

        double Rot_Z_Coords;
        double Rot_Y_Coords;

        int command_delay = 1000;
        double current_WP_value = 100;

        string power_record_file_path;
        string universal_path;

        int record_power = 0;
        Dictionary<int, string> statusText;

        private static AutoResetEvent CornerAlignmentEvent = new AutoResetEvent(false);
        private static AutoResetEvent LaserAlignEvent = new AutoResetEvent(false);

        // Declare the BeamGage Automation client
        private AutomatedBeamGage _bgtest;

        #endregion

        private void SetTaskState(NewTaskStatesArrivedEventArgs e)
        {
            lbl_TaskState.Text = e.TaskStates[this.taskIndex].ToString();
        }

        public InteractionForm()
        {
            InitializeComponent();
            backgroundWorker_AlignCorners.RunWorkerAsync(); // Background task for post intial tlit correction position
            backgroundWorker_LaserAlign.RunWorkerAsync(); // Background task for laser uScope Alignment

            statusText = new Dictionary<int, string>();
            statusText.Add(0, "OK");
            statusText.Add(1, "OVERRANGE");
            statusText.Add(2, "SATURATED");
            statusText.Add(3, "MISSING PULSE");
            statusText.Add(4, "RESET STATE IN ENERGY MEASUREMENT");
            statusText.Add(5, "WAITING");
            statusText.Add(6, "SUMMING");
            statusText.Add(7, "TIMEOUT");
            statusText.Add(8, "PEAK OVER");
            statusText.Add(9, "ENERGY OVER");

            statusText.Add(0x10000, "OK");          // x position ok
            statusText.Add(0x10000 + 1, "ERROR");   // x position error
            statusText.Add(0x20000, "OK");          // y position ok
            statusText.Add(0x20000 + 1, "ERROR");   // y position error
            statusText.Add(0x30000, "OK");          // size ok
            statusText.Add(0x30000 + 1, "ERROR");   // size error
            statusText.Add(0x30000 + 2, "WARNING"); // size warning
            statusText.Add(0x40000 + 1, "EVENT - SETTING CHANGED"); // event
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            btn_UpdateCoords.Enabled = false;

            // Set initial stage jog value

            lbl_JogValue.Text = JogValue.ToString() + " mm";

            //Initialising Microscope Camera

            Connect_Camera();

            //Laser and Button Initialisation

            laserbtninitialisation_labels();

            // Used for background thread process

            Control.CheckForIllegalCrossThreadCalls = false;                        
            
            // Ophir Device

            lm_Co1 = new OphirLMMeasurementLib.CoLMMeasurement();

            // Register delegates
            lm_Co1.DataReady += new OphirLMMeasurementLib._ICoLMMeasurementEvents_DataReadyEventHandler(this.DataReadyHandler);
            lm_Co1.PlugAndPlay += new OphirLMMeasurementLib._ICoLMMeasurementEvents_PlugAndPlayEventHandler(this.PlugAndPlayHandler);

            // Start Background Power Meter Reading

            ScanUSB();
            OpenDevice();
            Thread power_log = new Thread(new ThreadStart(start_power_monitoring));
            //power_log.Priority = ThreadPriority.Highest;
            power_log.IsBackground = true;
            power_log.Start();

            // Connect to Aerotech Controller

            Thread Aerotech_Axis_Pos = new Thread(new ThreadStart(Connect_Controller));
            Aerotech_Axis_Pos.IsBackground = true;
            Aerotech_Axis_Pos.Start();

            // Set Microscope

            eLight_Intensity(15);

            // Load Reference Values

            ablation_focus = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/ABLATION_FOCUS.txt", Encoding.UTF8)); /// SPECIFY PATH
            microscope_focus = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/MICROSCOPE.txt", Encoding.UTF8)); /// SPECIFY PATH   

            // Start BeamGuage Instance 

            _bgtest = new AutomatedBeamGage("ClientOne", true);
        }

        private void Connect_Controller()
        {
            try
            {
                // Connect to A3200 controller.  
                this.myController = Controller.Connect();                
                //EnableControls(true);
                              

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

        private void Connect_Camera()
        {
            if (!icImagingControl1.DeviceValid)
            {
                icImagingControl1.ShowDeviceSettingsDialog();

                if (!icImagingControl1.DeviceValid)
                {
                    MessageBox.Show("No device was selected.", "Grabbing an Image",
                                     MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            else
            {
                icImagingControl1.LiveDisplayDefault = false;

                icImagingControl1.LiveDisplayHeight = icImagingControl1.Height;
                icImagingControl1.LiveDisplayWidth = icImagingControl1.Width;
                icImagingControl1.LiveStart();

                OverlayBitmap ob = icImagingControl1.OverlayBitmap;
                //   Enable the overlay bitmap for drawing.

                ob.Enable = true;
                ob.DrawSolidRect(System.Drawing.Color.Red, 798, 530, 802, 670);
                ob.DrawSolidRect(System.Drawing.Color.Red, 730, 602, 870, 598);
            }
        }


        //private void btn_DisconnectController_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        // Disconnect the A3200 controller.
        //        Controller.Disconnect();
        //        chkbx_ConnectedVal.Checked = false;

        //        grpbx_AxControl.Enabled = false;
        //        btn_ConnectController.Enabled = true;
        //        btn_DisconnectController.Enabled = false;

        //        lbl_XStatus.Text = "Disabled";
        //        lbl_XStatus.BackColor = System.Drawing.SystemColors.Control;

        //        lbl_YStatus.Text = "Disabled";
        //        lbl_YStatus.BackColor = System.Drawing.SystemColors.Control;

        //        lbl_ZStatus.Text = "Disabled";
        //        lbl_ZStatus.BackColor = System.Drawing.SystemColors.Control;

        //        lbl_DStatus.Text = "Disabled";
        //        lbl_DStatus.BackColor = System.Drawing.SystemColors.Control;

        //        lbl_AStatus.Text = "Disabled";
        //        lbl_AStatus.BackColor = System.Drawing.SystemColors.Control;

        //        lbl_BStatus.Text = "Disabled";
        //        lbl_BStatus.BackColor = System.Drawing.SystemColors.Control;
        //    }
        //    catch (A3200Exception exception)
        //    {
        //        // lbl_ErrorMsg.Text = exception.Message;
        //    }
        //}

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

            lbl_XPos.Text = String.Format("{0:#,0.00000}", e.Data["X"].PositionFeedback);
            lbl_YPos.Text = String.Format("{0:#,0.00000}", e.Data["Y"].PositionFeedback);
            lbl_ZPos.Text = String.Format("{0:#,0.00000}", e.Data["Z"].PositionFeedback);
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
            myController.Commands.IO.DigitalOutputBit(0, "X", 1);

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
            this.folderBrowserDialog1.SelectedPath = "C:/Users/User/Desktop/Share/Chris/Abatlion Threshold - 50mm/Silicon";
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                universal_path = folderBrowserDialog1.SelectedPath;
            }

            ablation_focus = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/ABLATION_FOCUS.txt", Encoding.UTF8)); /// SPECIFY PATH
            microscope_focus = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/MICROSCOPE.txt", Encoding.UTF8)); /// SPECIFY PATH

            double Start_Xaxis = -36;
            double Start_Yaxis = -21.5;

            myController.Commands.Motion.Setup.Absolute();
            myController.Commands.Motion.Linear("D", 0, 2);
            myController.Commands.Axes["X", "Y", "Z", "D", "A", "B"].Motion.Linear(new double[] { Start_Xaxis, Start_Yaxis, ablation_focus, 0, 0, 0 }, 2.5);
            //myController.Commands.Motion.Linear("D", microscope_focus, 2); ********
            Movement_3D_uScope(Start_Xaxis, Start_Yaxis, 0, false);
            
            //Disable Buttons

            btn_PosJogD.Enabled = false;
            btn_NegJogD.Enabled = false;
             
            btn_PosJogA.Enabled = false;
            btn_NegJogA.Enabled = false;

            btn_PosJogB.Enabled = false;
            btn_NegJogB.Enabled = false;

            btn_cornerA.Enabled = true;
            btn_StartAlignment.Enabled = false;   

        }

        #region Collect Initial Corner Positions

        private void btn_cornerA_Click(object sender, EventArgs e)
        {
            A_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            A_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            A_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

            //A_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            //A_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            //A_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            //A_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            btn_cornerA.Enabled = false;
            btn_cornerB.Enabled = true;
        }

        private void btn_cornerB_Click(object sender, EventArgs e)
        {
            B_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            B_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            B_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

            //B_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            //B_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            //B_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            //B_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            
            btn_cornerB.Enabled = false;
            btn_cornerC.Enabled = true;
        }

        private void btn_cornerC_Click(object sender, EventArgs e)
        {
            C_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            C_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            C_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

            //C_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            //C_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            //C_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            //C_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            
            btn_cornerC.Enabled = false;
            btn_cornerD.Enabled = true;
        }

        private void btn_cornerD_Click(object sender, EventArgs e)
        {
            D_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            D_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            D_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

            //D_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            //D_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            //D_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            //D_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            
            btn_cornerD.Enabled = false;
            btn_TiltCorrection.Enabled = true;
        }

        #endregion

        #region uScope Tilt Correction

        private void btn_TiltCorrection_Click(object sender, EventArgs e)
        {

            PlaneFit(A_coords,B_coords,C_coords,D_coords);

            MessageBox.Show("Realign and refocus corner");

            btn_TiltCorrection.Enabled = false;

            btn_UpdateCoords.Enabled = true;

            CornerAlignmentEvent.Set();

        }

        private void PlaneFit(double [] A, double [] B, double [] C, double [] D)

        {
            // Set enviroment for matlab instance
            var activationContext = Type.GetTypeFromProgID("matlab.application.single");
            var matlab = (MLApp.MLApp)Activator.CreateInstance(activationContext);
            string tilt_path = "cd('C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Matlab Code')"; // change to path on UP PC
            matlab.Execute(tilt_path);

            // Send corner coordinates to Matlab

            matlab.PutWorkspaceData("X", "base", new[] { A[0], B[0], C[0], D[0] });
            matlab.PutWorkspaceData("Y", "base", new[] { A[1], B[1], C[1], D[1] });
            matlab.PutWorkspaceData("Z", "base", new[] { A[2], B[2], C[2], D[2] });

            //matlab.PutWorkspaceData("X", "base", new[] { A_Xaxis, B_Xaxis, C_Xaxis, D_Xaxis });
            //matlab.PutWorkspaceData("Y", "base", new[] { A_Yaxis, B_Yaxis, C_Yaxis, D_Yaxis });
            //matlab.PutWorkspaceData("Z", "base", new[] { A_Zaxis, B_Zaxis, C_Zaxis, D_Zaxis });

            // Calculate rotation angles

            matlab.Execute("[theta,phi] = rot_angles(X,Y,Z)");

            // Get rotation angles

            double theta = matlab.GetVariable("theta", "base");
            double phi = matlab.GetVariable("phi", "base");

            theta_hold = theta_hold + theta;
            phi_hold = phi_hold + phi;

            // Apply rotation angles

            myController.Commands.Motion.Setup.Absolute();
            //MessageBox.Show(theta_hold.ToString());
            myController.Commands.Motion.Linear("A", theta_hold, 1);
            //MessageBox.Show(phi_hold.ToString());
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
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { A_coords[0], A_coords[1] }, 1);
                    btn_UpdateCoords.Text = "Realign Corner A";

                    hold = 0;
                }
                else if (i == 1)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { B_coords[0], B_coords[1] }, 1);
                    btn_UpdateCoords.Text = "Realign Corner B";
                    hold = 1;
                }
                else if (i == 2)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { C_coords[0], C_coords[1] }, 1);
                    btn_UpdateCoords.Text = "Realign Corner C";
                    hold = 2;
                }
                else if (i == 3)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { D_coords[0], D_coords[1] }, 1);
                    btn_UpdateCoords.Text = "Realign Corner D";
                    hold = 3;
                }
                else if (i == 4)
                {
                    btn_UpdateCoords.Text = "Corners Reaquired";
                    btn_UpdateCoords.Enabled = false;
                    //MessageBox.Show("Calculating new tilt");
                    PlaneFit(A_coords,B_coords,C_coords,D_coords);
                    MessageBox.Show("Adjust microscope focus using D axis");
                    btn_SetuScopeFocus.Enabled = true;
                }
            }
            hold = 0;
        }

        private void btn_UpdateCoords_Click_1(object sender, EventArgs e)
        {
            if (hold == 0)
            {
                A_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                A_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                A_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

                //MessageBox.Show("Aquiring Axes Pos A");
                //A_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                //A_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                //A_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                //A_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            }
            else if (hold == 1)
            {
                B_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                B_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                B_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

                //MessageBox.Show("Aquiring Axes Pos B");
                //    B_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                //    B_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                //    B_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                //    B_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            }
            else if (hold == 2)
            {
                C_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                C_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                C_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

                //MessageBox.Show("Aquiring Axes Pos C");
                //C_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                //C_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                //C_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                //C_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            }
            else if (hold == 3)
            {
                D_coords[0] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                D_coords[1] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                D_coords[2] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

                //MessageBox.Show("Aquiring Axes Pos D");
                //D_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                //D_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                //D_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
                //D_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
                //microscope_focus = (A_Daxis + B_Daxis + C_Daxis + D_Daxis) / 4;
                btn_PosJogD.Enabled = true;
                btn_NegJogD.Enabled = true;
                Movement_3D_uScope(D_coords[0], D_coords[1], 0, false);
               // myController.Commands.Motion.Linear("Z", ablation_focus, 1);         *********       
            }

            CornerAlignmentEvent.Set();
        }

        private void btn_SetuScopeFocus_Click(object sender, EventArgs e)
        {
            microscope_focus = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            btn_SetuScopeFocus.Enabled = false;

            btn_PosJogD.Enabled = false;
            btn_NegJogD.Enabled = false;

            btn_SetuScopeFocus.Enabled = false;
            btn_AlignLaseruScope.Enabled = true;
        }

        #endregion

        #region Align Laser and uScope

        private void btn_AlignLaseruScope_Click(object sender, EventArgs e)
        {
            btn_AlignLaseruScope.Enabled = false;
            
            // Find X dimensions

            if (A_coords[0] > B_coords[0])
            {
                Refined_Xaxis[0] = A_coords[0] + StepIn;
                Refined_Xaxis[1] = A_coords[0] + StepIn;
            }
            else
            {
                Refined_Xaxis[0] = B_coords[0] + StepIn;
                Refined_Xaxis[1] = B_coords[0] + StepIn;
            }

            if (D_coords[0] > C_coords[0])
            {
                Refined_Xaxis[2] = C_coords[0] - StepIn;
                Refined_Xaxis[3] = C_coords[0] - StepIn;
            }
            else
            {
                Refined_Xaxis[2] = D_coords[0] - StepIn;
                Refined_Xaxis[3] = D_coords[0] - StepIn;
            }

            // Find Y dimensions

            if (A_coords[1] > D_coords[1])
            {
                Refined_Yaxis[0] = D_coords[1] - StepIn;
                Refined_Yaxis[3] = D_coords[1] - StepIn;
            }
            else
            {
                Refined_Yaxis[0] = A_coords[1] - StepIn;
                Refined_Yaxis[3] = A_coords[1] - StepIn;
            }

            if (B_coords[1] > C_coords[1])
            {
                Refined_Yaxis[1] = B_coords[1] + StepIn;
                Refined_Yaxis[2] = B_coords[1] + StepIn;
            }
            else
            {
                Refined_Yaxis[1] = C_coords[1] + StepIn;
                Refined_Yaxis[2] = C_coords[1] + StepIn;
            }

            // Find previous offset 

            Offset_Xaxis = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/OFFSETX.txt", Encoding.UTF8));
            Offset_Yaxis = Convert.ToDouble(File.ReadAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/OFFSETY.txt", Encoding.UTF8));

            // Set laser parameters

            //myController.Commands.Motion.Linear("D", 0, 2); *********
            //myController.Commands.Motion.Linear("Z", ablation_focus, 1); *********
                        
            MessageBox.Show("Insert Beam Dump");

            shutter_open();
            aommode_0();
            aomgate_high_trigger();
            talisker_attenuation(96);  //95 for 20x
            watt_pilot_attenuation(0);

            this.Update();

            // Setup Beam Guage
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            // Invoke an autocalibration cycle
            _bgtest.Calibration.SetupEGB();
            //Wait for beam block message
            Thread.Sleep(2000);
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            // Mark Fiducial Markers

            //myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[0] - Offset_Xaxis, Refined_Yaxis[0] - Offset_Yaxis }, 5); *****
            Movement_3D_ablation(Refined_Xaxis[0], Refined_Yaxis[0], 0, 5);
            MarkCross(0.25);
            //myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[1] - Offset_Xaxis, Refined_Yaxis[1] - Offset_Yaxis }, 5); *****            
            Movement_3D_ablation(Refined_Xaxis[1], Refined_Yaxis[1], 0, 5);
            MarkCross(0.25);
            //myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[2] - Offset_Xaxis, Refined_Yaxis[2] - Offset_Yaxis }, 5); *****
            Movement_3D_ablation(Refined_Xaxis[2], Refined_Yaxis[2], 0, 5);            
            MarkCross(0.25);
            //myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[3] - Offset_Xaxis, Refined_Yaxis[3] - Offset_Yaxis }, 5); *****
            Movement_3D_ablation(Refined_Xaxis[3], Refined_Yaxis[3], 0, 5);
            MarkCross(0.25);

            shutter_closed();

            btn_AlignLaseruScope.Enabled = false;

            btn_MarkerAligned.Enabled = true;

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

                Movement_3D_uScope(Refined_Xaxis[i], Refined_Yaxis[i], 0, false);

                //myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[i], Refined_Yaxis[i] }, 5); *****
                //myController.Commands.Axes["D"].Motion.Linear(new double[] { microscope_focus }); *****                
            }
        }

        private void btn_MarkerAligned_Click(object sender, EventArgs e)
        {
            double OffsetAccurate_Xaxis;
            double OffsetAccurate_Yaxis;

            Correction_Xaxis[hold] = (myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback) - Refined_Xaxis[hold]);
            Refined_Xaxis[hold] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);

            //MessageBox.Show(Correction_Xaxis[hold].ToString());

            Correction_Yaxis[hold] = (myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback) - Refined_Yaxis[hold]);
            Refined_Yaxis[hold] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);

            //MessageBox.Show(Correction_Yaxis[hold].ToString());

            if (hold == 3)
            {
                MessageBox.Show("Calculating Accurate offset");
                OffsetAccurate_Xaxis = Offset_Xaxis + Correction_Xaxis.Sum() / 4; 
                //MessageBox.Show("Original Offset X axis = " + Offset_Xaxis.ToString() + " New Offset X axis = " + OffsetAccurate_Xaxis.ToString());
                OffsetAccurate_Yaxis = Offset_Yaxis + Correction_Yaxis.Sum() / 4;
                //MessageBox.Show("Orignial Offset Y axis = " + Offset_Yaxis.ToString() + " New Offset Y axis = " + OffsetAccurate_Yaxis.ToString());
                btn_MarkerAligned.Enabled = false;

                File.WriteAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/OFFSETX.txt", OffsetAccurate_Xaxis.ToString());
                File.WriteAllText("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/OFFSETY.txt", OffsetAccurate_Yaxis.ToString());

                Offset_Xaxis = OffsetAccurate_Xaxis;
                Offset_Yaxis = OffsetAccurate_Yaxis;

                btn_MarkerAligned.Enabled = false;
                btn_FindFocus.Enabled = true;
            }

            LaserAlignEvent.Set();
        }

        private void MarkCross(double MarkLength)
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
            FindFocus_Xaxis(1,20,0.05, 0.25);

            //FindFocus_Yaxis();             

            // calculate average focus height            

            ablation_focus = LaserFocus_Zaxis.Sum() / 4; // Change 4 to number of recorded positions

            using (StreamWriter AF = new StreamWriter("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/ABLATION_FOCUS.txt",false))
            {
                AF.Write(ablation_focus.ToString());
            }

            //myController.Commands.Motion.Setup.Absolute();

            //myController.Commands.Motion.Linear("Z", ablation_focus_accurate, 1);

            MessageBox.Show("Reset Microscope Focus");

            btn_FindFocus.Enabled = false;

            btn_SetuScopeFocus_2.Enabled = true;
            btn_PosJogD.Enabled = true;
            btn_NegJogD.Enabled = true;

        }

        private void FindFocus_Xaxis(double Z_Scan_length, int Mark_Number, double Line_Spacing, double MarkLength)
        {
            int NumberOfLines = 0;

            double LaserFocus_Xaxis_Total = 0;
            double LaserFocus_Yaxis_Total = 0;
            double LaserFocus_Zaxis_Total = 0;            

            double[] Machined_X_Pos = new double[Mark_Number];
            double[] Machined_Y_Pos = new double[Mark_Number];
            double[] Machined_Z_Pos = new double[Mark_Number];

            int pos_count = 0; // Variable for recording position of minimum track width - starts on zero for X

            myController.Commands.Motion.Linear("D", 0, 1);

            shutter_open();
            aommode_0();
            aomgate_high_trigger();
            talisker_attenuation(96); // 99 for 20x + Wheel ATT
            watt_pilot_attenuation(0);

            for (int hold = 0; hold < 4; hold++)
            {
                //myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] + MarkLength / 2 - OffsetAccurate_Xaxis, 5); ******

                if (hold == 0)
                {
                    //myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] - 0.5 - OffsetAccurate_Yaxis, 5); ******
                    Movement_3D_ablation(Refined_Xaxis[hold] + MarkLength / 2, Refined_Yaxis[hold] - 0.5, 0, 5);
                }
                else if (hold == 1)
                {
                    //myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5 - OffsetAccurate_Yaxis, 5); ******
                    Movement_3D_ablation(Refined_Xaxis[hold] + MarkLength / 2, Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5, 0, 5);

                }
                else if (hold == 2)
                {
                    //myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5 - OffsetAccurate_Yaxis, 5); ******
                    Movement_3D_ablation(Refined_Xaxis[hold] + MarkLength / 2, Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5, 0, 5);
                }
                else if (hold == 3)
                {
                    //myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] - 0.5 - OffsetAccurate_Yaxis, 5); ******
                    Movement_3D_ablation(Refined_Xaxis[hold] + MarkLength / 2, Refined_Yaxis[hold] - 0.5, 0, 5);
                }

                // Ensure Z axis is at rough focus minus half scan length

                myController.Commands.Motion.Setup.Incremental();
                myController.Commands.Motion.Linear("Z", -(Z_Scan_length / 2), 1);

                // X axis find focus 
                for (int i = 0; i < Mark_Number; i++)
                {
                    // Change Z height
                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("Z", (Z_Scan_length / Mark_Number), 1);
                    Machined_X_Pos[i] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback) - MarkLength / 2;
                    Machined_Y_Pos[i] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
                    Machined_Z_Pos[i] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);                                      
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
                    myController.Commands.Motion.Linear("X", -MarkLength, 1);
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
                    myController.Commands.Motion.Linear("X", +MarkLength, 1);
                    myController.Commands.Motion.Linear("Y", -(Line_Spacing), 1);
                    myController.Commands.Motion.Setup.Absolute();
                }
            }

            // Inspect Lines and record minimum line thickness
                        

            ImageProcessing IPForm = new ImageProcessing();

            IPForm.Show();
            IPForm.Activate();

            for (int hold = 0; hold < 4; hold++)
            {
                //Current_X = Refined_Xaxis[hold]; ******

                //myController.Commands.Motion.Linear("X", Current_X, 5); ******

                if (hold == 0)
                {
                    //Current_Y = Refined_Yaxis[hold] - 0.5; ******
                    Movement_3D_uScope(Refined_Xaxis[hold], Refined_Yaxis[hold] - 0.5, 0, false);
                }
                else if (hold == 1)
                {
                    //Current_Y = Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5; ******
                    Movement_3D_uScope(Refined_Xaxis[hold], Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5, 0, false);
                }
                else if (hold == 2)
                {
                    //Current_Y = Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5; ******
                    Movement_3D_uScope(Refined_Xaxis[hold], Refined_Yaxis[hold] + (Line_Spacing * Mark_Number) + 0.5, 0, false);
                }
                else if (hold == 3)
                {
                    //Current_Y = Refined_Yaxis[hold] - 0.5; ******
                    Movement_3D_uScope(Refined_Xaxis[hold], Refined_Yaxis[hold] - 0.5, 0, false);
                }

                //myController.Commands.Motion.Linear("Y", Current_Y, 5); ******

                // Bring microscope into focus

                //myController.Commands.Motion.Linear("D", microscope_focus, 1); ******

                // Bring Z stage to intial ablation focus

                //myController.Commands.Motion.Linear("Z", ablation_focus, 1); ******


                #region Version 2

                LaserFocus_Xaxis_Total = 0;
                LaserFocus_Yaxis_Total = 0;
                LaserFocus_Zaxis_Total = 0;
                NumberOfLines = 0;                

                for (int i = 0; i < Mark_Number; i++)
                {
                    icImagingControl1.OverlayBitmap.Enable = false;

                    Thread.Sleep(1500);

                    icImagingControl1.MemorySnapImage(1000);
                    Bitmap live_bmp = icImagingControl1.ImageActiveBuffer.Bitmap;

                    IPForm.Detect_Line(40, live_bmp);

                    if (AlignmentFocus_Container.Feature_Visible == true)
                    {
                        LaserFocus_Xaxis_Total = LaserFocus_Xaxis_Total + Machined_X_Pos[i];
                        LaserFocus_Yaxis_Total = LaserFocus_Yaxis_Total + Machined_Y_Pos[i];
                        LaserFocus_Zaxis_Total = LaserFocus_Zaxis_Total + Machined_Z_Pos[i];
                        NumberOfLines++;

                        string save_dir = universal_path + "\\Focus Lines\\" + pos_count;
                        System.IO.Directory.CreateDirectory(save_dir);

                        string save_image = save_dir + "\\" + Machined_Z_Pos[i].ToString() + ".bmp";

                        Thread.Sleep(500);

                        icImagingControl1.MemorySnapImage();
                        icImagingControl1.MemorySaveImage(save_image);
                        
                    }

                    icImagingControl1.OverlayBitmap.Enable = true;
                    AlignmentFocus_Container.Feature_Visible = false;                    

                    #region Original 
                    //DialogResult Focus_Result = MessageBox.Show("Is line visible?", "Important Question", MessageBoxButtons.YesNo);
                    //if (Focus_Result == DialogResult.Yes)
                    //{
                    //    LaserFocus_Zaxis_Total = LaserFocus_Zaxis_Total + Machined_Z_Pos[i];
                    //    NumberOfLines++;

                    //    string save_dir = "C:/Users/User/Desktop/Share/Chris/Abatlion Threshold/Trial 9/Focus Lines/" + pos_count;
                    //    System.IO.Directory.CreateDirectory(save_dir);

                    //    string save_image = save_dir + "/" + Machined_Z_Pos[i].ToString() + ".bmp";

                    //    icImagingControl1.OverlayBitmap.Enable = false;
                    //    icImagingControl1.MemorySnapImage();
                    //    icImagingControl1.MemorySaveImage(save_image);
                    //    icImagingControl1.OverlayBitmap.Enable = true;
                    //}

                    #endregion

                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("Y", -(Line_Spacing), 1);
                    myController.Commands.Motion.Setup.Absolute();
                }

                LaserFocus_Xaxis[pos_count] = LaserFocus_Xaxis_Total / NumberOfLines;
                LaserFocus_Yaxis[pos_count] = LaserFocus_Yaxis_Total / NumberOfLines;
                LaserFocus_Zaxis[pos_count] = LaserFocus_Zaxis_Total / NumberOfLines;
                
                pos_count = pos_count + 1;

                #endregion

                #region Original

                //for (int i = 0; i < Mark_Number; i++)
                //{
                //    myController.Commands.Motion.Setup.Incremental();
                //    //myController.Commands.Motion.Linear("X", -MarkLength / 2, 1);
                //    DialogResult Focus_Result = MessageBox.Show("Is this result larger than the previous result?", "Important Question", MessageBoxButtons.YesNo);
                //    if (Focus_Result == DialogResult.Yes)
                //    {
                //        // Recording position of focus point
                //        LaserFocus_Xaxis[pos_count] = Current_X - MarkLength / 2;
                //        LaserFocus_Yaxis[pos_count] = Current_Y - Line_Spacing * (i - 1);
                //        LaserFocus_Zaxis[pos_count] = ablation_focus - Z_Scan_length / 2 + Z_Scan_length / Mark_Number * i;
                //        //MessageBox.Show(LaserFocus_Zaxis[pos_count].ToString());
                //        pos_count = pos_count + 1;
                //        i = Mark_Number; 
                //    }
                //    myController.Commands.Motion.Linear("Y", -(Line_Spacing), 1);
                //    myController.Commands.Motion.Setup.Absolute();
                //}

                #endregion
            }
            IPForm.Close();
            shutter_closed();

            double [] Focus_A_coords = { LaserFocus_Xaxis[0], LaserFocus_Yaxis[0], LaserFocus_Zaxis[0] };
            double [] Focus_B_coords = { LaserFocus_Xaxis[1], LaserFocus_Yaxis[1], LaserFocus_Zaxis[1] };
            double [] Focus_C_coords = { LaserFocus_Xaxis[2], LaserFocus_Yaxis[2], LaserFocus_Zaxis[2] };
            double [] Focus_D_coords = { LaserFocus_Xaxis[3], LaserFocus_Yaxis[3], LaserFocus_Zaxis[3] };

            //PlaneFit(Focus_A_coords, Focus_B_coords, Focus_C_coords, Focus_D_coords);
        }

        // Y axis find focus 

        private void FindFocus_Yaxis(double Z_Scan_length, int Mark_Number, double Line_Spacing, double MarkLength)
        {
            double Current_X = 0;
            double Current_Y = 0;

            myController.Commands.Motion.Setup.Absolute();

            myController.Commands.Motion.Linear("D", 0, 1);

            shutter_open();
            aommode_0();
            aomgate_high_trigger();
            talisker_attenuation(80);
            watt_pilot_attenuation(0);

            for (int hold = 0; hold < 4; hold++)
            {
                myController.Commands.Motion.Linear("Y", Refined_Yaxis[hold] + MarkLength / 2 - Offset_Yaxis, 5);

                if (hold == 0)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] + 0.5 - Offset_Xaxis, 5);
                }
                else if (hold == 1)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] + 0.5 - Offset_Xaxis, 5);
                }
                else if (hold == 2)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5 - Offset_Xaxis, 5);
                }
                else if (hold == 3)
                {
                    myController.Commands.Motion.Linear("X", Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5 - Offset_Xaxis, 5);
                }

                // Ensure Z axis is at rough focus minus half scan length

                myController.Commands.Motion.Linear("Z", ablation_focus - (Z_Scan_length / 2), 1);

                // Mark lines

                for (int i = 0; i < Mark_Number; i++)
                {
                    // Change Z height
                    myController.Commands.Motion.Setup.Incremental();
                    myController.Commands.Motion.Linear("Z", (Z_Scan_length / Mark_Number), 1);                    
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
                    myController.Commands.Motion.Linear("Y", -MarkLength, 1);
                    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
                    myController.Commands.Motion.Linear("Y", MarkLength, 1);
                    myController.Commands.Motion.Linear("X", Line_Spacing, 1);
                    myController.Commands.Motion.Setup.Absolute();
                }
            }

            // Inspect Lines and record minimum line thickness

            for (int hold = 0; hold < 4; hold++)
            {

                Current_Y = Refined_Yaxis[hold] + MarkLength / 2;

                myController.Commands.Motion.Linear("Y", Current_Y, 5);

                if (hold == 0)
                {
                    Current_X = Refined_Xaxis[hold] + 0.5;
                }
                else if (hold == 1)
                {
                    Current_X = Refined_Xaxis[hold] + 0.5;
                }
                else if (hold == 2)
                {
                    Current_X = Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5;
                }
                else if (hold == 3)
                {
                    Current_X = Refined_Xaxis[hold] - (Line_Spacing * Mark_Number) - 0.5;
                }

                myController.Commands.Motion.Linear("X", Current_X, 5);

                // Bring microscope into focus

                myController.Commands.Motion.Linear("D", microscope_focus, 1);

                // Bring Z stage to intial ablation focus

                myController.Commands.Motion.Linear("Z", ablation_focus, 1);

                int pos_count = 4; // Variable for recording position - starts with 4 for Y axis 

                // Y axis find focus 
                for (int i = 0; i < Mark_Number; i++)
                {                    
                    myController.Commands.Motion.Setup.Incremental();                    
                    //myController.Commands.Motion.Linear("Y", -MarkLength / 2, 1);
                    DialogResult Focus_Result = MessageBox.Show("Is this result larger than the previous result?", "Important Question", MessageBoxButtons.YesNo);
                    if (Focus_Result == DialogResult.Yes)
                    {
                        // Recording position of focus point
                        LaserFocus_Xaxis[pos_count] = Current_X + (Line_Spacing * (i - 1));
                        LaserFocus_Yaxis[pos_count] = Current_Y - MarkLength / 2;
                        LaserFocus_Zaxis[pos_count] = ablation_focus - Z_Scan_length / 2 + Z_Scan_length / Mark_Number * i;
                        pos_count++;
                        i = Mark_Number;
                    }
                    myController.Commands.Motion.Linear("X", (Line_Spacing * hold), 1);
                    myController.Commands.Motion.Setup.Absolute();
                }
            }
            shutter_closed();
        }

        private void btn_SetuScopeFocus_2_Click(object sender, EventArgs e)
        {
            microscope_focus = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            btn_SetuScopeFocus_2.Enabled = false;

            btn_PosJogD.Enabled = false;
            btn_NegJogD.Enabled = false;

            btn_SetuScopeFocus_2.Enabled = false;
            btn_RotationalCentre.Enabled = true;
        }

        #endregion

        #region Finding Centre of Rotation

        private void btn_RotationalCentre_Click(object sender, EventArgs e)
        {
            btn_PosJogD.Enabled = false;
            btn_NegJogD.Enabled = false;

            double CentreTestRotation = 10;

            // Move to rough marker position 

            myController.Commands.Motion.Setup.Absolute();
            //myController.Commands.Axes["X", "Y"].Motion.Linear(new double[] { Refined_Xaxis[1], Refined_Yaxis[1] }, 1);
            //myController.Commands.Motion.Linear("D", microscope_focus, 1);

            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], 0, false);

            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Z",  -5, 1);
            myController.Commands.Motion.Setup.Absolute();

            // Apply +10 degree rotation 

            myController.Commands.Motion.Setup.Absolute();
            myController.Commands.Motion.Linear("B", CentreTestRotation + phi_hold, 1);

            MessageBox.Show("When marker is aligned press Point 1 button");

            btn_RotationalCentre.Enabled = false;
            btn_point1.Enabled = true;            
        }

        private void btn_point1_Click(object sender, EventArgs e)
        {
            double CentreTestRotation = 10;

            Point1_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            Point1_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            Point1_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            // Due to having to Lower Z to find the marker which is higher than the ablation focus this has to be adjusted
                        
            double Point1_Zaxis_Measured = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            Point1_Zaxis = (z_stage_position - Point1_Zaxis_Measured) + z_stage_position;
            
            btn_point1.Enabled = false;          

            double theta_rad = CentreTestRotation * (Math.PI / 180); // Radians

            // Calculate Z Offset

            double p = -1 * ((Point1_Yaxis - Refined_Yaxis[1]) * Math.Sin(theta_rad) / (1 - Math.Cos(theta_rad)));

            double q = (Point1_Zaxis - z_stage_position);

            Z_Offset = 0.5 * (p + q);

            // Calculate Y Offset

            double a = (Point1_Yaxis - Refined_Yaxis[1]);

            double b = (Point1_Zaxis - z_stage_position) * (1 + Math.Cos(theta_rad)) / Math.Sin(theta_rad);

            Y_Offset = 0.5 * (a + b);

            Rot_Z_Coords = z_stage_position + Z_Offset;

            Rot_Y_Coords = Refined_Yaxis[1] + Y_Offset;

            MessageBox.Show("Rot Y = " + Rot_Y_Coords.ToString());
            MessageBox.Show("Rot Z = " + Rot_Z_Coords.ToString());

            // Delete previous reference file 

            File.Delete("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/Rotpoints.txt");

            // Write all variables to text file 

            using (StreamWriter writer = new StreamWriter("C:/Users/User/Documents/GitHub/PrecisionPlatformControl/Reference Values/Rotpoints.txt", true))
            {
                writer.WriteLine("Rotation = " + CentreTestRotation.ToString());

                writer.WriteLine("Reference Y = " + Refined_Yaxis[1].ToString());
                writer.WriteLine("Reference Z = " + z_stage_position.ToString());
                writer.WriteLine("Ablation Focus = " + ablation_focus.ToString());
                writer.WriteLine("Microscope at ablation focus  = " + microscope_focus.ToString());

                writer.WriteLine("Point 1 Y = " + Point1_Yaxis.ToString());
                writer.WriteLine("Point 1 Z Measured = " + Point1_Zaxis_Measured.ToString());
                writer.WriteLine("Point 1 Z = " + Point1_Zaxis.ToString());

                writer.WriteLine("Z Offset = " + Z_Offset.ToString());
                writer.WriteLine("Y Offset = " + Y_Offset.ToString());

                writer.WriteLine("Z Rot Centre = " + Rot_Z_Coords.ToString());
                writer.WriteLine("Y Rot Centre = " + Rot_Y_Coords.ToString());
            }

            myController.Commands.Motion.Setup.Absolute();
            myController.Commands.Motion.Linear("B", phi_hold, 1); 
            myController.Commands.Motion.Setup.Absolute();

            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], 0, false);

            btn_point1.Enabled = false;
            btn_Move_Zoom.Enabled = true;          

        }

        private void btn_RotationTest_Click(object sender, EventArgs e)
        {
            // Move to have marker in focus at accurate ablation focus and visible on the microscope 

            btn_Move_Zoom.Enabled = true;

            myController.Commands.Motion.Setup.Absolute();
            //myController.Commands.Axes["X", "Y", "Z", "D"].Motion.Linear(new double[] { Refined_Xaxis[2], Refined_Yaxis[2], ablation_focus_accurate, microscope_focus}, 5); ******
            //myController.Commands.Motion.Linear("B", phi_hold, 1); ******

            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], 0, false);

            MessageBox.Show("Is marker in focus and visible?");

            double Rotation = Convert.ToDouble(txtbx_rotangle.Text);

            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], Rotation, false);

            //myController.Commands.Motion.Linear("D", microscope_focus, 2);

            MessageBox.Show("Visible?");

            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], 0, false);

        }

        private void Movement_3D_uScope(double X, double Y, double B, Boolean zoom)
        {
            lbl_uscopemoving.Visible = true;

            this.Update();

            // X, Y, B are all the requested coordinates

            double theta_rad = B * (Math.PI / 180); // Radians            

            //Refined_Yaxis[1] is the input used to calcualte the correction for both Y and Z

            // Y distance from point of interest and rotational centre

            double PointOffset_Y = Math.Abs(Y - Rot_Y_Coords);

            double Point_calc_Y = Y + PointOffset_Y * (1 - Math.Cos(theta_rad)) - Z_Offset * Math.Sin(theta_rad);

            double Point_calc_Z = z_stage_position + (Z_Offset * (1 - Math.Cos(theta_rad))) + (PointOffset_Y * Math.Sin(theta_rad));

            double Correction_Z = Point_calc_Z - z_stage_position;

            double Movement_z = z_stage_position - Correction_Z;

            // Perform compensated movement 

            myController.Commands.Motion.Setup.Absolute();      

            if (B_hold != B + phi_hold)
            {
                myController.Commands.Motion.Linear("Z", z_stage_position - 10, 5);
                myController.Commands.Motion.Linear("B", B + phi_hold, 1);
            }

            B_hold = B + phi_hold;

            

            //myController.Commands.Motion.Linear("Z", Movement_z, 1);

            //myController.Commands.Motion.Linear("B", B + phi_hold, 1);

            //myController.Commands.Motion.Linear("Y", Point_calc_Y);

            //myController.Commands.Motion.Linear("X", X);

            if (zoom == false)
            {
                myController.Commands.Axes["X", "Y", "Z"].Motion.Linear(new double[] { X, Point_calc_Y, Movement_z }, 5);

                myController.Commands.Motion.Linear("D", microscope_focus, 2);

                //uScope_focus_check(zoom);
            }
            else if (zoom == true)
            {
                myController.Commands.Axes["X", "Y", "Z"].Motion.Linear(new double[] { X + microscope_zoom_x_correction, Point_calc_Y + microscope_zoom_y_correction, Movement_z }, 5);

                myController.Commands.Motion.Linear("D", microscope_focus + zoom_offset, 2);

                lbl_Moving_Zoom.Visible = true;
                this.Update();

                //uScope_zoom_SerialPortCommunicator.SerialPort.Write("XH\r");

                //Thread.Sleep(7500);

                uScope_zoom_SerialPortCommunicator.SerialPort.Write("XG006C48\r");

                Thread.Sleep(7500);

                //uScope_focus_check(zoom);

                lbl_Moving_Zoom.Visible = false;
                this.Update();
            }

            lbl_uscopemoving.Visible = false;
            this.Update();

        }

        private void Movement_3D_ablation(double X, double Y, double B, double Speed)
        {
            // X, Y, B are all the requested coordinates

            double theta_rad = B * (Math.PI / 180); // Radians            

            //Refined_Yaxis[1] is the input used to calcualte the correction for both Y and Z

            // Y distance from point of interest and rotational centre

            double PointOffset_Y = -1 * (Y - Rot_Y_Coords); // Math.Abs(Y - Rot_Y_Coords);

            double Point_calc_Y = Y + PointOffset_Y * (1 - Math.Cos(theta_rad)) - Z_Offset * Math.Sin(theta_rad);

            double Point_calc_Z = ablation_focus + (Z_Offset * (1 - Math.Cos(theta_rad))) + (PointOffset_Y * Math.Sin(theta_rad));

            double Correction_Z = Point_calc_Z - ablation_focus;

            double Movement_z = ablation_focus - Correction_Z;

            // Perform compensated movement 

            myController.Commands.Motion.Setup.Absolute();

            myController.Commands.Motion.Linear("D", 0, 2);

            if (B_hold != B + phi_hold)
            {
                myController.Commands.Motion.Linear("Z", ablation_focus - 10, 5);
                myController.Commands.Motion.Linear("B", B + phi_hold, 1);
            }

            B_hold = B + phi_hold;                   

            myController.Commands.Axes["X", "Y", "Z"].Motion.Linear(new double[] { X - Offset_Xaxis, Point_calc_Y - Offset_Yaxis, Movement_z}, 5);

            //myController.Commands.Motion.Linear("Z", Movement_z, 1);

            //myController.Commands.Motion.Linear("Y", Point_calc_Y - OffsetAccurate_Yaxis, Speed);

            //myController.Commands.Motion.Linear("X", X - OffsetAccurate_Xaxis, Speed);
            
        }

        private void btn_Move_Zoom_Click(object sender, EventArgs e)
        {
            btn_PosJogD.Enabled = true;
            btn_NegJogD.Enabled = true;

            eLight_Intensity(100);

            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], 0, true);

            myController.Commands.Motion.Linear("D", microscope_focus + zoom_offset, 2);

            MessageBox.Show("Align Marker");

            btn_Set_Zoom.Enabled = true;
        }
        private void btn_Set_Zoom_Click(object sender, EventArgs e)
        {
            //double current_D = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            double current_X = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            double current_Y = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);

            //zoom_offset = current_D - microscope_focus;
            microscope_zoom_x_correction = current_X - Refined_Xaxis[1];
            microscope_zoom_y_correction = current_Y - Refined_Yaxis[1];

            btn_BoxAblation.Enabled = true;
        }

        #endregion

        #region Machining Button

        private void btn_BoxAblation_Click(object sender, EventArgs e)
        {           
            Thread laser_ablation_thread = new Thread(new ThreadStart(laser_ablation));
            laser_ablation_thread.IsBackground = true;
            laser_ablation_thread.Start();

            //double box_width  = 0.5;
            //double overlap = 50;
            //double increment_total = 0;

            ////double angle = Convert.ToDouble(txtbx_AngForAblation.Text);

            //double speed = 1;
            //int talikser_attentuation_value = 90;
            //double wattpilot_attenutation_value = 0;

            //talisker_attenuation(talikser_attentuation_value);
            //watt_pilot_attenuation(wattpilot_attenutation_value);

            //aomgate_high_trigger();
            //shutter_open();
            //aommode_0();


            #region Uphill and Downhill Test

            //// Drill Line down slope 

            //// Flat

            //angle = 0;

            //Movement_3D_ablation(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 0.5, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Downhill

            //angle = 30;

            //Movement_3D_ablation(Refined_Xaxis[1] + 1, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 1, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Flat

            //angle = 0;

            //Movement_3D_ablation(Refined_Xaxis[1] + 1.5, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 1.5, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Uphill 

            //angle = 30;

            //Movement_3D_ablation(Refined_Xaxis[1] + 2, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 2, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Flat

            //angle = 0;

            //Movement_3D_ablation(Refined_Xaxis[1] + 2.5, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 2.5, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Downhill Opposite

            //angle = -30;

            //Movement_3D_ablation(Refined_Xaxis[1] + 3, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 3, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Flat

            //angle = 0;

            //Movement_3D_ablation(Refined_Xaxis[1] + 3.5, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 3.5, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //// Uphill Opposite

            //angle = -30;

            //Movement_3D_ablation(Refined_Xaxis[1] + 4, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 4, Refined_Yaxis[0] - 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            #endregion

            #region Drill Single line in X and inspect

            //angle = 0;

            //Movement_3D_ablation(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + 0.25, Refined_Yaxis[1] + 0.5, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //Movement_3D_uScope(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.5, 0);

            //angle = 20;

            //Movement_3D_ablation(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.75, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + 0.25, Refined_Yaxis[1] + 0.75, angle, 5);
            //myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //Movement_3D_uScope(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.75, 0);

            #endregion

            #region Drill holes a varying angles and inspect each one 

            //for (int count = 0; count < 10; count++)
            //{
            //    // Drill

            //    angle = 5 * count;
            //    Movement_3D_ablation(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.5 + increment_total, angle, speed);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //    Thread.Sleep(100);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //    increment_total = increment_total + 0.25;

            //    // Inspect

            //    Movement_3D_uScope(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + 0.5 + increment_total, 0);
            //    MessageBox.Show("Dilled hole visible?");
            //}

            #endregion

            #region Machines a single square at a single angle 

            //angle = 30;
            //increment_total = 0;
            //double Y_Start = 0.5;


            //for (int count = 0; count < overlap; count++)
            //{
            //    Movement_3D_ablation(Refined_Xaxis[1] + 0.5, Refined_Yaxis[1] + Y_Start + increment_total, angle, speed);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //    Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + box_width, Refined_Yaxis[1] + Y_Start + increment_total, angle, speed);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //    increment_total = increment_total + (box_width / overlap);
            //}                       

            #endregion

            #region Ablation threshold at different angles test grid

            //// Set Repition Rate and number of pulses per trigger

            //talisker_rep_rate(10000);
            //aommode_3();
            //talisker_burst_pulses(2000);

            //// Drill reference grid along X axis

            //double ref_angle = 0;

            //for (int power = 0; power < 11; power++)
            //{
            //    watt_pilot_attenuation(0);                
            //    Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (0.25 * power), Refined_Yaxis[1] + 0.25 + (0.25 * ref_angle), ref_angle * 10, 5);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //    Thread.Sleep(1000);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //}

            //// Drill hole grid 

            //// Angle for loop

            //for (int angle = 0; angle < 4; angle++)
            //{
            //    //Power for loop
            //    for(int power = 0; power < 11; power+=2)
            //    {
            //        watt_pilot_attenuation(power * 10);
            //        Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (0.25*power), Refined_Yaxis[1] + 0.5 + (0.25*angle), angle*10, 5);
            //        myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //        Thread.Sleep(1000);
            //        myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //    }
            //}

            #endregion

            #region Ablation threshold at different angles using lines

            //// Set Repition Rate and number of pulses per trigger

            //talisker_rep_rate(10000);
            //aommode_2();

            //// Drill reference grid along X axis

            //double ref_angle = 0;

            //for (int power = 0; power < 11; power+=2)
            //{
            //    watt_pilot_attenuation(0);
            //    Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (1.25 * power/2), Refined_Yaxis[1] + 0.25 + (0.25 * ref_angle), ref_angle * 10, 5);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //    Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (1.25 * power/2) + 1, Refined_Yaxis[1] + 0.25 + (0.25 * ref_angle), ref_angle * 10, 1);
            //    myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //}

            //// Drill line grid - high power             

            //for (int angle = 0; angle < 4; angle++)
            //{
            //    //Power for loop
            //    for (int power = 0; power < 11; power += 2)
            //    {
            //        watt_pilot_attenuation(power * 10);
            //        Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (1.25 * power/2), Refined_Yaxis[1] + 0.5 + (0.25 * angle), angle * 10, 5);
            //        myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //        Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (1.25 * power/2) + 1, Refined_Yaxis[1] + 0.5 + (0.25 * angle), angle * 10, 1);
            //        myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //    }
            //}

            //// Drill line grid - low power             

            //for (int angle = 0; angle < 4; angle++)
            //{
            //    //Power for loop
            //    for (int power = 0; power < 11; power += 2)
            //    {
            //        watt_pilot_attenuation(80+(power*2));
            //        Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (1.25 * power / 2), Refined_Yaxis[1] + 1.5 + (0.25 * angle), angle * 10, 5);
            //        myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            //        Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (1.25 * power / 2) + 1, Refined_Yaxis[1] + 1.5 + (0.25 * angle), angle * 10, 1);
            //        myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            //    }
            //}


            #endregion

        }

        private void laser_control(Boolean fire)
        {
            if (fire == true)
            {
                record_power = 1; // Start recording power

                _bgtest.Logger.EnableLogging(ALoggerTypes.RESULTS, true); // Start logging diameter and position

                myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            }
            else if (fire == false)
            {
                record_power = 0; // Start recording power

                _bgtest.Logger.EnableLogging(ALoggerTypes.RESULTS, false); // Start logging diameter and position

                myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            }
        }

        private void laser_ablation()
        {
            System.IO.Directory.CreateDirectory(universal_path);

            // Take Dark Image

            icImagingControl1.OverlayBitmap.Enable = false;

            eLight_Intensity(0);
            Movement_3D_uScope(Refined_Xaxis[0] + 0.5, Refined_Yaxis[0] + 0.5, 0, true);

            string dark_image_path = universal_path + "\\Dark_Image.bmp";

            icImagingControl1.MemorySnapImage();

            icImagingControl1.MemorySaveImage(dark_image_path);

            //MessageBox.Show("Captured Dark Field");

            eLight_Intensity(100);

            // Take Flat field images

            for (int a = 0; a < 3; a++)
            {
                for (int b = 0; b < 3; b++)
                {
                    string flatfield_dir = universal_path + "\\Flat Field";
                    System.IO.Directory.CreateDirectory(flatfield_dir);

                    string flatfield_image = flatfield_dir + "\\A" + a + "B" + b + ".bmp";

                    Movement_3D_uScope(Refined_Xaxis[1] + 0.5 + (0.25 * a), Refined_Yaxis[1] + 0.5 + (0.25 * b), 0, true);

                    icImagingControl1.MemorySnapImage();

                    icImagingControl1.MemorySaveImage(flatfield_image);
                }

            }

            //MessageBox.Show("Captured Flat Field");

            talisker_attenuation(94);
            watt_pilot_attenuation(0);

            //MessageBox.Show("Do not remove beam dump");

            aomgate_high_trigger();
            aommode_0();
            
            shutter_open();

            //MessageBox.Show("Check Shutter is open");

            // Setup Beam Guage
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
            // Invoke an autocalibration cycle
            _bgtest.Calibration.SetupEGB();
            //Wait for beam block message
            Thread.Sleep(2000);
            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            Thread.Sleep(4000);

            #region Find Ablation Threshold of Silicon                       

            // Drill reference grid along X axis

            for (int iteration = 0; iteration < 10; iteration++)
            {
                //watt_pilot_attenuation(0);
                Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (0.25 * iteration), Refined_Yaxis[1] + 0.25, 0, 5);
                myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);
                Thread.Sleep(1 * 1000);
                myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
            }

            // Drill line grid             

            //MessageBox.Show("Remove Beam Dump");

            talisker_attenuation(94); // *** Check Requested Power Using AOM Repeatability

            watt_pilot_attenuation(0);

            //Power Sweep 100 to 0 using two loops fixed dwell time = 5 seconds

            int Holes_Total = 50;

            double[] Holes_X = new double[Holes_Total];
            double[] Holes_Y = new double[Holes_Total];
            double[] Holes_Z = new double[Holes_Total];
            int Holes_Hold = 0;

            int dwell_time = 5;

            #region Change Power

            for (int power_a = 0; power_a < 10; power_a++) // Change Y
            {
                //Power for loop
                for (int power_b = 0; power_b < 10; power_b +=2) // Adjust - Change X
                {
                    int power = power_a * 10 + power_b;

                    string power_string = power.ToString("0000");

                    string Save_Dir = universal_path + "\\Power = " + power_string + "\\Power"; // Adjust 

                    lbl_file_dir.Text = Save_Dir;

                    System.IO.Directory.CreateDirectory(Save_Dir);

                    power_record_file_path = Save_Dir + "\\Power " + power + ".txt";

                    lbl_file_path.Text = power_record_file_path;

                    this.Update();

                    string BeamGuage_Dir = universal_path + "\\Power = " + power_string + "\\BeamGuage";

                    System.IO.Directory.CreateDirectory(BeamGuage_Dir);

                    string BeamGuage_file_path = BeamGuage_Dir + "\\Power " + power;

                    // Set save path
                    _bgtest.Logger.SetLogName(ALoggerTypes.RESULTS, BeamGuage_file_path);

                    // Set WATT Pilot ATT

                    watt_pilot_attenuation(power);

                    // Hole position

                    Movement_3D_ablation(Refined_Xaxis[1] + 0.5 + (0.25 * (power_b/2)), Refined_Yaxis[1] + 0.5 + (0.25 * power_a), 0, 5);

                    laser_control(true);

                    Thread.Sleep(dwell_time * 1000);

                    laser_control(false);

                    // Store Hole location

                    Holes_X[Holes_Hold] = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback) + Offset_Xaxis;
                    Holes_Y[Holes_Hold] = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback) + Offset_Yaxis;
                    Holes_Z[Holes_Hold] = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);

                    using (StreamWriter Holes_X_File = new StreamWriter(universal_path + "\\Holes X.txt", true))
                    {
                        Holes_X_File.WriteLine(Holes_X[Holes_Hold].ToString());
                    }
                    using (StreamWriter Holes_Y_File = new StreamWriter(universal_path + "\\Holes Y.txt", true))
                    {
                        Holes_Y_File.WriteLine(Holes_Y[Holes_Hold].ToString());
                    }
                    using (StreamWriter Holes_Z_File = new StreamWriter(universal_path + "\\Holes Z.txt", true))
                    {
                        Holes_Z_File.WriteLine(Holes_Z[Holes_Hold].ToString());
                    }

                    Holes_Hold++;
                }
            }

            shutter_closed();

            Holes_Hold = 0;

            // Capture Holes using Microscope        

            for (int power_a = 0; power_a < 10; power_a++) // Change Y
            {
                //Power for loop
                for (int power_b = 0; power_b < 10; power_b +=2) // Adjust - Change X
                {
                    int power = power_a * 10 + power_b;

                    string power_string = power.ToString("0000");

                    string Save_Dir = universal_path + "\\Power = " + power_string + "\\Images";

                    lbl_file_dir.Text = Save_Dir;

                    System.IO.Directory.CreateDirectory(Save_Dir);

                    // Hole position

                    Movement_3D_uScope(Holes_X[Holes_Hold], Holes_Y[Holes_Hold], 0, true);

                    // Take Backup Picture Before Auto Focus

                    icImagingControl1.OverlayBitmap.Enable = false;

                    Thread.Sleep(1 * 1000);

                    icImagingControl1.MemorySnapImage();

                    icImagingControl1.MemorySaveImage(Save_Dir + "\\Backup.bmp");

                    // Auto Focus

                    uScope_focus_check(true);

                    if (AlignmentFocus_Container.Feature_Visible == true)
                    {
                        using (StreamWriter writer = new StreamWriter(Save_Dir + "\\visible.txt"))
                        {
                            writer.Write("Visible");
                        }

                        // Correct zoom offset

                        double current_D = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
                        double current_X = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                        double current_Y = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);

                        zoom_offset = current_D - microscope_focus;
                        microscope_zoom_x_correction = current_X - (Refined_Xaxis[1] + 0.5 + (0.25 * power_b));
                        microscope_zoom_y_correction = current_Y - (Refined_Yaxis[1] + 0.5 + (0.25 * power_a));
                    }

                    AlignmentFocus_Container.Feature_Visible = false;

                    for (int a = 0; a < 1; a++)
                    {
                        icImagingControl1.OverlayBitmap.Enable = false;

                        Thread.Sleep(3000);

                        string image_record_file_path = Save_Dir + "\\A" + a + ".bmp";

                        lbl_file_path.Text = image_record_file_path;

                        icImagingControl1.MemorySnapImage();

                        icImagingControl1.MemorySaveImage(image_record_file_path);
                    }

                    Holes_Hold++;
                }

            }

            icImagingControl1.OverlayBitmap.Enable = true;

            #endregion



            #endregion
        }



        #endregion

        #region Talisker and Watt Pilot Control Functions

        // Functions for serial controls 

        private void shutter_closed()
        {
            string shutter_closed = "s=0\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(shutter_closed);
            Thread.Sleep(command_delay);
            btn_Shutter.Text = "Open Shutter";
            lbl_ShutterStatus.Text = "Closed";
            lbl_ShutterStatus.BackColor = Color.Lime;            
        }

        private void shutter_open()
        {            
            string shutter_open = "s=1\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(shutter_open);
            Thread.Sleep(command_delay);
            btn_Shutter.Text = "Close Shutter";
            lbl_ShutterStatus.Text = "Open";
            lbl_ShutterStatus.BackColor = Color.Red;
        }

        private void aommode_0()
        {
            string aommode0 = "AOMMODE=0\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aommode0);
            Thread.Sleep(command_delay);
            lbl_AOMMode.Text = "Continuous";
        }

        private void aommode_2()
        {
            string aommode0 = "AOMMODE=2\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aommode0);
            Thread.Sleep(command_delay);
            lbl_AOMMode.Text = "Divided";
        }

        private void aommode_3()
        {
            string aommode0 = "AOMMODE=3\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aommode0);
            Thread.Sleep(command_delay);
            lbl_AOMMode.Text = "Burst";
        }

        private void aomgate_low_trigger()
        {
            string aomgate_low_trigger = "AOMGATE=0\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aomgate_low_trigger);
            Thread.Sleep(command_delay);
            btn_AOMGATE.Text = "AOMGate - High";
            lbl_AOMGateStatus.Text = "Low";
        }

        private void aomgate_high_trigger()
        {
            string aomgate_high_trigger = "AOMGATE=1\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(aomgate_high_trigger);
            Thread.Sleep(command_delay);
            btn_AOMGATE.Text = "AOMGate - Low";
            lbl_AOMGateStatus.Text = "High";                  
        }

        private void talisker_attenuation(int value)
        {
            string atten_command = "ATT=100\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(atten_command);
            Thread.Sleep(5000);
            atten_command = "ATT=" + value + "\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(atten_command);
            Thread.Sleep(command_delay);
            lbl_TaliskerATT.Text = value.ToString("0");
        }

        private void watt_pilot_attenuation(double value)
        {
           if (value != current_WP_value)
            {
                // Offset for watt pilot 1064 = -443 532 = +1520 355 = +7870
                double offset = -1184;
                double stepsPerUnit = 43.333;
                double resolution = 2;
                double ratio = (100 - value) / 100;
                double angle = ((Math.Acos(Math.Sqrt(ratio))) * 180.0) / (2.0 * Math.PI);
                double steps = (angle * stepsPerUnit * resolution) + offset;
                string command = "g " + steps + "\r";
                WattPilot_SerialPortCommunicator.SerialPort.Write(command); //WattPilot_1064.Write(command);
                Thread.Sleep(5000);
                lbl_WPATT.Text = (value).ToString("0");
                current_WP_value = value;
            }            
        }

        private void talisker_burst_pulses(int value)
        {
            string burst_pulses = "BURST=" + value + "\r\n";
            Talisker_SerialPortCommunicator.SerialPort.Write(burst_pulses);
            Thread.Sleep(command_delay);
            lbl_BurstPulses.Text = value.ToString("0");
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
                lbl_RepRate.Text = value.ToString("0");
            }
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

        private string GetStatus(int index)
        {
            if (statusText.ContainsKey(index))  // if unknown status - ignore it, else - get it
                return statusText[index];
            else
                return "";
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

                        if (status == 2)
                        {

                            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);
                            shutter_closed();

                            using (StreamWriter Power_Record = new StreamWriter(power_record_file_path, true))
                            {
                                Power_Record.WriteLine("Sensor over saturated");
                            }

                            Application.Exit();
                        }
                        
                        // Power or energy measurement
                        if (measurementType == powerEnergyMeasurementType)
                        {
                            measurementStr = dataArr[ind].ToString();
                            statusStr = GetStatus(statusArr[ind]);
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
                    }


                    // Display last measured data
                    LabelTime0.Text = timestampStr;
                    LabelMeasurement0.Text = measurementStr + " W";

                    if (record_power == 1)
                    {
                        using (StreamWriter Power_Record = new StreamWriter(power_record_file_path, true))
                        {
                            Power_Record.WriteLine(timestampStr + " " + measurementStr);
                        }
                    }

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
            ScanUSB();
            //OpenDevice();
            //Thread power_log = new Thread(new ThreadStart(start_power_monitoring));
            //power_log.IsBackground = true;
            //power_log.Start();
        }

        private void ScanUSB()
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
            OpenDevice();
        }

        private void OpenDevice()
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

                // **** Set whether filter is in or not *****

                int nHandle = getCurrentDeviceHandle();
                int nChannel = 0;
                int index = 1; // 0 = no filter  1 = filter
                lm_Co1.SetFilter(nHandle, nChannel, index);

                // **** Set Stream Mode ****

                lm_Co1.ConfigureStreamMode(nHandle, nChannel, 2, 1); // Immediate mode turned on

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

        private void btn_StopStream_Click(object sender, EventArgs e)
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
                    
                    lm_Co1.StopStream(nHandle, nChannel);
                }
                catch (Exception ex)
                {
                    //displayError(ex);
                }
            }

        }

        public void start_power_monitoring()
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
                    //MessageBox.Show("started streaming");
                }
                catch (Exception ex)
                {
                    //displayError(ex);
                }
            }
        }

        public void stop_power_monitoring()
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

                    lm_Co1.StopStream(nHandle, nChannel);
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
                watt_pilot_attenuation(Convert.ToDouble(txtbx_RequestedWPATT.Text));
            }
        }

        private void txtbx_RequestedTaliskerATT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                talisker_attenuation(Convert.ToInt32(txtbx_RequestedTaliskerATT.Text));
            }
        }

        private void txtbx_RequestedBurstPulses_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                talisker_burst_pulses(Convert.ToInt32(txtbx_RequestedBurstPulses.Text));                
            }
        }

        private void txtbx_RequestedRepRate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                talisker_rep_rate(Convert.ToInt32(txtbx_RequestedRepRate.Text));
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

        private void laserbtninitialisation_labels()
        {
            // Shutter

            btn_Shutter.Text = "Open Shutter";
            lbl_ShutterStatus.Text = "Closed";
            lbl_ShutterStatus.BackColor = Color.Lime;

            // Talisker ATT

            lbl_TaliskerATT.Text = 100.ToString("0");

            // Watt Pilot

            lbl_WPATT.Text = (100).ToString("0.0");

            // AOM Mode

            lbl_AOMMode.Text = "Continuous";

            // AOM GATE

            btn_AOMGATE.Text = "AOMGate - Low";
            lbl_AOMGateStatus.Text = "High";

            // Talikser Burst

            lbl_BurstPulses.Text = 1.ToString("0");

            // Talisker Rep Rate

            lbl_RepRate.Text = 200000.ToString("0");
            
            //shutter_closed();
            //watt_pilot_attenuation(100);
            //talisker_attenuation(100);
            //aommode_0();
            //aomgate_high_trigger();
            //talisker_burst_pulses(1);
            //talisker_rep_rate(200000);
        }

        #endregion




        private void btn_AOMrepeatability_Click(object sender, EventArgs e)
        {
            Thread movement = new Thread(new ThreadStart(move));
            movement.IsBackground = true;
            movement.Start();
        }

        public void move()
        {                        
            string Save_Dir = "C:/Users/User/Desktop/Share/Chris";

            lbl_file_dir.Text = Save_Dir;

            power_record_file_path = Save_Dir + "/Power_Record.txt";

            lbl_file_path.Text = power_record_file_path;

            int T_ATT = Convert.ToInt32(lbl_TaliskerATT.Text);

            int WP_ATT = Convert.ToInt32(lbl_WPATT.Text);

            set_check_laser_power(T_ATT, WP_ATT, 5, 5);
         
            #region Previous Tests

            ///////////////////////////////////////////////////////////////////////////////////////////////

            //int[] on_time = { 30, 10 };
            //int[] off_time = { 10, 30 };

            //Random on_time_random = new Random();
            //Random off_time_random = new Random();

            //for (int WP_att_setting = 0; WP_att_setting < 110; WP_att_setting += 10)
            //{

            //    for (int count = 0; count < 10; count++)
            //    {

            //        string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - Drill Hole Replication 2/" + (WP_att_setting).ToString("00");

            //        lbl_file_dir.Text = Save_Dir;

            //        System.IO.Directory.CreateDirectory(Save_Dir);

            //        string count_string = (count).ToString("0000");

            //        power_record_file_path = Save_Dir + "/Power_Record_" + count_string + ".txt";

            //        lbl_file_path.Text = power_record_file_path;

            //        set_check_laser_power(99, WP_att_setting, 2, 6);
            //    }

            //}

            ///////////////////////////////////////////////////////////////////////////////////////////////

            //string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - AOM Rise";

            //lbl_file_dir.Text = Save_Dir;

            //System.IO.Directory.CreateDirectory(Save_Dir);

            //power_record_file_path = Save_Dir + "/Power_Record_01.txt";

            //lbl_file_path.Text = power_record_file_path;

            //set_check_laser_power(97, 0, 3*60, 10);

            //Thread.Sleep(15 * 60 * 1000);

            //shutter_closed();
            //aomgate_low_trigger();
            //Thread.Sleep(3 * 60 * 1000);
            //aomgate_high_trigger();

            //for (int count = 1; count < 101; count++)
            //{

            //    string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - no warm up";

            //    lbl_file_dir.Text = Save_Dir;

            //    System.IO.Directory.CreateDirectory(Save_Dir);

            //    string count_string = count.ToString("0000");

            //    power_record_file_path = Save_Dir + "/Power_Record_" + count_string + ".txt";

            //    lbl_file_path.Text = power_record_file_path;

            //    set_check_laser_power(97, 0, 10, 10);
            //}

            //for (int count_2 = 0; count_2 < 3; count_2++)
            //{
            //    for (int count = 1; count < 101; count++)  
            //    {
            //        if (count_2 == 0)
            //        {
            //            int on_time_int = on_time_random.Next(5, 30);
            //            //off_time_random.Next(5, 30);
            //            int off_time_fixed = 10;

            //            string on_time_string = on_time_int.ToString("00");
            //            string off_time_string = off_time_fixed.ToString("00");

            //            string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - Random - On Time";

            //            lbl_file_dir.Text = Save_Dir;

            //            System.IO.Directory.CreateDirectory(Save_Dir);

            //            using (StreamWriter on_time_off_time = new StreamWriter(Save_Dir + "/On Time - Off Time.txt", true))
            //            {
            //                on_time_off_time.WriteLine(on_time_string + "  " + off_time_string);
            //            }

            //            string count_string = count.ToString("0000");                        

            //            power_record_file_path = Save_Dir + "/Power_Record_" + count_string + ".txt";

            //            lbl_file_path.Text = power_record_file_path;

            //            set_check_laser_power(0, 25, on_time_int, off_time_fixed);

            //        }

            //        else if (count_2 == 1)
            //        {
            //            //on_time_random.Next(5, 30);
            //            int on_time_fixed = 10;
            //            int off_time_int = off_time_random.Next(5, 30);                        

            //            string on_time_string = on_time_fixed.ToString("00");
            //            string off_time_string = off_time_int.ToString("00");

            //            string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - Random - Off Time";

            //            lbl_file_dir.Text = Save_Dir;

            //            System.IO.Directory.CreateDirectory(Save_Dir);

            //            using (StreamWriter on_time_off_time = new StreamWriter(Save_Dir + "/On Time - Off Time.txt", true))
            //            {
            //                on_time_off_time.WriteLine(on_time_string + "  " + off_time_string);
            //            }

            //            string count_string = count.ToString("0000");

            //            System.IO.Directory.CreateDirectory("C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - Random");

            //            power_record_file_path = Save_Dir + "/Power_Record_" + count_string + ".txt";

            //            lbl_file_path.Text = power_record_file_path;

            //            set_check_laser_power(0, 25, on_time_fixed, off_time_int);

            //        }

            //        else if (count_2 == 2)
            //        {
            //            int on_time_int = on_time_random.Next(5, 30);
            //            int off_time_int = off_time_random.Next(5, 30);

            //            string on_time_string = on_time_int.ToString("00");
            //            string off_time_string = off_time_int.ToString("00");

            //            string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - Random - On & Off Time";

            //            lbl_file_dir.Text = Save_Dir;

            //            System.IO.Directory.CreateDirectory(Save_Dir);

            //            using (StreamWriter on_time_off_time = new StreamWriter(Save_Dir + "/On Time - Off Time.txt", true))
            //            {
            //                on_time_off_time.WriteLine(on_time_string + "  " + off_time_string);
            //            }

            //            string count_string = count.ToString("0000");

            //            power_record_file_path = Save_Dir + "/Power_Record_" + count_string + ".txt";

            //            lbl_file_path.Text = power_record_file_path;

            //            set_check_laser_power(0, 25, on_time_int, off_time_int);

            //        }

            //        else
            //        {

            //            string on_time_string = on_time[count_2].ToString("00");
            //            string off_time_string = off_time[count_2].ToString("00");

            //            string Save_Dir = "C:/Users/User/Desktop/Share/Chris/AOM Power Repeatability - On Time =" + on_time_string + " Off Time = " + off_time_string;

            //            lbl_file_dir.Text = Save_Dir;

            //            string count_string = count.ToString("0000");

            //            System.IO.Directory.CreateDirectory(Save_Dir);

            //            power_record_file_path = Save_Dir + "/Power_Record_" + count_string + ".txt";

            //            lbl_file_path.Text = power_record_file_path;

            //            set_check_laser_power(0, 25, on_time[count_2], off_time[count_2]);
            //        }
            //    }
            //Thread.Sleep(15*60*1000);

            //stop_power_monitoring();
            //}

            #endregion

            shutter_closed();

                MessageBox.Show("Completed");
            
        }

        public void set_check_laser_power(int talisker_att, int wattpilot_att, int on_time, int off_time) // on_time & off_time are in seconds 
        {
            //setup laser parameters

            watt_pilot_attenuation(wattpilot_att);
            talisker_attenuation(talisker_att);
            shutter_open();
                        
            // Move to safe of sample location

            //// Need to define safe spot

            //myController.Commands.Motion.Setup.Absolute();

            //myController.Commands.Motion.Linear("Y", 2.25, 1);

            record_power = 1;

            //using (StreamWriter Power_Record = new StreamWriter(power_record_file_path, true))
            //{
            //    Power_Record.WriteLine("Laser ON");
            //}

            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.On);

            Thread.Sleep(on_time*1000);

            myController.Commands.PSO.Control("X", Aerotech.A3200.Commands.PsoMode.Off);

            //using (StreamWriter Power_Record = new StreamWriter(power_record_file_path, true))
            //{
            //    Power_Record.WriteLine("Laser OFF");
            //}

            record_power = 0;

            Thread.Sleep(off_time*1000);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            shutter_closed();
            eLight_Intensity(0);
            _bgtest.Instance.Shutdown();
            Application.Exit();
        }

        #region Microscope Control

        private void cmdSaveBitmap_Click(object sender, EventArgs e)
        {
            icImagingControl1.OverlayBitmap.Enable = false;
            SaveFileDialog saveFileDialog1;
            icImagingControl1.MemorySnapImage();
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                icImagingControl1.MemorySaveImage(saveFileDialog1.FileName);
            }
            icImagingControl1.OverlayBitmap.Enable = true;
        }

        private void Home_Zoom()
        {
            uScope_zoom_SerialPortCommunicator.SerialPort.Write("XH\r");
            Thread.Sleep(7500);
        }

        private void btn_Min_Zoom_Click(object sender, EventArgs e)
        {
            Min_Zoom(0);
        }

        private void Min_Zoom(int use_type)
        {
            lbl_Moving_Zoom.Visible = true;
            this.Update();                     

            btn_Min_Zoom.Enabled = false;

            uScope_zoom_SerialPortCommunicator.SerialPort.Write("XG000000\r");
            Thread.Sleep(7500);
            btn_Max_Zoom.Enabled = true;

            if (use_type == 0)  // button press
            {
                double current_D = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
                double current_X = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                double current_Y = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);

                zoom_offset = current_D - min_zoom_focus_d_btn;
                microscope_zoom_x_correction = current_X - min_zoom_focus_x_btn;
                microscope_zoom_y_correction = current_Y - min_zoom_focus_y_btn;

                myController.Commands.Motion.Linear("D", min_zoom_focus_d_btn, 2);
                myController.Commands.Motion.Linear("X", min_zoom_focus_x_btn, 2);
                myController.Commands.Motion.Linear("Y", min_zoom_focus_y_btn, 2);
            }
            else if (use_type == 1) // Called in process
            {
                myController.Commands.Motion.Linear("D", microscope_focus, 2);
            }
            else if (use_type == 2)
            {
            }
            
            lbl_Moving_Zoom.Visible = false;
            this.Update();
        }


        private void btn_Max_Zoom_Click(object sender, EventArgs e)
        {
            Max_Zoom(0);
        }

        private void Max_Zoom(int use_type)
        {
            lbl_Moving_Zoom.Visible = true;
            this.Update();
                        
            btn_Max_Zoom.Enabled = false;

            if (use_type == 0)  // button press
            {
                min_zoom_focus_d_btn = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
                min_zoom_focus_x_btn = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
                min_zoom_focus_y_btn = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);

                myController.Commands.Motion.Setup.Absolute();
                myController.Commands.Motion.Linear("D", min_zoom_focus_d_btn + zoom_offset, 2);
                myController.Commands.Motion.Linear("X", min_zoom_focus_x_btn + microscope_zoom_x_correction, 2);
                myController.Commands.Motion.Linear("Y", min_zoom_focus_y_btn + microscope_zoom_y_correction, 2);

            }

            uScope_zoom_SerialPortCommunicator.SerialPort.Write("XH\r");

            Thread.Sleep(7500);

            uScope_zoom_SerialPortCommunicator.SerialPort.Write("XG006C48\r");

            Thread.Sleep(7500);

            btn_Min_Zoom.Enabled = true;

            

            lbl_Moving_Zoom.Visible = false;
            this.Update();
        }

        private void btn_Min_Intensity_Click(object sender, EventArgs e)
        {
            eLight_Intensity(15);            
        }               

        private void btn_Max_Intensity_Click(object sender, EventArgs e)
        {
            eLight_Intensity(100);            
        }

        private void eLight_Intensity(int percentage_intensity)
        {
            int scaled = Convert.ToInt32(percentage_intensity * (242 / 100));

            string hexOutput = scaled.ToString("X");

            elight_SerialPortCommunicator.SerialPort.Write("&");
            elight_SerialPortCommunicator.SerialPort.Write("i" + hexOutput);
            elight_SerialPortCommunicator.SerialPort.Write("\r");

            lbl_Intensity.Text = percentage_intensity.ToString() + "%";
            trackbar_intensity.Value = percentage_intensity;
        }
        
        private void trackbar_intensity_Scroll(object sender, EventArgs e)
        {
            eLight_Intensity(trackbar_intensity.Value);            
        }

        private void btn_zoom_test_Click(object sender, EventArgs e)
        {
            Movement_3D_ablation(Refined_Xaxis[1], Refined_Yaxis[1], 0, 5);
            Movement_3D_uScope(Refined_Xaxis[1], Refined_Yaxis[1], 0, true);
            shutter_closed();

        }

        private void btn_zoom_D_set_Click(object sender, EventArgs e)
        {
            double current_D = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

            zoom_offset = current_D - microscope_focus;
        }

        //private void uScope_img_alignment(Boolean zoom)
        //{
        //    icImagingControl1.OverlayBitmap.Enable = false;

        //    ImageProcessing IPForm = new ImageProcessing();
        //    IPForm.Show();
        //    IPForm.Activate();

        //    while (AlignmentFocus_Container.Aligned != true)
        //    {
        //        icImagingControl1.MemorySnapImage(1000);
        //        Bitmap live_bmp = icImagingControl1.ImageActiveBuffer.Bitmap;
        //        IPForm.DetectHole(15, live_bmp, zoom); //30

        //        myController.Commands.Motion.Setup.Incremental();
        //        myController.Commands.Motion.Linear("X", -AlignmentFocus_Container.X_Correction_MM, 1); //*** Check which way correction should be applied
        //        myController.Commands.Motion.Linear("Y", -AlignmentFocus_Container.Y_Correction_MM, 1); //*** Check which way correction should be applied
        //        myController.Commands.Motion.Setup.Absolute();

        //        if (AlignmentFocus_Container.X_Correction_MM == 0 && AlignmentFocus_Container.Y_Correction_MM == 0)
        //        {
        //            AlignmentFocus_Container.Aligned = true;
        //        }
        //    }

        //    IPForm.Close();
        //    icImagingControl1.OverlayBitmap.Enable = true;
        //    AlignmentFocus_Container.Aligned = false;
        //}

        private void btn_autoalign_Click(object sender, EventArgs e)
        {
            Boolean zoom = false;

            if (btn_Max_Zoom.Enabled == true)
            {
                zoom = false;
            }
            else if (btn_Min_Zoom.Enabled == true)
            {
                zoom = true;
            }

            //uScope_img_alignment(zoom);
        }

        private void uScope_focus_check(Boolean zoom)
        {
            icImagingControl1.OverlayBitmap.Enable = false;

            ImageProcessing IPForm = new ImageProcessing();

            IPForm.Show();
            IPForm.Activate();

            double Focus_max_area = 0;
            double Laplace_max = 0;
            double scan_length = 0;
            double scan_length_2 = 0;
            double scan_length_3 = 0;
            double scan_steps;
            int iteration = 0;
            int cycle_count = 0;
            Boolean Completed_Gross_Pass = false;

            if (zoom == false)
            {
                scan_length = 0.5;
                scan_length_2 = 0.1;
                scan_length_3 = 0.01;
                scan_steps = scan_length / 0.01;                
            }
            else
            {
                scan_length = 0.3;
                scan_length_2 = 0.02;
                scan_length_3 = 0.005;
                scan_steps = scan_length / 0.001;                
            }
             
            double Daxis_intial = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            double Daxis_max = 0;
            
            while (AlignmentFocus_Container.Focused != true)
            {
                for (int i = 0; i < scan_steps; i++)
                {
                    myController.Commands.Motion.Setup.Absolute();
                    myController.Commands.Motion.Linear("D", Daxis_intial + scan_length / 2 - scan_length / scan_steps * i, 1);

                    icImagingControl1.MemorySnapImage(1000);
                    Bitmap live_bmp = icImagingControl1.ImageActiveBuffer.Bitmap;

                    //string file_directory = "C:/Users/User/Desktop/Share/Chris/Laplace" + scan_length.ToString();
                    //System.IO.Directory.CreateDirectory(file_directory);
                    //live_bmp.Save(file_directory + "/" + i + ".bmp");

                    IPForm.DetectHole(15, live_bmp, true);
                    
                    if (AlignmentFocus_Container.Max_Area >= Focus_max_area)
                    {
                        Focus_max_area = AlignmentFocus_Container.Max_Area;
                        Daxis_max = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);

                        myController.Commands.Motion.Setup.Incremental();
                        myController.Commands.Motion.Linear("X", -AlignmentFocus_Container.X_Correction_MM, 1);
                        myController.Commands.Motion.Linear("Y", -AlignmentFocus_Container.Y_Correction_MM, 1);
                        myController.Commands.Motion.Setup.Absolute(); ;

                        iteration = i;
                    }

                    IPForm.StepValue((Daxis_intial + scan_length / 2 - scan_length / scan_steps * i).ToString());
                    IPForm.IterationValue(i.ToString());
                }

                Daxis_intial = Daxis_max;
                scan_length = scan_length_2;
                scan_steps = scan_length_2 / 0.0001;
                              

                if (iteration >= (scan_steps / 2 - scan_steps / 10) && iteration <= (scan_steps / 2 + scan_steps / 10) && Completed_Gross_Pass == true)
                {                
                    AlignmentFocus_Container.Focused = true;                    
                }
                
                if (cycle_count == 2)
                {
                    AlignmentFocus_Container.Focused = true;
                }

                Completed_Gross_Pass = true;
                cycle_count = cycle_count + 1;              
            }

            // Laplace Transform Scan

            Daxis_intial = Daxis_max;
            scan_length = scan_length_3;
            scan_steps = scan_length_3 / 0.0001;            
            
            for (int i = 0; i < scan_steps; i++)
            {
                myController.Commands.Motion.Setup.Absolute();
                myController.Commands.Motion.Linear("D", Daxis_intial + scan_length / 2 - scan_length / scan_steps * i, 1);

                icImagingControl1.MemorySnapImage(1000);
                Bitmap live_bmp = icImagingControl1.ImageActiveBuffer.Bitmap;
                                
                IPForm.focus_determination(live_bmp);

                if (AlignmentFocus_Container.Laplace_std >= Laplace_max)
                {
                    Laplace_max = AlignmentFocus_Container.Laplace_std;
                    Daxis_max = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
                    iteration = i;
                }

                IPForm.StepValue((Daxis_intial + scan_length / 2 - scan_length / scan_steps * i).ToString());
                IPForm.IterationValue(i.ToString());
            }

            myController.Commands.Motion.Linear("D", Daxis_max, 1);

            icImagingControl1.OverlayBitmap.Enable = true;
            IPForm.Close();
            AlignmentFocus_Container.Focused = false;
            Completed_Gross_Pass = false;
            Focus_max_area = 0;
            Laplace_max = 0;
        }
                
        private void btn_AutoFocus_Click(object sender, EventArgs e)
        {
            Boolean zoom = false;

            if (btn_Max_Zoom.Enabled == true)
            {
                zoom = false;
            }
            else if (btn_Min_Zoom.Enabled == true)
            {
                zoom = true;
            }

            uScope_focus_check(zoom);
        }

        #endregion

        private void btn_LaserFocus_Click(object sender, EventArgs e)
        {
            microscope_focus = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
            Movement_3D_ablation(myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback), myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback), 0, 5);
        }

        private void btn_uScopeFocus_Click(object sender, EventArgs e)
        {
            Movement_3D_uScope(myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback) + Offset_Xaxis, myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback) + Offset_Yaxis, 0, false);
        }
    }

    public static class AlignmentFocus_Container
    {
        public static Boolean Aligned = false;
        public static Boolean Focused = false;

        public static double Max_Area;
        public static double X_Correction_MM;
        public static double Y_Correction_MM;

        public static double Laplace_std;

        public static Boolean Feature_Visible;

    }

}

