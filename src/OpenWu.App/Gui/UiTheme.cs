using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenWu.App.Gui;

public static class UiTheme
{
    // Color Palette Tokens
    public static readonly Color HeaderBack = Color.FromArgb(15, 23, 42);      // #0F172A Slate 900
    public static readonly Color HeaderText = Color.White;
    public static readonly Color HeaderMuted = Color.FromArgb(148, 163, 184); // #94A3B8 Slate 400

    public static readonly Color PageBack = Color.FromArgb(248, 250, 252);     // #F8FAFC Slate 50
    public static readonly Color SurfaceBack = Color.White;
    public static readonly Color BorderColor = Color.FromArgb(226, 232, 240);  // #E2E8F0 Slate 200

    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);     // #0F172A
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);    // #64748B Slate 500

    public static readonly Color AccentPrimary = Color.FromArgb(14, 165, 233); // #0EA5E9 Sky 500
    public static readonly Color AccentHover = Color.FromArgb(2, 132, 199);    // #0284C7 Sky 600

    // Severity & Status Colors
    public static readonly Color SeverityCriticalText = Color.FromArgb(220, 38, 38); // #DC2626
    public static readonly Color SeverityCriticalBack = Color.FromArgb(254, 242, 242); // #FEF2F2

    public static readonly Color SeverityImportantText = Color.FromArgb(217, 119, 6); // #D97706
    public static readonly Color SeverityImportantBack = Color.FromArgb(255, 247, 237); // #FFF7ED

    public static readonly Color SeverityModerateText = Color.FromArgb(37, 99, 235); // #2563EB
    public static readonly Color SeverityModerateBack = Color.FromArgb(239, 246, 255); // #EFF6FF

    public static readonly Color StatusSuccessText = Color.FromArgb(16, 185, 129); // #10B981
    public static readonly Color StatusSuccessBack = Color.FromArgb(236, 253, 245); // #ECFDF5

    public static readonly Color StatusFailedText = Color.FromArgb(239, 68, 68); // #EF4444
    public static readonly Color StatusFailedBack = Color.FromArgb(254, 242, 242); // #FEF2F2

    // Grid Row Styling
    public static readonly Color GridRowAltBack = Color.FromArgb(248, 250, 252);
    public static readonly Color GridSelectionBack = Color.FromArgb(224, 242, 254); // #E0F2FE Sky 100
    public static readonly Color GridSelectionText = Color.FromArgb(15, 23, 42);

    // Font Helpers
    public static readonly Font FontBody = new("Segoe UI", 9F, FontStyle.Regular);
    public static readonly Font FontBold = new("Segoe UI", 9F, FontStyle.Bold);
    public static readonly Font FontHeader = new("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font FontTitle = new("Segoe UI", 14F, FontStyle.Bold);
    public static readonly Font FontSmall = new("Segoe UI", 8.5F, FontStyle.Regular);

    public static void ApplyGridStyle(DataGridView grid)
    {
        grid.BackgroundColor = SurfaceBack;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = BorderColor;
        grid.Font = FontBody;

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249), // #F1F5F9 Slate 100
            ForeColor = TextPrimary,
            Font = FontBold,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 4, 6, 4)
        };
        grid.ColumnHeadersHeight = 32;

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SurfaceBack,
            ForeColor = TextPrimary,
            SelectionBackColor = GridSelectionBack,
            SelectionForeColor = GridSelectionText,
            Padding = new Padding(6, 2, 6, 2),
            WrapMode = DataGridViewTriState.False
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = GridRowAltBack,
            ForeColor = TextPrimary,
            SelectionBackColor = GridSelectionBack,
            SelectionForeColor = GridSelectionText
        };

        grid.RowTemplate.Height = 28;
    }

    public static void ApplyToolStripStyle(ToolStrip toolStrip)
    {
        toolStrip.BackColor = SurfaceBack;
        toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        toolStrip.Padding = new Padding(6, 4, 6, 4);
        toolStrip.Font = FontBody;
        toolStrip.RenderMode = ToolStripRenderMode.System;
    }

    public static void ApplyStatusStripStyle(StatusStrip statusStrip)
    {
        statusStrip.BackColor = SurfaceBack;
        statusStrip.ForeColor = TextMuted;
        statusStrip.Font = FontSmall;
        statusStrip.Padding = new Padding(6, 2, 6, 2);
    }
}
