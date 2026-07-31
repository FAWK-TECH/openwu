using System;
using System.Windows.Forms;

namespace OpenWu.App.Gui;

public static class UiInvoke
{
    public static void SafeInvoke(this Control control, Action action)
    {
        if (control == null || control.IsDisposed || !control.IsHandleCreated) return;
        if (control.InvokeRequired)
        {
            control.BeginInvoke(action);
        }
        else
        {
            action();
        }
    }
}
