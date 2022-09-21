using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using AdvancedPowerMenu.Properties;
using System.Collections.Generic;

namespace AdvancedPowerMenu
{
    public partial class FrmMain : Form
    {
        [DllImport("user32")]
        public static extern void LockWorkStation();

        private Thread tThread;
        private TcpClient tcTcpClient;
        private TcpListener tlTcpListener;
        private NetworkStream nsNetworkStream;

        private int iPortNumber = Settings.Default.PortNumber;
        private String sRootDir = "www\\";
        private String sImagesDir = "images\\";
        private String sScreenshotFileName = "screenshot.png";
        private List<String> slMissingFiles = new List<String>();

        public FrmMain()
        {
            InitializeComponent();

            DetectWindowsTheme();

            GetUserValues();

            CheckWebFiles(AppDomain.CurrentDomain.BaseDirectory);

            FilesCheckResult();
        }
  
        public void Start()
        {
            tThread = new Thread(new ThreadStart(Listen));
            tThread.Start();
        }

        public void Listen()
        {
            tlTcpListener = new TcpListener(IPAddress.Any, iPortNumber);
            tlTcpListener.Start();

            while (true)
            {
                Console.WriteLine("Listening for clients...");
                tcTcpClient = tlTcpListener.AcceptTcpClient();
                ProcessStream();
            }
        }

        public void ProcessStream()
        {
            byte[] request = new byte[1024];
            nsNetworkStream = tcTcpClient.GetStream();

            try
            {
                int iI = 0;
                String sData = "";

                do
                {
                    iI = nsNetworkStream.Read(request, 0, request.Length);
                    sData = Encoding.ASCII.GetString(request, 0, iI);
                } while (nsNetworkStream.DataAvailable);

                Console.WriteLine(sData);

                int iStartPos = 0;
                String sRequest;
                String sRequestedFile = "";
                String sRequestedCommand = "";
                String sPhysicalFilePath = "";

                if (!sData.Contains("HTTP"))
                {
                    return;
                }

                iStartPos = sData.IndexOf("HTTP", 1);
                sRequest = sData.Substring(0, iStartPos - 1);
                sRequest.Replace("\\", "/");

                if ((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
                {
                    sRequest = sRequest + "/";
                }

                iStartPos = sRequest.LastIndexOf("/") + 1;
                sRequestedFile = sRequest.Substring(iStartPos);

                if (sRequestedFile.Length < 1)
                {
                    iStartPos = 0;
                    sRequest = "";

                    iStartPos = sData.IndexOf("HTTP", 1);
                    sRequest = sData.Substring(0, iStartPos - 1);

                    sRequest.Replace("\\", "/");

                    if ((sRequest.IndexOf("_") < 1) && (!sRequest.EndsWith("/")))
                    {
                        sRequest = sRequest + "/";
                    }

                    iStartPos = sRequest.LastIndexOf("/") + 1;

                    sRequestedCommand = sRequest.Substring(iStartPos);

                    Console.WriteLine("--------Begin of request--------");
                    Console.WriteLine(sData);

                    if (sRequestedCommand == "?inquiry_is=Way")
                    {
                        Console.WriteLine("Host Name Request.");

                        byte[] sendData = Encoding.UTF8.GetBytes(Dns.GetHostName() + "|" + Environment.UserName);
                        nsNetworkStream.Write(sendData, 0, sendData.Length);
                    }
                    else
                    {
                        if (sRequestedCommand == "?action_is=Shutdown")
                        {
                            Console.WriteLine("Computer Shut Down Request.");

                            SendPage("text/html", "action.html", "Shut Down");

                            var psi = new ProcessStartInfo("shutdown", "/s /t 0");
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = false;
                            Process.Start(psi);
                        }
                        else if (sRequestedCommand == "?action_is=Restart")
                        {
                            Console.WriteLine("Computer Restart Request.");

                            SendPage("text/html", "action.html", "Restarted");

                            var psi = new ProcessStartInfo("shutdown", "/r /t 0");
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = false;
                            Process.Start(psi);
                        }
                        else if (sRequestedCommand ==  "?action_is=Lock")
                        {
                            Console.WriteLine("Computer Lock Request.");

                            SendPage("text/html", "action.html", "is Locked");

                            LockWorkStation();
                        }
                        else if (sRequestedCommand == "?action_is=Screenshot")
                        {
                            Console.WriteLine("Screenshot Request.");

                            TakeScreenshot();

                            SendPage("text/html", "capture.html", "Screen Captured");
                        }
                        else if (sRequestedCommand == "?action_is=MobileApp_Screenshot")
                        {
                            Console.WriteLine("Screenshot Request from Android Mobile App.");

                            TakeScreenshot();
                            string sApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory + sRootDir + sImagesDir;
                            Console.WriteLine("Directory Requested : " + sApplicationDirectory);

                            sPhysicalFilePath = sApplicationDirectory + sScreenshotFileName;
                            Console.WriteLine("File Requested : " + sPhysicalFilePath);

                            byte[] file = File.ReadAllBytes(sPhysicalFilePath);
                            byte[] fileSize = BitConverter.GetBytes(file.Length);
                            nsNetworkStream.Write(file, 0, file.Length);
                        }
                        else
                        {
                            Console.WriteLine("Advanced Power Menu Request.");

                            SendPage("text/html", "index.html", "OK");
                        }
                    }

                }
                else if (sRequestedFile.Length > 0)
                {
                    String sApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory + sRootDir + sImagesDir;
                    Console.WriteLine("Directory Requested : " + sApplicationDirectory);

                    sPhysicalFilePath = sApplicationDirectory + sRequestedFile;
                    Console.WriteLine("File Requested : " + sPhysicalFilePath);

                    int iTotBytes = 0;

                    FileStream fs = new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read);
                    BinaryReader reader = new BinaryReader(fs);
                    byte[] bytes = new byte[fs.Length];
                    int read;
                    while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        iTotBytes = iTotBytes + read;
                    }
                    reader.Close();
                    fs.Close();

                    SendHeader(bytes.Length, "image/png");
                    nsNetworkStream.Write(bytes, 0, bytes.Length);
                }

                Console.WriteLine("--------End of request---------");
                Console.Write("\n");
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: ", e);
                return;
            }
            catch (IOException e)
            {
                Console.WriteLine("Exception: " + e);
                return;
            }
            finally
            {
                nsNetworkStream.Close();
                tcTcpClient.Close();
            }
        }

