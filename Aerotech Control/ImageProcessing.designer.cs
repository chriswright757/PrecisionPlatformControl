namespace Aerotech_Control
{
    partial class ImageProcessing
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageProcessing));
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.btn_LoadImage = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.fileNameTextBox = new System.Windows.Forms.TextBox();
            this.lbl_thresholdvalue = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.blobsCountLabel = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.blobsBrowser = new BlobsExplorer.BlobsBrowser();
            this.showRectangleAroundSelectionCheck = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.highlightTypeCombo = new System.Windows.Forms.ComboBox();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.lbl_laplacestd = new System.Windows.Forms.Label();
            this.lbl_step = new System.Windows.Forms.Label();
            this.lbl_iteration = new System.Windows.Forms.Label();
            this.lbl_largestblob = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Location = new System.Drawing.Point(16, 12);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(400, 300);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox.TabIndex = 1;
            this.pictureBox.TabStop = false;
            // 
            // btn_LoadImage
            // 
            this.btn_LoadImage.Location = new System.Drawing.Point(341, 412);
            this.btn_LoadImage.Name = "btn_LoadImage";
            this.btn_LoadImage.Size = new System.Drawing.Size(75, 23);
            this.btn_LoadImage.TabIndex = 2;
            this.btn_LoadImage.Text = "Load Image";
            this.btn_LoadImage.UseVisualStyleBackColor = true;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // fileNameTextBox
            // 
            this.fileNameTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fileNameTextBox.Location = new System.Drawing.Point(16, 399);
            this.fileNameTextBox.Multiline = true;
            this.fileNameTextBox.Name = "fileNameTextBox";
            this.fileNameTextBox.ReadOnly = true;
            this.fileNameTextBox.Size = new System.Drawing.Size(323, 49);
            this.fileNameTextBox.TabIndex = 15;
            // 
            // lbl_thresholdvalue
            // 
            this.lbl_thresholdvalue.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.lbl_thresholdvalue.AutoSize = true;
            this.lbl_thresholdvalue.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_thresholdvalue.Location = new System.Drawing.Point(141, 366);
            this.lbl_thresholdvalue.Name = "lbl_thresholdvalue";
            this.lbl_thresholdvalue.Size = new System.Drawing.Size(150, 24);
            this.lbl_thresholdvalue.TabIndex = 17;
            this.lbl_thresholdvalue.Text = "Threshold Value";
            this.lbl_thresholdvalue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // trackBar1
            // 
            this.trackBar1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.trackBar1.LargeChange = 25;
            this.trackBar1.Location = new System.Drawing.Point(16, 318);
            this.trackBar1.Maximum = 255;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(400, 45);
            this.trackBar1.TabIndex = 16;
            this.trackBar1.TickFrequency = 5;
            // 
            // blobsCountLabel
            // 
            this.blobsCountLabel.Location = new System.Drawing.Point(430, 318);
            this.blobsCountLabel.Name = "blobsCountLabel";
            this.blobsCountLabel.Size = new System.Drawing.Size(404, 23);
            this.blobsCountLabel.TabIndex = 19;
            this.blobsCountLabel.Text = "Blobs Count";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Location = new System.Drawing.Point(433, 12);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.blobsBrowser);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.showRectangleAroundSelectionCheck);
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Panel2.Controls.Add(this.propertyGrid);
            this.splitContainer1.Size = new System.Drawing.Size(770, 300);
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.TabIndex = 20;
            // 
            // blobsBrowser
            // 
            this.blobsBrowser.Highlighting = BlobsExplorer.BlobsBrowser.HightlightType.Quadrilateral;
            this.blobsBrowser.Location = new System.Drawing.Point(39, 29);
            this.blobsBrowser.Name = "blobsBrowser";
            this.blobsBrowser.ShowRectangleAroundSelection = false;
            this.blobsBrowser.Size = new System.Drawing.Size(322, 242);
            this.blobsBrowser.TabIndex = 18;
            this.blobsBrowser.Text = "blobsBrowser";
            this.blobsBrowser.BlobSelected += new BlobsExplorer.BlobSelectionHandler(this.blobsBrowser_BlobSelected);
            // 
            // showRectangleAroundSelectionCheck
            // 
            this.showRectangleAroundSelectionCheck.AutoSize = true;
            this.showRectangleAroundSelectionCheck.Location = new System.Drawing.Point(3, 69);
            this.showRectangleAroundSelectionCheck.Name = "showRectangleAroundSelectionCheck";
            this.showRectangleAroundSelectionCheck.Size = new System.Drawing.Size(181, 17);
            this.showRectangleAroundSelectionCheck.TabIndex = 1;
            this.showRectangleAroundSelectionCheck.Text = "Show rectangle around selection";
            this.showRectangleAroundSelectionCheck.UseVisualStyleBackColor = true;
            this.showRectangleAroundSelectionCheck.CheckedChanged += new System.EventHandler(this.showRectangleAroundSelectionCheck_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.highlightTypeCombo);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(363, 65);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Highlight Type";
            // 
            // highlightTypeCombo
            // 
            this.highlightTypeCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.highlightTypeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.highlightTypeCombo.FormattingEnabled = true;
            this.highlightTypeCombo.Items.AddRange(new object[] {
            "Convex Hull",
            "Left/Right Edges",
            "Top/Bottom Edges",
            "Quadrilateral"});
            this.highlightTypeCombo.Location = new System.Drawing.Point(6, 20);
            this.highlightTypeCombo.Name = "highlightTypeCombo";
            this.highlightTypeCombo.Size = new System.Drawing.Size(351, 21);
            this.highlightTypeCombo.TabIndex = 0;
            this.highlightTypeCombo.SelectedIndexChanged += new System.EventHandler(this.highlightTypeCombo_SelectedIndexChanged);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid.HelpVisible = false;
            this.propertyGrid.LineColor = System.Drawing.SystemColors.ControlDark;
            this.propertyGrid.Location = new System.Drawing.Point(3, 92);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(363, 208);
            this.propertyGrid.TabIndex = 2;
            this.propertyGrid.ToolbarVisible = false;
            // 
            // lbl_laplacestd
            // 
            this.lbl_laplacestd.AutoSize = true;
            this.lbl_laplacestd.Location = new System.Drawing.Point(430, 366);
            this.lbl_laplacestd.Name = "lbl_laplacestd";
            this.lbl_laplacestd.Size = new System.Drawing.Size(64, 13);
            this.lbl_laplacestd.TabIndex = 23;
            this.lbl_laplacestd.Text = "Laplace Std";
            // 
            // lbl_step
            // 
            this.lbl_step.AutoSize = true;
            this.lbl_step.Location = new System.Drawing.Point(430, 397);
            this.lbl_step.Name = "lbl_step";
            this.lbl_step.Size = new System.Drawing.Size(29, 13);
            this.lbl_step.TabIndex = 24;
            this.lbl_step.Text = "Step";
            // 
            // lbl_iteration
            // 
            this.lbl_iteration.AutoSize = true;
            this.lbl_iteration.Location = new System.Drawing.Point(433, 421);
            this.lbl_iteration.Name = "lbl_iteration";
            this.lbl_iteration.Size = new System.Drawing.Size(45, 13);
            this.lbl_iteration.TabIndex = 25;
            this.lbl_iteration.Text = "Iteration";
            // 
            // lbl_largestblob
            // 
            this.lbl_largestblob.AutoSize = true;
            this.lbl_largestblob.Location = new System.Drawing.Point(433, 341);
            this.lbl_largestblob.Name = "lbl_largestblob";
            this.lbl_largestblob.Size = new System.Drawing.Size(66, 13);
            this.lbl_largestblob.TabIndex = 26;
            this.lbl_largestblob.Text = "Largest Blob";
            // 
            // ImageProcessing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1215, 459);
            this.Controls.Add(this.lbl_largestblob);
            this.Controls.Add(this.lbl_iteration);
            this.Controls.Add(this.lbl_step);
            this.Controls.Add(this.lbl_laplacestd);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.blobsCountLabel);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.lbl_thresholdvalue);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.fileNameTextBox);
            this.Controls.Add(this.btn_LoadImage);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ImageProcessing";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Image Processing";
            this.Load += new System.EventHandler(this.ImageProcessing_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Button btn_LoadImage;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox fileNameTextBox;
        private System.Windows.Forms.Label lbl_thresholdvalue;
        private System.Windows.Forms.TrackBar trackBar1;
        private BlobsExplorer.BlobsBrowser blobsBrowser;
        private System.Windows.Forms.Label blobsCountLabel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox showRectangleAroundSelectionCheck;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox highlightTypeCombo;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.Label lbl_laplacestd;
        private System.Windows.Forms.Label lbl_step;
        private System.Windows.Forms.Label lbl_iteration;
        private System.Windows.Forms.Label lbl_largestblob;
    }
}

