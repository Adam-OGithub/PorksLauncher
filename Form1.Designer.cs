namespace PorksLauncher
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.btn_add = new System.Windows.Forms.Button();
            this.btn_rm = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.rename_btn = new System.Windows.Forms.Button();
            this.settings_btn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_add
            // 
            this.btn_add.BackColor = System.Drawing.Color.White;
            this.btn_add.Font = new System.Drawing.Font("Comic Sans MS", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_add.ForeColor = System.Drawing.Color.ForestGreen;
            this.btn_add.Location = new System.Drawing.Point(18, 18);
            this.btn_add.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_add.Name = "btn_add";
            this.btn_add.Size = new System.Drawing.Size(157, 35);
            this.btn_add.TabIndex = 0;
            this.btn_add.Text = "Add Exe";
            this.btn_add.UseVisualStyleBackColor = false;
            this.btn_add.Click += new System.EventHandler(this.btn_add_Click);
            // 
            // btn_rm
            // 
            this.btn_rm.BackColor = System.Drawing.Color.White;
            this.btn_rm.Font = new System.Drawing.Font("Comic Sans MS", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_rm.ForeColor = System.Drawing.Color.Firebrick;
            this.btn_rm.Location = new System.Drawing.Point(208, 18);
            this.btn_rm.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btn_rm.Name = "btn_rm";
            this.btn_rm.Size = new System.Drawing.Size(172, 35);
            this.btn_rm.TabIndex = 1;
            this.btn_rm.Text = "Remove Exe";
            this.btn_rm.UseVisualStyleBackColor = false;
            this.btn_rm.Click += new System.EventHandler(this.btn_rm_Click);
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.Color.Cyan;
            this.listBox1.Font = new System.Drawing.Font("Comic Sans MS", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 33;
            this.listBox1.Location = new System.Drawing.Point(18, 75);
            this.listBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(667, 565);
            this.listBox1.TabIndex = 2;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // rename_btn
            // 
            this.rename_btn.BackColor = System.Drawing.Color.White;
            this.rename_btn.Font = new System.Drawing.Font("Comic Sans MS", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rename_btn.ForeColor = System.Drawing.Color.Blue;
            this.rename_btn.Location = new System.Drawing.Point(400, 18);
            this.rename_btn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rename_btn.Name = "rename_btn";
            this.rename_btn.Size = new System.Drawing.Size(172, 35);
            this.rename_btn.TabIndex = 3;
            this.rename_btn.Text = "Rename Exe";
            this.rename_btn.UseVisualStyleBackColor = false;
            this.rename_btn.Click += new System.EventHandler(this.rename_btn_Click);
            // 
            // settings_btn
            // 
            this.settings_btn.BackColor = System.Drawing.Color.White;
            this.settings_btn.Font = new System.Drawing.Font("Comic Sans MS", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.settings_btn.Location = new System.Drawing.Point(594, 18);
            this.settings_btn.Name = "settings_btn";
            this.settings_btn.Size = new System.Drawing.Size(91, 35);
            this.settings_btn.TabIndex = 4;
            this.settings_btn.Text = "Settings";
            this.settings_btn.UseVisualStyleBackColor = false;
            this.settings_btn.Click += new System.EventHandler(this.settings_btn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(698, 624);
            this.Controls.Add(this.settings_btn);
            this.Controls.Add(this.rename_btn);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.btn_rm);
            this.Controls.Add(this.btn_add);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximumSize = new System.Drawing.Size(720, 680);
            this.MinimumSize = new System.Drawing.Size(720, 680);
            this.Name = "Form1";
            this.Text = "PorksLauncher";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_add;
        private System.Windows.Forms.Button btn_rm;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button rename_btn;
        private System.Windows.Forms.Button settings_btn;
    }
}