        public void GetUserValues()
        {
            runAtStartupToolStripMenuItem.Checked = (bool)Settings.Default.RunAtStartup;
            tbxPortNumber.Text = Convert.ToString(iPortNumber);
        }

        public void FilesCheckResult()
        {
            if (slMissingFiles.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < slMissingFiles.Count; i++)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(slMissingFiles[i]);
                }

                DialogResult result = MessageBox.Show("Following File(s) Missing:" + sb.ToString(), "Error", MessageBoxButtons.OK);
                if (result == DialogResult.OK)
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                Start();
            }
        }

        public void CheckWebFiles(string filePath)
        {
            List<string> fileNames = new List<string>();

            fileNames.Add(filePath + sRootDir + "index.html");
            fileNames.Add(filePath + sRootDir + "action.html");
            fileNames.Add(filePath + sRootDir + "capture.html");
            fileNames.Add(filePath + sRootDir + sImagesDir + "favicon-16x16.png");
            fileNames.Add(filePath + sRootDir + sImagesDir + "favicon-192x192.png");

            slMissingFiles.Clear();

            for(int i = 0; i < fileNames.Count; i++)
            {
                try
                {
                    if (!File.Exists(fileNames[i]))
                    {
                        slMissingFiles.Add(fileNames[i]);
                    }
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine("Missing File: " + e);
                }
            }
        }

        public void DetectWindowsTheme()
        {
            try
            {
                int sys = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", -1);

                if (sys == 1)
                {
                    notifyIcon.Icon = Resources.notificationblack;
                }
                else
                {
                    notifyIcon.Icon = Resources.notificationwhite;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error, RegistryKey Not Found: " + e);
            }
        }

        public String ReadHTML(String sHtmlName, String sStatus)
        {
            String sFilePath = AppDomain.CurrentDomain.BaseDirectory + sRootDir + sHtmlName;
            FileStream fsFileStream = new FileStream(sFilePath, FileMode.Open, FileAccess.Read);

            String sFileContents;
            using (StreamReader srStreamReader = new StreamReader(fsFileStream))
            {
                sFileContents = srStreamReader.ReadToEnd();
            }

            fsFileStream.Close();

            String sFinal = sFileContents.Replace("{rComputerName}", Dns.GetHostName()).Replace("{rUserName}", Environment.UserName).Replace("{rIpAddress}", Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString()).Replace("{rStatus}", sStatus);

            return sFinal;
        }

        public void SendPage(String sMimeTypes, String sHtmlName, String sStatus)
        {
            StringBuilder sbStringBuilder = new StringBuilder();
            sbStringBuilder.AppendLine("HTTP/1.1 200 OK");
            sbStringBuilder.AppendLine("Content-type: " + sMimeTypes + "; charset=utf-8");
            sbStringBuilder.AppendLine("Connection: keep-alive");
            sbStringBuilder.AppendLine("Content-Length: " + ReadHTML(sHtmlName, sStatus).Length);
            sbStringBuilder.AppendLine();
            sbStringBuilder.AppendLine(ReadHTML(sHtmlName, sStatus));

            Byte[] bSendData = Encoding.ASCII.GetBytes(sbStringBuilder.ToString());
            nsNetworkStream.Write(bSendData, 0, bSendData.Length);
        }

        public void SendHeader(int iTotBytes, String sMimeTypes)
        {
            StringBuilder sbStringBuilder = new StringBuilder();
            sbStringBuilder.AppendLine("HTTP/1.1 200 OK");
            sbStringBuilder.AppendLine("Content-type: " + sMimeTypes + "; charset=utf-8");
            sbStringBuilder.AppendLine("Connection: keep-alive");
            sbStringBuilder.AppendLine("Content-Length: " + iTotBytes);
            sbStringBuilder.AppendLine();
            Byte[] bSendData = Encoding.ASCII.GetBytes(sbStringBuilder.ToString());
            nsNetworkStream.Write(bSendData, 0, bSendData.Length);
        }


        public byte[] ImageToBytes(Image iImage)
        {
            using (MemoryStream msMemoryStream = new MemoryStream())
            {
                iImage.Save(msMemoryStream, ImageFormat.Png);
                return msMemoryStream.ToArray();
            }
        }

        public void TakeScreenshot()
        {
            Rectangle rBounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bBitmap = new Bitmap(rBounds.Width, rBounds.Height))
            {
                Graphics gGraphics = Graphics.FromImage(bBitmap);
                using (gGraphics)
                {
                    gGraphics.CopyFromScreen(new Point(rBounds.Left, rBounds.Top), Point.Empty, rBounds.Size);
                }

                Bitmap bNewResolution = new Bitmap(1024, 567);
                gGraphics = Graphics.FromImage(bNewResolution);
                gGraphics.DrawImage(bBitmap, new RectangleF(0, 0, bNewResolution.Width, bNewResolution.Height));

                bNewResolution.Save(AppDomain.CurrentDomain.BaseDirectory + sRootDir + sImagesDir + sScreenshotFileName, ImageFormat.Png);
            }
        }

        private void tbxPortNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Hide();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Settings.Default.PortNumber = Int32.Parse(tbxPortNumber.Text);
            Settings.Default.Save();

            DialogResult dialogResult = MessageBox.Show("Start Application Manually.", "Done!", MessageBoxButtons.OK);
            if (dialogResult == DialogResult.OK)
            {
                tlTcpListener.Stop();
                tThread.Abort();
                Application.Exit();
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tlTcpListener.Stop();
            tThread.Abort();
            Application.Exit();
        }

        private void runAtStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (runAtStartupToolStripMenuItem.Checked)
            {
                runAtStartupToolStripMenuItem.Checked = false;
                Settings.Default.RunAtStartup = false;

                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.DeleteValue(Application.ProductName, false);
            }
            else
            {
                runAtStartupToolStripMenuItem.Checked = true;
                Settings.Default.RunAtStartup = true;

                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.SetValue(Application.ProductName, Application.ExecutablePath.ToString());
            }
            Settings.Default.Save();
        }
    }
}
