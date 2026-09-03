using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Matric_scope
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "Matric_scope_SingleInstance_Mutex", out createdNew))
            {
                // If createdNew is true, this is the first instance
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new FrmAuto());
                }
                else
                {
                    // If createdNew is false, the app is already running
                    // (Optional) Bring up a message box to let the user know
                    MessageBox.Show("The application is already running.", "Matric Scope", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // The program simply ends here, preventing a second instance
                }
            }
        }
    }
}