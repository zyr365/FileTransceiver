using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.Net.Sockets;


namespace FileTransceiver
{
    public partial class MainForm : DevComponents.DotNetBar.OfficeForm
    {
        private string fileName;
        private string filePath;
        private long fileSize;
        private string remoteIP;
        private int localPort;
        private int remotePort;
        public static int pathFlag = 1;
        public static string storagePath = null;
        private string picSavePath1 = null, picSavePath2 = null, videoSavePath = null;
        public MainForm()
        {
            try
            {
                this.EnableGlass = false;
                InitializeComponent();
                ReadConfigXML();

                FileRecive fr = new FileRecive(this, localPort);
                Thread fileRecive = new Thread(fr.run);         //开启数据接收线程                      
                fileRecive.Start();
                fileRecive.IsBackground = true;                //后台运行

                TestFileSystemWatcher();
            }
            catch (Exception ex)
            {
                logWrite(ex.Message, ex.StackTrace);
            }
        }
        private void ReadConfigXML()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
                XmlNode settingNode = xmlDoc.DocumentElement;

                XmlElement e = settingNode.SelectSingleNode("RemoteIP") as XmlElement;
                if (e != null)
                {
                    remoteIP = e.InnerText;
                }
                else
                {
                    remoteIP = "192.168.1.119";
                }

                e = settingNode.SelectSingleNode("RemotePort") as XmlElement;
                if (e != null)
                {
                    remotePort = int.Parse(e.InnerText);
                }
                else
                {
                    remotePort = 8007;
                }

                e = settingNode.SelectSingleNode("LocalPort") as XmlElement;
                if (e != null)
                {
                    localPort = int.Parse(e.InnerText);
                }
                else
                {
                    localPort = 8007;
                }

                e = settingNode.SelectSingleNode("PicSavePath1") as XmlElement;
                if (e != null)
                {
                    picSavePath1 = e.InnerText;
                }
                else
                {
                    picSavePath1 = @"D:\Picture\device1";
                }
                e = settingNode.SelectSingleNode("PicSavePath2") as XmlElement;
                if (e != null)
                {
                    picSavePath2 = e.InnerText;
                }
                else
                {
                    picSavePath2 = @"D:\Picture\device2";
                }
                e = settingNode.SelectSingleNode("VideoSavePath") as XmlElement;
                if (e != null)
                {
                    videoSavePath = e.InnerText;
                }
                else
                {
                    videoSavePath = @"D:\Record";
                }
            }

