namespace KControl
{
    partial class ConfigIO
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.TextView = new System.Windows.Forms.TreeView();
            this.OK = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(201, 29);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(337, 339);
            this.panel1.TabIndex = 0;
            // 
            // TextView
            // 
            this.TextView.Font = new System.Drawing.Font("宋体", 12F);
            this.TextView.Location = new System.Drawing.Point(3, 29);
            this.TextView.Name = "TextView";
            this.TextView.ShowLines = false;
            this.TextView.ShowNodeToolTips = true;
            this.TextView.ShowPlusMinus = false;
            this.TextView.ShowRootLines = false;
            this.TextView.Size = new System.Drawing.Size(192, 381);
            this.TextView.TabIndex = 1;
            this.TextView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TextView_AfterSelect);
            // 
            // OK
            // 
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.OK.Location = new System.Drawing.Point(201, 374);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 38);
            this.OK.TabIndex = 2;
            this.OK.Text = "确定";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Location = new System.Drawing.Point(282, 374);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 38);
            this.button1.TabIndex = 2;
            this.button1.Text = "取消";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.label1.Font = new System.Drawing.Font("宋体", 15F);
            this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label1.Location = new System.Drawing.Point(-13, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(569, 20);
            this.label1.TabIndex = 3;
            this.label1.Text = "                        端口配置                        ";
            // 
            // ConfigIO
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Info;
            this.ClientSize = new System.Drawing.Size(545, 422);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.TextView);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ConfigIO";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "配置IO端口";
            this.Load += new System.EventHandler(this.ConfigIO_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TreeView TextView;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
    }
}