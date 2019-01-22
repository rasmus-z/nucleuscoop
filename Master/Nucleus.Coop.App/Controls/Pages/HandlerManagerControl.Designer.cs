﻿namespace Nucleus.Coop.App.Controls
{
    partial class HandlerManagerControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.list_left = new Nucleus.Gaming.Platform.Windows.Controls.ControlListBox();
            this.label_developer = new System.Windows.Forms.Label();
            this.label_version = new System.Windows.Forms.Label();
            this.panel_titleSizeReference = new System.Windows.Forms.Panel();
            this.label_nukeVer = new System.Windows.Forms.Label();
            this.btn_uninstall = new System.Windows.Forms.Button();
            this.panel_gameData = new System.Windows.Forms.Panel();
            this.panel_installedGames = new System.Windows.Forms.Panel();
            this.list_installedGames = new Nucleus.Gaming.Platform.Windows.Controls.ControlListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.panel_gameData.SuspendLayout();
            this.panel_installedGames.SuspendLayout();
            this.SuspendLayout();
            // 
            // list_left
            // 
            this.list_left.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.list_left.AutoScroll = true;
            this.list_left.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(47)))), ((int)(((byte)(49)))), ((int)(((byte)(54)))));
            this.list_left.Border = 2;
            this.list_left.Location = new System.Drawing.Point(1, 0);
            this.list_left.Name = "list_left";
            this.list_left.Offset = new System.Drawing.Size(0, 0);
            this.list_left.Size = new System.Drawing.Size(272, 629);
            this.list_left.TabIndex = 4;
            // 
            // label_developer
            // 
            this.label_developer.AutoSize = true;
            this.label_developer.Location = new System.Drawing.Point(0, 0);
            this.label_developer.Name = "label_developer";
            this.label_developer.Size = new System.Drawing.Size(127, 21);
            this.label_developer.TabIndex = 5;
            this.label_developer.Text = "Developer Name";
            // 
            // label_version
            // 
            this.label_version.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_version.AutoSize = true;
            this.label_version.Location = new System.Drawing.Point(469, 0);
            this.label_version.Name = "label_version";
            this.label_version.Size = new System.Drawing.Size(31, 21);
            this.label_version.TabIndex = 6;
            this.label_version.Text = "0.0";
            // 
            // panel_titleSizeReference
            // 
            this.panel_titleSizeReference.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panel_titleSizeReference.Location = new System.Drawing.Point(279, 0);
            this.panel_titleSizeReference.Name = "panel_titleSizeReference";
            this.panel_titleSizeReference.Size = new System.Drawing.Size(503, 52);
            this.panel_titleSizeReference.TabIndex = 7;
            // 
            // label_nukeVer
            // 
            this.label_nukeVer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label_nukeVer.AutoSize = true;
            this.label_nukeVer.Location = new System.Drawing.Point(420, 21);
            this.label_nukeVer.Name = "label_nukeVer";
            this.label_nukeVer.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.label_nukeVer.Size = new System.Drawing.Size(79, 21);
            this.label_nukeVer.TabIndex = 8;
            this.label_nukeVer.Text = "Nucleus 9";
            // 
            // btn_uninstall
            // 
            this.btn_uninstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_uninstall.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_uninstall.Location = new System.Drawing.Point(313, 517);
            this.btn_uninstall.Name = "btn_uninstall";
            this.btn_uninstall.Size = new System.Drawing.Size(184, 54);
            this.btn_uninstall.TabIndex = 9;
            this.btn_uninstall.Text = "Uninstall";
            this.btn_uninstall.UseVisualStyleBackColor = true;
            this.btn_uninstall.Click += new System.EventHandler(this.btn_uninstall_Click);
            // 
            // panel_gameData
            // 
            this.panel_gameData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_gameData.Controls.Add(this.label1);
            this.panel_gameData.Controls.Add(this.btn_uninstall);
            this.panel_gameData.Controls.Add(this.label_version);
            this.panel_gameData.Controls.Add(this.label_nukeVer);
            this.panel_gameData.Controls.Add(this.label_developer);
            this.panel_gameData.Location = new System.Drawing.Point(279, 55);
            this.panel_gameData.Name = "panel_gameData";
            this.panel_gameData.Size = new System.Drawing.Size(500, 571);
            this.panel_gameData.TabIndex = 10;
            this.panel_gameData.Visible = false;
            // 
            // panel_installedGames
            // 
            this.panel_installedGames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_installedGames.Controls.Add(this.list_installedGames);
            this.panel_installedGames.Location = new System.Drawing.Point(274, 55);
            this.panel_installedGames.Name = "panel_installedGames";
            this.panel_installedGames.Size = new System.Drawing.Size(507, 571);
            this.panel_installedGames.TabIndex = 10;
            // 
            // list_installedGames
            // 
            this.list_installedGames.Border = 1;
            this.list_installedGames.Dock = System.Windows.Forms.DockStyle.Fill;
            this.list_installedGames.Location = new System.Drawing.Point(0, 0);
            this.list_installedGames.Name = "list_installedGames";
            this.list_installedGames.Offset = new System.Drawing.Size(0, 0);
            this.list_installedGames.Size = new System.Drawing.Size(507, 571);
            this.list_installedGames.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 46);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(125, 21);
            this.label1.TabIndex = 10;
            this.label1.Text = "Handler Settings";
            // 
            // HandlerManagerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel_gameData);
            this.Controls.Add(this.panel_installedGames);
            this.Controls.Add(this.panel_titleSizeReference);
            this.Controls.Add(this.list_left);
            this.Name = "HandlerManagerControl";
            this.Size = new System.Drawing.Size(782, 629);
            this.panel_gameData.ResumeLayout(false);
            this.panel_gameData.PerformLayout();
            this.panel_installedGames.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private Gaming.Platform.Windows.Controls.ControlListBox list_left;
        private System.Windows.Forms.Label label_developer;
        private System.Windows.Forms.Label label_version;
        private System.Windows.Forms.Panel panel_titleSizeReference;
        private System.Windows.Forms.Label label_nukeVer;
        private System.Windows.Forms.Button btn_uninstall;
        private System.Windows.Forms.Panel panel_gameData;
        private System.Windows.Forms.Panel panel_installedGames;
        private Gaming.Platform.Windows.Controls.ControlListBox list_installedGames;
        private System.Windows.Forms.Label label1;
    }
}
