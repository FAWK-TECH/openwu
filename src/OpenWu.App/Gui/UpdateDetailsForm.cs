using System;
using System.Drawing;
using System.Windows.Forms;
using OpenWu.Core.Model;

namespace OpenWu.App.Gui;

public sealed class UpdateDetailsForm : Form
{
    private readonly UpdateRow _update;

    public UpdateDetailsForm(UpdateRow update)
    {
        _update = update;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = $"Update Details — {_update.Kb}";
        Size = new Size(650, 480);
        MinimumSize = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;

        var txt = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10F, FontStyle.Regular),
            Text = $@"KB Article:   {_update.Kb}
Title:        {_update.Title}
Size:         {_update.SizeMB:F1} MB
Category:     {_update.Categories}
Severity:     {_update.Severity}
Is Driver:    {_update.IsDriver}
Downloaded:   {_update.IsDownloaded}
Hidden:       {_update.IsHidden}
Reboot Req:   {_update.RebootRequired}
Identity:     {_update.Identity} (Rev {_update.Revision})
Support URL:  {_update.SupportUrl}

Description:
{_update.Description}"
        };

        var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 45 };
        var btnClose = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Size = new Size(90, 30),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom
        };
        btnClose.Location = new Point(pnlBottom.Width - 100, 8);
        pnlBottom.Controls.Add(btnClose);

        Controls.Add(txt);
        Controls.Add(pnlBottom);
        AcceptButton = btnClose;
    }
}
