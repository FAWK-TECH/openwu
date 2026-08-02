using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenWu.Core;
using OpenWu.Core.Model;

namespace OpenWu.App.Gui;

public sealed class HistoryControl : UserControl
{
    private readonly UpdateService _service;
    private DataGridView _grid = null!;
    private Button _btnRefresh = null!;
    private Label _lblEmpty = null!;

    public HistoryControl(UpdateService service)
    {
        _service = service;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.PageBack;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = UiTheme.SurfaceBack,
            Padding = new Padding(12, 8, 12, 8)
        };

        var borderBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = UiTheme.BorderColor
        };
        topPanel.Controls.Add(borderBottom);

        _btnRefresh = new Button
        {
            Text = "Refresh History",
            Location = new Point(12, 7),
            Size = new Size(130, 28),
            Font = UiTheme.FontBody,
            FlatStyle = FlatStyle.System
        };
        _btnRefresh.Click += async (s, e) => await LoadHistoryAsync();
        topPanel.Controls.Add(_btnRefresh);

        _lblEmpty = new Label
        {
            Text = "No history entries returned.",
            Font = UiTheme.FontHeader,
            ForeColor = UiTheme.TextMuted,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Visible = false
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ShowCellToolTips = true
        };

        UiTheme.ApplyGridStyle(_grid);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date / Time", Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kb", HeaderText = "KB", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title", FillWeight = 250, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "Result", Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

        _grid.CellToolTipTextNeeded += (s, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _grid.Rows.Count && e.ColumnIndex >= 0)
            {
                if (_grid.Columns[e.ColumnIndex].Name == "Title" && _grid.Rows[e.RowIndex].Tag is HistoryRow h)
                {
                    e.ToolTipText = h.Title;
                }
            }
        };

        Controls.Add(_grid);
        Controls.Add(_lblEmpty);
        Controls.Add(topPanel);
    }

    public async Task LoadHistoryAsync()
    {
        _btnRefresh.Enabled = false;
        try
        {
            var history = await _service.GetHistoryAsync(50);
            _grid.Rows.Clear();

            if (history.Count == 0)
            {
                _lblEmpty.Visible = true;
                _grid.Visible = false;
            }
            else
            {
                _lblEmpty.Visible = false;
                _grid.Visible = true;
                _grid.SuspendLayout();

                foreach (var h in history)
                {
                    int rowIndex = _grid.Rows.Add(h.Date.ToString("yyyy-MM-dd HH:mm"), h.Kb, h.Title, h.Result);
                    var row = _grid.Rows[rowIndex];
                    row.Tag = h;

                    var resCell = row.Cells["Result"];
                    resCell.Style.Font = UiTheme.FontBold;
                    if (h.Result.Contains("Succeeded", StringComparison.OrdinalIgnoreCase))
                    {
                        resCell.Style.ForeColor = UiTheme.StatusSuccessText;
                    }
                    else if (h.Result.Contains("Failed", StringComparison.OrdinalIgnoreCase) || h.Result.Contains("Aborted", StringComparison.OrdinalIgnoreCase))
                    {
                        resCell.Style.ForeColor = UiTheme.StatusFailedText;
                    }
                    else
                    {
                        resCell.Style.ForeColor = UiTheme.TextMuted;
                    }
                }
                _grid.ResumeLayout();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load history: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnRefresh.Enabled = true;
        }
    }
}
