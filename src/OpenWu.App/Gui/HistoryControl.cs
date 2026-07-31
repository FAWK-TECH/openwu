using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenWu.Core;

namespace OpenWu.App.Gui;

public sealed class HistoryControl : UserControl
{
    private readonly UpdateService _service;
    private DataGridView _grid = null!;
    private Button _btnRefresh = null!;

    public HistoryControl(UpdateService service)
    {
        _service = service;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
        _btnRefresh = new Button { Text = "Refresh History", Location = new Point(10, 6), Size = new Size(130, 28) };
        _btnRefresh.Click += async (s, e) => await LoadHistoryAsync();
        topPanel.Controls.Add(_btnRefresh);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        _grid.Columns.Add("Date", "Date");
        _grid.Columns.Add("Kb", "KB");
        _grid.Columns.Add("Title", "Title");
        _grid.Columns.Add("Result", "Result");

        _grid.Columns[0].Width = 150;
        _grid.Columns[1].Width = 100;
        _grid.Columns[2].FillWeight = 200;
        _grid.Columns[3].Width = 140;

        Controls.Add(_grid);
        Controls.Add(topPanel);
    }

    public async Task LoadHistoryAsync()
    {
        _btnRefresh.Enabled = false;
        try
        {
            var history = await _service.GetHistoryAsync(50);
            _grid.Rows.Clear();
            foreach (var h in history)
            {
                _grid.Rows.Add(h.Date.ToString("yyyy-MM-dd HH:mm"), h.Kb, h.Title, h.Result);
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