            catch (Exception ex)
            {
                logWrite(ex.Message, ex.StackTrace);
            }
        }
        public void TestFileSystemWatcher()
        {
            try
            {
                FileSystemWatcher watcher1 = new FileSystemWatcher();
                FileSystemWatcher watcher2 = new FileSystemWatcher();
                FileSystemWatcher watcher3 = new FileSystemWatcher();
                try
                {
                    watcher1.Path = @"D:\Picture\device1";
                    watcher2.Path = @"D:\Picture\device2";
                    watcher3.Path = @"D:\Record";
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
                //设置监视文件的哪些修改行为
                watcher1.NotifyFilter = NotifyFilters.LastAccess
                    | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // watcher.Filter = "*.jpg";
                watcher1.Created += new FileSystemEventHandler(OnChanged);
                watcher1.EnableRaisingEvents = true;

                watcher2.NotifyFilter = NotifyFilters.LastAccess
                     | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher2.Created += new FileSystemEventHandler(OnChanged);
                watcher2.EnableRaisingEvents = true;

                watcher3.NotifyFilter = NotifyFilters.LastAccess
                    | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher3.IncludeSubdirectories = true;
                watcher3.Created += new FileSystemEventHandler(OnChanged);
                watcher3.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                logWrite(ex.Message, ex.StackTrace);
            }
        }

        public void OnChanged(object source, FileSystemEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {

                //string ip = "101.91.224.228";
                //string ip = "192.168.0.194";
                //string port = "8008";
                if (Directory.Exists(e.FullPath))
                {
                    return;
                }
                    string[] videoPath;
                    while (IsFileInUse(e.FullPath)) ;
                    FileInfo f = new FileInfo(e.FullPath);
                    this.fileSize = f.Length;
                    //this.filePath = f.DirectoryName;
                    this.filePath = f.FullName;
                    this.fileName = f.Name;
                    if (fileName.Length == 0)
                    {
                        Tip("请选择文件");
                        return;
                    }
                    if (!File.Exists(f.FullName))
                    {
                        Tip("不存在" + f.FullName);
                        return;
                    }

                
                    videoPath = f.DirectoryName.Split('\\');
                    switch (f.DirectoryName)
                    {
                        case @"D:\Picture\device1": storagePath = picSavePath1; break;
                        case @"D:\Picture\device2": storagePath = picSavePath2; break;
                        //default: storagePath = videoSavePath + "\\"+ fileName; break;
                        default:
                            if (videoPath.Length >= 2)
                                storagePath = videoSavePath + "\\" + videoPath[2]; break;
                    }
                    //Thread.Sleep(1000);
                  
                    while (IsFileInUse(f.FullName)) ;
                    logWrite(e.FullPath, fileSize.ToString());

                    var c = new FileSend(this, new string[] { remoteIP, remotePort.ToString(), fileName, filePath, fileSize.ToString(), storagePath });
                    //new Thread(c.Send).Start();
                    Thread fileSendThread = new Thread(c.Send);
                    fileSendThread.Start();
                    fileSendThread.IsBackground = true;

                }
                catch (Exception ex)
                {
                    logWrite(ex.Message, ex.StackTrace);
                }
            });
        }
        //public void OnChanged_v1(object source, FileSystemEventArgs e)
        //{
        //    try
        //    {
        //        FileInfo f = new FileInfo(e.FullPath);
        //        FileSystemWatcher watcher1 = new FileSystemWatcher();
        //        try
        //        {
        //            watcher1.Path = f.FullName;
        //        }
        //        catch (ArgumentException ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //            return;
        //        }
        //        //设置监视文件的哪些修改行为
        //        watcher1.NotifyFilter = NotifyFilters.LastAccess
        //            | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        //        watcher1.Created += new FileSystemEventHandler(OnChanged);
        //        watcher1.EnableRaisingEvents = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        logWrite(ex.Message, ex.StackTrace);
        //    }


        //}
            public static bool IsFileInUse(string fileName)
        {
            bool inUse = true;

            FileStream fs = null;
            try
            {

                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,

                FileShare.None);

                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)

                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        }
        public void Tip(string msg)
        {
            MessageBox.Show(msg, "温馨提示");
        }
        public void SetState(string state)
        {
            label1.Text = state;
        }
        public void UpDateProgress(int value)
        {

            this.progressBarX1.Value = value;
            this.label2.Text = value + "%";
            System.Windows.Forms.Application.DoEvents();
        }

        #region //WinForm 之 窗口最小化到托盘及右键图标显示菜单 
        //https://www.cnblogs.com/xinaixia/p/6216670.html
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)//判断鼠标的按键            
            {
                //点击时判断form是否显示,显示就隐藏,隐藏就显示               
                if (this.WindowState == FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.Hide();
                }
                else if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    this.Activate();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                //右键退出事件                
                if (MessageBox.Show("是否需要关闭程序？", "提示:", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK)//出错提示                
                {
                    //关闭窗口                    
                    DialogResult = DialogResult.No;
                    Dispose();
                    Close();
                }
            }

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                // 关闭所有的线程
                this.Dispose();
                this.Close();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            //判断是否选择的是最小化按钮
            if (WindowState == FormWindowState.Minimized)
            {
                //隐藏任务栏区图标
                this.ShowInTaskbar = false;
                //图标显示在托盘区
                notifyIcon1.Visible = true;
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否确认退出程序？", "退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                // 关闭所有的线程
                this.Dispose();
                this.Close();
            }
        }

        private void 显示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }
        #endregion


        /// <summary>
        /// 退出程序
        /// </summary>
        public void Exit()
        {

            Application.Exit();
        }

        public void logWrite(string Message,string StackTrace)
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt"))
                File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt").Close();
            string fileName = AppDomain.CurrentDomain.BaseDirectory + "\\log.txt";
            string content = DateTime.Now.ToLocalTime() + Message + "\n" + StackTrace + "\r\n";
            StreamWriter sw = new StreamWriter(fileName, true);
            sw.Write(content);
            sw.Close(); sw.Dispose();
        }

    }
}
