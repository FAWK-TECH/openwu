using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace OpenWu.App.Gui;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.2.0";
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var logDir = Path.Combine(programData, "OpenWU", "logs");

        Text = "About OpenWU";
        Size = new Size(540, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 250, 252);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(20, 15, 20, 15)
        };

        var lblTitle = new Label
        {
            Text = $"OpenWU v{version}",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 14)
        };

        var lblSubtitle = new Label
        {
            Text = "Open, auditable Windows Update control",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Location = new Point(22, 46)
        };

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);

        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var txtInfo = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Text = $@"OpenWU is an open-source Windows Update manager built on .NET 8 and native Windows Update Agent (WUA) COM APIs.

Repository: https://github.com/FAWK-TECH/openwu
License: MIT License — Copyright (c) 2026 OpenWU Contributors

DISCLAIMER:
OpenWU is an administrator utility that interacts directly with Windows Update Agent APIs on the local system. It is not a substitute for enterprise patch policy platforms (e.g. Microsoft Intune, WSUS, MECM). Hiding updates can increase security risks — use update hiding as an exception, not a default.

Action Log Path:
{logDir}"
        };

        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            Padding = new Padding(15)
        };

        var btnOpenLogs = new Button
        {
            Text = "Open Log Folder",
            Size = new Size(130, 30),
            Location = new Point(20, 10)
        };
        btnOpenLogs.Click += (s, e) =>
        {
            try
            {
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                Process.Start("explorer.exe", logDir);
            }
            catch { }
        };

        var btnClose = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Size = new Size(90, 30),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Location = new Point(pnlBottom.Width - 110, 10)
        };

        pnlBottom.Controls.Add(btnOpenLogs);
        pnlBottom.Controls.Add(btnClose);

        pnlContent.Controls.Add(txtInfo);

        Controls.Add(pnlContent);
        Controls.Add(pnlHeader);
        Controls.Add(pnlBottom);

        AcceptButton = btnClose;
    }
}
