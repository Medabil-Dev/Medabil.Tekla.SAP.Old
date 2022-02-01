using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CAMMBS
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            DateTime T = new DateTime(2021, 10, 10);
            DateTime agora = DateTime.Now;
            if (agora > T)
            {
                MessageBox.Show("Aplicativo desatualizado. Contacte suporte\nDaniel Lins Maciel\ndaniel.maciel@medabil.com.br");
                Application.Exit();
            }
            else
            {
                Application.Run(new DLM.cam.Form1());
            }
        
        }
    }
}
