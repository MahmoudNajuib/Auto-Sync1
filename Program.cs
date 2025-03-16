namespace Auto_Sync
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // التحقق من وجود الترخيص
            string license = LicenseManager.LoadLicense();

            if (!string.IsNullOrEmpty(license) && LicenseManager.ValidateSerial(license))
            {
                // الترخيص صالح، استمرار في تشغيل البرنامج
                RunApplication();
            }
            else
            {
                // عرض نموذج التفعيل
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                ActivationForm activationForm = new ActivationForm();
                if (activationForm.ShowDialog() == DialogResult.OK)
                {
                    // تم التفعيل بنجاح، تشغيل البرنامج
                    RunApplication();
                }
                else
                {
                    // إنهاء البرنامج إذا لم يتم التفعيل
                    Environment.Exit(0);
                }
            }
        }

        // تشغيل البرنامج الرئيسي بعد التحقق من الترخيص
        private static void RunApplication()
        {
            // تفعيل أنماط العرض الحديثة
            Application.EnableVisualStyles();

            // ضبط إعدادات التوافق لنصوص Windows Forms
            Application.SetCompatibleTextRenderingDefault(false);

            // تشغيل النموذج الرئيسي للتطبيق
            Application.Run(new Form1());

            // عرض رسالة بعد تشغيل البرنامج (إذا لزم الأمر)
            MessageBox.Show("تم التحقق من الترخيص بنجاح! البرنامج جاهز للاستخدام.", "Auto_Software");
        }

    }
}