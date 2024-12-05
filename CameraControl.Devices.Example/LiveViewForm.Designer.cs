namespace CameraControl.Devices.Example
{
  partial class LiveViewForm
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
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btn_start = new System.Windows.Forms.Button();
            this.btn_stop = new System.Windows.Forms.Button();
            this.FocusNear10 = new System.Windows.Forms.Button();
            this.FocusF10 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(12, 43);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(555, 416);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // btn_start
            // 
            this.btn_start.Location = new System.Drawing.Point(12, 12);
            this.btn_start.Name = "btn_start";
            this.btn_start.Size = new System.Drawing.Size(75, 23);
            this.btn_start.TabIndex = 1;
            this.btn_start.Text = "Start";
            this.btn_start.UseVisualStyleBackColor = true;
            this.btn_start.Click += new System.EventHandler(this.btn_start_Click);
            // 
            // btn_stop
            // 
            this.btn_stop.Location = new System.Drawing.Point(492, 14);
            this.btn_stop.Name = "btn_stop";
            this.btn_stop.Size = new System.Drawing.Size(75, 23);
            this.btn_stop.TabIndex = 2;
            this.btn_stop.Text = "Stop";
            this.btn_stop.UseVisualStyleBackColor = true;
            this.btn_stop.Click += new System.EventHandler(this.btn_stop_Click);
            // 
            // FocusNear10
            // 
            this.FocusNear10.Location = new System.Drawing.Point(287, 14);
            this.FocusNear10.Name = "FocusNear10";
            this.FocusNear10.Size = new System.Drawing.Size(101, 23);
            this.FocusNear10.TabIndex = 7;
            this.FocusNear10.Text = "FocusNear10";
            this.FocusNear10.UseVisualStyleBackColor = true;
            this.FocusNear10.Click += new System.EventHandler(this.FocusNear10_Click);
            // 
            // FocusF10
            // 
            this.FocusF10.Location = new System.Drawing.Point(107, 14);
            this.FocusF10.Name = "FocusF10";
            this.FocusF10.Size = new System.Drawing.Size(101, 23);
            this.FocusF10.TabIndex = 6;
            this.FocusF10.Text = "Focus Far 10";
            this.FocusF10.UseVisualStyleBackColor = true;
            this.FocusF10.Click += new System.EventHandler(this.FocusF10_Click);
            // 
            // LiveViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 471);
            this.Controls.Add(this.FocusNear10);
            this.Controls.Add(this.FocusF10);
            this.Controls.Add(this.btn_stop);
            this.Controls.Add(this.btn_start);
            this.Controls.Add(this.pictureBox1);
            this.Name = "LiveViewForm";
            this.Text = "LiveViewForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LiveViewForm_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.Button btn_start;
    private System.Windows.Forms.Button btn_stop;
        private System.Windows.Forms.Button FocusNear10;
        private System.Windows.Forms.Button FocusF10;
    }
}