using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Evade_proxy
{
    internal static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            //AllocConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}