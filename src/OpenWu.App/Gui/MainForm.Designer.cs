using System.Drawing;
using System.Windows.Forms;

namespace OpenWu.App.Gui;

public partial class MainForm : Form
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        Text = "OpenWU — Windows Update";
        Size = new Size(1020, 660);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
    }
}
