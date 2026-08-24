using System;
using System.Windows.Forms;
using ProjectHEQTCSDL.FormUI;

namespace ProjectHEQTCSDL
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new FrmLogin());
        }
    }
}