
namespace Detect_xk
{
    partial class Operator
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
            this.btn_Operator = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Operator
            // 
            this.btn_Operator.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_Operator.Location = new System.Drawing.Point(302, 133);
            this.btn_Operator.Name = "btn_Operator";
            this.btn_Operator.Size = new System.Drawing.Size(135, 33);
            this.btn_Operator.TabIndex = 1;
            this.btn_Operator.Text = "开始检测";
            this.btn_Operator.UseVisualStyleBackColor = true;
            // 
            // Operator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(779, 318);
            this.Controls.Add(this.btn_Operator);
            this.Name = "Operator";
            this.Text = "用户操作";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Operator;
    }
}