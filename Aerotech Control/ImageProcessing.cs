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
using System.Threading;

using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Imaging.Textures;

namespace Aerotech_Control
{
    public partial class ImageProcessing : Form
    {
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

        public void DetectHole(int threshold_value, Bitmap sourceImage)
        {

            System.Drawing.Bitmap Threshold_Image;
            System.Drawing.Bitmap Invert_Image;

            lbl_thresholdvalue.Text = trackBar1.Value.ToString();

            sourceImage = (Bitmap)Bitmap.FromFile(AlignmentFocus_Container.temp_img_path);
                
            // save original image
            Bitmap originalImage = sourceImage;

            pictureBox.Image = null;
            pictureBox.Image = originalImage;

            this.Update();

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
            double largest_area = 0;
            double X_centroid = 0;
            double Y_centroid = 0;

            int foundBlobsCount = blobsBrowser.SetImage(Resize_Image);

            blobsCountLabel.Text = string.Format("Found blobs' count: {0}", foundBlobsCount);

            double[,] stats = new double[foundBlobsCount, 3];

            foreach (Blob blob in blobsBrowser.blobs)
            {

                stats[0, 0] = blob.Area;
                stats[0, 1] = blob.CenterOfGravity.X;
                stats[0, 2] = blob.CenterOfGravity.Y;

                if (stats[0, 0] > largest_area)
                {

                    largest_area = stats[0, 0];
                    X_centroid = stats[0, 1];
                    Y_centroid = stats[0, 2];

                }
            }

            if (largest_area >= 1500)
            {
                // Calculate X & Y correction 

                double diff_x = 200 - X_centroid;
                double diff_y = 150 - Y_centroid;

                double PixeltoMM = 29200.3044;  // 29200.3044 pixels per mm 

                if (Math.Abs(diff_x) > 15 || Math.Abs(diff_y) > 15)
                {
                    AlignmentFocus_Container.X_Correction_MM = diff_x * 4 / PixeltoMM;
                    AlignmentFocus_Container.Y_Correction_MM = diff_y * 4 / PixeltoMM;
                }
                else
                {
                    AlignmentFocus_Container.X_Correction_MM = 0;
                    AlignmentFocus_Container.Y_Correction_MM = 0;
                    AlignmentFocus_Container.Aligned = true;
                }
            }                        
        }

       
        public void focus_determination(Bitmap sourceImage)
        {
            
            lbl_thresholdvalue.Text = trackBar1.Value.ToString();

            // get grayscale image
            sourceImage = Grayscale.CommonAlgorithms.RMY.Apply(sourceImage);

            // save original image
            Bitmap originalImage = sourceImage;
                        
            // Laplace transform to find focus

            // Crop image 

            Crop Crop_Filter = new Crop(new Rectangle(400, 400, 800, 400));
            Bitmap Crop_Image = Crop_Filter.Apply(sourceImage);


            // define emboss kernel
            int[,] kernel = {
                                { 0, -1,   0 },
                                { -1, 4,  -1 },
                                { 0, -1,   0 } };
            // create filter
            Convolution Laplace_Filter = new Convolution(kernel);
            // apply the filter
            Bitmap Laplace_Image = Laplace_Filter.Apply(Crop_Image);

            ImageStatistics Laplace_Stats = new ImageStatistics(Laplace_Image);

            lbl_laplacestd.Text = "Laplace Std = " + Math.Pow(Laplace_Stats.Gray.StdDev, 2).ToString();

            AlignmentFocus_Container.Laplace_std = Math.Pow(Laplace_Stats.Gray.StdDev, 2);

            pictureBox.Image = null;
            pictureBox.Image = sourceImage;

            this.Update();

            Thread.Sleep(1000);
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

        //private void trackBar1_Scroll(object sender, EventArgs e)
        //{
        //    DetectHole(trackBar1.Value);
        //}

        //private void btn_LoadImage_Click(object sender, EventArgs e)
        //{
        //    DialogResult result = openFileDialog1.ShowDialog();
        //    if (result == DialogResult.OK || result == DialogResult.Yes)
        //    {
        //        fileNameTextBox.Text = openFileDialog1.FileName;
        //    }
        //}   
        public void StepValue(string s)
        {
            lbl_step.Text = s;
        }

        public void IterationValue(string s)
        {
            lbl_iteration.Text = s;
        }
    }

    

}
