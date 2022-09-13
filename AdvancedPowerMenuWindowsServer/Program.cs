using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdvancedPowerMenu
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            string Path = AppDomain.CurrentDomain.BaseDirectory;
            if (System.Diagnostics.Process.GetProcessesByName(System.Diagnostics.Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                MessageBox.Show("Advanced Power Menu is already running.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FrmMain());
            }
        }
    }
}
