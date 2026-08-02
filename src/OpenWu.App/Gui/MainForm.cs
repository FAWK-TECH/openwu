using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenWu.Core;
using OpenWu.Core.Guard;
using OpenWu.Core.Model;

namespace OpenWu.App.Gui;

public partial class MainForm : Form
{
    private readonly UpdateService _service;
    private readonly List<UpdateRow> _currentRows = new();

    private ToolStrip _toolStrip = null!;
    private ToolStripButton _btnRefresh = null!;
    private ToolStripButton _btnDownload = null!;
    private ToolStripButton _btnInstall = null!;
    private ToolStripButton _btnHide = null!;
    private ToolStripButton _btnUnhide = null!;
    private ToolStripButton _btnSelectSecurity = null!;
    private ToolStripButton _btnSelectAll = null!;
    private ToolStripButton _btnSelectNone = null!;
    private ToolStripButton _btnHistoryTab = null!;
    private ToolStripButton _btnAbout = null!;

    private CheckBox _chkDrivers = null!;
    private CheckBox _chkOptional = null!;
    private CheckBox _chkMsUpdate = null!;
    private CheckBox _chkPersistHide = null!;
    private CheckBox _chkAllowDc = null!;
    private CheckBox _chkReboot = null!;

    private TabControl _tabControl = null!;
    private TabPage _tabUpdates = null!;
    private TabPage _tabHistory = null!;
    private TabPage _tabSettings = null!;

    private DataGridView _grid = null!;
    private SplitContainer _updatesSplit = null!;

    private Panel _pnlEmptyState = null!;
    private Label _lblEmptyTitle = null!;
    private Label _lblEmptyBody = null!;
    private Label _lblEmptyLastChecked = null!;
    private Button _btnEmptyRefresh = null!;
    private DateTime? _lastCheckedTime;

    private Panel _detailPanel = null!;
    private Label _detailTitle = null!;
    private Label _detailMeta = null!;
    private TextBox _detailDescription = null!;
    private LinkLabel _detailSupportLink = null!;
    private ContextMenuStrip _gridContextMenu = null!;

    private HistoryControl _historyControl = null!;
    private SettingsControl _settingsControl = null!;

    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private ToolStripStatusLabel _lblCount = null!;
    private ToolStripStatusLabel _lblElevation = null!;
    private ToolStripStatusLabel _lblReboot = null!;
    private ToolStripProgressBar _progressBar = null!;

    private CancellationTokenSource? _cts;

    public MainForm()
    {
        InitializeComponent();
        _service = new UpdateService();
        BuildUi();
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;
        Shown += async (s, e) => await RefreshPendingUpdatesAsync();
    }

    private void BuildUi()
    {
        Font = UiTheme.FontBody;
        Text = "OpenWU — Windows Update Manager";
        MinimumSize = new Size(960, 600);
        Size = new Size(1040, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.PageBack;

        try
        {
            var path = Environment.ProcessPath ?? Application.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                Icon = Icon.ExtractAssociatedIcon(path);
        }
        catch { /* decorative */ }

        _toolStrip = new ToolStrip();
        UiTheme.ApplyToolStripStyle(_toolStrip);

        _btnRefresh = new ToolStripButton("Refresh (F5)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnDownload = new ToolStripButton("Download") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnInstall = new ToolStripButton("Install")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Font = UiTheme.FontBold,
            ForeColor = UiTheme.AccentHover
        };
        _btnHide = new ToolStripButton("Hide") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnUnhide = new ToolStripButton("Unhide") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnSelectSecurity = new ToolStripButton("Select Security") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnSelectAll = new ToolStripButton("Select All") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnSelectNone = new ToolStripButton("Select None") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnHistoryTab = new ToolStripButton("History") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnAbout = new ToolStripButton("About") { DisplayStyle = ToolStripItemDisplayStyle.Text };

        _btnRefresh.Click += async (s, e) => await RefreshPendingUpdatesAsync();
        _btnDownload.Click += async (s, e) => await DownloadSelectedAsync();
        _btnInstall.Click += async (s, e) => await InstallSelectedAsync();
        _btnHide.Click += async (s, e) => await HideSelectedAsync();
        _btnUnhide.Click += async (s, e) => await UnhideSelectedAsync();
        _btnSelectSecurity.Click += (s, e) => SelectSecurityRows();
        _btnSelectAll.Click += (s, e) => SelectAllRows();
        _btnSelectNone.Click += (s, e) => SelectNoneRows();
        _btnHistoryTab.Click += (s, e) => _tabControl.SelectedTab = _tabHistory;
        _btnAbout.Click += (s, e) => ShowAboutForm();

        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _btnRefresh,
            new ToolStripSeparator(),
            _btnDownload,
            _btnInstall,
            new ToolStripSeparator(),
            _btnHide,
            _btnUnhide,
            new ToolStripSeparator(),
            _btnSelectSecurity,
            _btnSelectAll,
            _btnSelectNone,
            new ToolStripSeparator(),
            _btnHistoryTab,
            new ToolStripSeparator(),
            _btnAbout
        });

