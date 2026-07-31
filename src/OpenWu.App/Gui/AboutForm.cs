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
        Size = new Size(560, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(248, 250, 252);
        try
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath);
        }
        catch { /* optional */ }

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = Color.FromArgb(28, 28, 30),
            Padding = new Padding(16, 14, 16, 14)
        };

        var pic = new PictureBox
        {
            Size = new Size(64, 64),
            Location = new Point(16, 14),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        TryLoadMark(pic);

        var lblTitle = new Label
        {
            Text = $"OpenWU v{version}",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(94, 18)
        };

        var lblSubtitle = new Label
        {
            Text = "Open, auditable Windows Update control",
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(180, 180, 185),
            AutoSize = true,
            Location = new Point(96, 52)
        };

        pnlHeader.Controls.Add(pic);
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);

        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 8)
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
            Text =
                "OpenWU is an open-source Windows Update manager built on .NET 8 " +
                "and native Windows Update Agent (WUA) COM APIs." + Environment.NewLine + Environment.NewLine +
                "Repository: https://github.com/FAWK-TECH/openwu" + Environment.NewLine +
                "License: MIT — Copyright (c) 2026 OpenWU Contributors" + Environment.NewLine + Environment.NewLine +
                "DISCLAIMER" + Environment.NewLine +
                "OpenWU is an administrator utility for the local machine. " +
                "It is not a substitute for enterprise patch platforms (Intune, WSUS, MECM). " +
                "Hiding updates can increase risk — use hide as an exception, not a default." + Environment.NewLine + Environment.NewLine +
                "Action log folder:" + Environment.NewLine +
                logDir
        };

        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(16, 10, 16, 10)
        };

        var btnOpenLogs = new Button
        {
            Text = "Open Log Folder",
            Size = new Size(130, 30),
            Location = new Point(16, 10)
        };
        btnOpenLogs.Click += (_, _) =>
        {
            try
            {
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                Process.Start("explorer.exe", logDir);
            }
            catch { /* ignore */ }
        };

        var btnClose = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Size = new Size(90, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        pnlBottom.Resize += (_, _) =>
        {
            btnClose.Location = new Point(pnlBottom.ClientSize.Width - btnClose.Width - 16, 10);
        };
        btnClose.Location = new Point(pnlBottom.ClientSize.Width - btnClose.Width - 16, 10);

        pnlBottom.Controls.Add(btnOpenLogs);
        pnlBottom.Controls.Add(btnClose);
        pnlContent.Controls.Add(txtInfo);

        Controls.Add(pnlContent);
        Controls.Add(pnlHeader);
        Controls.Add(pnlBottom);

        AcceptButton = btnClose;
    }

    private static void TryLoadMark(PictureBox pic)
    {
        try
        {
            var mark = Path.Combine(AppContext.BaseDirectory, "Assets", "openwu-mark.png");
            if (!File.Exists(mark))
                mark = Path.Combine(AppContext.BaseDirectory, "openwu-mark.png");
            if (File.Exists(mark))
            {
                // Clone so file is not locked
                using var fs = File.OpenRead(mark);
                pic.Image = Image.FromStream(fs);
                return;
            }

            var ico = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Application.ExecutablePath);
            if (ico != null)
                pic.Image = ico.ToBitmap();
        }
        catch { /* decorative */ }
    }
}
