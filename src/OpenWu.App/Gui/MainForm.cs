using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
    private static readonly Color RowAltBack = Color.FromArgb(248, 249, 251);
    private static readonly Color RowNormalBack = Color.White;
    private static readonly Color SeverityCritical = Color.FromArgb(185, 28, 28);
    private static readonly Color SeverityImportant = Color.FromArgb(194, 65, 12);
    private static readonly Color SeverityModerate = Color.FromArgb(161, 98, 7);
    private static readonly Color SeverityLow = Color.FromArgb(21, 128, 61);
    private static readonly Color SeverityDefault = Color.FromArgb(55, 65, 81);

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
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Text = "OpenWU — Windows Update";
        MinimumSize = new Size(920, 580);
        Size = new Size(1040, 680);
        StartPosition = FormStartPosition.CenterScreen;

        _toolStrip = new ToolStrip
        {
            ImageScalingSize = new Size(16, 16),
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(4, 2, 4, 2),
            Font = new Font("Segoe UI", 9F)
        };

        _btnRefresh = new ToolStripButton("Refresh (F5)") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnDownload = new ToolStripButton("Download") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnInstall = new ToolStripButton("Install") { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font(Font, FontStyle.Bold) };
        _btnHide = new ToolStripButton("Hide") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnUnhide = new ToolStripButton("Unhide") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnSelectSecurity = new ToolStripButton("Select Security") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnSelectAll = new ToolStripButton("Select All") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnSelectNone = new ToolStripButton("Select None") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnHistoryTab = new ToolStripButton("History") { DisplayStyle = ToolStripItemDisplayStyle.Text };

        _btnRefresh.Click += async (s, e) => await RefreshPendingUpdatesAsync();
        _btnDownload.Click += async (s, e) => await DownloadSelectedAsync();
        _btnInstall.Click += async (s, e) => await InstallSelectedAsync();
        _btnHide.Click += async (s, e) => await HideSelectedAsync();
        _btnUnhide.Click += async (s, e) => await UnhideSelectedAsync();
        _btnSelectSecurity.Click += (s, e) => SelectSecurityRows();
        _btnSelectAll.Click += (s, e) => SelectAllRows();
        _btnSelectNone.Click += (s, e) => SelectNoneRows();
        _btnHistoryTab.Click += (s, e) => _tabControl.SelectedTab = _tabHistory;

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
            _btnHistoryTab
        });

        var pnlOptions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(6, 3, 6, 2),
            Margin = Padding.Empty,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _chkDrivers = CompactCheck("Include drivers");
        _chkOptional = CompactCheck("Include optional");
        _chkMsUpdate = CompactCheck("Microsoft Update", checkedByDefault: true);
        _chkPersistHide = CompactCheck("Persist hides");
        _chkAllowDc = CompactCheck("Allow DC install");
        _chkAllowDc.ForeColor = Color.DarkRed;
        _chkReboot = CompactCheck("Reboot if required");

        pnlOptions.Controls.AddRange(new Control[]
        {
            _chkDrivers, _chkOptional, _chkMsUpdate, _chkPersistHide, _chkAllowDc, _chkReboot
        });

        BuildGrid();
        BuildDetailPane();
        BuildContextMenu();

        _updatesSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            Panel1MinSize = 120,
            Panel2MinSize = 100,
            FixedPanel = FixedPanel.Panel2
        };
        _updatesSplit.Panel1.Controls.Add(_grid);
        _updatesSplit.Panel2.Controls.Add(_detailPanel);

        // Set splitter after handle exists
        HandleCreated += (_, _) =>
        {
            try
            {
                if (_updatesSplit.Height > 200)
                    _updatesSplit.SplitterDistance = Math.Max(160, _updatesSplit.Height - 180);
            }
            catch { /* layout not ready */ }
        };

        _tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };
        _tabUpdates = new TabPage("Updates") { Padding = new Padding(0) };
        _tabHistory = new TabPage("History");
        _tabSettings = new TabPage("Settings / Policy");

        // Order: fill first, then top options so options stay on top
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

        _statusStrip = new StatusStrip { Font = new Font("Segoe UI", 8.5F) };
        _lblStatus = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _lblCount = new ToolStripStatusLabel("Updates: 0") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(5, 0, 5, 0) };
        _lblElevation = new ToolStripStatusLabel(SafetyGuards.IsElevated() ? "Admin: OK" : "Admin: Required")
        {
            BorderSides = ToolStripStatusLabelBorderSides.Left,
            ForeColor = SafetyGuards.IsElevated() ? Color.DarkGreen : Color.Red,
            Padding = new Padding(5, 0, 5, 0)
        };
        _lblReboot = new ToolStripStatusLabel("Reboot: Clean") { BorderSides = ToolStripStatusLabelBorderSides.Left, Padding = new Padding(5, 0, 5, 0) };
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
            Margin = new Padding(4, 3, 10, 2),
            Font = new Font("Segoe UI", 8.5F)
        };

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
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(226, 232, 240),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 26,
            RowTemplate = { Height = 22 },
            Font = new Font("Segoe UI", 8.75F),
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI Semibold", 8.5F),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 4, 0),
            SelectionBackColor = Color.FromArgb(241, 245, 249),
            SelectionForeColor = Color.FromArgb(30, 41, 59)
        };

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowNormalBack,
            ForeColor = Color.FromArgb(15, 23, 42),
            SelectionBackColor = Color.FromArgb(219, 234, 254),
            SelectionForeColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(2, 0, 2, 0),
            WrapMode = DataGridViewTriState.False
        };

        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowAltBack,
            ForeColor = Color.FromArgb(15, 23, 42),
            SelectionBackColor = Color.FromArgb(219, 234, 254),
            SelectionForeColor = Color.FromArgb(15, 23, 42)
        };

        var chkCol = new DataGridViewCheckBoxColumn
        {
            Name = "Check",
            HeaderText = "",
            Width = 28,
            FillWeight = 18,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            FlatStyle = FlatStyle.Standard
        };
        _grid.Columns.Add(chkCol);

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "KB",
            HeaderText = "KB",
            Width = 88,
            FillWeight = 55,
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
            Width = 56,
            FillWeight = 35,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Category",
            HeaderText = "Category",
            Width = 120,
            FillWeight = 80,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Severity",
            HeaderText = "Severity",
            Width = 84,
            FillWeight = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            Width = 88,
            FillWeight = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });

        foreach (DataGridViewColumn col in _grid.Columns)
        {
            if (col.Name != "Check")
                col.ReadOnly = true;
            col.SortMode = DataGridViewColumnSortMode.Automatic;
        }

        _grid.CellDoubleClick += Grid_CellDoubleClick;
        _grid.SelectionChanged += (_, _) => UpdateDetailFromSelection();
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.CellMouseDown += Grid_CellMouseDown;
        _grid.KeyDown += Grid_KeyDown;
    }

    private void BuildDetailPane()
    {
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 8, 10, 8),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        _detailTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(15, 23, 42),
            AutoEllipsis = true,
            Text = "Select an update"
        };

        _detailMeta = new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Font = new Font("Segoe UI", 8.25F),
            ForeColor = Color.FromArgb(71, 85, 105),
            AutoEllipsis = true,
            Text = ""
        };

        _detailSupportLink = new LinkLabel
        {
            Dock = DockStyle.Bottom,
            Height = 20,
            Text = "",
            LinkColor = Color.FromArgb(37, 99, 235),
            ActiveLinkColor = Color.FromArgb(29, 78, 216),
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
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 8.75F),
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            Text = "Description and release notes appear here when you select a row."
        };

        // Add fill first, then docked edges (WinForms: last added for Dock.Top ends up highest)
        _detailPanel.Controls.Add(_detailDescription);
        _detailPanel.Controls.Add(_detailSupportLink);
        _detailPanel.Controls.Add(_detailMeta);
        _detailPanel.Controls.Add(_detailTitle);
    }

    private void BuildContextMenu()
    {
        _gridContextMenu = new ContextMenuStrip { Font = new Font("Segoe UI", 9F) };

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
        e.CellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
        e.CellStyle.SelectionForeColor = SeverityColor(sev);
    }

    private static Color SeverityColor(string severity)
    {
        if (severity.Contains("Critical", StringComparison.OrdinalIgnoreCase))
            return SeverityCritical;
        if (severity.Contains("Important", StringComparison.OrdinalIgnoreCase))
            return SeverityImportant;
        if (severity.Contains("Moderate", StringComparison.OrdinalIgnoreCase))
            return SeverityModerate;
        if (severity.Contains("Low", StringComparison.OrdinalIgnoreCase))
            return SeverityLow;
        return SeverityDefault;
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
            $"{(u.IsDownloaded ? "Downloaded" : "Not downloaded")}" +
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
        _detailTitle.Text = "Select an update";
        _detailMeta.Text = "KB · size · category · severity";
        _detailDescription.Text = "Description and release notes appear here when you select a row. Double-click or use the context menu for a full details window.";
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

        // Temporarily treat as checked path
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

            _currentRows.AddRange(items);
            _grid.SuspendLayout();
            foreach (var u in items)
            {
                string statusStr = u.IsDownloaded ? "Downloaded" : (u.IsHidden ? "Hidden" : "Pending");
                int rowIndex = _grid.Rows.Add(false, u.Kb, u.Title, u.SizeMB.ToString("F1"), u.Categories, u.Severity, statusStr);
                var row = _grid.Rows[rowIndex];
                row.Tag = u;
                ApplyRowChrome(row, rowIndex, u);
            }

            _grid.ResumeLayout();

            if (_grid.Rows.Count > 0)
            {
                _grid.ClearSelection();
                _grid.Rows[0].Selected = true;
                _grid.CurrentCell = _grid.Rows[0].Cells["KB"];
                UpdateDetailFromSelection();
            }

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
        var back = (index % 2 == 0) ? RowNormalBack : RowAltBack;
        row.DefaultCellStyle.BackColor = back;

        // Subtle left emphasis for security rows
        if (SafetyGuards.IsSecurityUpdate(update))
        {
            row.Cells["KB"].Style.Font = new Font(_grid.Font, FontStyle.Bold);
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

    private List<UpdateRow> GetCheckedRows()
    {
        var list = new List<UpdateRow>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            bool isChecked = Convert.ToBoolean(row.Cells["Check"].Value ?? false);
            if (isChecked && row.Tag is UpdateRow u)
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

        SetBusy(true, "Downloading updates...");
        try
        {
            var progress = new Progress<OpProgress>(p =>
            {
                this.SafeInvoke(() =>
                {
                    _lblStatus.Text = $"{p.Operation} ({p.Percent}%)";
                    _progressBar.Style = ProgressBarStyle.Continuous;
                    _progressBar.Value = Math.Clamp(p.Percent, 0, 100);
                });
            });

            _cts = new CancellationTokenSource();
            await _service.DownloadAsync(checkedItems, progress, _cts.Token);
            MessageBox.Show("Download complete.", "Download", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Download failed: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show("No updates selected for installation.", "Install Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (SafetyGuards.IsDomainController() && !_chkAllowDc.Checked)
        {
            var confirm = MessageBox.Show(
                "THIS MACHINE IS A DOMAIN CONTROLLER.\n\nAre you absolutely sure you want to install updates?",
                "Domain Controller Warning",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;
        }

        var opts = new InstallOptions
        {
            WhatIf = false,
            Force = false,
            RebootIfRequired = _chkReboot.Checked,
            AllowDomainController = _chkAllowDc.Checked
        };

        SetBusy(true, "Installing updates...");
        try
        {
            var progress = new Progress<OpProgress>(p =>
            {
                this.SafeInvoke(() =>
                {
                    _lblStatus.Text = $"{p.Operation} ({p.Percent}%)";
                    _progressBar.Style = ProgressBarStyle.Continuous;
                    _progressBar.Value = Math.Clamp(p.Percent, 0, 100);
                });
            });

            _cts = new CancellationTokenSource();
            var res = await _service.InstallAsync(checkedItems, opts, progress, _cts.Token);

            if (res.RebootRequired)
            {
                _lblReboot.Text = "Reboot: Required";
                _lblReboot.ForeColor = Color.Red;
            }

            MessageBox.Show(res.Message, res.Success ? "Install Complete" : "Install Failed", MessageBoxButtons.OK,
                res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            await RefreshPendingUpdatesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Install error: {ex.Message}", "Install Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            foreach (DataGridViewRow r in _grid.Rows)
                r.Cells["Check"].Value = true;
        }
    }

    private void SetBusy(bool busy, string statusMessage = "Ready")
    {
        _toolStrip.Enabled = !busy;
        _grid.Enabled = !busy;
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
