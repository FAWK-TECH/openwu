using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using OpenWu.Core;
using OpenWu.Core.Policy;

namespace OpenWu.App.Gui;

public sealed class SettingsControl : UserControl
{
    private readonly UpdateService _service;
    private CheckBox _chkIncludeDrivers = null!;
    private CheckBox _chkIncludeOptional = null!;
    private CheckBox _chkAllowDc = null!;
    private ComboBox _cmbService = null!;
    private ComboBox _cmbReboot = null!;
    private TextBox _txtDenyTitles = null!;
    private TextBox _txtHiddenKbs = null!;
    private Button _btnSave = null!;
    private Button _btnReset = null!;

    public SettingsControl(UpdateService service)
    {
        _service = service;
        InitializeComponent();
        LoadPolicy();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.PageBack;
        Padding = new Padding(16);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(0, 0, 0, 8)
        };

        var lblBlurb = new Label
        {
            Text = "Policy Configuration — Saved to %ProgramData%\\OpenWU\\policy.json. Appiles to both GUI and CLI.",
            Font = UiTheme.FontBold,
            ForeColor = UiTheme.TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlHeader.Controls.Add(lblBlurb);

        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnSave = new Button
        {
            Text = "Save Policy",
            Size = new Size(120, 32),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Location = new Point(pnlButtons.Width - 120, 8),
            Font = UiTheme.FontBold,
            FlatStyle = FlatStyle.System
        };

        _btnReset = new Button
        {
            Text = "Reset to Defaults",
            Size = new Size(130, 32),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Location = new Point(pnlButtons.Width - 260, 8),
            Font = UiTheme.FontBody,
            FlatStyle = FlatStyle.System
        };

        _btnSave.Click += (s, e) => SavePolicy();
        _btnReset.Click += (s, e) => ResetPolicy();

        pnlButtons.Controls.Add(_btnSave);
        pnlButtons.Controls.Add(_btnReset);

        var pnlMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(0, 4, 0, 4)
        };
        pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        pnlMain.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));

        // Group 1: Source & Defaults
        var grpDefaults = new GroupBox
        {
            Text = "Update Source & Install Defaults",
            Font = UiTheme.FontBold,
            ForeColor = UiTheme.TextPrimary,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 8, 8),
            Padding = new Padding(12)
        };

        var flowDefaults = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        flowDefaults.Controls.Add(new Label { Text = "Catalog Service:", Font = UiTheme.FontBody, AutoSize = true, Margin = new Padding(0, 4, 0, 2) });
        _cmbService = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = UiTheme.FontBody };
        _cmbService.Items.AddRange(new object[] { "MicrosoftUpdate", "WindowsUpdate" });
        flowDefaults.Controls.Add(_cmbService);

        flowDefaults.Controls.Add(new Label { Text = "Default Reboot Action:", Font = UiTheme.FontBody, AutoSize = true, Margin = new Padding(0, 10, 0, 2) });
        _cmbReboot = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Font = UiTheme.FontBody };
        _cmbReboot.Items.AddRange(new object[] { "Never", "IfRequired", "Always" });
        flowDefaults.Controls.Add(_cmbReboot);

        _chkIncludeDrivers = new CheckBox { Text = "Include Driver Updates by Default", Font = UiTheme.FontBody, AutoSize = true, Margin = new Padding(0, 12, 0, 2) };
        _chkIncludeOptional = new CheckBox { Text = "Include Optional Updates by Default", Font = UiTheme.FontBody, AutoSize = true, Margin = new Padding(0, 6, 0, 2) };

        flowDefaults.Controls.Add(_chkIncludeDrivers);
        flowDefaults.Controls.Add(_chkIncludeOptional);

        grpDefaults.Controls.Add(flowDefaults);

        // Group 2: Domain Controller & Guards
        var grpSafety = new GroupBox
        {
            Text = "Domain Controller & Safety Guards",
            Font = UiTheme.FontBold,
            ForeColor = UiTheme.TextPrimary,
            Dock = DockStyle.Fill,
            Margin = new Padding(8, 0, 0, 8),
            Padding = new Padding(12)
        };

        var flowSafety = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        _chkAllowDc = new CheckBox
        {
            Text = "Allow installation on Domain Controller",
            Font = UiTheme.FontBold,
            ForeColor = Color.FromArgb(185, 28, 28),
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 8)
        };
        flowSafety.Controls.Add(_chkAllowDc);

        flowSafety.Controls.Add(new Label { Text = "Deny Titles Containing (one per line):", Font = UiTheme.FontBody, AutoSize = true, Margin = new Padding(0, 4, 0, 2) });
        _txtDenyTitles = new TextBox
        {
            Multiline = true,
            Height = 110,
            Width = 240,
            Font = UiTheme.FontBody,
            ScrollBars = ScrollBars.Vertical
        };
        flowSafety.Controls.Add(_txtDenyTitles);

        grpSafety.Controls.Add(flowSafety);

        // Group 3: Hidden KBs
        var grpHidden = new GroupBox
        {
            Text = "Persisted Hidden KBs",
            Font = UiTheme.FontBold,
            ForeColor = UiTheme.TextPrimary,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 8, 0),
            Padding = new Padding(12)
        };

        var pnlHidden = new Panel { Dock = DockStyle.Fill };
        pnlHidden.Controls.Add(new Label { Text = "KBs hidden across search results (one per line, e.g. KB5031234):", Font = UiTheme.FontBody, Dock = DockStyle.Top, Height = 22 });
        _txtHiddenKbs = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            Font = UiTheme.FontBody,
            ScrollBars = ScrollBars.Vertical
        };
        pnlHidden.Controls.Add(_txtHiddenKbs);
        grpHidden.Controls.Add(pnlHidden);

        pnlMain.Controls.Add(grpDefaults, 0, 0);
        pnlMain.Controls.Add(grpSafety, 1, 0);
        pnlMain.Controls.Add(grpHidden, 0, 1);
        pnlMain.SetColumnSpan(grpHidden, 2);

        Controls.Add(pnlMain);
        Controls.Add(pnlHeader);
        Controls.Add(pnlButtons);
    }

    public void LoadPolicy()
    {
        var p = _service.PolicyStore.Load();
        _cmbService.SelectedItem = p.Service;
        _cmbReboot.SelectedItem = p.Reboot;
        _chkIncludeDrivers.Checked = p.IncludeDrivers;
        _chkIncludeOptional.Checked = p.IncludeOptional;
        _chkAllowDc.Checked = p.AllowOnDomainController;
        _txtDenyTitles.Text = string.Join(Environment.NewLine, p.DenyTitlesContains);
        _txtHiddenKbs.Text = string.Join(Environment.NewLine, p.HiddenKBs);
    }

    private void SavePolicy()
    {
        try
        {
            var p = new PolicyModel
            {
                Service = _cmbService.SelectedItem?.ToString() ?? "MicrosoftUpdate",
                Reboot = _cmbReboot.SelectedItem?.ToString() ?? "Never",
                IncludeDrivers = _chkIncludeDrivers.Checked,
                IncludeOptional = _chkIncludeOptional.Checked,
                AllowOnDomainController = _chkAllowDc.Checked,
                DenyTitlesContains = _txtDenyTitles.Lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
                HiddenKBs = _txtHiddenKbs.Lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList()
            };

            _service.PolicyStore.Save(p);
            MessageBox.Show("Policy successfully saved to %ProgramData%\\OpenWU\\policy.json", "Policy Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save policy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetPolicy()
    {
        if (MessageBox.Show("Reset policy to default values?", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _service.PolicyStore.Reset();
            LoadPolicy();
        }
    }
}
