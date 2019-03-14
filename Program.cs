using System;
using System.Windows.Forms;

namespace CertExpiration
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var config = AppConfig.Load();

            var form = new Form1();
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                form.StartUrls = args[0];

            form.Config = config;

            Application.Run(form);
        }
    }
}
