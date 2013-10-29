using System;
using System.Security.Principal;
using System.Windows.Forms;

namespace DataCollector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!IsRunAsAdministrator())
            {
                MessageBox.Show(Consts.MsgNonAdminText, Consts.MsgCaption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }

            Application.Run(new ProgressForm());
        }

        private static bool IsRunAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
