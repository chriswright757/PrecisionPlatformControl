using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

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

namespace Aerotech_Control
{
    public partial class Form1 : Form
    {
        #region Global Variables Aerotech and Control

        private Controller myController;
        private int axisIndex;
        private int taskIndex;
        ControllerDiagPacket controllerDiagPacket;

        AxisMask cont_AxisMask;

        List<string> listA = new List<string>();
        List<string> listB = new List<string>();
        List<string> listC = new List<string>();
        List<string> zVal = new List<string>();

        int list_Size = 0;

        double cont_XPos;
        double cont_YPos;
        double cont_ZPos;
        double JogValue = 0.1;

        #endregion

        private void SetTaskState(NewTaskStatesArrivedEventArgs e)
        {
            lbl_TaskState.Text = e.TaskStates[this.taskIndex].ToString();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lbl_JogValue.Text = JogValue.ToString() + " mm";

            // Initialising Microscope Camera

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

            //icImagingControl1.LiveStart();
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
                              

                myController.Variables.Global.Doubles[0].Value = 0;
             
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
                //******this.Invoke(new Action<NewDiagPacketArrivedEventArgs>(SetAxisState), e);
            }
            catch
            {
            }
        }

        private void btn_EnableAxis_Click(object sender, EventArgs e)
        {
            try
            {
                this.myController.Commands[this.taskIndex].Axes[this.axisIndex].Motion.Enable();
            }
            catch (A3200Exception exception)
            {
               // txtbx_Error.Text = exception.Message;
            }
        }


        #endregion ControllerEvents

        private void btn_EnableAir_Click(object sender, EventArgs e)
        {
            myController.Commands.IO.DigitalOutput(1, "X", 1);
        }

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
        }

        private void btn_NegJogX_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("X", -JogValue, 1);
        }

        private void btn_PosJogY_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Y", JogValue, 1);
        }

        private void btn_NegJogY_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("X", -JogValue, 1);
        }

        private void btn_PosJogZ_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Z", JogValue, 1);
        }

        private void btn_NegJogZ_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("Z", -JogValue, 1);
        }

        private void btn_PosJogD_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("D", JogValue, 1);
        }

        private void btn_NegJogD_Click(object sender, EventArgs e)
        {
            myController.Commands.Motion.Setup.Incremental();
            myController.Commands.Motion.Linear("D", -JogValue, 1);
        }

        #endregion

        #region Collect Initial Corner Positions

        private void btn_cornerA_Click(object sender, EventArgs e)
        {
            double A_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            double A_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            double A_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            double A_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
        }

        private void btn_cornerB_Click(object sender, EventArgs e)
        {
            double B_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            double B_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            double B_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            double B_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
        }

        private void btn_cornerC_Click(object sender, EventArgs e)
        {
            double C_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            double C_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            double C_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            double C_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
        }

        private void btn_cornerD_Click(object sender, EventArgs e)
        {
            double D_Xaxis = myController.Commands.Status.AxisStatus("X", AxisStatusSignal.ProgramPositionFeedback);
            double D_Yaxis = myController.Commands.Status.AxisStatus("Y", AxisStatusSignal.ProgramPositionFeedback);
            double D_Zaxis = myController.Commands.Status.AxisStatus("Z", AxisStatusSignal.ProgramPositionFeedback);
            double D_Daxis = myController.Commands.Status.AxisStatus("D", AxisStatusSignal.ProgramPositionFeedback);
        }

        #endregion

        private void btn_XHome_Click(object sender, EventArgs e)
        {
            
        }

        private void btn_TiltCorrection_Click(object sender, EventArgs e)
        {
            double[,] X_coords = new double[1, 4];

            
        }
    }
}
