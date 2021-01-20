using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;

namespace InterfaceForm
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                FrmLogin frm = new FrmLogin();

                if (args != null && args.Length > 0)
                {
                    string p_Args = Encoding.Default.GetString(Convert.FromBase64String(args[0]));

                    string[] pS_Args = p_Args.Split(',');

                    frm.LoginDoctorName = pS_Args[0];
                }

                Application.Run(frm);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

                Application.Exit();
            }
        }
    }
}