        var pnlOptions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(8, 4, 8, 2),
            Margin = Padding.Empty,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.SurfaceBack
        };

        _chkDrivers = CompactCheck("Include drivers");
        _chkOptional = CompactCheck("Include optional");
        _chkMsUpdate = CompactCheck("Microsoft Update", checkedByDefault: true);
        _chkPersistHide = CompactCheck("Persist hides");
        _chkAllowDc = CompactCheck("Allow DC install");
        _chkAllowDc.ForeColor = Color.FromArgb(185, 28, 28);
        _chkReboot = CompactCheck("Reboot if required");

        pnlOptions.Controls.AddRange(new Control[]
        {
            _chkDrivers, _chkOptional, _chkMsUpdate, _chkPersistHide, _chkAllowDc, _chkReboot
        });

        BuildGrid();
        BuildEmptyStatePanel();
        BuildDetailPane();
        BuildContextMenu();

        _updatesSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            Panel1MinSize = 140,
            Panel2MinSize = 120,
            FixedPanel = FixedPanel.Panel2,
            BackColor = UiTheme.BorderColor
        };
        _updatesSplit.Panel1.BackColor = UiTheme.SurfaceBack;
        _updatesSplit.Panel2.BackColor = UiTheme.SurfaceBack;

        var pnlGridContainer = new Panel { Dock = DockStyle.Fill };
        pnlGridContainer.Controls.Add(_pnlEmptyState);
        pnlGridContainer.Controls.Add(_grid);

        _updatesSplit.Panel1.Controls.Add(pnlGridContainer);
        _updatesSplit.Panel2.Controls.Add(_detailPanel);

        HandleCreated += (_, _) =>
        {
            try
            {
                if (_updatesSplit.Height > 240)
                    _updatesSplit.SplitterDistance = Math.Max(180, _updatesSplit.Height - 180);
            }
            catch { /* layout safety */ }
        };

        _tabControl = new TabControl { Dock = DockStyle.Fill, Font = UiTheme.FontBody };
        _tabUpdates = new TabPage("Updates") { Padding = new Padding(0), BackColor = UiTheme.PageBack };
        _tabHistory = new TabPage("History") { BackColor = UiTheme.PageBack };
        _tabSettings = new TabPage("Settings / Policy") { BackColor = UiTheme.PageBack };

        _tabUpdates.Controls.Add(_updatesSplit);
        _tabUpdates.Controls.Add(pnlOptions);

        _historyControl = new HistoryControl(_service);
        _tabHistory.Controls.Add(_historyControl);

        _settingsControl = new SettingsControl(_service);
        _tabSettings.Controls.Add(_settingsControl);

        _tabControl.TabPages.Add(_tabUpdates);
        _tabControl.TabPages.Add(_tabHistory);
        _tabControl.TabPages.Add(_tabSettings);

        _tabControl.SelectedIndexChanged += async (s, e) =>
        {
            if (_tabControl.SelectedTab == _tabHistory)
                await _historyControl.LoadHistoryAsync();
            else if (_tabControl.SelectedTab == _tabSettings)
                _settingsControl.LoadPolicy();
        };

        _statusStrip = new StatusStrip();
        UiTheme.ApplyStatusStripStyle(_statusStrip);

        _lblStatus = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _lblCount = new ToolStripStatusLabel("Updates: 0") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(6, 0, 6, 0) };
        _lblElevation = new ToolStripStatusLabel(SafetyGuards.IsElevated() ? "Admin: OK" : "Admin: Required")
        {
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            ForeColor = SafetyGuards.IsElevated() ? UiTheme.StatusSuccessText : UiTheme.StatusFailedText,
            Padding = new Padding(6, 0, 6, 0),
            Font = UiTheme.FontBold
        };
        _lblReboot = new ToolStripStatusLabel("Reboot: Clean") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(6, 0, 6, 0) };
        _progressBar = new ToolStripProgressBar { Visible = false, Width = 140 };

        _statusStrip.Items.AddRange(new ToolStripItem[]
        {
            _lblStatus, _progressBar, _lblCount, _lblElevation, _lblReboot
        });

        Controls.Add(_tabControl);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        ClearDetailPane();
    }

    private static CheckBox CompactCheck(string text, bool checkedByDefault = false) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Checked = checkedByDefault,
            Margin = new Padding(4, 2, 12, 2),
            Font = UiTheme.FontSmall
        };

    private void BuildEmptyStatePanel()
    {
        _pnlEmptyState = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.SurfaceBack,
            Visible = false
        };

        var pnlCenter = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Anchor = AnchorStyles.None
        };

        _lblEmptyTitle = new Label
        {
            Text = "No pending updates",
            Font = UiTheme.FontTitle,
            ForeColor = UiTheme.TextPrimary,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblEmptyBody = new Label
        {
            Text = "This PC is up to date for the current filters, or nothing matched.",
            Font = UiTheme.FontBody,
            ForeColor = UiTheme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblEmptyLastChecked = new Label
        {
            Text = "Last checked: Never",
            Font = UiTheme.FontSmall,
            ForeColor = UiTheme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _btnEmptyRefresh = new Button
        {
            Text = "Refresh Updates",
            Font = UiTheme.FontBold,
            Size = new Size(150, 34),
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 0, 0, 0)
        };
        _btnEmptyRefresh.Click += async (s, e) => await RefreshPendingUpdatesAsync();

        pnlCenter.Controls.Add(_lblEmptyTitle);
        pnlCenter.Controls.Add(_lblEmptyBody);
        pnlCenter.Controls.Add(_lblEmptyLastChecked);
        pnlCenter.Controls.Add(_btnEmptyRefresh);

        _pnlEmptyState.Controls.Add(pnlCenter);
        _pnlEmptyState.Resize += (_, _) =>
        {
            pnlCenter.Location = new Point(
                Math.Max(10, (_pnlEmptyState.Width - pnlCenter.Width) / 2),
                Math.Max(10, (_pnlEmptyState.Height - pnlCenter.Height) / 2)
            );
        };
    }

    private void BuildGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            ShowCellToolTips = true,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
        };

        UiTheme.ApplyGridStyle(_grid);

        var chkCol = new DataGridViewCheckBoxColumn
        {
            Name = "Check",
            HeaderText = "",
            Width = 30,
            FillWeight = 20,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            FlatStyle = FlatStyle.Standard
        };
        _grid.Columns.Add(chkCol);

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "KB",
            HeaderText = "KB",
            Width = 95,
            FillWeight = 60,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Title",
            HeaderText = "Title",
            FillWeight = 280,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SizeMB",
            HeaderText = "MB",
            Width = 60,
            FillWeight = 38,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Category",
            HeaderText = "Category",
            Width = 130,
            FillWeight = 85,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Severity",
            HeaderText = "Severity",
            Width = 90,
            FillWeight = 55,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            Width = 90,
            FillWeight = 55,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });

        foreach (DataGridViewColumn col in _grid.Columns)
        {
            if (col.Name != "Check")
                col.ReadOnly = true;
            col.SortMode = DataGridViewColumnSortMode.Automatic;
        }

        _grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        _grid.CellDoubleClick += Grid_CellDoubleClick;
        _grid.SelectionChanged += (_, _) => UpdateDetailFromSelection();
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellMouseDown += Grid_CellMouseDown;
        _grid.KeyDown += Grid_KeyDown;
    }

    private void Grid_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < _grid.Rows.Count && e.ColumnIndex >= 0)
        {
            if (_grid.Rows[e.RowIndex].Tag is UpdateRow u)
            {
                if (_grid.Columns[e.ColumnIndex].Name == "Title")
                {
                    var descSnippet = string.IsNullOrWhiteSpace(u.Description) ? "" : "\n\n" + (u.Description.Length > 180 ? u.Description.Substring(0, 177) + "..." : u.Description);
                    e.ToolTipText = $"{u.Title}{descSnippet}";
                }
                else if (_grid.Columns[e.ColumnIndex].Name == "KB")
                {
                    e.ToolTipText = u.Kb;
                }
            }
        }
    }

    private void BuildDetailPane()
    {
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = UiTheme.SurfaceBack
        };

        _detailTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = UiTheme.FontHeader,
            ForeColor = UiTheme.TextPrimary,
            AutoEllipsis = true,
            Text = "Select an update"
        };

        _detailMeta = new Label
        {
            Dock = DockStyle.Top,
            Height = 20,
            Font = UiTheme.FontSmall,
            ForeColor = UiTheme.TextMuted,
            AutoEllipsis = true,
            Text = ""
        };

        _detailSupportLink = new LinkLabel
        {
            Dock = DockStyle.Bottom,
            Height = 20,
            Text = "",
            Font = UiTheme.FontBody,
            LinkColor = UiTheme.AccentPrimary,
            ActiveLinkColor = UiTheme.AccentHover,
            AutoEllipsis = true,
            Visible = false
        };
        _detailSupportLink.LinkClicked += (_, e) =>
        {
            if (e.Link?.LinkData is string url && !string.IsNullOrWhiteSpace(url))
                OpenUrl(url);
            else if (!string.IsNullOrWhiteSpace(_detailSupportLink.Text) &&
                     _detailSupportLink.Text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                OpenUrl(_detailSupportLink.Text);
        };

        _detailDescription = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = UiTheme.SurfaceBack,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.FontBody,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Text = "Description and release notes appear here when you select a row."
        };

        _detailPanel.Controls.Add(_detailDescription);
        _detailPanel.Controls.Add(_detailSupportLink);
        _detailPanel.Controls.Add(_detailMeta);
        _detailPanel.Controls.Add(_detailTitle);
    }

    private void BuildContextMenu()
    {
        _gridContextMenu = new ContextMenuStrip { Font = UiTheme.FontBody };

        var miDetails = new ToolStripMenuItem("View details…", null, (_, _) => ShowDetailsForContextOrSelection());
        var miCopyKb = new ToolStripMenuItem("Copy KB", null, (_, _) => CopySelectedKb());
        var miCopyTitle = new ToolStripMenuItem("Copy title", null, (_, _) => CopySelectedTitle());
        var miHide = new ToolStripMenuItem("Hide", null, async (_, _) => await HideContextOrCheckedAsync());
        var miOpenSupport = new ToolStripMenuItem("Open support URL", null, (_, _) => OpenSupportForSelection());
        var miCheck = new ToolStripMenuItem("Check row", null, (_, _) => SetContextRowChecked(true));
        var miUncheck = new ToolStripMenuItem("Uncheck row", null, (_, _) => SetContextRowChecked(false));

        _gridContextMenu.Items.AddRange(new ToolStripItem[]
        {
            miDetails,
            new ToolStripSeparator(),
            miCopyKb,
            miCopyTitle,
            new ToolStripSeparator(),
            miCheck,
            miUncheck,
            miHide,
            new ToolStripSeparator(),
            miOpenSupport
        });

        _gridContextMenu.Opening += (_, e) =>
        {
            var row = GetContextOrSelectedUpdate();
            bool has = row != null;
            miDetails.Enabled = has;
            miCopyKb.Enabled = has && !string.IsNullOrWhiteSpace(row!.Kb);
            miCopyTitle.Enabled = has;
            miHide.Enabled = has;
            miOpenSupport.Enabled = has && !string.IsNullOrWhiteSpace(row!.SupportUrl);
            miCheck.Enabled = has;
            miUncheck.Enabled = has;
            if (!has && _grid.RowCount == 0)
                e.Cancel = true;
        };

        _grid.ContextMenuStrip = _gridContextMenu;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_grid.Columns[e.ColumnIndex].Name != "Severity") return;
        if (e.Value is not string sev || string.IsNullOrWhiteSpace(sev)) return;

        e.CellStyle ??= new DataGridViewCellStyle();
        e.CellStyle.ForeColor = SeverityColor(sev);
        e.CellStyle.Font = UiTheme.FontBold;
        e.CellStyle.SelectionForeColor = SeverityColor(sev);
    }

    private static Color SeverityColor(string severity)
    {
        if (severity.Contains("Critical", StringComparison.OrdinalIgnoreCase))
            return UiTheme.SeverityCriticalText;
        if (severity.Contains("Important", StringComparison.OrdinalIgnoreCase))
            return UiTheme.SeverityImportantText;
        if (severity.Contains("Moderate", StringComparison.OrdinalIgnoreCase))
            return UiTheme.SeverityModerateText;
        return UiTheme.TextMuted;
    }

    private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
        _grid.ClearSelection();
        _grid.Rows[e.RowIndex].Selected = true;
        _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[Math.Min(1, _grid.Columns.Count - 1)];
        UpdateDetailFromSelection();
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C)
        {
            CopySelectedKb();
            e.Handled = true;
        }
    }

    private void UpdateDetailFromSelection()
    {
        var u = GetSelectedUpdate();
        if (u == null)
        {
            ClearDetailPane();
            return;
        }

        _detailTitle.Text = string.IsNullOrWhiteSpace(u.Title) ? u.Kb : u.Title;
        _detailMeta.Text =
            $"{u.Kb}  ·  {u.SizeMB:F1} MB  ·  {u.Categories}  ·  {u.Severity}  ·  " +
            $"{(u.IsDownloaded ? "Downloaded" : "Pending")}" +
            $"{(u.IsHidden ? "  ·  Hidden" : "")}" +
            $"{(u.RebootRequired ? "  ·  Reboot required" : "")}" +
            $"  ·  Rev {u.Revision}";

        var desc = string.IsNullOrWhiteSpace(u.Description)
            ? "(No description returned by Windows Update for this item.)"
            : u.Description;
        _detailDescription.Text = desc;

        if (!string.IsNullOrWhiteSpace(u.SupportUrl))
        {
            _detailSupportLink.Visible = true;
            _detailSupportLink.Text = u.SupportUrl;
            _detailSupportLink.Links.Clear();
            _detailSupportLink.Links.Add(0, u.SupportUrl.Length, u.SupportUrl);
        }
        else
        {
            _detailSupportLink.Visible = false;
            _detailSupportLink.Text = "";
            _detailSupportLink.Links.Clear();
        }
    }

    private void ClearDetailPane()
    {
        _detailTitle.Text = _grid.Rows.Count > 0 ? "Select an update to see details" : "Nothing to show";
        _detailMeta.Text = "KB · size · category · severity";
        _detailDescription.Text = _grid.Rows.Count > 0
            ? "Select a row to view complete release notes and description."
            : "No updates currently listed. Click Refresh to scan Windows Update.";
        _detailSupportLink.Visible = false;
        _detailSupportLink.Text = "";
        _detailSupportLink.Links.Clear();
    }

    private UpdateRow? GetSelectedUpdate()
    {
        if (_grid.CurrentRow?.Tag is UpdateRow u) return u;
        if (_grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is UpdateRow u2) return u2;
        return null;
    }

    private UpdateRow? GetContextOrSelectedUpdate() => GetSelectedUpdate();

    private void ShowDetailsForContextOrSelection()
    {
        var u = GetContextOrSelectedUpdate();
        if (u == null) return;
        using var details = new UpdateDetailsForm(u);
        details.ShowDialog(this);
    }

    private void CopySelectedKb()
    {
        var u = GetSelectedUpdate();
        if (u == null || string.IsNullOrWhiteSpace(u.Kb)) return;
        try
        {
            Clipboard.SetText(u.Kb);
            _lblStatus.Text = $"Copied {u.Kb}";
        }
        catch
        {
            _lblStatus.Text = "Could not copy to clipboard.";
        }
    }

    private void CopySelectedTitle()
    {
        var u = GetSelectedUpdate();
        if (u == null || string.IsNullOrWhiteSpace(u.Title)) return;
        try
        {
            Clipboard.SetText(u.Title);
            _lblStatus.Text = "Copied title.";
        }
        catch
        {
            _lblStatus.Text = "Could not copy to clipboard.";
        }
    }

    private void OpenSupportForSelection()
    {
        var u = GetSelectedUpdate();
        if (u == null || string.IsNullOrWhiteSpace(u.SupportUrl)) return;
        OpenUrl(u.SupportUrl);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open URL:\n{ex.Message}", "OpenWU", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetContextRowChecked(bool value)
    {
        if (_grid.CurrentRow == null) return;
        _grid.CurrentRow.Cells["Check"].Value = value;
    }

    private async Task HideContextOrCheckedAsync()
    {
        var checkedItems = GetCheckedRows();
        if (checkedItems.Count == 0)
        {
            var one = GetSelectedUpdate();
            if (one != null)
                checkedItems = new List<UpdateRow> { one };
        }

        if (checkedItems.Count == 0)
        {
            MessageBox.Show("No updates selected to hide.", "Hide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true, "Hiding updates...");
        try
        {
            _cts = new CancellationTokenSource();
            await _service.HideAsync(checkedItems, _chkPersistHide.Checked, _cts.Token);
            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hide failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshPendingUpdatesAsync()
    {
        SetBusy(true, "Searching pending updates...");
        _grid.Rows.Clear();
        _currentRows.Clear();
        ClearDetailPane();

        try
        {
            var options = new SearchOptions
            {
                IncludeDrivers = _chkDrivers.Checked,
                IncludeOptional = _chkOptional.Checked,
                IncludeHidden = false,
                UseMicrosoftUpdate = _chkMsUpdate.Checked
            };

            var statusProgress = new Progress<string>(msg =>
            {
                this.SafeInvoke(() => _lblStatus.Text = msg);
            });

            _cts = new CancellationTokenSource();
            var items = await _service.SearchPendingAsync(options, statusProgress, _cts.Token);

            _lastCheckedTime = DateTime.Now;
            _lblEmptyLastChecked.Text = $"Last checked: {_lastCheckedTime.Value:yyyy-MM-dd HH:mm:ss}";

            _currentRows.AddRange(items);
            _grid.SuspendLayout();

            if (items.Count == 0)
            {
                _pnlEmptyState.Visible = true;
                _grid.Visible = false;
            }
            else
            {
                _pnlEmptyState.Visible = false;
                _grid.Visible = true;

                foreach (var u in items)
                {
                    string statusStr = u.IsDownloaded ? "Downloaded" : (u.IsHidden ? "Hidden" : "Pending");
                    int rowIndex = _grid.Rows.Add(false, u.Kb, u.Title, u.SizeMB.ToString("F1"), u.Categories, u.Severity, statusStr);
                    var row = _grid.Rows[rowIndex];
                    row.Tag = u;
                    ApplyRowChrome(row, rowIndex, u);
                }

                _grid.ClearSelection();
                _grid.Rows[0].Selected = true;
                _grid.CurrentCell = _grid.Rows[0].Cells["KB"];
                UpdateDetailFromSelection();
            }

            _grid.ResumeLayout();

            _lblCount.Text = $"Updates: {items.Count}";
            _lblStatus.Text = $"Ready. Found {items.Count} update(s).";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Search failed.";
            MessageBox.Show($"Search failed: {ex.Message}", "Search Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyRowChrome(DataGridViewRow row, int index, UpdateRow update)
    {
        var back = (index % 2 == 0) ? UiTheme.SurfaceBack : UiTheme.GridRowAltBack;
        row.DefaultCellStyle.BackColor = back;

        if (SafetyGuards.IsSecurityUpdate(update))
        {
            row.Cells["KB"].Style.Font = UiTheme.FontBold;
        }
    }

    private void SelectSecurityRows()
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is UpdateRow update)
                row.Cells["Check"].Value = SafetyGuards.IsSecurityUpdate(update);
        }
    }

    private void SelectAllRows()
    {
        foreach (DataGridViewRow row in _grid.Rows)
            row.Cells["Check"].Value = true;
    }

    private void SelectNoneRows()
    {
        foreach (DataGridViewRow row in _grid.Rows)
            row.Cells["Check"].Value = false;
    }

    private void ShowAboutForm()
    {
        using var about = new AboutForm();
        about.ShowDialog(this);
    }

    private List<UpdateRow> GetCheckedRows()
    {
        var list = new List<UpdateRow>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (Convert.ToBoolean(row.Cells["Check"].Value) && row.Tag is UpdateRow u)
                list.Add(u);
        }
        return list;
    }

    private async Task DownloadSelectedAsync()
    {
        var checkedItems = GetCheckedRows();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show("No updates selected to download.", "Download", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true, "Downloading selected updates...");
        try
        {
            var progress = new Progress<OpProgress>(p =>
            {
                this.SafeInvoke(() =>
                {
                    _lblStatus.Text = $"[{p.Percent}%] {p.Operation}";
                    _progressBar.Style = ProgressBarStyle.Continuous;
                    _progressBar.Value = Math.Clamp(p.Percent, 0, 100);
                });
            });

            _cts = new CancellationTokenSource();
            await _service.DownloadAsync(checkedItems, progress, _cts.Token);
            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Download failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task InstallSelectedAsync()
    {
        var checkedItems = GetCheckedRows();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show("No updates selected to install.", "Install", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var opt = new InstallOptions
        {
            RebootIfRequired = _chkReboot.Checked,
            AllowDomainController = _chkAllowDc.Checked
        };

        SetBusy(true, "Installing selected updates...");
        try
        {
            var progress = new Progress<OpProgress>(p =>
            {
                this.SafeInvoke(() =>
                {
                    _lblStatus.Text = $"[{p.Percent}%] {p.Operation}";
                    _progressBar.Style = ProgressBarStyle.Continuous;
                    _progressBar.Value = Math.Clamp(p.Percent, 0, 100);
                });
            });

            _cts = new CancellationTokenSource();
            var res = await _service.InstallAsync(checkedItems, opt, progress, _cts.Token);

            MessageBox.Show(
                $"Installation complete:\nInstalled: {res.InstalledCount}\nFailed: {res.FailedCount}\nReboot required: {res.RebootRequired}\n\n{res.Message}",
                res.Success ? "Success" : "Warning",
                MessageBoxButtons.OK,
                res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Install failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task HideSelectedAsync()
    {
        var checkedItems = GetCheckedRows();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show("No updates selected to hide.", "Hide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true, "Hiding updates...");
        try
        {
            _cts = new CancellationTokenSource();
            await _service.HideAsync(checkedItems, _chkPersistHide.Checked, _cts.Token);
            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hide failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task UnhideSelectedAsync()
    {
        var checkedItems = GetCheckedRows();
        if (checkedItems.Count == 0)
        {
            MessageBox.Show("No updates selected to unhide.", "Unhide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetBusy(true, "Unhiding updates...");
        try
        {
            _cts = new CancellationTokenSource();
            await _service.UnhideAsync(checkedItems, _cts.Token);
            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unhide failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < _grid.Rows.Count &&
            _grid.Rows[e.RowIndex].Tag is UpdateRow u)
        {
            using var details = new UpdateDetailsForm(u);
            details.ShowDialog(this);
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            e.Handled = true;
            _ = RefreshPendingUpdatesAsync();
        }
        else if (e.Control && e.KeyCode == Keys.A)
        {
            e.Handled = true;
            SelectAllRows();
        }
    }

    private void SetBusy(bool busy, string statusMessage = "Ready")
    {
        _toolStrip.Enabled = !busy;
        _grid.Enabled = !busy;
        _chkDrivers.Enabled = !busy;
        _chkOptional.Enabled = !busy;
        _chkMsUpdate.Enabled = !busy;
        _chkPersistHide.Enabled = !busy;
        _chkAllowDc.Enabled = !busy;
        _chkReboot.Enabled = !busy;

        _lblStatus.Text = statusMessage;
        _progressBar.Visible = busy;
        if (busy)
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Value = 0;
            if (statusMessage == "Ready" && _lblStatus.Text.StartsWith("Searching", StringComparison.OrdinalIgnoreCase))
                _lblStatus.Text = "Ready";
        }
    }
}
