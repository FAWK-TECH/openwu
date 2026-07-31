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
        Padding = new Padding(15);

        var lblBlurb = new Label
        {
            Text = "Policy Settings — Saved to %ProgramData%\\OpenWU\\policy.json. Changes apply to both GUI and CLI.",
            Font = new Font(Font, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30
        };

        var pnlGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        pnlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        pnlGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // 1. Service
        pnlGrid.Controls.Add(new Label { Text = "Update Service:", Anchor = AnchorStyles.Left }, 0, 0);
        _cmbService = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cmbService.Items.AddRange(new object[] { "MicrosoftUpdate", "WindowsUpdate" });
        pnlGrid.Controls.Add(_cmbService, 1, 0);

        // 2. Reboot Behavior
        pnlGrid.Controls.Add(new Label { Text = "Default Reboot:", Anchor = AnchorStyles.Left }, 0, 1);
        _cmbReboot = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        _cmbReboot.Items.AddRange(new object[] { "Never", "IfRequired", "Always" });
        pnlGrid.Controls.Add(_cmbReboot, 1, 1);

        // 3. Flags
        _chkIncludeDrivers = new CheckBox { Text = "Include Driver Updates by Default", AutoSize = true };
        _chkIncludeOptional = new CheckBox { Text = "Include Optional Updates by Default", AutoSize = true };
        _chkAllowDc = new CheckBox { Text = "Allow Installation on Domain Controller", AutoSize = true, ForeColor = Color.DarkRed };

        var flowFlags = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
        flowFlags.Controls.Add(_chkIncludeDrivers);
        flowFlags.Controls.Add(_chkIncludeOptional);
        flowFlags.Controls.Add(_chkAllowDc);

        pnlGrid.Controls.Add(new Label { Text = "Defaults & Security:", Anchor = AnchorStyles.Left }, 0, 2);
        pnlGrid.Controls.Add(flowFlags, 1, 2);

        // 4. Deny Titles
        pnlGrid.Controls.Add(new Label { Text = "Deny Titles Contains:\n(One per line)", Anchor = AnchorStyles.Top | AnchorStyles.Left }, 0, 3);
        _txtDenyTitles = new TextBox { Multiline = true, Height = 70, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
        pnlGrid.Controls.Add(_txtDenyTitles, 1, 3);

        // 5. Hidden KBs
        pnlGrid.Controls.Add(new Label { Text = "Persisted Hidden KBs:\n(One per line)", Anchor = AnchorStyles.Top | AnchorStyles.Left }, 0, 4);
        _txtHiddenKbs = new TextBox { Multiline = true, Height = 70, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
        pnlGrid.Controls.Add(_txtHiddenKbs, 1, 4);

        // Buttons
        var pnlButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft };
        _btnReset = new Button { Text = "Reset to Defaults", Size = new Size(130, 32), Margin = new Padding(5) };
        _btnSave = new Button { Text = "Save Policy", Size = new Size(110, 32), Margin = new Padding(5) };

        _btnSave.Click += (s, e) => SavePolicy();
        _btnReset.Click += (s, e) => ResetPolicy();

        pnlButtons.Controls.Add(_btnSave);
        pnlButtons.Controls.Add(_btnReset);

        Controls.Add(pnlGrid);
        Controls.Add(lblBlurb);
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
            MessageBox.Show("Policy successfully saved.", "Policy Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
