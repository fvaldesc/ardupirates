﻿namespace ArdupilotMega
{
    partial class temp
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
            this.button1 = new ArdupilotMega.MyButton();
            this.BUT_wipeeeprom = new ArdupilotMega.MyButton();
            this.BUT_flashdl = new ArdupilotMega.MyButton();
            this.BUT_flashup = new ArdupilotMega.MyButton();
            this.BUT_dleeprom = new ArdupilotMega.MyButton();
            this.BUT_copy1280 = new ArdupilotMega.MyButton();
            this.BUT_copy2560 = new ArdupilotMega.MyButton();
            this.BUT_copyto2560 = new ArdupilotMega.MyButton();
            this.BUT_copyto1280 = new ArdupilotMega.MyButton();
            this.button2 = new ArdupilotMega.MyButton();
            this.label1 = new System.Windows.Forms.Label();
            this.CHK_mavlinkdebug = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.BUT_geinjection = new ArdupilotMega.MyButton();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(534, 97);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(125, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "upload eeprom";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // BUT_wipeeeprom
            // 
            this.BUT_wipeeeprom.Location = new System.Drawing.Point(534, 126);
            this.BUT_wipeeeprom.Name = "BUT_wipeeeprom";
            this.BUT_wipeeeprom.Size = new System.Drawing.Size(125, 23);
            this.BUT_wipeeeprom.TabIndex = 1;
            this.BUT_wipeeeprom.Text = "WIPE eeprom";
            this.BUT_wipeeeprom.UseVisualStyleBackColor = true;
            this.BUT_wipeeeprom.Click += new System.EventHandler(this.BUT_wipeeeprom_Click);
            // 
            // BUT_flashdl
            // 
            this.BUT_flashdl.Location = new System.Drawing.Point(534, 156);
            this.BUT_flashdl.Name = "BUT_flashdl";
            this.BUT_flashdl.Size = new System.Drawing.Size(125, 23);
            this.BUT_flashdl.TabIndex = 2;
            this.BUT_flashdl.Text = "download flash";
            this.BUT_flashdl.UseVisualStyleBackColor = true;
            this.BUT_flashdl.Click += new System.EventHandler(this.BUT_flashdl_Click);
            // 
            // BUT_flashup
            // 
            this.BUT_flashup.Location = new System.Drawing.Point(534, 185);
            this.BUT_flashup.Name = "BUT_flashup";
            this.BUT_flashup.Size = new System.Drawing.Size(125, 23);
            this.BUT_flashup.TabIndex = 3;
            this.BUT_flashup.Text = "upload flash";
            this.BUT_flashup.UseVisualStyleBackColor = true;
            this.BUT_flashup.Click += new System.EventHandler(this.BUT_flashup_Click);
            // 
            // BUT_dleeprom
            // 
            this.BUT_dleeprom.Location = new System.Drawing.Point(403, 156);
            this.BUT_dleeprom.Name = "BUT_dleeprom";
            this.BUT_dleeprom.Size = new System.Drawing.Size(125, 23);
            this.BUT_dleeprom.TabIndex = 4;
            this.BUT_dleeprom.Text = "download eeprom";
            this.BUT_dleeprom.UseVisualStyleBackColor = true;
            this.BUT_dleeprom.Click += new System.EventHandler(this.BUT_dleeprom_Click);
            // 
            // BUT_copy1280
            // 
            this.BUT_copy1280.Location = new System.Drawing.Point(12, 12);
            this.BUT_copy1280.Name = "BUT_copy1280";
            this.BUT_copy1280.Size = new System.Drawing.Size(125, 23);
            this.BUT_copy1280.TabIndex = 5;
            this.BUT_copy1280.Text = "Copy APM 1280";
            this.BUT_copy1280.UseVisualStyleBackColor = true;
            this.BUT_copy1280.Click += new System.EventHandler(this.BUT_copy1280_Click);
            // 
            // BUT_copy2560
            // 
            this.BUT_copy2560.Location = new System.Drawing.Point(12, 41);
            this.BUT_copy2560.Name = "BUT_copy2560";
            this.BUT_copy2560.Size = new System.Drawing.Size(125, 23);
            this.BUT_copy2560.TabIndex = 6;
            this.BUT_copy2560.Text = "Copy APM 2560";
            this.BUT_copy2560.UseVisualStyleBackColor = true;
            this.BUT_copy2560.Click += new System.EventHandler(this.BUT_copy2560_Click);
            // 
            // BUT_copyto2560
            // 
            this.BUT_copyto2560.Location = new System.Drawing.Point(143, 41);
            this.BUT_copyto2560.Name = "BUT_copyto2560";
            this.BUT_copyto2560.Size = new System.Drawing.Size(125, 23);
            this.BUT_copyto2560.TabIndex = 8;
            this.BUT_copyto2560.Text = "Copy to APM 2560";
            this.BUT_copyto2560.UseVisualStyleBackColor = true;
            // 
            // BUT_copyto1280
            // 
            this.BUT_copyto1280.Location = new System.Drawing.Point(143, 12);
            this.BUT_copyto1280.Name = "BUT_copyto1280";
            this.BUT_copyto1280.Size = new System.Drawing.Size(125, 23);
            this.BUT_copyto1280.TabIndex = 7;
            this.BUT_copyto1280.Text = "Copy to APM 1280";
            this.BUT_copyto1280.UseVisualStyleBackColor = true;
            this.BUT_copyto1280.Click += new System.EventHandler(this.BUT_copyto1280_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(41, 156);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(159, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "firmware.hex 2 firmware.bin";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(274, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Includes eeprom";
            // 
            // CHK_mavlinkdebug
            // 
            this.CHK_mavlinkdebug.AutoSize = true;
            this.CHK_mavlinkdebug.Location = new System.Drawing.Point(308, 240);
            this.CHK_mavlinkdebug.Name = "CHK_mavlinkdebug";
            this.CHK_mavlinkdebug.Size = new System.Drawing.Size(104, 17);
            this.CHK_mavlinkdebug.TabIndex = 11;
            this.CHK_mavlinkdebug.Text = "MAVLink Debug";
            this.CHK_mavlinkdebug.UseVisualStyleBackColor = true;
            this.CHK_mavlinkdebug.CheckedChanged += new System.EventHandler(this.CHK_mavlinkdebug_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(274, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Does not include eeprom";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(56, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(273, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "1280 - can do full copy\'s   2560- full read, write flash only";
            // 
            // BUT_geinjection
            // 
            this.BUT_geinjection.Location = new System.Drawing.Point(59, 240);
            this.BUT_geinjection.Name = "BUT_geinjection";
            this.BUT_geinjection.Size = new System.Drawing.Size(159, 23);
            this.BUT_geinjection.TabIndex = 14;
            this.BUT_geinjection.Text = "Inject GE into database";
            this.BUT_geinjection.UseVisualStyleBackColor = true;
            this.BUT_geinjection.Click += new System.EventHandler(this.BUT_geinjection_Click);
            // 
            // temp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(731, 281);
            this.Controls.Add(this.BUT_geinjection);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CHK_mavlinkdebug);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.BUT_copyto2560);
            this.Controls.Add(this.BUT_copyto1280);
            this.Controls.Add(this.BUT_copy2560);
            this.Controls.Add(this.BUT_copy1280);
            this.Controls.Add(this.BUT_dleeprom);
            this.Controls.Add(this.BUT_flashup);
            this.Controls.Add(this.BUT_flashdl);
            this.Controls.Add(this.BUT_wipeeeprom);
            this.Controls.Add(this.button1);
            this.Name = "temp";
            this.Text = "temp";
            this.Load += new System.EventHandler(this.temp_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MyButton button1;
        private MyButton BUT_wipeeeprom;
        private MyButton BUT_flashdl;
        private MyButton BUT_flashup;
        private MyButton BUT_dleeprom;
        private MyButton BUT_copy1280;
        private MyButton BUT_copy2560;
        private MyButton BUT_copyto2560;
        private MyButton BUT_copyto1280;
        private MyButton button2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox CHK_mavlinkdebug;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private MyButton BUT_geinjection;
        //private SharpVectors.Renderers.Forms.SvgPictureBox svgPictureBox1;

    }
}