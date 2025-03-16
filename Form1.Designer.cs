namespace Auto_Sync
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            menuStrip1 = new MenuStrip();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            تشغيلمعبدءالويندوزToolStripMenuItem = new ToolStripMenuItem();
            forceCloseToolStripMenuItem = new ToolStripMenuItem();
            إدارةToolStripMenuItem = new ToolStripMenuItem();
            تغييركلمةالسرالفرعيةToolStripMenuItem = new ToolStripMenuItem();
            button3 = new Button();
            button2 = new Button();
            listBox1 = new ListBox();
            textBox2 = new TextBox();
            textBox1 = new TextBox();
            label2 = new Label();
            label1 = new Label();
            button1 = new Button();
            notifyIcon1 = new NotifyIcon(components);
            textBox3 = new TextBox();
            label3 = new Label();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { optionsToolStripMenuItem, forceCloseToolStripMenuItem, إدارةToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(574, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { تشغيلمعبدءالويندوزToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(65, 20);
            optionsToolStripMenuItem.Text = "الإعدادات";
            // 
            // تشغيلمعبدءالويندوزToolStripMenuItem
            // 
            تشغيلمعبدءالويندوزToolStripMenuItem.CheckOnClick = true;
            تشغيلمعبدءالويندوزToolStripMenuItem.Name = "تشغيلمعبدءالويندوزToolStripMenuItem";
            تشغيلمعبدءالويندوزToolStripMenuItem.Size = new Size(184, 22);
            تشغيلمعبدءالويندوزToolStripMenuItem.Text = "تشغيل مع بدء الويندوز";
            تشغيلمعبدءالويندوزToolStripMenuItem.Click += تشغيلمعبدءالويندوزToolStripMenuItem_Click;
            // 
            // forceCloseToolStripMenuItem
            // 
            forceCloseToolStripMenuItem.Name = "forceCloseToolStripMenuItem";
            forceCloseToolStripMenuItem.Size = new Size(72, 20);
            forceCloseToolStripMenuItem.Text = "غلق إجباري";
            forceCloseToolStripMenuItem.Click += forceCloseToolStripMenuItem_Click;
            // 
            // إدارةToolStripMenuItem
            // 
            إدارةToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { تغييركلمةالسرالفرعيةToolStripMenuItem });
            إدارةToolStripMenuItem.Name = "إدارةToolStripMenuItem";
            إدارةToolStripMenuItem.Size = new Size(41, 20);
            إدارةToolStripMenuItem.Text = "إدارة";
            // 
            // تغييركلمةالسرالفرعيةToolStripMenuItem
            // 
            تغييركلمةالسرالفرعيةToolStripMenuItem.Enabled = false;
            تغييركلمةالسرالفرعيةToolStripMenuItem.Name = "تغييركلمةالسرالفرعيةToolStripMenuItem";
            تغييركلمةالسرالفرعيةToolStripMenuItem.Size = new Size(148, 22);
            تغييركلمةالسرالفرعيةToolStripMenuItem.Text = "تغيير كلمة السر";
            // 
            // button3
            // 
            button3.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            button3.Location = new Point(493, 84);
            button3.Name = "button3";
            button3.Size = new Size(35, 23);
            button3.TabIndex = 13;
            button3.Text = "2";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button2
            // 
            button2.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            button2.Location = new Point(493, 48);
            button2.Name = "button2";
            button2.Size = new Size(35, 23);
            button2.TabIndex = 14;
            button2.Text = "1";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new Point(27, 128);
            listBox1.Name = "listBox1";
            listBox1.RightToLeft = RightToLeft.Yes;
            listBox1.Size = new Size(520, 319);
            listBox1.TabIndex = 12;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(160, 84);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(327, 23);
            textBox2.TabIndex = 11;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(160, 48);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(327, 23);
            textBox1.TabIndex = 10;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(27, 87);
            label2.Name = "label2";
            label2.Size = new Size(117, 15);
            label2.TabIndex = 9;
            label2.Text = "مسار المجلد الاحتياطي";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(37, 52);
            label1.Name = "label1";
            label1.Size = new Size(105, 15);
            label1.TabIndex = 8;
            label1.Text = "مسار المجلد الرئيسي";
            // 
            // button1
            // 
            button1.Location = new Point(232, 479);
            button1.Name = "button1";
            button1.Size = new Size(104, 49);
            button1.TabIndex = 7;
            button1.Text = "بدء المراقبة";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "notifyIcon1";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseDoubleClick += NotifyIcon1_MouseDoubleClick;
            // 
            // textBox3
            // 
            textBox3.BackColor = Color.WhiteSmoke;
            textBox3.BorderStyle = BorderStyle.None;
            textBox3.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            textBox3.Location = new Point(17, 530);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(111, 18);
            textBox3.TabIndex = 15;
            textBox3.Text = "+2) 01274096624";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(406, 527);
            label3.MaximumSize = new Size(150, 15);
            label3.MinimumSize = new Size(150, 15);
            label3.Name = "label3";
            label3.RightToLeft = RightToLeft.Yes;
            label3.Size = new Size(150, 15);
            label3.TabIndex = 9;
            label3.TextChanged += label3_TextChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(574, 560);
            Controls.Add(textBox3);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(listBox1);
            Controls.Add(textBox2);
            Controls.Add(textBox1);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button1);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MaximumSize = new Size(590, 599);
            MinimumSize = new Size(590, 599);
            Name = "Form1";
            Text = "Auto Sync Juba";
            Load += Form1_Load_1;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem تشغيلمعبدءالويندوزToolStripMenuItem;
        private ToolStripMenuItem forceCloseToolStripMenuItem;
        private Button button3;
        private Button button2;
        private ListBox listBox1;
        private TextBox textBox2;
        private TextBox textBox1;
        private Label label2;
        private Label label1;
        private Button button1;
        private NotifyIcon notifyIcon1;
        private TextBox textBox3;
        private ToolStripMenuItem إدارةToolStripMenuItem;
        private ToolStripMenuItem تغييركلمةالسرالفرعيةToolStripMenuItem;
        private Label label3;
    }
}
