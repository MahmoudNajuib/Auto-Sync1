using System.Text;
using Microsoft.Win32;
using System.Management;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Diagnostics;

namespace Auto_Sync
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher watcher;
        private const string appName = "Auto_Sync";
        private const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private bool isMonitoringEnabled = true; // تشغيل المراقبة تلقائيًا عند بدء التشغيل
        private CancellationTokenSource cancellationTokenSource;
        private bool isInitializing = false; // مؤشر لمعرفة ما إذا كان البرنامج في مرحلة التهيئة
        private HashSet<string> successfullyCopiedLargeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //private System.Windows.Forms.Timer licenseTimer; // إضافة Timer لتحديث العرض

        public Form1()
        {
            InitializeComponent();
            LoadStartupState(); // تحميل حالة التشغيل عند بدء التطبيق
            Updatelabel3(); // تحديث أولي عند بدء البرنامج
            notifyIcon1.Visible = false; // تأكد أن الأيقونة غير ظاهرة إلا عند التصغير
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            // إعداد NotifyIcon
            notifyIcon1.MouseDoubleClick += NotifyIcon1_MouseDoubleClick;
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem forceCloseToolStripMenuItem = new ToolStripMenuItem("Quit");
            forceCloseToolStripMenuItem.Click += forceCloseToolStripMenuItem_Click; // ربط الحدث
            contextMenu.Items.Add(forceCloseToolStripMenuItem);
            notifyIcon1.ContextMenuStrip = contextMenu; // ربط ContextMenuStrip بـ NotifyIcon
            cancellationTokenSource = new CancellationTokenSource();

            // تهيئة قائمة الملفات الكبيرة المنسوخة
            successfullyCopiedLargeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // تحديث أول مرة
            label3_TextChanged(null, null);
        }

        private async void Form1_Load_1(object sender, EventArgs e)
        {
            isInitializing = true;
            LoadSettings(); // تحميل الإعدادات المحفوظة

            if (File.Exists("log.txt"))
            {
                listBox1.Text = File.ReadAllText("log.txt");
            }

            string appName = "Auto_Sync";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                تشغيلمعبدءالويندوزToolStripMenuItem.Checked = key?.GetValue(appName) != null;
            }

            // تأجيل تشغيل المراقبة للسماح للواجهة بالظهور أولاً
            await Task.Delay(500);

            // بدء المراقبة في مهمة منفصلة لتجنب تعليق واجهة المستخدم
            await Task.Run(() =>
            {
                if (isMonitoringEnabled &&
                    !string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) &&
                    Directory.Exists(textBox1.Text) && Directory.Exists(textBox2.Text))
                {
                    StartMonitoring();
                }
            });

            isInitializing = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) // عند الضغط على X
            {
                e.Cancel = true; // إلغاء الإغلاق
                this.Hide(); // إخفاء النافذة بدلًا من الخروج
                notifyIcon1.Visible = true; // إظهار أيقونة بجوار الساعة
                notifyIcon1.ShowBalloonTip(1000, "Auto_Sync", "البرنامج لا يزال يعمل في الخلفية.", ToolTipIcon.Info);
            }
        }

        private void SaveSettings()
        {
            try
            {
                string[] settings = {
                    textBox1.Text.Trim(), // حفظ المسار المصدر بعد إزالة الفراغات
                    textBox2.Text.Trim(),  // حفظ المسار الوجهة بعد إزالة الفراغات
                    isMonitoringEnabled ? "1" : "0" // حفظ حالة المراقبة
                };

                // التحقق مما إذا كان يمكن الكتابة في الملف
                File.WriteAllLines(settingsFilePath, settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ الإعدادات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    string[] settings = File.ReadAllLines(settingsFilePath);
                    if (settings.Length >= 2)
                    {
                        textBox1.Text = settings[0]; // تحميل المسار الرئيسي
                        textBox2.Text = settings[1]; // تحميل المسار الاحتياطي

                        // تحميل حالة المراقبة إذا كانت متوفرة
                        if (settings.Length >= 3)
                        {
                            isMonitoringEnabled = settings[2] == "1";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء تحميل الإعدادات: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // تحديث الزر مباشرة
            button1.Text = isMonitoringEnabled ? "إيقاف المراقبة" : "بدء المراقبة";
        }

        private void NotifyIcon1_MouseDoubleClick(object sender, EventArgs e)
        {
            this.Show(); // إعادة إظهار النافذة
            this.WindowState = FormWindowState.Normal; // استعادة النافذة
            notifyIcon1.Visible = false; // إخفاء الأيقونة من شريط المهام
        }

        private void forceCloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("أدخل الرقم السري لغلق التطبيق:", "إيقاف المراقبة", "", -1, -1);

            if (input == "2020") // التحقق من الرقم السري الفرعي قبل الإيقاف
            {
                cancellationTokenSource.Cancel(); // إلغاء أي مهام قيد التنفيذ
                Application.Exit(); // إغلاق التطبيق بالكامل
            }
            else if (!string.IsNullOrEmpty(input))
            {
                MessageBox.Show("الرقم السري غير صحيح!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadStartupState()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(registryPath, false);
            تشغيلمعبدءالويندوزToolStripMenuItem.Checked = regKey?.GetValue(appName) != null;
            isMonitoringEnabled = true;
        }

        private void تشغيلمعبدءالويندوزToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string appName = "Auto_Sync"; // اسم التطبيق في الريجستري
            string appPath = Application.ExecutablePath;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key.GetValue(appName) == null) // لم يتم تمكين التشغيل التلقائي
                {
                    key.SetValue(appName, appPath);
                    تشغيلمعبدءالويندوزToolStripMenuItem.Checked = true;
                    MessageBox.Show("تم تمكين التشغيل التلقائي مع بدء تشغيل الويندوز!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else // التشغيل التلقائي مفعل، قم بإزالته
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox("أدخل الرقم السري لإيقاف المراقبة:", "إيقاف المراقبة", "", -1, -1);

                    if (input == "2020") // التحقق من الرقم السري الفرعي قبل الإيقاف
                    {
                        key.DeleteValue(appName, false);
                        تشغيلمعبدءالويندوزToolStripMenuItem.Checked = false;
                        MessageBox.Show("تم تعطيل التشغيل التلقائي مع بدء تشغيل الويندوز!", "إيقاف", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (!string.IsNullOrEmpty(input))
                    {
                        MessageBox.Show("الرقم السري غير صحيح!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        تشغيلمعبدءالويندوزToolStripMenuItem.Checked = true;
                    }
                }
            }
        }

        private async Task ProcessFile(string filePath, string destinationPath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string relativePath = filePath.Substring(textBox1.Text.Length).TrimStart('\\');
                string destFile = Path.Combine(destinationPath, relativePath);
                string destDir = Path.GetDirectoryName(destFile);

                // التأكد من وجود دليل الوجهة
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                // التحقق من حجم الملف
                FileInfo fileInfo = new FileInfo(filePath);
                bool isLargeFile = fileInfo.Exists && fileInfo.Length > 100 * 1024 * 1024; // أكبر من 100 ميجابايت

                // للملفات الكبيرة، تحقق أولاً إذا كانت موجودة في قائمة الملفات التي تم نسخها بنجاح
                if (isLargeFile)
                {
                    string normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
                    if (successfullyCopiedLargeFiles.Contains(normalizedPath) && File.Exists(destFile))
                    {
                        FileInfo destInfo = new FileInfo(destFile);
                        if (fileInfo.Length == destInfo.Length)
                        {
                            UpdateListBox($"⏩ تم تخطي الملف الكبير (تم نسخه بنجاح سابقًا): {fileName}");
                            return;
                        }
                    }
                }

                // التحقق مما إذا كان الملف يحتاج للنسخ
                if (await ShouldCopyFileAsync(filePath, destFile))
                {
                    if (await IsFileStableAsync(filePath, isLargeFile ? 5 : 2, cancellationTokenSource.Token))
                    {
                        if (isLargeFile)
                        {
                            UpdateListBox($"🔄 بدء نسخ ملف كبير: {fileName} ({(fileInfo.Length / (1024.0 * 1024.0)).ToString("F2")} ميجابايت)");
                            await CopyLargeFileAsync(filePath, destFile, cancellationTokenSource.Token);

                            // إضافة إلى قائمة الملفات المنسوخة بنجاح بعد تأكيد نجاح النسخ
                            string normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
                            successfullyCopiedLargeFiles.Add(normalizedPath);
                            UpdateListBox($"✅ تم نسخ الملف الكبير بنجاح: {fileName}");
                        }
                        else
                        {
                            await CopyFileWithAttributesAsync(filePath, destFile, cancellationTokenSource.Token);
                        }
                    }
                    else
                    {
                        UpdateListBox($"⚠️ الملف {fileName} غير مستقر، سيتم تجاهله");
                    }
                }
                else
                {
                    UpdateListBox($"⏩ تخطي الملف (غير محتاج للنسخ): {fileName}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ الملف {filePath} قيدالاستخدام حاليًا، سيتم المحاولة لاحقًا..!");
            }
        }

        private async Task<bool> ShouldCopyFileAsync(string sourceFile, string destFile)
        {
            try
            {
                // If destination doesn't exist, always copy
                if (!File.Exists(destFile))
                {
                    return true;
                }

                // Get file info
                FileInfo sourceInfo = new FileInfo(sourceFile);
                FileInfo destInfo = new FileInfo(destFile);

                // للملفات العادية، قارن الحجم والتاريخ
                bool sizeMatch = sourceInfo.Length == destInfo.Length;
                bool timeMatch = Math.Abs((sourceInfo.LastWriteTime - destInfo.LastWriteTime).TotalSeconds) <= 2;

                // إذا كان الحجم والتاريخ متطابقين، فلا حاجة للنسخ
                return !(sizeMatch && timeMatch);
            }
            catch
            {
                return true; // In case of any error, copy for safety
            }
        }

        private async Task<bool> IsFileStableAsync(string filePath, int stabilityCheckCount, CancellationToken token)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // متغيرات للتحقق من استقرار الملف
                int consecutiveStableChecks = 0;
                long lastSize = 0;
                DateTime lastUpdateTime = DateTime.MinValue;
                DateTime startTime = DateTime.Now;
                int maxWaitMinutes = 30; // الحد الأقصى للانتظار بالدقائق

                // سجل الحجم الأولي
                FileInfo initialInfo = new FileInfo(filePath);
                lastSize = initialInfo.Length;

                // للملفات الصغيرة جدًا، نعتبرها مستقرة مباشرة
                if (lastSize < 10 * 1024) // أقل من 10 كيلوبايت
                    return true;

                UpdateListBox($"⏳ انتظار استقرار الملف: {Path.GetFileName(filePath)} ({(lastSize / (1024.0 * 1024.0)).ToString("F2")} ميجابايت)");

                while (consecutiveStableChecks < stabilityCheckCount)
                {
                    token.ThrowIfCancellationRequested();

                    // التحقق من تجاوز الحد الأقصى للانتظار
                    if ((DateTime.Now - startTime).TotalMinutes > maxWaitMinutes)
                    {
                        UpdateListBox($"⚠️ تجاوز الحد الأقصى للانتظار ({maxWaitMinutes} دقيقة) للملف: {Path.GetFileName(filePath)}");
                        return false;
                    }

                    // انتظار قبل التحقق التالي - فترة أقصر للملفات الصغيرة، وأطول للملفات الكبيرة
                    int waitTime = lastSize > 500 * 1024 * 1024 ? 5000 : // أكثر من 500 ميجابايت
                                  lastSize > 100 * 1024 * 1024 ? 3000 : // أكثر من 100 ميجابايت
                                  1000; // للملفات الأصغر

                    await Task.Delay(waitTime, token);

                    if (!File.Exists(filePath)) // إذا تم حذف الملف أثناء الانتظار
                        return false;

                    // التحقق من حجم الملف الحالي
                    FileInfo currentInfo = new FileInfo(filePath);
                    long currentSize = currentInfo.Length;
                    bool sizeChanged = currentSize != lastSize;

                    // إذا تغير الحجم، أعد ضبط العداد وحدث القيم
                    if (sizeChanged)
                    {
                        consecutiveStableChecks = 0;

                        // تحديث رسالة الحالة مرة واحدة فقط كل دقيقة
                        if ((DateTime.Now - lastUpdateTime).TotalMinutes >= 1)
                        {
                            UpdateListBox($"⏳ انتظار استقرار الملف: {Path.GetFileName(filePath)} ({(currentSize / (1024.0 * 1024.0)).ToString("F2")} ميجابايت)");
                            lastUpdateTime = DateTime.Now;
                        }
                    }
                    else
                    {
                        // إذا لم يتغير الحجم، زد عداد الاستقرار
                        consecutiveStableChecks++;

                        // تحديث الرسالة عند التقدم في عداد الاستقرار، ولكن ليس في كل مرة
                        if (consecutiveStableChecks == stabilityCheckCount / 2 ||
                            (consecutiveStableChecks == 1 && (DateTime.Now - lastUpdateTime).TotalMinutes >= 1))
                        {
                            //UpdateListBox($"⏳ الملف في طريقه للاستقرار: {Path.GetFileName(filePath)} ({consecutiveStableChecks}/{stabilityCheckCount})");
                            lastUpdateTime = DateTime.Now;
                        }
                    }

                    // تحديث حجم الملف الأخير
                    lastSize = currentSize;
                }

                // الملف مستقر الآن
                UpdateListBox($"✅ الملف أصبح مستقرًا: {Path.GetFileName(filePath)} ({(lastSize / (1024.0 * 1024.0)).ToString("F2")} ميجابايت)");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // لا نظهر رسائل الخطأ بناء على طلبك
                // UpdateListBox($"⚠️ خطأ أثناء التحقق من استقرار الملف: {ex.Message}");
                return false;
            }
        }

        private async Task CopyFileWithAttributesAsync(string sourceFile, string destFile, CancellationToken token)
        {
            try
            {
                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
                using (FileStream destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    await sourceStream.CopyToAsync(destStream, 81920, token);
                    destStream.Flush(true);
                }

                // نسخ سمات الملف
                File.SetAttributes(destFile, File.GetAttributes(sourceFile));
                File.SetCreationTime(destFile, File.GetCreationTime(sourceFile));
                File.SetLastWriteTime(destFile, File.GetLastWriteTime(sourceFile));
                File.SetLastAccessTime(destFile, File.GetLastAccessTime(sourceFile));

                UpdateListBox($"✅ تم نسخ: {Path.GetFileName(sourceFile)}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (!IsFileStable(sourceFile))
                {
                    UpdateListBox($"⚠️ الملف {Path.GetFileName(sourceFile)} قيدالاستخدام حاليًا، سيتم المحاولة لاحقًا..!");
                }
                throw;
            }
        }

        private async Task<bool> CopyLargeFileAsync(string sourceFile, string destFile, CancellationToken token)
        {
            try
            {
                FileInfo sourceInfo = new FileInfo(sourceFile);
                long totalSize = sourceInfo.Length;
                long copiedSize = 0;
                int bufferSize = 4 * 1024 * 1024; // 4 ميجابايت بافر للقراءة/الكتابة
                bool success = false;
                int retryCount = 0;
                int maxRetries = 3;

                // محاولة النسخ مع إعادة المحاولة
                while (!success && retryCount <= maxRetries && !token.IsCancellationRequested)
                {
                    try
                    {
                        // تنظيف الملف الهدف إذا كان موجودًا من محاولات سابقة
                        if (retryCount > 0 && File.Exists(destFile))
                        {
                            File.Delete(destFile);
                            await Task.Delay(500, token); // انتظار للتأكد من إكمال الحذف
                        }

                        using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan))
                        using (FileStream destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.WriteThrough))
                        {
                            byte[] buffer = new byte[bufferSize];
                            int bytesRead;
                            DateTime lastUpdateTime = DateTime.Now;

                            // قراءة ونسخ الملف بأجزاء
                            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                            {
                                await destStream.WriteAsync(buffer, 0, bytesRead, token);
                                copiedSize += bytesRead;

                                // تحديث حالة النسخ كل 2 ثانية
                                if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 2)
                                {
                                    double percentage = (double)copiedSize / totalSize * 100;
                                    double speed = copiedSize / (DateTime.Now - lastUpdateTime.AddSeconds(-2)).TotalSeconds / (1024 * 1024);
                                    UpdateListBox($"📦 نسخ: {Path.GetFileName(sourceFile)} - {percentage:F1}% ({speed:F2} ميجابايت/ثانية)");
                                    lastUpdateTime = DateTime.Now;
                                }

                                token.ThrowIfCancellationRequested();
                            }

                            // ضمان كتابة جميع البيانات إلى القرص
                            destStream.Flush(true);
                        }

                        // التحقق من نجاح النسخ
                        success = await VerifyCopySuccess(sourceFile, destFile, token);

                        if (success)
                        {
                            // نسخ سمات الملف
                            File.SetAttributes(destFile, File.GetAttributes(sourceFile));
                            File.SetCreationTime(destFile, File.GetCreationTime(sourceFile));
                            File.SetLastWriteTime(destFile, File.GetLastWriteTime(sourceFile));
                            File.SetLastAccessTime(destFile, File.GetLastAccessTime(sourceFile));
                            UpdateListBox($"✅ تم نسخ الملف الكبير بنجاح: {Path.GetFileName(sourceFile)}");
                        }
                        else
                        {
                            retryCount++;
                            UpdateListBox($"🔄 فشل التحقق من نسخ الملف، محاولة {retryCount} من {maxRetries}: {Path.GetFileName(sourceFile)}");
                            await Task.Delay(1000, token); // انتظار قبل إعادة المحاولة
                        }
                    }
                    catch (IOException ioEx)
                    {
                        retryCount++;
                        UpdateListBox($"⚠️ خطأ I/O أثناء النسخ (محاولة {retryCount} من {maxRetries}): {ioEx.Message}");
                        await Task.Delay(2000, token); // انتظار أطول في حالة خطأ I/O
                    }
                }

                if (!success && retryCount > maxRetries)
                {
                    UpdateListBox($"❌ فشل نسخ الملف بعد {maxRetries} محاولات: {Path.GetFileName(sourceFile)}");
                }

                return success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                UpdateListBox($"❌ خطأ أثناء نسخ الملف الكبير: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> VerifyCopySuccess(string sourceFile, string destFile, CancellationToken token)
        {
            try
            {
                if (!File.Exists(sourceFile) || !File.Exists(destFile))
                    return false;

                FileInfo sourceInfo = new FileInfo(sourceFile);
                FileInfo destInfo = new FileInfo(destFile);

                // التحقق من تطابق الحجم
                if (sourceInfo.Length != destInfo.Length)
                {
                    UpdateListBox($"⚠️ حجم الملف المنسوخ غير مطابق: {Path.GetFileName(sourceFile)} ({sourceInfo.Length} بايت مقابل {destInfo.Length} بايت)");
                    return false;
                }

                // للملفات الكبيرة جدًا (أكثر من 1 جيجابايت)، نكتفي بالتحقق من الحجم
                if (sourceInfo.Length > 1024 * 1024 * 1024)
                    return true;

                // للملفات الكبيرة (أكثر من 100 ميجابايت)، نتحقق من عينات عشوائية
                if (sourceInfo.Length > 100 * 1024 * 1024)
                {
                    return await VerifyLargeFileSamples(sourceFile, destFile, token);
                }

                // للملفات الأصغر، نتحقق من كامل المحتوى
                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream destStream = new FileStream(destFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int bufferSize = 4 * 1024 * 1024; // 4 ميجابايت
                    byte[] sourceBuffer = new byte[bufferSize];
                    byte[] destBuffer = new byte[bufferSize];
                    int bytesRead;

                    while ((bytesRead = await sourceStream.ReadAsync(sourceBuffer, 0, bufferSize, token)) > 0)
                    {
                        int destBytesRead = await destStream.ReadAsync(destBuffer, 0, bytesRead, token);

                        if (bytesRead != destBytesRead)
                            return false;

                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (sourceBuffer[i] != destBuffer[i])
                                return false;
                        }

                        token.ThrowIfCancellationRequested();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء التحقق من نجاح النسخ: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> VerifyLargeFileSamples(string sourceFile, string destFile, CancellationToken token)
        {
            try
            {
                FileInfo sourceInfo = new FileInfo(sourceFile);
                long fileSize = sourceInfo.Length;

                // عدد العينات يعتمد على حجم الملف
                int sampleCount = (int)Math.Min(50, Math.Max(5, fileSize / (10 * 1024 * 1024)));
                int sampleSize = 16 * 1024; // 16 كيلوبايت لكل عينة

                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream destStream = new FileStream(destFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] sourceBuffer = new byte[sampleSize];
                    byte[] destBuffer = new byte[sampleSize];
                    Random random = new Random();

                    for (int i = 0; i < sampleCount; i++)
                    {
                        // اختيار موقع عشوائي (مع تجنب آخر جزء من الملف لتفادي قراءة جزئية)
                        long position = random.Next(0, (int)(fileSize - sampleSize - 1));

                        sourceStream.Position = position;
                        destStream.Position = position;

                        int sourceBytesRead = await sourceStream.ReadAsync(sourceBuffer, 0, sampleSize, token);
                        int destBytesRead = await destStream.ReadAsync(destBuffer, 0, sampleSize, token);

                        if (sourceBytesRead != destBytesRead)
                            return false;

                        for (int j = 0; j < sourceBytesRead; j++)
                        {
                            if (sourceBuffer[j] != destBuffer[j])
                                return false;
                        }

                        token.ThrowIfCancellationRequested();
                    }

                    // تحقق إضافي من بداية ونهاية الملف
                    // بداية الملف
                    sourceStream.Position = 0;
                    destStream.Position = 0;
                    await sourceStream.ReadAsync(sourceBuffer, 0, sampleSize, token);
                    await destStream.ReadAsync(destBuffer, 0, sampleSize, token);
                    for (int j = 0; j < sampleSize; j++)
                    {
                        if (sourceBuffer[j] != destBuffer[j])
                            return false;
                    }

                    // نهاية الملف
                    long endPosition = Math.Max(0, fileSize - sampleSize);
                    sourceStream.Position = endPosition;
                    destStream.Position = endPosition;
                    await sourceStream.ReadAsync(sourceBuffer, 0, sampleSize, token);
                    await destStream.ReadAsync(destBuffer, 0, sampleSize, token);
                    for (int j = 0; j < sampleSize; j++)
                    {
                        if (sourceBuffer[j] != destBuffer[j])
                            return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء التحقق من عينات الملف: {ex.Message}");
                return false;
            }
        }

        private void ProcessDirectory(string dirPath, string destinationBasePath)
        {
            try
            {
                string relativePath = dirPath.Substring(textBox1.Text.Length).TrimStart('\\');
                string destDir = Path.Combine(destinationBasePath, relativePath);

                // إنشاء المجلد في الوجهة إذا لم يكن موجوداً
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    CopyDirectoryAttributes(dirPath, destDir);
                    UpdateListBox($"✅ تم إنشاء المجلد: {relativePath}");
                }
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء معالجة المجلد: {ex.Message}");
            }
        }

        private async Task CopyExistingFilesAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken, HashSet<string> processedFiles = null)
        {
            if (processedFiles == null)
                processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // التحقق من إلغاء العملية
                if (cancellationToken.IsCancellationRequested)
                    return;

                // معالجة الملفات في المجلد الحالي
                foreach (var file in Directory.GetFiles(sourcePath))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    // تخطي الملفات التي تمت معالجتها بالفعل
                    if (processedFiles.Contains(file))
                        continue;

                    // إضافة الملف إلى قائمة الملفات المعالجة
                    processedFiles.Add(file);

                    // استدعاء معالجة الملف
                    await ProcessFile(file, destinationPath);

                    // توقف قصير لتجنب استهلاك الموارد
                    await Task.Delay(5, cancellationToken);
                }

                // معالجة المجلدات الفرعية
                foreach (var directory in Directory.GetDirectories(sourcePath))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    string relativePath = directory.Substring(textBox1.Text.Length).TrimStart('\\');
                    string destDir = Path.Combine(destinationPath, relativePath);

                    // إنشاء المجلد إذا لم يكن موجودًا
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                        CopyDirectoryAttributes(directory, destDir);
                        UpdateListBox($"✅ تم إنشاء المجلد: {relativePath}");
                    }

                    // استدعاء متكرر لمعالجة المجلد الفرعي
                    await CopyExistingFilesAsync(directory, destinationPath, cancellationToken, processedFiles);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء نسخ المجلدات: {ex.Message}");
            }
        }

        private bool IsFileStable(string filePath, int retryCount = 20)
        {
            //Again1:
            // زيادة عدد المحاولات للملفات الكبيرة
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    if (!fileInfo.Exists)
                        return false; // الملف غير موجود

                    long initialSize = fileInfo.Length;
                    bool isLargeFile = initialSize > 10 * 1024 * 1024; // أكبر من 10 ميجابايت

                    // وقت انتظار أطول للملفات الكبيرة
                    int waitTime = isLargeFile ? 15000 : 5000; // 15 ثانية للملفات الكبيرة، 5 ثوانٍ للصغيرة
                    Thread.Sleep(waitTime);

                    fileInfo.Refresh();

                    // التحقق من الحجم بعد الانتظار
                    if (initialSize != fileInfo.Length)
                    {
                        UpdateListBox($"⏳ الملف {Path.GetFileName(filePath)} لا يزال يتغير حجمه، الانتظار...");
                        //Task.Delay(10000).Wait(); // ✅ تحسين: استبدال `Thread.Sleep` بـ `Task.Delay`
                        //return;
                        //goto Again1;
                    }

                    if (!CanAccessFile(filePath))
                    {
                        return false;
                    }

                    // التحقق من إمكانية الوصول للملف
                    using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // لا نحتاج لفعل أي شيء، فقط نتحقق من أنه يمكن فتح الملف للقراءة
                        return true;
                    }
                }
                catch (IOException)
                {
                    // الملف لا يزال مقفلاً، ننتظر ثم نحاول مرة أخرى
                    UpdateListBox($"⚠️ الملف قيد الاستخدام حاليًا، ستتم المحاولة لاحقًا...!: {Path.GetFileName(filePath)}");
                    Task.Delay(10000).Wait(); // ✅ تحسين: استبدال `Thread.Sleep` بـ `Task.Delay`
                                              //return;
                                              //goto Again1;
                }
                catch (Exception ex)
                {
                    UpdateListBox($"⚠️ خطأ في فحص استقرار الملف: {ex.Message}");
                    return false;
                }
            }

            return false; // بعد كل المحاولات، الملف ما زال غير مستقر
        }

        private bool CanAccessFile(string filePath)
        {
            try
            {
                // استخدام FileShare.ReadWrite لتجنب رفض الوصول إذا كان الملف مفتوحًا للقراءة فقط
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return true; // الملف يمكن الوصول إليه
                }
            }
            catch (IOException)
            {
                return false; // الملف قيد الاستخدام بشكل حصري
            }
        }

        private void CopyExistingFiles(string sourcePath, string destinationPath)
        {
            try
            {
                // نسخ الملفات الموجودة في المجلد الرئيسي
                foreach (var file in Directory.GetFiles(sourcePath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationPath, fileName);
                    CopyFileWithAttributes(file, destFile);
                }

                // نسخ المجلدات الفرعية بمحتوياتها
                foreach (var directory in Directory.GetDirectories(sourcePath))
                {
                    string dirName = Path.GetFileName(directory);
                    string destDir = Path.Combine(destinationPath, dirName);

                    // إنشاء المجلد إذا لم يكن موجوداً
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                        CopyDirectoryAttributes(directory, destDir);
                        UpdateListBox($"✅ تم إنشاء المجلد: {dirName}");
                    }

                    // استدعاء متكرر لنسخ محتويات المجلد الفرعي
                    CopyExistingFiles(directory, destDir);
                }
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء نسخ المجلدات: {ex.Message}");
            }
        }

        private void CopyDirectoryAttributes(string sourceDir, string destDir)
        {
            try
            {
                DirectoryInfo sourceInfo = new DirectoryInfo(sourceDir);
                DirectoryInfo destInfo = new DirectoryInfo(destDir);

                // نسخ سمات المجلد
                destInfo.Attributes = sourceInfo.Attributes;

                // نسخ أوقات الإنشاء والتعديل
                Directory.SetCreationTime(destDir, Directory.GetCreationTime(sourceDir));
                Directory.SetLastAccessTime(destDir, Directory.GetLastAccessTime(sourceDir));
                Directory.SetLastWriteTime(destDir, Directory.GetLastWriteTime(sourceDir));

                // نسخ أذونات المجلد (إذا كان ممكناً)
                try
                {
                    DirectorySecurity security = sourceInfo.GetAccessControl();
                    destInfo.SetAccessControl(security);
                }
                catch (PrivilegeNotHeldException)
                {
                    // تجاهل أخطاء الأذونات إذا لم تكن كافية
                    UpdateListBox($"⚠️ لم يتم نسخ أذونات المجلد {Path.GetFileName(sourceDir)} (حقوق وصول غير كافية)");
                }
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء نسخ خصائص المجلد {Path.GetFileName(sourceDir)}: {ex.Message}");
            }
        }

        private void CopyFileWithAttributes(string sourceFile, string destFile)
        {
            string fileName = Path.GetFileName(sourceFile);

            // Check if destination file exists
            bool needsCopy = true;
            if (File.Exists(destFile))
            {
                // Compare file attributes and timestamps before copying
                try
                {
                    // الحصول على معلومات الملفات
                    FileInfo sourceInfo = new FileInfo(sourceFile);
                    FileInfo destInfo = new FileInfo(destFile);

                    // مقارنة حجم الملف
                    if (sourceInfo.Length != destInfo.Length)
                    {
                        needsCopy = true;
                        //UpdateListBox($"ℹ️ الحجم مختلف: {fileName}");
                    }
                    // مقارنة خصائص الملف
                    else if (File.GetAttributes(sourceFile) != File.GetAttributes(destFile))
                    {
                        needsCopy = true;
                        //UpdateListBox($"ℹ️ الخصائص مختلفة: {fileName}");
                    }
                    // مقارنة وقت التعديل الأخير
                    else if (File.GetLastWriteTime(sourceFile) != File.GetLastWriteTime(destFile))
                    {
                        needsCopy = true;
                        //UpdateListBox($"ℹ️ تاريخ التعديل مختلف: {fileName}");
                    }
                    else
                    {
                        // الملفات متطابقة، لا داعي للنسخ
                        needsCopy = false;
                        UpdateListBox($"⏩ تم تخطي الملف (متطابق): {fileName}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // في حالة فشل المقارنة، المتابعة بالنسخ
                    UpdateListBox($"⚠️ خطأ أثناء مقارنة الملف: {fileName} - {ex.Message}");
                    needsCopy = true;
                }
            }
            else
            {
                // الملف غير موجود في الوجهة
                //UpdateListBox($"ℹ️ ملف جديد: {fileName}");
            }

            // إذا لم تكن هناك حاجة للنسخ، نخرج من الدالة
            if (!needsCopy)
                return;

            Again:
            try
            {
                // التحقق مما إذا كان الملف مقفلاً أو قيد الاستخدام
                if (IsFileStable(sourceFile))
                {
                    // نسخ الملف
                    File.Copy(sourceFile, destFile, true);
                    // نسخ السمات والخصائص
                    File.SetCreationTime(destFile, File.GetCreationTime(sourceFile));
                    File.SetLastAccessTime(destFile, File.GetLastAccessTime(sourceFile));
                    File.SetLastWriteTime(destFile, File.GetLastWriteTime(sourceFile));
                    File.SetAttributes(destFile, File.GetAttributes(sourceFile));
                    UpdateListBox($"✅ تم نسخ الملف: {fileName}");
                }
                else
                {
                    UpdateListBox($"⚠️ الملف قيد الاستخدام حاليًا، ستتم المحاولة لاحقًا...!: {Path.GetFileName(sourceFile)}");
                    Task.Delay(5000).Wait(); // ✅ تحسين: استبدال `Thread.Sleep` بـ `Task.Delay`
                                             //return;
                    goto Again;
                }
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ محاولة...!");
            }
        }

        private string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");

        private void SaveMonitoringState(bool isEnabled)
        {
            isMonitoringEnabled = isEnabled;
            SaveSettings();
        }

        private async void StartMonitoring()
        {
            string sourcePath = textBox1.Text;
            string destinationPath = textBox2.Text;

            UpdateListBox("📣 جاري بدء المراقبة...");

            try
            {
                // إعادة إنشاء CancellationTokenSource لمنع استخدام مصدر ملغى
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                }

                watcher?.Dispose();
                watcher = new FileSystemWatcher(sourcePath)
                {
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName
                };

                // بدء نسخ الملفات الموجودة فقط عند تغيير المسار أو عند بدء التشغيل الأول
                await CopyExistingFilesAsync(sourcePath, destinationPath, cancellationTokenSource.Token);

                // مراقبة التغييرات في المجلد
                watcher.Created += (s, ev) =>
                {
                    if (Directory.Exists(ev.FullPath))
                        ProcessDirectory(ev.FullPath, destinationPath);
                    else
                        ProcessFile(ev.FullPath, destinationPath);
                };

                watcher.Changed += (s, ev) =>
                {
                    if (!Directory.Exists(ev.FullPath)) // فقط للملفات (المجلدات لا تحتاج إلى تحديث عند تغييرها)
                        ProcessFile(ev.FullPath, destinationPath);
                };

                watcher.Deleted += (s, ev) => DeleteFile(ev.FullPath, destinationPath);
                watcher.Renamed += (s, ev) => RenamedFile(ev.OldFullPath, ev.FullPath, destinationPath);

                UpdateListBox("📣 تم تفعيل المراقبة بنجاح!");

                // تحديث واجهة المستخدم على الـ UI thread
                this.Invoke(new Action(() =>
                {
                    button1.Text = "إيقاف المراقبة";
                }));
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء بدء المراقبة: {ex.Message}");

                // تحديث واجهة المستخدم على الـ UI thread
                this.Invoke(new Action(() =>
                {
                    button1.Text = "بدء المراقبة";
                    isMonitoringEnabled = false;
                }));
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (isMonitoringEnabled)
            {

                string input = Microsoft.VisualBasic.Interaction.InputBox("أدخل الرقم السري لإيقاف المراقبة:", "إيقاف المراقبة", "", -1, -1);

                if (input == "2020") // التحقق من الرقم السري الفرعي قبل الإيقاف
                {
                    // Stop monitoring logic
                    cancellationTokenSource.Cancel();
                    watcher?.Dispose();
                    watcher = null;
                    isMonitoringEnabled = false;
                    button1.Text = "بدء المراقبة";
                    SaveMonitoringState(false);
                    UpdateListBox("📣 تم إيقاف المراقبة!");
                }
                else if (!string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("⚠ الرقم السري غير صحيح!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Start monitoring logic
                string sourcePath = textBox1.Text;
                string destinationPath = textBox2.Text;

                if (Directory.Exists(sourcePath) && Directory.Exists(destinationPath))
                {
                    isMonitoringEnabled = true;
                    button1.Text = "إيقاف المراقبة";

                    // Create new cancellation token source
                    if (cancellationTokenSource.IsCancellationRequested)
                        cancellationTokenSource = new CancellationTokenSource();

                    try
                    {
                        UpdateListBox("📣 جاري بدء المراقبة...");

                        // Initialize FileSystemWatcher
                        watcher?.Dispose();
                        watcher = new FileSystemWatcher(sourcePath)
                        {
                            EnableRaisingEvents = true,
                            IncludeSubdirectories = true,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName
                        };

                        // Set up event handlers
                        watcher.Created += (s, ev) =>
                        {
                            if (Directory.Exists(ev.FullPath))
                                ProcessDirectory(ev.FullPath, destinationPath);
                            else
                                ProcessFile(ev.FullPath, destinationPath);
                        };

                        watcher.Changed += (s, ev) =>
                        {
                            if (!Directory.Exists(ev.FullPath))
                                ProcessFile(ev.FullPath, destinationPath);
                        };

                        watcher.Deleted += (s, ev) => DeleteFile(ev.FullPath, destinationPath);
                        watcher.Renamed += (s, ev) => RenamedFile(ev.OldFullPath, ev.FullPath, destinationPath);

                        // Copy existing files asynchronously with proper checking
                        await Task.Run(async () =>
                        {
                            await CopyExistingFilesAsync(sourcePath, destinationPath, cancellationTokenSource.Token);
                        });

                        SaveMonitoringState(true);
                        UpdateListBox("📣 تم تفعيل المراقبة بنجاح!");
                    }
                    catch (Exception ex)
                    {
                        UpdateListBox($"⚠️ خطأ أثناء بدء المراقبة: {ex.Message}");
                        isMonitoringEnabled = false;
                        button1.Text = "بدء المراقبة";
                    }
                }
                else
                {
                    MessageBox.Show("يجب تحديد مسار مصدر ووجهة صالحين قبل بدء المراقبة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    isMonitoringEnabled = false;
                    button1.Text = "بدء المراقبة";
                }
            }

            SaveSettings();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox1.Text = folderDialog.SelectedPath;
                    SaveSettings();

                    // نسخ الملفات فور اختيار المجلد
                    if (Directory.Exists(textBox1.Text) && Directory.Exists(textBox2.Text))
                    {
                        CopyExistingFiles(textBox1.Text, textBox2.Text);
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = folderDialog.SelectedPath;
                    SaveSettings();

                    // نسخ الملفات فور اختيار المجلد
                    if (Directory.Exists(textBox1.Text) && Directory.Exists(textBox2.Text))
                    {
                        CopyExistingFiles(textBox1.Text, textBox2.Text);
                    }
                }
            }
        }

        private void DeleteFile(string filePath, string destinationPath)
        {
            try
            {
                string relativePath = filePath.Substring(textBox1.Text.Length).TrimStart('\\');
                string destPath = Path.Combine(destinationPath, relativePath);

                // التحقق مما إذا كان مجلداً
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true); // حذف المجلد بمحتوياته
                    UpdateListBox($"✅ تم حذف المجلد: {relativePath}");
                }
                else if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                    UpdateListBox($"✅ تم حذف الملف: {relativePath}");
                }
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء الحذف: {ex.Message}");
            }
        }

        private void RenamedFile(string oldPath, string newPath, string destinationPath)
        {
            try
            {
                string oldRelPath = oldPath.Substring(textBox1.Text.Length).TrimStart('\\');
                string newRelPath = newPath.Substring(textBox1.Text.Length).TrimStart('\\');

                string oldDestPath = Path.Combine(destinationPath, oldRelPath);
                string newDestPath = Path.Combine(destinationPath, newRelPath);

                // إنشاء المجلد الأب للمسار الجديد إذا لم يكن موجوداً
                string parentDir = Path.GetDirectoryName(newDestPath);
                if (!Directory.Exists(parentDir) && !string.IsNullOrEmpty(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }

                // التعامل مع المجلدات
                if (Directory.Exists(oldDestPath))
                {
                    // نقل المجلد بأكمله
                    Directory.Move(oldDestPath, newDestPath);
                    UpdateListBox($"✅ تم إعادة تسمية المجلد من {oldRelPath} إلى {newRelPath}");
                }
                // التعامل مع الملفات
                else if (File.Exists(oldDestPath))
                {
                    File.Move(oldDestPath, newDestPath);
                    UpdateListBox($"✅ تم إعادة تسمية الملف من {oldRelPath} إلى {newRelPath}");
                }
            }
            catch (Exception ex)
            {
                UpdateListBox($"⚠️ خطأ أثناء إعادة التسمية: {ex.Message}");
            }
        }

        private void UpdateListBox(string message)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() =>
                {
                    listBox1.Items.Insert(0, message);
                    listBox1.Font = new Font("Tahoma", 10, FontStyle.Regular);
                }));
            }
            else
            {
                listBox1.Items.Insert(0, message);
                listBox1.Font = new Font("Tahoma", 10, FontStyle.Regular);
            }
        }

        private void Updatelabel3()
        {
            int days = LicenseManager.GetRemainingDays();
            label3.Text = $"الأيام المتبقية:   {days}";
        }

        private void label3_TextChanged(object sender, EventArgs e)
        {
            int days = LicenseManager.GetRemainingDays();
            this.Text = $"الأيام المتبقية: {days}"; // تحديث عنوان النافذة

        }
    }

    public class ActivationForm : Form
    {
        private TextBox txtSerial;
        private Button btnActivate;
        private Button btnCancel;
        private Label lblMachineId;

        public ActivationForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "تفعيل البرنامج     Whatsapp  00201274096624";
            this.Width = 450;
            this.Height = 250;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            Label lblInfo = new Label();
            lblInfo.Text = "أدخل رمز التفعيل الخاص بك:";
            lblInfo.Location = new System.Drawing.Point(30, 30);
            lblInfo.AutoSize = true;

            txtSerial = new TextBox();
            txtSerial.Location = new System.Drawing.Point(30, 60);
            txtSerial.Width = 370;

            lblMachineId = new Label();
            lblMachineId.Text = "\nكود التفعيل: \n\n" + LicenseManager.GetMachineId();
            lblMachineId.Location = new System.Drawing.Point(30, 90);
            lblMachineId.AutoSize = true;
            lblMachineId.ForeColor = System.Drawing.Color.Gray;

            Button btnCopyMachineId = new Button();
            btnCopyMachineId.Text = "📋"; // يمكنك استخدام أيقونة أو نص مثل "نسخ"
            btnCopyMachineId.Location = new System.Drawing.Point(110, 100);
            btnCopyMachineId.Size = new System.Drawing.Size(30, 25); // حجم الزر
            btnCopyMachineId.Click += BtnCopyMachineId_Click; // ربط حدث النقر

            btnActivate = new Button();
            btnActivate.Text = "تفعيل";
            btnActivate.Location = new System.Drawing.Point(60, 160);
            btnActivate.Width = 100;
            btnActivate.Click += BtnActivate_Click;

            btnCancel = new Button();
            btnCancel.Text = "إلغاء";
            btnCancel.Location = new System.Drawing.Point(180, 160);
            btnCancel.Width = 100;
            btnCancel.Click += BtnCancel_Click;

            this.Controls.Add(btnCopyMachineId);
            this.Controls.Add(txtSerial);
            this.Controls.Add(lblMachineId);
            this.Controls.Add(lblInfo);
            this.Controls.Add(btnActivate);
            this.Controls.Add(btnCancel);
        }

        // حدث النقر على زر النسخ
        private void BtnCopyMachineId_Click(object sender, EventArgs e)
        {
            try
            {
                // نسخ معرف الجهاز إلى الحافظة
                Clipboard.SetText(LicenseManager.GetMachineId());
                MessageBox.Show("تم نسخ كود التفعيل إلى الحافظة!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء النسخ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnActivate_Click(object sender, EventArgs e)
        {
            string serial = txtSerial.Text.Trim();

            if (string.IsNullOrEmpty(serial))
            {
                MessageBox.Show("الرجاء إدخال رمز التفعيل", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (LicenseManager.ValidateSerial(serial))
            {
                LicenseManager.SaveLicense(serial);
                MessageBox.Show("تم تفعيل البرنامج بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                Application.Restart();
                this.Close();
            }
            else
            {
                MessageBox.Show("رمز التفعيل غير صالح أو منتهي الصلاحية", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        // استخراج معرف الجهاز من سيريال
        public static string ExtractMachineIdFromSerial(string serial)
        {
            try
            {
                string decrypted = LicenseManager.Decrypt(serial);
                string[] parts = decrypted.Split('|');
                if (parts.Length >= 1)
                {
                    return parts[0];
                }
            }
            catch
            {
                // فشل في فك تشفير السيريال
            }

            return string.Empty;
        }

        // استخراج تاريخ انتهاء الصلاحية من سيريال
        public static DateTime? ExtractExpiryDateFromSerial(string serial)
        {
            try
            {
                string decrypted = LicenseManager.Decrypt(serial);
                string[] parts = decrypted.Split('|');
                if (parts.Length >= 2)
                {
                    return DateTime.ParseExact(parts[1], "yyyy-MM-dd", null);
                }
            }
            catch
            {
                // فشل في فك تشفير السيريال
            }

            return null;
        }
    }

    public class LicenseManager
    {
        // مفتاح التشفير - يجب تغييره إلى مفتاح خاص بك
        private static readonly string EncryptionKey = "85C7D3E9A6B2F1084H5I6J7K8L9M0N1";

        // متغيرات قابلة للتعديل من خارج الفئة
        private static string _appName = "Auto_Sync";
        private static string _regPath = @"SOFTWARE\Auto_Sync\License";

        // خصائص للوصول إلى المتغيرات من خارج الفئة
        public static string AppName
        {
            get { return _appName; }
            set { _appName = value; }
        }

        public static string RegPath
        {
            get { return _regPath; }
            set { _regPath = value; }
        }

        public static bool ValidateSerial(string serial)
        {
            try
            {
                if (string.IsNullOrEmpty(serial))
                {
                    MessageBox.Show("الرجاء إدخال السيريال", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string decrypted = Decrypt(serial);
                string[] parts = decrypted.Split('|');

                if (parts.Length != 2)
                {
                    MessageBox.Show("صيغة السيريال غير صحيحة", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                string serialMachineId = parts[0];
                string currentMachineId = GetMachineId();

                if (string.IsNullOrEmpty(currentMachineId))
                {
                    MessageBox.Show("لم يتم العثور على معرف الجهاز", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (serialMachineId != currentMachineId)
                {
                    MessageBox.Show("معرف الجهاز غير متطابق", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (DateTime.TryParseExact(parts[1], "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime expiryDate))
                {
                    if (DateTime.Now > expiryDate)
                    {
                        MessageBox.Show($"السيريال منتهي الصلاحية. تاريخ الانتهاء: {expiryDate:yyyy-MM-dd}",
                                      "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                else
                {
                    MessageBox.Show("تاريخ انتهاء الصلاحية غير صحيح", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التحقق من السيريال: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static string GetCPUId()
        {
            try
            {
                string cpuId = string.Empty;
                ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
                scope.Connect();

                ObjectQuery query = new ObjectQuery("SELECT ProcessorId FROM Win32_Processor");
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        cpuId = obj["ProcessorId"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(cpuId))
                            break;
                    }
                }

                if (string.IsNullOrEmpty(cpuId))
                {
                    // إذا لم نجد ProcessorId، نحاول الحصول على معلومات أخرى عن المعالج
                    query = new ObjectQuery("SELECT Name, Manufacturer, MaxClockSpeed FROM Win32_Processor");
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            string name = obj["Name"]?.ToString() ?? "";
                            string manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                            string speed = obj["MaxClockSpeed"]?.ToString() ?? "";
                            cpuId = $"{manufacturer}_{name}_{speed}".Replace(" ", "");
                            if (!string.IsNullOrEmpty(cpuId))
                                break;
                        }
                    }
                }

                return cpuId;
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("ليس لديك صلاحيات كافية للوصول إلى معلومات المعالج. حاول تشغيل البرنامج كمسؤول.",
                               "خطأ في الصلاحيات", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            catch (Exception ex)
            {
                // محاولة استخدام طريقة بديلة
                try
                {
                    using (var proc = new Process())
                    {
                        proc.StartInfo.FileName = "wmic";
                        proc.StartInfo.Arguments = "cpu get ProcessorId";
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.CreateNoWindow = true;
                        proc.Start();

                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();

                        string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length >= 2)
                        {
                            return lines[1].Trim();
                        }
                    }
                }
                catch
                {
                    MessageBox.Show($"تعذر الحصول على معرف المعالج. الرجاء تشغيل البرنامج كمسؤول.\nتفاصيل الخطأ: {ex.Message}",
                                   "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return string.Empty;
            }
        }

        // تحديث GetMachineId لاستخدام GetCPUId فقط
        public static string GetMachineId()
        {
            string cpuId = GetCPUId();
            if (string.IsNullOrEmpty(cpuId))
            {
                MessageBox.Show("لم يتم العثور على معرف المعالج!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
            return cpuId;
        }

        private static string Encrypt(string plainText)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    // تحويل المفتاح إلى 32 بايت
                    byte[] keyBytes = new byte[32];
                    byte[] sourceBytes = Encoding.UTF8.GetBytes(EncryptionKey);
                    Array.Copy(sourceBytes, keyBytes, Math.Min(sourceBytes.Length, keyBytes.Length));

                    aes.Key = keyBytes;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // إنشاء IV عشوائي
                    aes.GenerateIV();
                    byte[] iv = aes.IV;

                    using (var memoryStream = new MemoryStream())
                    {
                        // كتابة الـ IV أولاً
                        memoryStream.Write(iv, 0, iv.Length);

                        using (var cryptoStream = new CryptoStream(
                            memoryStream,
                            aes.CreateEncryptor(),
                            CryptoStreamMode.Write))
                        {
                            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                            cryptoStream.FlushFinalBlock();
                        }

                        return Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في التشفير: {ex.Message}");
            }
        }

        public static string Decrypt(string cipherText)
        {
            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    byte[] keyBytes = new byte[32];
                    byte[] sourceBytes = Encoding.UTF8.GetBytes(EncryptionKey);
                    Array.Copy(sourceBytes, keyBytes, Math.Min(sourceBytes.Length, keyBytes.Length));

                    aes.Key = keyBytes;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // استخراج الـ IV من بداية النص المشفر
                    byte[] iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    // استخراج النص المشفر بدون الـ IV
                    byte[] cipher = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                    using (var memoryStream = new MemoryStream(cipher))
                    using (var cryptoStream = new CryptoStream(
                        memoryStream,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Read))
                    using (var streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في فك التشفير: {ex.Message}");
            }
        }

        public static string GenerateValidSerial(string machineId, DateTime expiryDate)
        {
            try
            {
                string plainText = $"{machineId}|{expiryDate:yyyy-MM-dd}";
                return Encrypt(plainText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في توليد السيريال: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        public static void SaveLicense(string serial)
        {
            try
            {
                Registry.CurrentUser.CreateSubKey(RegPath);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("LicenseKey", Encrypt(serial));
                    }
                }
                Registry.LocalMachine.CreateSubKey(RegPath);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("LicenseKey", Encrypt(serial));
                    }
                }
                Registry.Users.CreateSubKey(RegPath);
                using (RegistryKey key = Registry.Users.OpenSubKey(RegPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("LicenseKey", Encrypt(serial));
                    }
                }
                Registry.ClassesRoot.CreateSubKey(RegPath);
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(RegPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("LicenseKey", Encrypt(serial));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ الترخيص: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string LoadLicense()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath))
                {
                    if (key != null)
                    {
                        string encryptedSerial = key.GetValue("LicenseKey") as string;
                        if (!string.IsNullOrEmpty(encryptedSerial))
                        {
                            return Decrypt(encryptedSerial);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل الترخيص: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return string.Empty;
        }

        public static int GetRemainingDays()
        {
            try
            {
                string serial = LoadLicense(); // استخدم الدالة الموجودة لتحميل السيريال
                if (string.IsNullOrEmpty(serial))
                    return 0;

                string decrypted = Decrypt(serial);
                string[] parts = decrypted.Split('|');

                if (parts.Length != 2)
                    return 0;

                if (DateTime.TryParseExact(parts[1], "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out DateTime expiryDate))
                {
                    int remainingDays = (expiryDate - DateTime.Now.Date).Days;
                    return Math.Max(0, remainingDays); // لا يُظهر أرقام سالبة
                }
            }
            catch
            {
                // تجاهل الأخطاء وإرجاع 0
            }

            return 0;
        }

        public static void SaveLicenseExpiry(string serial)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath, true))
                {
                    if (key != null)
                    {
                        // تشفير وحفظ السيريال
                        key.SetValue("LicenseKey", Encrypt(serial));

                        // فك تشفير السيريال للحصول على تاريخ الانتهاء
                        string decrypted = Decrypt(serial);
                        string[] parts = decrypted.Split('|');

                        if (parts.Length == 2 &&
                            DateTime.TryParseExact(parts[1], "yyyy-MM-dd", null,
                            System.Globalization.DateTimeStyles.None, out DateTime expiryDate))
                        {
                            key.SetValue("LicenseExpiry", expiryDate.ToString("yyyy-MM-dd"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ الترخيص: {ex.Message}",
                              "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static DateTime? GetExpiryDateFromSavedLicense()
        {
            try
            {
                string serial = LoadLicense();
                if (!string.IsNullOrEmpty(serial))
                {
                    string decrypted = Decrypt(serial);
                    string[] parts = decrypted.Split('|');

                    if (parts.Length == 2 &&
                        DateTime.TryParseExact(parts[1], "yyyy-MM-dd", null,
                        System.Globalization.DateTimeStyles.None, out DateTime expiryDate))
                    {
                        return expiryDate;
                    }
                }
            }
            catch
            {
                // تجاهل الأخطاء
            }

            return null;
        }

    }

}