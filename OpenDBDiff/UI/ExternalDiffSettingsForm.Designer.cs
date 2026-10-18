using System.ComponentModel;
using System.Windows.Forms;

namespace OpenDBDiff.UI
{
    partial class ExternalDiffSettingsForm
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblExecutable = new System.Windows.Forms.Label();
            this.txtExecutable = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblFileArguments = new System.Windows.Forms.Label();
            this.txtFileArguments = new System.Windows.Forms.TextBox();
            this.lblFolderArguments = new System.Windows.Forms.Label();
            this.txtFolderArguments = new System.Windows.Forms.TextBox();
            this.lblPlaceholders = new System.Windows.Forms.Label();
            this.btnWinMerge = new System.Windows.Forms.Button();
            this.btnBeyondCompare = new System.Windows.Forms.Button();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblExecutable
            //
            this.lblExecutable.AutoSize = true;
            this.lblExecutable.Location = new System.Drawing.Point(12, 15);
            this.lblExecutable.Name = "lblExecutable";
            this.lblExecutable.Size = new System.Drawing.Size(63, 13);
            this.lblExecutable.TabIndex = 0;
            this.lblExecutable.Text = "Executable:";
            //
            // txtExecutable
            //
            this.txtExecutable.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtExecutable.Location = new System.Drawing.Point(12, 31);
            this.txtExecutable.Name = "txtExecutable";
            this.txtExecutable.Size = new System.Drawing.Size(470, 20);
            this.txtExecutable.TabIndex = 1;
            //
            // btnBrowse
            //
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(488, 29);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            //
            // lblFileArguments
            //
            this.lblFileArguments.AutoSize = true;
            this.lblFileArguments.Location = new System.Drawing.Point(12, 64);
            this.lblFileArguments.Name = "lblFileArguments";
            this.lblFileArguments.Size = new System.Drawing.Size(149, 13);
            this.lblFileArguments.TabIndex = 3;
            this.lblFileArguments.Text = "Arguments (selected object):";
            //
            // txtFileArguments
            //
            this.txtFileArguments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileArguments.Location = new System.Drawing.Point(12, 80);
            this.txtFileArguments.Name = "txtFileArguments";
            this.txtFileArguments.Size = new System.Drawing.Size(551, 20);
            this.txtFileArguments.TabIndex = 4;
            //
            // lblFolderArguments
            //
            this.lblFolderArguments.AutoSize = true;
            this.lblFolderArguments.Location = new System.Drawing.Point(12, 113);
            this.lblFolderArguments.Name = "lblFolderArguments";
            this.lblFolderArguments.Size = new System.Drawing.Size(155, 13);
            this.lblFolderArguments.TabIndex = 5;
            this.lblFolderArguments.Text = "Arguments (all differences):";
            //
            // txtFolderArguments
            //
            this.txtFolderArguments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFolderArguments.Location = new System.Drawing.Point(12, 129);
            this.txtFolderArguments.Name = "txtFolderArguments";
            this.txtFolderArguments.Size = new System.Drawing.Size(551, 20);
            this.txtFolderArguments.TabIndex = 6;
            //
            // lblPlaceholders
            //
            this.lblPlaceholders.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPlaceholders.Location = new System.Drawing.Point(12, 160);
            this.lblPlaceholders.Name = "lblPlaceholders";
            this.lblPlaceholders.Size = new System.Drawing.Size(551, 40);
            this.lblPlaceholders.TabIndex = 7;
            this.lblPlaceholders.Text = "Placeholders: {left} and {right} are the old/new SQL files or folders. {lefttitle} and {righttitle} are pane titles. Left is the current schema; right is the desired schema.";
            //
            // btnWinMerge
            //
            this.btnWinMerge.Location = new System.Drawing.Point(12, 211);
            this.btnWinMerge.Name = "btnWinMerge";
            this.btnWinMerge.Size = new System.Drawing.Size(110, 23);
            this.btnWinMerge.TabIndex = 8;
            this.btnWinMerge.Text = "WinMerge";
            this.btnWinMerge.UseVisualStyleBackColor = true;
            this.btnWinMerge.Click += new System.EventHandler(this.btnWinMerge_Click);
            //
            // btnBeyondCompare
            //
            this.btnBeyondCompare.Location = new System.Drawing.Point(128, 211);
            this.btnBeyondCompare.Name = "btnBeyondCompare";
            this.btnBeyondCompare.Size = new System.Drawing.Size(110, 23);
            this.btnBeyondCompare.TabIndex = 9;
            this.btnBeyondCompare.Text = "Beyond Compare";
            this.btnBeyondCompare.UseVisualStyleBackColor = true;
            this.btnBeyondCompare.Click += new System.EventHandler(this.btnBeyondCompare_Click);
            //
            // btnTest
            //
            this.btnTest.Location = new System.Drawing.Point(244, 211);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 10;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(407, 256);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 11;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(488, 256);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // ExternalDiffSettingsForm
            //
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(575, 291);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnBeyondCompare);
            this.Controls.Add(this.btnWinMerge);
            this.Controls.Add(this.lblPlaceholders);
            this.Controls.Add(this.txtFolderArguments);
            this.Controls.Add(this.lblFolderArguments);
            this.Controls.Add(this.txtFileArguments);
            this.Controls.Add(this.lblFileArguments);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtExecutable);
            this.Controls.Add(this.lblExecutable);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExternalDiffSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "External comparison tool";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Label lblExecutable;
        private TextBox txtExecutable;
        private Button btnBrowse;
        private Label lblFileArguments;
        private TextBox txtFileArguments;
        private Label lblFolderArguments;
        private TextBox txtFolderArguments;
        private Label lblPlaceholders;
        private Button btnWinMerge;
        private Button btnBeyondCompare;
        private Button btnTest;
        private Button btnOK;
        private Button btnCancel;
    }
}
