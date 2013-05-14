using System;
using System.Drawing;
using System.Xml;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Resources;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using VanillaLib;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Collections.Generic;


namespace Vanilla
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class CrawlerForm : System.Windows.Forms.Form
    {
        private Hashtable DictUrls;
        // unique Uri's queue
        private Queue queueURLS;
        // thread that take the browse editor text to parse it
        private Thread threadParse;
        private Thread threadIndex;
        // binary tree to keep unique Uri's
        private SortTree urlStorage;
        private Hashtable DictUrlCnts;
        private IndexHTML htmlIndexer;
        private SearchUrls LPRecommander;

        #region Beta
        private SortedList<string, Dictionary<string, int>> mapLnkTxt;
        #endregion

        private static readonly object mutex = new object();

        // download folder
        private string strDownloadfolder;
        private string Downloadfolder
        {
            get { return strDownloadfolder; }
            set
            {
                strDownloadfolder = value;
                strDownloadfolder = strDownloadfolder.TrimEnd('\\');
            }
        }

        // threads array
        private Thread[] threadsRun;
        // number of running threads
        private int nThreadCount;
        private int ThreadCount
        {
            get { return nThreadCount; }
            set
            {
                Monitor.Enter(mutex);
                try
                {
                    for (int nIndex = 0; nIndex < value; nIndex++)
                    {
                        // check if thread not created or not suspended
                        if (threadsRun[nIndex] == null || threadsRun[nIndex].ThreadState != ThreadState.Suspended)
                        {
                            // create new thread
                            threadsRun[nIndex] = new Thread(new ThreadStart(ThreadRunFunction));
                            // set thread name equal to its index
                            threadsRun[nIndex].Name = nIndex.ToString();
                            // start thread working function
                            threadsRun[nIndex].Start();
                            // check if thread dosn't added to the view							
                        }
                    }
                    // change thread value
                    nThreadCount = value;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                Monitor.Exit(mutex);
            }
        }

        #region Member
        // MIME types string
        private string strMIMETypes;

        // encoding text that includes all settings types in one string
        private Encoding encoding;

        // timeout of sockets send and receive
        private int nRequestTimeout;

        // the time that each thread sleeps when the refs queue empty
        private int nSleepFetchTime;

        // the number of requests to keep in the requests view for review requests details
        // private int nLastRequestCount;

        // the time that each thread sleep after handling any request, 
        // which is very important value to prevent Hosts from blocking the crawler due to heavy load
        private int nSleepConnectTime;

        // represents the depth of navigation in the crawling process
        private int nWebDepth;

        // MIME types are the types that are supported to be downloaded by the crawler 
        // and the crawler includes a default types to be used. 
        private bool bAllMIMETypes;

        // to limit crawling process to the same host of the original URL
        private bool bKeepSameServer;

        // means keep socket connection opened for subsequent requests to avoid reconnect time
        private bool bKeepAlive;

        // flag to be used to stop all running threads when user request to stop
        bool ThreadsRunning;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.MenuItem menuItemAbout;
        private System.Windows.Forms.ToolBarButton toolBarButton4;
        private System.Windows.Forms.Button buttonGo;
        private TextBox textBoxWeb;
        private Label label1;
        private Button buttonIndex;
        private Button buttonRecommander;
        private TextBox textBoxKeyWord;
        private Label label2;
        private ComboBox comboBoxURL;
        private Label label3;
        private Button buttonBrowse;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private TextBox textBoxURL;
        private Label label4;
        private PictureBox pictureBox_Crawler;
        private PictureBox pictureBox_Index;
        private Button buttonImport;
        private Button buttonExport;
        private SaveFileDialog saveFileDialog;
        private OpenFileDialog openFileDialog;
        private TextBox textBoxFilePath;
        private PictureBox pictureBox_Editor;
        private ErrorProvider errorProviderWebSite;
        private ErrorProvider errorProviderIndex;
        private ErrorProvider errorProviderRecommend;
        private ErrorProvider errorProviderImport;
        private ToolTip toolTip1;
        private ToolTip toolTip2;
        private ToolTip toolTip3;
        private System.Windows.Forms.MenuItem menuItem5;

        #endregion

        public CrawlerForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            this.htmlIndexer = new IndexHTML();
            this.htmlIndexer.OnShowDocPath += new IndexHTML.ShowDocPath(htmlIndexer_OnShowDocPath);


            this.urlStorage = new SortTree();
            this.threadsRun = new Thread[200];
            this.queueURLS = new Queue();

            this.strMIMETypes = GetMIMETypes();
            this.encoding = GetTextEncoding();

            DictUrls = new Hashtable();
            DictUrlCnts = new Hashtable();
            mapLnkTxt = new System.Collections.Generic.SortedList<string, Dictionary<string, int>>();

            errorProviderWebSite.Clear();
            errorProviderIndex.Clear();
            errorProviderRecommend.Clear();
            errorProviderImport.Clear();
        }

        void htmlIndexer_OnShowDocPath(string docPath)
        {
            this.textBoxURL.Text = Path.GetFileName(docPath);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            //this.StopParsing();

            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);

            System.Environment.Exit(0);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CrawlerForm));
            this.menuItemAbout = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.toolBarButton4 = new System.Windows.Forms.ToolBarButton();
            this.buttonGo = new System.Windows.Forms.Button();
            this.textBoxWeb = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonIndex = new System.Windows.Forms.Button();
            this.buttonRecommander = new System.Windows.Forms.Button();
            this.textBoxKeyWord = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxURL = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox_Crawler = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pictureBox_Index = new System.Windows.Forms.PictureBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxURL = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.pictureBox_Editor = new System.Windows.Forms.PictureBox();
            this.textBoxFilePath = new System.Windows.Forms.TextBox();
            this.buttonImport = new System.Windows.Forms.Button();
            this.buttonExport = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.errorProviderWebSite = new System.Windows.Forms.ErrorProvider(this.components);
            this.errorProviderIndex = new System.Windows.Forms.ErrorProvider(this.components);
            this.errorProviderRecommend = new System.Windows.Forms.ErrorProvider(this.components);
            this.errorProviderImport = new System.Windows.Forms.ErrorProvider(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip2 = new System.Windows.Forms.ToolTip(this.components);
            this.toolTip3 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Crawler)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Index)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Editor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderWebSite)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderIndex)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderRecommend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderImport)).BeginInit();
            this.SuspendLayout();
            // 
            // menuItemAbout
            // 
            this.menuItemAbout.Index = -1;
            this.menuItemAbout.Text = "";
            // 
            // menuItem5
            // 
            this.menuItem5.Index = -1;
            this.menuItem5.Text = "-";
            // 
            // toolBarButton4
            // 
            this.toolBarButton4.Name = "toolBarButton4";
            this.toolBarButton4.Text = "Go";
            // 
            // buttonGo
            // 
            this.buttonGo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonGo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonGo.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonGo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonGo.Location = new System.Drawing.Point(297, 18);
            this.buttonGo.Name = "buttonGo";
            this.buttonGo.Size = new System.Drawing.Size(90, 33);
            this.buttonGo.TabIndex = 10;
            this.buttonGo.Text = "网站分析";
            this.buttonGo.UseVisualStyleBackColor = true;
            this.buttonGo.Click += new System.EventHandler(this.buttonGo_Click);
            // 
            // textBoxWeb
            // 
            this.textBoxWeb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxWeb.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBoxWeb.Location = new System.Drawing.Point(49, 24);
            this.textBoxWeb.Name = "textBoxWeb";
            this.textBoxWeb.Size = new System.Drawing.Size(226, 23);
            this.textBoxWeb.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(8, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(40, 20);
            this.label1.TabIndex = 12;
            this.label1.Text = "网址:";
            // 
            // buttonIndex
            // 
            this.buttonIndex.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonIndex.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonIndex.Location = new System.Drawing.Point(296, 20);
            this.buttonIndex.Name = "buttonIndex";
            this.buttonIndex.Size = new System.Drawing.Size(90, 33);
            this.buttonIndex.TabIndex = 13;
            this.buttonIndex.Text = "网页分析";
            this.buttonIndex.UseVisualStyleBackColor = true;
            this.buttonIndex.Click += new System.EventHandler(this.buttonIndex_Click);
            // 
            // buttonRecommander
            // 
            this.buttonRecommander.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonRecommander.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonRecommander.Location = new System.Drawing.Point(226, 17);
            this.buttonRecommander.Name = "buttonRecommander";
            this.buttonRecommander.Size = new System.Drawing.Size(90, 33);
            this.buttonRecommander.TabIndex = 14;
            this.buttonRecommander.Text = "推荐";
            this.buttonRecommander.UseVisualStyleBackColor = true;
            this.buttonRecommander.Click += new System.EventHandler(this.buttonRecommander_Click);
            // 
            // textBoxKeyWord
            // 
            this.textBoxKeyWord.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxKeyWord.Location = new System.Drawing.Point(61, 24);
            this.textBoxKeyWord.Name = "textBoxKeyWord";
            this.textBoxKeyWord.Size = new System.Drawing.Size(146, 22);
            this.textBoxKeyWord.TabIndex = 15;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft YaHei", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(8, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 20);
            this.label2.TabIndex = 16;
            this.label2.Text = "关键词:";
            // 
            // comboBoxURL
            // 
            this.comboBoxURL.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxURL.FormattingEnabled = true;
            this.comboBoxURL.Location = new System.Drawing.Point(80, 62);
            this.comboBoxURL.Name = "comboBoxURL";
            this.comboBoxURL.Size = new System.Drawing.Size(308, 22);
            this.comboBoxURL.TabIndex = 17;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft YaHei", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(6, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 20);
            this.label3.TabIndex = 18;
            this.label3.Text = "目标网址:";
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonBrowse.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonBrowse.Location = new System.Drawing.Point(338, 17);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(90, 33);
            this.buttonBrowse.TabIndex = 19;
            this.buttonBrowse.Text = "访问";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.groupBox1.Controls.Add(this.pictureBox_Crawler);
            this.groupBox1.Controls.Add(this.textBoxWeb);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.buttonGo);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox1.Location = new System.Drawing.Point(6, 9);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(434, 61);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            // 
            // pictureBox_Crawler
            // 
            this.pictureBox_Crawler.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox_Crawler.Image")));
            this.pictureBox_Crawler.Location = new System.Drawing.Point(393, 18);
            this.pictureBox_Crawler.Name = "pictureBox_Crawler";
            this.pictureBox_Crawler.Size = new System.Drawing.Size(35, 32);
            this.pictureBox_Crawler.TabIndex = 13;
            this.pictureBox_Crawler.TabStop = false;
            this.toolTip1.SetToolTip(this.pictureBox_Crawler, "网页抓取中...");
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pictureBox_Index);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.textBoxURL);
            this.groupBox2.Controls.Add(this.buttonIndex);
            this.groupBox2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox2.Location = new System.Drawing.Point(6, 70);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(434, 61);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            // 
            // pictureBox_Index
            // 
            this.pictureBox_Index.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox_Index.Image")));
            this.pictureBox_Index.Location = new System.Drawing.Point(398, 22);
            this.pictureBox_Index.Name = "pictureBox_Index";
            this.pictureBox_Index.Size = new System.Drawing.Size(26, 27);
            this.pictureBox_Index.TabIndex = 16;
            this.pictureBox_Index.TabStop = false;
            this.toolTip2.SetToolTip(this.pictureBox_Index, "索引建立中...");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft YaHei", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(8, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 20);
            this.label4.TabIndex = 15;
            this.label4.Text = "网页:";
            // 
            // textBoxURL
            // 
            this.textBoxURL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxURL.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBoxURL.Location = new System.Drawing.Point(49, 26);
            this.textBoxURL.Name = "textBoxURL";
            this.textBoxURL.ReadOnly = true;
            this.textBoxURL.Size = new System.Drawing.Size(226, 23);
            this.textBoxURL.TabIndex = 14;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.pictureBox_Editor);
            this.groupBox3.Controls.Add(this.textBoxFilePath);
            this.groupBox3.Controls.Add(this.buttonImport);
            this.groupBox3.Controls.Add(this.buttonExport);
            this.groupBox3.Controls.Add(this.buttonRecommander);
            this.groupBox3.Controls.Add(this.textBoxKeyWord);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.buttonBrowse);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.comboBoxURL);
            this.groupBox3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox3.Location = new System.Drawing.Point(6, 138);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(434, 137);
            this.groupBox3.TabIndex = 22;
            this.groupBox3.TabStop = false;
            // 
            // pictureBox_Editor
            // 
            this.pictureBox_Editor.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox_Editor.Image")));
            this.pictureBox_Editor.InitialImage = null;
            this.pictureBox_Editor.Location = new System.Drawing.Point(394, 56);
            this.pictureBox_Editor.Name = "pictureBox_Editor";
            this.pictureBox_Editor.Size = new System.Drawing.Size(34, 34);
            this.pictureBox_Editor.TabIndex = 23;
            this.pictureBox_Editor.TabStop = false;
            this.toolTip3.SetToolTip(this.pictureBox_Editor, "推荐URL生成中...");
            // 
            // textBoxFilePath
            // 
            this.textBoxFilePath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxFilePath.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBoxFilePath.Location = new System.Drawing.Point(11, 103);
            this.textBoxFilePath.Name = "textBoxFilePath";
            this.textBoxFilePath.ReadOnly = true;
            this.textBoxFilePath.Size = new System.Drawing.Size(196, 23);
            this.textBoxFilePath.TabIndex = 22;
            // 
            // buttonImport
            // 
            this.buttonImport.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonImport.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonImport.Location = new System.Drawing.Point(226, 97);
            this.buttonImport.Name = "buttonImport";
            this.buttonImport.Size = new System.Drawing.Size(98, 33);
            this.buttonImport.TabIndex = 21;
            this.buttonImport.Text = "导入关键词列表";
            this.buttonImport.UseVisualStyleBackColor = true;
            this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
            // 
            // buttonExport
            // 
            this.buttonExport.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonExport.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonExport.Location = new System.Drawing.Point(330, 97);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(98, 33);
            this.buttonExport.TabIndex = 20;
            this.buttonExport.Text = "批量生成URLs";
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.buttonExport_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog";
            // 
            // errorProviderWebSite
            // 
            this.errorProviderWebSite.ContainerControl = this;
            // 
            // errorProviderIndex
            // 
            this.errorProviderIndex.ContainerControl = this;
            // 
            // errorProviderRecommend
            // 
            this.errorProviderRecommend.ContainerControl = this;
            // 
            // errorProviderImport
            // 
            this.errorProviderImport.ContainerControl = this;
            // 
            // CrawlerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(7, 15);
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(444, 291);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.Navy;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(460, 330);
            this.MinimumSize = new System.Drawing.Size(460, 330);
            this.Name = "CrawlerForm";
            this.Text = "Landing Page Recommender(Beta)";
            this.Load += new System.EventHandler(this.CrawlerForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Crawler)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Index)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Editor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderWebSite)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderIndex)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderRecommend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProviderImport)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new CrawlerForm());
        }

        private void CrawlerForm_Load(object sender, System.EventArgs e)
        {
            InitValues();
            this.pictureBox_Crawler.Enabled = false;
            this.pictureBox_Crawler.Visible = false;

            this.pictureBox_Index.Enabled = false;
            this.pictureBox_Index.Visible = false;

            pictureBox_Editor.Enabled = false;
            pictureBox_Editor.Visible = false;
        }

        [DllImport("wininet")]
        public static extern int InternetGetConnectedState(ref int lpdwFlags, int dwReserved);
        [DllImport("wininet")]
        public static extern int InternetAutodial(int dwFlags, int hwndParent);
        int nFirstTimeCheckConnection = 0;
        string InternetGetConnectedStateString()
        {
            string strState = "";
            try
            {
                int nState = 0;
                // check internet connection state
                if (InternetGetConnectedState(ref nState, 0) == 0)
                    return "You are currently not connected to the internet";
                if ((nState & 1) == 1)
                    strState = "Modem connection";
                else if ((nState & 2) == 2)
                    strState = "LAN connection";
                else if ((nState & 4) == 4)
                    strState = "Proxy connection";
                else if ((nState & 8) == 8)
                    strState = "Modem is busy with a non-Internet connection";
                else if ((nState & 0x10) == 0x10)
                    strState = "Remote Access Server is installed";
                else if ((nState & 0x20) == 0x20)
                    return "Offline";
                else if ((nState & 0x40) == 0x40)
                    return "Internet connection is currently configured";
            }
            catch
            {
                throw;
            }
            return strState;
        }
        void ConnectionInfo()
        {
            try
            {
                int nState = 0;
                if (InternetGetConnectedState(ref nState, 0) == 0)
                {
                    if (nFirstTimeCheckConnection++ == 0)
                        // ask for dial up or DSL connection
                        if (InternetAutodial(1, 0) != 0)
                            // check internet connection state again
                            InternetGetConnectedState(ref nState, 0);
                }
                if ((nState & 2) == 2 || (nState & 4) == 4)
                    // reset to reask for connection agina
                    nFirstTimeCheckConnection = 0;
            }
            catch
            {
                throw;
            }

        }

        private void menuItemExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        string[] ExcludeHosts;
        string[] ExcludeWords;
        string[] ExcludeFiles;

        void InitValues()
        {
            nWebDepth = 3;
            nRequestTimeout = 10;
            nSleepFetchTime = 1;
            nSleepConnectTime = 1;
            bKeepSameServer = true;
            bAllMIMETypes = false;
            bKeepAlive = true;
            ExcludeHosts = new string[] { ".org", ".gov" };
            ExcludeWords = new string[] { "" };// Settings.GetValue("Exclude words", "").Split(';');
            ExcludeFiles = new string[] { "" };// Settings.GetValue("Exclude files", "").Replace("*", "").ToLower().Split(';');
            //nLastRequestCount = 20;
            Downloadfolder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Vanilla");
            strMIMETypes = GetMIMETypes();
            encoding = GetTextEncoding();
        }

        private Encoding GetTextEncoding()
        {
            return Encoding.Default;
        }

        // construct MIME types string from settings xml file
        private string GetMIMETypes()
        {
            string str = "";
            // check for settings xml file existence
            if (File.Exists(Application.StartupPath + "\\Settings.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(Application.StartupPath + "\\Settings.xml");
                XmlNode element = doc.DocumentElement.SelectSingleNode("SettingsForm-listViewFileMatches");
                if (element != null)
                {
                    for (int n = 0; n < element.ChildNodes.Count; n++)
                    {
                        XmlNode xmlnode = element.ChildNodes[n];
                        XmlAttribute attribute = xmlnode.Attributes["Checked"];
                        if (attribute == null || attribute.Value.ToLower() != "true")
                            continue;
                        string[] items = xmlnode.InnerText.Split('\t');
                        if (items.Length > 1)
                        {
                            str += items[0];
                            if (items.Length > 2)
                                str += '[' + items[1] + ',' + items[2] + ']';
                            str += ';';
                        }
                    }
                }
            }
            return str;
        }

        private void buttonGo_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxWeb.Text))
            {
                errorProviderWebSite.SetError(textBoxWeb, "请输入有效的网址,例如 http://www.baidu.com");
                return;
            }
            else
            {
                errorProviderWebSite.SetError(textBoxWeb, string.Empty);
            }

            this.pictureBox_Crawler.Visible = true;
            this.pictureBox_Crawler.Enabled = true;
            StartParsing();
        }

        private void buttonIndex_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxWeb.Text))
            {
                //Validation failed, so set an appropriate error message
                errorProviderIndex.SetError(textBoxURL, "请在网址区输入待索引的网站地址");
                return;
            }
            else
            {
                //Clear previous error message
                errorProviderIndex.SetError(textBoxURL, string.Empty);
            }

            this.pictureBox_Index.Visible = true;
            this.pictureBox_Index.Enabled = true;

            //htmlIndexer.StartIndexing(Downloadfolder, new MyUri(textBoxWeb.Text).Host);
            threadIndex = new Thread(new ThreadStart(IndexThreadMethod));
            threadIndex.IsBackground = true;
            threadIndex.Priority = ThreadPriority.BelowNormal;
            threadIndex.Start();

            buttonIndex.Enabled = false;
        }

        private void IndexThreadMethod()
        {
            CheckForIllegalCrossThreadCalls = false;
            htmlIndexer.StartIndexing(Downloadfolder, new MyUri(textBoxWeb.Text).Host);

            this.pictureBox_Index.Enabled = false;
            this.pictureBox_Index.Visible = false;
            this.buttonIndex.Enabled = true;
        }

        private void StartParsing()
        {
            this.buttonGo.Enabled = false;
            this.textBoxWeb.ReadOnly = true;

            if (threadParse == null || threadParse.ThreadState != ThreadState.Suspended)
            {
                this.urlStorage.Clear();
                // start parsing thread                
                threadParse = new Thread(new ThreadStart(RunParser));
                threadParse.IsBackground = true;
                threadParse.Priority = ThreadPriority.BelowNormal;

                threadParse.Start();
            }
            // 线程数暂设为16个
            ThreadCount = 16;
        }

        private void ThreadRunFunction()
        {
            MyWebRequest request = null;
            int threadSleepFetchTime = nSleepFetchTime;
            while (ThreadsRunning && int.Parse(Thread.CurrentThread.Name) < this.ThreadCount)
            {
                MyUri uri = DequeueUri();
                if (uri != null)
                {
                    if (nSleepConnectTime > 0)
                        Thread.Sleep((nSleepConnectTime * 1000) / 5);
                    ParseUri(uri, ref request);
                    threadSleepFetchTime = nSleepFetchTime;
                }
                else
                {
                    Thread.Sleep(threadSleepFetchTime * 1000);
                    threadSleepFetchTime *= 2;
                    if (threadSleepFetchTime > 32)
                        return;
                }
            }
        }

        // push uri to the queue
        private bool EnqueueUri(MyUri uri, bool bCheckRepetition)
        {
            // add the uri to the binary tree to check if it is duplicated or not
            if (bCheckRepetition == true && AddURL(ref uri) == false)
                return false;

            Monitor.Enter(queueURLS);
            try
            {
                queueURLS.Enqueue(uri);
            }
            catch (Exception)
            {
                throw;
            }
            Monitor.Exit(queueURLS);

            return true;
        }

        // pop uri from the queue
        private MyUri DequeueUri()
        {
            Monitor.Enter(queueURLS);
            MyUri uri = null;
            try
            {
                if (queueURLS.Count > 0)
                    uri = (MyUri)queueURLS.Dequeue();
            }
            catch (Exception)
            {
                throw;
            }
            Monitor.Exit(queueURLS);
            return uri;
        }

        private string GetMD5Code(string Str)
        {
            MD5CryptoServiceProvider MD5CSP = new MD5CryptoServiceProvider();
            Byte[] bHashTable = MD5CSP.ComputeHash(System.Text.Encoding.Unicode.GetBytes(Str));
            return System.BitConverter.ToString(bHashTable).Replace("-", "");
        }

        private void RunParser()
        {
            CheckForIllegalCrossThreadCalls = false;
            ThreadsRunning = true;
            try
            {
                string strUri = this.textBoxWeb.Text.Trim();

                if (Directory.Exists(strUri) == true)
                    ParseFolder(strUri, 0);
                else
                {
                    if (File.Exists(strUri) == false)
                    {
                        Normalize(ref strUri);
                        this.textBoxWeb.Text = strUri;
                    }
                    MyUri uri = new MyUri(strUri);
                    this.EnqueueUri(uri, false);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            for (int i = 0; i < 16; ++i)
            {
                threadsRun[i].Join();
            }

            // 网页URL及本地文件名映射关系
            MyUri _uri = new MyUri(this.textBoxWeb.Text.Trim());
            FileStream fs = new FileStream(this.Downloadfolder + "\\" + _uri.Host + "_DB", FileMode.Create);
            BinaryFormatter binFmt = new BinaryFormatter();
            binFmt.Serialize(fs, DictUrls);
            fs.Close();

            //URL对应的网页中所含的超级链接数
            FileStream fsLnk = new FileStream(this.Downloadfolder + "\\" + _uri.Host + "_Link", FileMode.Create);
            BinaryFormatter binFmtLnk = new BinaryFormatter();
            binFmtLnk.Serialize(fsLnk, this.DictUrlCnts);
            fsLnk.Close();

            #region 网页内容预处理(同目录下，去除各个网页均包含的内容)
            //链接文字描述短语
            FileStream fsLnkDesc = new FileStream(this.Downloadfolder + "\\" + _uri.Host + "_LinkDesc", FileMode.Create);
            BinaryFormatter binFmtLnkDesc = new BinaryFormatter();
            binFmtLnkDesc.Serialize(fsLnkDesc, mapLnkTxt);
            fsLnkDesc.Close();
            #endregion

            this.pictureBox_Crawler.Visible = false;
            this.pictureBox_Crawler.Enabled = false;
            this.textBoxWeb.Enabled = true;
            buttonGo.Enabled = true;
        }

        private void Normalize(ref string strURL)
        {
            if (strURL.StartsWith("http://") == false)
                strURL = "http://" + strURL;
            if (strURL.IndexOf("/", 8) == -1)
                strURL += '/';
        }

        bool AddURL(ref MyUri uri)
        {
            foreach (string str in ExcludeHosts)
            {
                if (str.Trim().Length > 0 && uri.Host.ToLower().IndexOf(str.Trim()) != -1)
                {
                    return false;
                }
            }
            Monitor.Enter(urlStorage);
            bool bNew = false;
            try
            {
                string strURL = uri.AbsoluteUri;
                bNew = urlStorage.Add(ref strURL).Count == 1;
            }
            catch (Exception)
            {
                throw;
            }
            Monitor.Exit(urlStorage);
            return bNew;
        }


        private void ParseUri(MyUri uri, ref MyWebRequest request)
        {
            string strStatus = string.Empty;

            // check if connection is kept alive from previous connections or not
            if (request != null && request.response.KeepAlive)
                strStatus += "Connection live to: " + uri.Host + "\r\n\r\n";
            else
                strStatus += "Connecting: " + uri.Host + "\r\n\r\n";

            try
            {
                // create web request
                request = MyWebRequest.Create(uri, request, bKeepAlive);
                // set request timeout
                request.Timeout = nRequestTimeout * 1000;
                // retrieve response from web request
                MyWebResponse response = request.GetResponse();
                // update status text with the request and response headers
                strStatus += request.Header + response.Header;

                // check for redirection
                if (response.ResponseUri.Equals(uri) == false)
                {
                    // add the new uri to the queue
                    this.EnqueueUri(new MyUri(response.ResponseUri.AbsoluteUri), true);

                    // update status
                    strStatus += "Redirected to: " + response.ResponseUri + "\r\n";
                    // log current uri status
                    // reset current request to avoid response socket opening case
                    request = null;
                    return;
                }

                // check for allowed MIME types
                if (bAllMIMETypes == false && response.ContentType != null && strMIMETypes.Length > 0)
                {
                    string strContentType = response.ContentType.ToLower();
                    int nExtIndex = strContentType.IndexOf(';');
                    if (nExtIndex != -1)
                        strContentType = strContentType.Substring(0, nExtIndex);
                    if (strContentType.IndexOf('*') == -1 && (nExtIndex = strMIMETypes.IndexOf(strContentType)) == -1)
                    {
                        request = null;
                        return;
                    }
                    // find numbers
                    Match match = new Regex(@"\d+").Match(strMIMETypes, nExtIndex);
                    int nMin = int.Parse(match.Value) * 1024;
                    match = match.NextMatch();
                    int nMax = int.Parse(match.Value) * 1024;
                    if (nMin < nMax && (response.ContentLength < nMin || response.ContentLength > nMax))
                    {
                        request = null;
                        return;
                    }
                }

                // check for response extention
                string[] ExtArray = { ".gif", ".jpg", ".css", ".zip", ".exe", ".doc", ".txt", ".wmv", ".mpg", ".rar" };
                bool bParse = true;
                foreach (string ext in ExtArray)
                    if (uri.AbsoluteUri.ToLower().EndsWith(ext) == true)
                    {
                        bParse = false;
                        break;
                    }
                //???
                foreach (string ext in ExcludeFiles)
                    if (ext.Trim().Length > 0 && uri.AbsoluteUri.ToLower().EndsWith(ext) == true)
                    {
                        bParse = false;
                        break;
                    }

                //Ugly Code
                if (uri.AbsoluteUri.ToLower().Contains("/bbs/") 
                    || uri.AbsoluteUri.ToLower().Contains("/about/")
                    || uri.AbsoluteUri.ToLower().Contains("/news/")
                    || uri.AbsoluteUri.ToLower().Contains("/download/"))
                    bParse = false;

                // construct path in the hard disk
                string strLocalPath = uri.LocalPath;
                // check if the path ends with / to can crate the file on the HD 
                if (strLocalPath.EndsWith("/") == true)
                    // check if there is no query like (.asp?i=32&j=212)
                    if (uri.Query == "")
                        // add a default name for / ended pathes
                        strLocalPath += "default.html";
                // check if the uri includes a query string
                if (uri.Query != "")
                    // construct the name from the query hash value to be the same if we download it again
                    strLocalPath += uri.Query.GetHashCode() + ".html";
                // construct the full path folder
                string BasePath = this.Downloadfolder + "\\" + uri.Host + Path.GetDirectoryName(uri.AbsolutePath);
                // check if the folder not found
                if (Directory.Exists(BasePath) == false)
                    // create the folder
                    Directory.CreateDirectory(BasePath);
                // construct the full path name of the file
                string PathName = Path.GetFullPath(this.Downloadfolder + "\\" + uri.Host + strLocalPath.Replace("%20", " "));

                if (!DictUrls.ContainsKey(GetMD5Code(PathName)))
                    DictUrls.Add(GetMD5Code(PathName), uri.AbsoluteUri);

                // open the output file
                FileStream streamOut = File.Open(PathName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                BinaryWriter writer = new BinaryWriter(streamOut);

                //itemLog.SubItems[2].Text = "Download";
                //itemLog.ForeColor = Color.Black;
                // receive response buffer
                string strResponse = string.Empty;

                byte[] RecvBuffer = new byte[10240];
                int nBytes, nTotalBytes = 0;
                // loop to receive response buffer
                while ((nBytes = response.socket.Receive(RecvBuffer, 0, 10240, SocketFlags.None)) > 0)
                {
                    // increment total received bytes
                    nTotalBytes += nBytes;
                    // write received buffer to file
                    writer.Write(RecvBuffer, 0, nBytes);
                    // check if the uri type not binary to can be parsed for refs
                    if (bParse == true)
                        // add received buffer to response string
                        strResponse += Encoding.ASCII.GetString(RecvBuffer, 0, nBytes);
                    if (response.KeepAlive && nTotalBytes >= response.ContentLength && response.ContentLength > 0)
                        break;
                }
                // close output stream
                writer.Close();
                streamOut.Close();

                if (response.KeepAlive)
                    strStatus += "Connection kept alive to be used in subpages.\r\n";
                else
                {
                    // close response
                    response.Close();
                    strStatus += "Connection closed.\r\n";
                }
                // update status
                strStatus += Commas(nTotalBytes) + " bytes, downloaded to \"" + PathName + "\"\r\n";
                // increment total file count
                //FileCount++;
                // increment total bytes count
                //ByteCount += nTotalBytes;

                if (ThreadsRunning == true && bParse == true && uri.Depth < nWebDepth)
                {
                    strStatus += "\r\nParsing page ...\r\n";

                    //// check for restricted words
                    foreach (string strExcludeWord in ExcludeWords)
                        if (strExcludeWord.Trim().Length > 0 && strResponse.IndexOf(strExcludeWord) != -1)
                        {
                            File.Delete(PathName);
                            return;
                        }

                    ThreadPool.QueueUserWorkItem(new WaitCallback(LinkTagExtract), PathName);
                    // parse the page to search for refs
                    //string strRef = @"(href|HREF|src|SRC)[ ]*=[ ]*[""'][^""'#>]+[""']";
                    string strRef = @"(href|HREF)[ ]*=[ ]*[""'][^""'#>]+[""']";
                    MatchCollection matches = new Regex(strRef).Matches(strResponse);
                    strStatus += "Found: " + matches.Count + " ref(s)\r\n";

                    if (!DictUrlCnts.ContainsKey(uri.AbsoluteUri))
                        DictUrlCnts.Add(uri.AbsoluteUri, matches.Count);

                    foreach (Match match in matches)
                    {
                        strRef = match.Value.Substring(match.Value.IndexOf('=') + 1).Trim('"', '\'', '#', ' ', '>');
                        try
                        {
                            if (strRef.IndexOf("..") != -1 || strRef.StartsWith("/") == true || strRef.StartsWith("http://") == false)
                                strRef = new Uri(uri, strRef).AbsoluteUri;
                            Normalize(ref strRef);
                            MyUri newUri = new MyUri(strRef);
                            if (newUri.Scheme != Uri.UriSchemeHttp && newUri.Scheme != Uri.UriSchemeHttps)
                                continue;
                            if (newUri.Host != uri.Host && bKeepSameServer == true)
                                continue;
                            newUri.Depth = uri.Depth + 1;
                            if (this.EnqueueUri(newUri, true) == true)
                                strStatus += newUri.AbsoluteUri + "\r\n";
                        }
                        catch
                        {
                            Console.WriteLine(strRef);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                request = null;
            }
            finally
            {

            }
        }


        // 抽取链接的文字描述信息
        void LinkTagExtract(Object PathName)
        {
            StreamReader sr = new StreamReader(PathName as string, Encoding.Default);
            string WebPageContent = sr.ReadToEnd();
            MyUri uri = new MyUri(DictUrls[GetMD5Code(PathName as string)] as string);
            string href = @"<(a|A)[^>]*(href|HREF)=(""(?<href>[^""]*)""|'(?<href>[^']*)'|(?<href>[^\s>]*))[^>]*>(?<text>.*?)</(a|A)>";
            Regex re = new Regex(href, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            MatchCollection matches = re.Matches(WebPageContent as string);
            foreach (Match m in matches)
            {
                string link = m.Groups["href"].Value;
                string text = Regex.Replace(m.Groups["text"].Value, "<[^>]*>", "");

                try
                {
                    if (link.IndexOf("..") != -1 || link.StartsWith("/") == true || link.StartsWith("http://") == false)
                        link = new Uri(uri, link).AbsoluteUri;
                    Normalize(ref link);
                    MyUri newUri = new MyUri(link);

                    if (string.IsNullOrEmpty(text) || !newUri.Host.Equals(uri.Host))
                        continue;

                    if (!mapLnkTxt.ContainsKey(newUri.AbsoluteUri))
                        mapLnkTxt[newUri.AbsoluteUri] = new Dictionary<string, int>();

                    text.Replace("&nbsp;",string.Empty);

                    if (!mapLnkTxt[newUri.AbsoluteUri].ContainsKey(text))
                    {
                        mapLnkTxt[newUri.AbsoluteUri].Add(text, 1);
                    }
                    else
                    {
                        mapLnkTxt[newUri.AbsoluteUri][text]++;
                    }
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
            }
        }

        void ParseFolder(string folderName, int nDepth)
        {
            DirectoryInfo dir = new DirectoryInfo(folderName);
            FileInfo[] fia = dir.GetFiles("*.txt");
            foreach (FileInfo f in fia)
            {
                if (ThreadsRunning == false)
                    break;
                MyUri uri = new MyUri(f.FullName);
                uri.Depth = nDepth;
                this.EnqueueUri(uri, true);
            }

            DirectoryInfo[] dia = dir.GetDirectories();
            foreach (DirectoryInfo d in dia)
            {
                if (ThreadsRunning == false)
                    break;
                ParseFolder(d.FullName, nDepth + 1);
            }
        }


        void DeleteAllItems()
        {
            if (MessageBox.Show(this, "Do you want to delete all?", "Verify", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //this.listViewErrors.Items.Clear();
                //this.listViewRequests.Items.Clear();
                this.urlStorage = new SortTree();
                //this.URLCount = 0;				
                //this.ByteCount = 0;
                //this.ErrorCount = 0;
            }
        }

        string Commas(int nNum)
        {
            string str = nNum.ToString();
            int nIndex = str.Length;
            while (nIndex > 3)
            {
                str = str.Insert(nIndex - 3, ",");
                nIndex -= 3;
            }
            return str;
        }

        private void buttonRecommander_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxWeb.Text))
            {
                //Validation failed, so set an appropriate error message
                errorProviderRecommend.SetError(textBoxKeyWord, "请在网址区输入待推荐的网站地址");
                return;
            }
            else
            {
                //Clear previous error message
                errorProviderRecommend.SetError(textBoxKeyWord, string.Empty);
            }

            this.LPRecommander = new SearchUrls(textBoxWeb.Text.Trim(), Downloadfolder);
            LPRecommander.StartQuerying(System.IO.Path.Combine(Downloadfolder, "Index", new MyUri(textBoxWeb.Text).Host), textBoxKeyWord.Text);
            this.comboBoxURL.DataSource = null;
            this.comboBoxURL.DataSource = LPRecommander.UrlList;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.FileName = this.comboBoxURL.Text;
            process.Start();
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxWeb.Text))
            {
                //Validation failed, so set an appropriate error message
                errorProviderImport.SetError(textBoxFilePath, "请在网址区输入待推荐的网站地址");
                return;
            }
            else
            {
                //Clear previous error message
                errorProviderImport.SetError(textBoxFilePath, string.Empty);
            }

            if (this.openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.textBoxFilePath.Text = openFileDialog.FileName;
        }

        private void RecommandThreadMethod()
        {
            CheckForIllegalCrossThreadCalls = false;

            if (!Path.GetExtension(saveFileDialog.FileName).Equals(".csv"))
                return;

            #region 关键词预处理 (分析关键词列表，预分配词权重Boost)
            #endregion

            FileStream outFs = new FileStream(saveFileDialog.FileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(outFs, System.Text.Encoding.Default);
            FileStream inFs = new FileStream(this.textBoxFilePath.Text.Trim(), FileMode.Open);
            StreamReader sr = new StreamReader(inFs, System.Text.Encoding.Default);
            SearchUrls lpr;

            this.pictureBox_Editor.Enabled = true;
            this.pictureBox_Editor.Visible = true;
            //Skip Title
            sr.ReadLine();

            for (System.String kwInfo = sr.ReadLine(); kwInfo != null; kwInfo = sr.ReadLine())
            {
                string[] kwAttr = kwInfo.Split(new char[] { '\t' });
                string kw = kwAttr[0];
                lpr = new SearchUrls(this.textBoxWeb.Text.Trim(), Downloadfolder);
                lpr.StartQuerying(System.IO.Path.Combine(Downloadfolder, "Index", new MyUri(textBoxWeb.Text).Host), kw);

                StringBuilder strbuilder = new StringBuilder();
                strbuilder.Append(kw);
                strbuilder.Append(",");
                //strbuilder.Append(kwAttr[6]);
                //strbuilder.Append(",");
                //strbuilder.Append(kwAttr[4]);
                //strbuilder.Append(",");

                if (lpr.UrlList != null && lpr.UrlList.Count > 0)
                {
                    //for (int ix = 0; ix < (lpr.UrlList.Count == 1 ? lpr.UrlList.Count : 1); ++ix)
                    //{
                    strbuilder.Append(lpr.UrlList[0]);
                    //strbuilder.Append(",");
                    //strbuilder.Append(lpr.UrLTitleMap[lpr.UrlList[ix]]);
                    //strbuilder.Append(",");
                    //}
                }
                //strbuilder.Append(kwAttr[3]);
                //strbuilder.Append(",");
                sw.WriteLine(strbuilder.ToString());
            }

            sr.Close();
            sw.Close();
            inFs.Close();
            outFs.Close();

            this.pictureBox_Editor.Enabled = false;
            this.pictureBox_Editor.Visible = false;
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "|*.csv";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                threadIndex = new Thread(new ThreadStart(RecommandThreadMethod));
                threadIndex.IsBackground = true;
                threadIndex.Priority = ThreadPriority.BelowNormal;
                threadIndex.Start();
            }
        }
    }
}
