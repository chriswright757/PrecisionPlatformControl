using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Imaging.Textures;

namespace Aerotech_Control
{
    public partial class ImageProcessing : Form
    {
        private System.Drawing.Bitmap sourceImage;
        private System.Drawing.Bitmap Threshold_Image;
        private System.Drawing.Bitmap Invert_Image;

        int att = 0;
        int dwell_time = 1;

        double largest_area = 0;
        double X_centroid;
        double Y_centroid;

        public Boolean Aligned { get; set; }


        public ImageProcessing()
        {
            InitializeComponent();

            highlightTypeCombo.SelectedIndex = 0;
            showRectangleAroundSelectionCheck.Checked = blobsBrowser.ShowRectangleAroundSelection;
        }

        private void ImageProcessing_Load(object sender, EventArgs e)
        {
            trackBar1.Value = 30;
        }


        

        public void DetectHole(int threshold_value)
        {

                lbl_thresholdvalue.Text = trackBar1.Value.ToString();

                sourceImage = (Bitmap)Bitmap.FromFile(fileNameTextBox.Text);
                

                // save original image
                Bitmap originalImage = sourceImage;

                // get grayscale image
                sourceImage = Grayscale.CommonAlgorithms.RMY.Apply(sourceImage);

                // apply threshold filter
                Threshold Threshold_Filter = new Threshold(threshold_value);
                Threshold_Image = Threshold_Filter.Apply(sourceImage);

                // apply invert filter
                Invert Invert_Filter = new Invert();
                Invert_Image = Invert_Filter.Apply(Threshold_Image);

                // create Resize filter
                ResizeBilinear Resize_Filter = new ResizeBilinear(400, 300);
                Bitmap Resize_Image = Resize_Filter.Apply(Invert_Image);

                //Perfrom Blob Detection
                ProcessImage(Resize_Image);
                
                // Laplace transform to find focus

                // Crop image 

                Crop Crop_Filter = new Crop(new Rectangle(400,400,800,400));
                Bitmap Crop_Image = Crop_Filter.Apply(sourceImage);
                                    

                // define emboss kernel
                int[,] kernel = {
                                    { 0, 1,  0 },
                                    { 1, 4,  1 },
                                    { 0, 1,  0 } };
                // create filter
                Convolution Laplace_Filter = new Convolution(kernel);
                // apply the filter
                Bitmap Laplace_Image = Laplace_Filter.Apply(Crop_Image);
                
                ImageStatistics Laplace_Stats = new ImageStatistics(Laplace_Image);

                lbl_laplacestd.Text = Math.Pow(Laplace_Stats.Gray.StdDev,2).ToString();

                pictureBox.Image = null;
                pictureBox.Image = Laplace_Image;
                this.Update();             

            
        }

        private void ProcessImage(Bitmap image)
        {
            int foundBlobsCount = blobsBrowser.SetImage(image);

            blobsCountLabel.Text = string.Format("Found blobs' count: {0}", foundBlobsCount);

            double[,] stats = new double[foundBlobsCount, 3];            

            foreach (Blob blob in blobsBrowser.blobs)
            {
                //PointF cog = blob.CenterOfGravity.ToPointF();

                stats[0, 0] = blob.Area;
                stats[0, 1] = blob.CenterOfGravity.X;
                stats[0, 2] = blob.CenterOfGravity.Y;

                if (stats[0,0] > largest_area)
                {

                    largest_area = stats[0, 0];
                    X_centroid = stats[0, 1];
                    Y_centroid = stats[0, 2];

                }

                lbl_area.Text = largest_area.ToString();
            }
        }



        // Blob was selected - display its information
        private void blobsBrowser_BlobSelected(object sender, Blob blob)
        {
            propertyGrid.SelectedObject = blob;
            propertyGrid.ExpandAllGridItems();
        }

        // Change type of blobs' highlighting
        private void highlightTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            blobsBrowser.Highlighting = (BlobsExplorer.BlobsBrowser.HightlightType) highlightTypeCombo.SelectedIndex;

        }

        // Toggle displaying of rectangle around selection
        private void showRectangleAroundSelectionCheck_CheckedChanged(object sender, EventArgs e)
        {
            blobsBrowser.ShowRectangleAroundSelection = showRectangleAroundSelectionCheck.Checked;
        }



        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            DetectHole(trackBar1.Value);
        }

        private void btn_LoadImage_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                fileNameTextBox.Text = openFileDialog1.FileName;
            }

            //string image_name = "D:/OneDrive - University Of Cambridge/Cambridge - PhD/Experiments/Silicon Ablation Threshold/Trial 3/Power = " + att.ToString("0000") + "/Images/Dwell Time = " + dwell_time + "/A0.bmp";

            //fileNameTextBox.Text = image_name;
            ////}


            ////att = att + 10;
            //dwell_time = dwell_time + 1;

            //if (dwell_time == 11)
            //{
            //    dwell_time = 1;
            //    att = att + 10;
            //}
        }

        private void fileNameTextBox_TextChanged(object sender, EventArgs e)
        {
            DetectHole(30);
        }


    }
}
