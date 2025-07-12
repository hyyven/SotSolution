using System;
using System.Windows.Forms;

namespace ProxyLoader
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var authClient = new AuthClient();
            Application.Run(new LoginForm(authClient));
        }
    }
}