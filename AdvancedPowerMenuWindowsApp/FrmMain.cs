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

namespace AdvancedPowerMenu
{
    public partial class FrmMain : Form
    {
        //Lock Computer
        [DllImport("user32")]
        public static extern void LockWorkStation();

        private Thread thread;
        private TcpClient tcpClient;
        private TcpListener tcpListener;
        private NetworkStream networkStream;

        private String versionNumber = "v1.3";
        private String faviconFileName = "favicon-16x16.png";
        private String screenshotFileName = "screenshot.png";
        private int portNumer = Properties.Settings.Default.PortNumber;

        public FrmMain()
        {
            InitializeComponent();
            runAtStartupToolStripMenuItem.Checked = (bool)Properties.Settings.Default.RunAtStartup;
            tbxPortNumber.Text = Convert.ToString(portNumer);
            Start();
        }

        public void Start()
        {
            thread = new Thread(new ThreadStart(Listen));
            thread.Start();
        }

        public void Listen()
        {
            tcpListener = new TcpListener(IPAddress.Any, portNumer);
            tcpListener.Start();

            while (true)
            {
                Console.WriteLine("Listening for clients...");
                notifyIcon.Icon = Properties.Resources.trayIconRunnig;
                tcpClient = tcpListener.AcceptTcpClient();
                ProcessStream();
            }
        }

        public void ProcessStream()
        {
            byte[] request = new byte[2048];
            networkStream = tcpClient.GetStream();

            try
            {
                int i = 0;
                String data = "";

                do
                {
                    i = networkStream.Read(request, 0, request.Length);
                    data = Encoding.ASCII.GetString(request, 0, i);
                } while (networkStream.DataAvailable);

                Console.WriteLine(data);

                int iStartPos = 0;
                String sRequest;
                String sRequestedFile = "";
                String sLocalDir;
                String sPhysicalFilePath = "";
                String sResponse = "";

                if (!data.Contains("HTTP"))
                {
                    return;
                }

                iStartPos = data.IndexOf("HTTP", 1);
                sRequest = data.Substring(0, iStartPos - 1);
                sRequest.Replace("\\", "/");

                if ((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
                {
                    sRequest = sRequest + "/";
                }

                iStartPos = sRequest.LastIndexOf("/") + 1;
                sRequestedFile = sRequest.Substring(iStartPos);

                Console.WriteLine("Requested File " + sRequestedFile);

                if (sRequestedFile.Length == 0)
                {
                    Console.WriteLine("--------Begin of request--------");
                    Console.WriteLine(data);

                    if (data.Contains("inquiry_is=Way"))
                    {
                        Console.WriteLine("Host Name is Requested.");

                        byte[] sendData = Encoding.UTF8.GetBytes(Dns.GetHostName() + "|" + Environment.UserName);
                        networkStream.Write(sendData, 0, sendData.Length);
                    }
                    else if (data.Contains("action_is=Shutdown"))
                    {
                        Console.WriteLine("Shutting Down Computer was Requested.");

                        byte[] sendData = Encoding.UTF8.GetBytes(htmlPage(htmlActionDone("Shut down")));
                        networkStream.Write(sendData, 0, sendData.Length);

                        var psi = new ProcessStartInfo("shutdown", "/s /t 0");
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process.Start(psi);
                    }
                    else if (data.Contains("action_is=Restart"))
                    {
                        Console.WriteLine("Restart Computer was Requested.");

                        byte[] sendData = Encoding.UTF8.GetBytes(htmlPage(htmlActionDone("Restarted")));
                        networkStream.Write(sendData, 0, sendData.Length);

                        var psi = new ProcessStartInfo("shutdown", "/r /t 0");
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process.Start(psi);
                    }
                    else if (data.Contains("action_is=Lock"))
                    {
                        Console.WriteLine("Lock Computer was Requested.");

                        byte[] sendData = Encoding.UTF8.GetBytes(htmlPage(htmlActionDone("is Locked")));
                        networkStream.Write(sendData, 0, sendData.Length);

                        LockWorkStation();
                    }
                    else if (data.Contains("action_is=Screenshot"))
                    {
                        Console.WriteLine("Screenshot was Requested.");

                        takeScreenshot();

                        byte[] sendData = Encoding.UTF8.GetBytes(indexHTML());
                        SendHeader(networkStream, sendData.Length, "text/html");
                        networkStream.Write(sendData, 0, sendData.Length);
                    }
                    else if (data.Contains("action_is=MobileApp_Screenshot"))
                    {
                        Console.WriteLine("Screenshot was Requested from Mobile App.");

                        takeScreenshot();

                        sLocalDir = AppDomain.CurrentDomain.BaseDirectory;
                        Console.WriteLine("Directory Requested : " + sLocalDir);

                        sPhysicalFilePath = sLocalDir + screenshotFileName;
                        Console.WriteLine("File Requested : " + sPhysicalFilePath);

                        byte[] file = File.ReadAllBytes(sPhysicalFilePath);
                        byte[] fileSize = BitConverter.GetBytes(file.Length);
                        networkStream.Write(file, 0, file.Length);

                        Console.WriteLine("Screenshot was sent to Mobile App.");
                    }
                    else
                    {
                        Console.WriteLine("Advanced Power Menu was Requested.");

                        byte[] sendData = Encoding.UTF8.GetBytes(htmlPage(htmlActionPost()));
                        networkStream.Write(sendData, 0, sendData.Length);
                    }
                }
                else if (sRequestedFile == screenshotFileName)
                {
                    sLocalDir = AppDomain.CurrentDomain.BaseDirectory;
                    Console.WriteLine("Directory Requested : " + sLocalDir);

                    sPhysicalFilePath = sLocalDir + sRequestedFile;
                    Console.WriteLine("File Requested : " + sPhysicalFilePath);

                    int iTotBytes = 0;
                    sResponse = "";

                    FileStream fs = new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read);
                    BinaryReader reader = new BinaryReader(fs);
                    byte[] bytes = new byte[fs.Length];
                    int read;
                    while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        sResponse = sResponse + Encoding.ASCII.GetString(bytes, 0, read);
                        iTotBytes = iTotBytes + read;
                    }
                    reader.Close();
                    fs.Close();

                    byte[] file = File.ReadAllBytes(sPhysicalFilePath);
                    SendHeader(networkStream, file.Length, "image/png");
                    networkStream.Write(bytes, 0, bytes.Length);

                    Console.WriteLine("Screenshot was Sent.");
                }
                else if (sRequestedFile == faviconFileName)
                {
                    byte[] sendData = (ImageToBytes(Properties.Resources.favicon_16x16));
                    SendHeader(networkStream, sendData.Length, "image/png");
                    networkStream.Write(sendData, 0, sendData.Length);

                    Console.WriteLine("Favicon was Sent.");
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
                networkStream.Close();
                tcpClient.Close();
                notifyIcon.Icon = Properties.Resources.trayIconNotRunning;
            }
        }

        public void SendHeader(NetworkStream ns, int iTotBytes, string mimeTypes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("HTTP/1.1 200 OK");
            stringBuilder.AppendLine("Content-type: "+ mimeTypes + "; charset=utf-8");
            stringBuilder.AppendLine("Connection: keep-alive");
            stringBuilder.AppendLine("Content-Length: " + iTotBytes);
            stringBuilder.AppendLine();
            Byte[] bSendData = Encoding.ASCII.GetBytes(stringBuilder.ToString());
            ns.Write(bSendData, 0, bSendData.Length);
        }

        public byte[] ImageToBytes(Image imageIn)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                imageIn.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public void takeScreenshot()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                Graphics graphics = Graphics.FromImage(bitmap);
                using (graphics)
                {
                    graphics.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
                }

                Bitmap newResolution = new Bitmap(1024, 567);
                graphics = Graphics.FromImage(newResolution);
                graphics.DrawImage(bitmap, new RectangleF(0, 0, newResolution.Width, newResolution.Height));

                newResolution.Save(AppDomain.CurrentDomain.BaseDirectory + screenshotFileName, ImageFormat.Png);
            }
        }

        public String htmlPage(String htmlResponseContent)
        {
            string htmlResponseTop = "<!DOCTYPE html><html lang=\"en\"><head><title>Advanced Power Menu</title><link rel=\"icon\" type=\"image/png\" sizes=\"16x16\" href=\"" + faviconFileName + "\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><style>.bodybackground {background-color: #252525;font-family: Tahoma;color:white;}.centeroutside {display: flex;justify-content: center;}.center {text-align: center;width:320px;}.button {background-color: #4CAF50;border-radius: 6px;border: none;color: white;padding: 10px 32px;text-align: center;text-decoration: none;display: inline-block;font-size: 17px;width: 310px;font-family:verdana; margin: 10px 0px 0px 0px;}.button:hover {background-color: #3e8e41;}.head {font-size: 26px; font-weight: bold; margin: 20px 0px 20px 0px;}.form{margin: 20px 0px 0px 0px;}.actionisdone{font-size: 23px; font-weight: bold; color: #a6caff; margin: 20px 0px 0px 0px;}.version {font-size: 10px; text-align: right; margin: 5px 6px 0px 0px;}.hr{margin: 10px 5px 0px 5px;}.table{border-radius: 8px;border: 2px solid #4CAF50;padding: 15px;margin: 0px 5px 0px 5px;}.tableresult{border-radius: 8px;background: #1fa3ec;padding: 15px;margin: 0px 5px 0px 5px;}.heads {font-size: 12px;color: #fff;}.details {font-size: 20px;font-weight: bold;color: #a6caff;margin: 0px 0px 00px 0px;}</style></head><body class=\"bodybackground\"><div class=\"centeroutside\"><div class=\"center\"><div class=\"head\">Advanced Power Menu</div>";
            string htmlResponseBottom = "<hr class=\"hr\"><div class=\"version\">APM " + versionNumber + " by Venovar</div></div></div></body></html>";

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("HTTP/1.1 200 OK");
            stringBuilder.AppendLine("Content-type: text/html; charset=utf-8");
            stringBuilder.AppendLine("Connection: keep-alive");
            stringBuilder.AppendLine("Content-Length: " + (htmlResponseTop.Length + htmlResponseContent.Length + htmlResponseBottom.Length));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(htmlResponseTop);
            stringBuilder.AppendLine(htmlResponseContent);
            stringBuilder.AppendLine(htmlResponseBottom);

            String response = stringBuilder.ToString();

            return response;
        }

        public String htmlComputerDetails()
        {
            string computerDetails = "<div class=\"table\"><div class=\"heads\">Computer Name:</div><div class=\"details\">" + Dns.GetHostName() + "</div><br><div class=\"heads\">Logged User Name:</div><div class=\"details\">" + Environment.UserName + "</div><br><div class=\"heads\">IP Address:</div><div class=\"details\">" + Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString() + "</div></div>";

            return computerDetails;
        }

        public String htmlActionPost()
        {
            string htmlWelcome = htmlComputerDetails() + "<div class=\"form\"><form method=\"GET\"><input type=\"submit\" class=\"button\" id=\"shutdown\" name=\"action_is\" value=\"Shutdown\"><input type=\"submit\" class=\"button\" id=\"restart\" name=\"action_is\" value=\"Restart\"><input type=\"submit\" class=\"button\" id=\"lock\" name=\"action_is\" value=\"Lock\"><input type=\"submit\" class=\"button\" id=\"screenshot\" name=\"action_is\" value=\"Screenshot\"></form></div>";

            return htmlWelcome;
        }

        public String htmlActionDone(String action)
        {
            string htmlAction = htmlComputerDetails() + "<br><div class=\"tableresult\"><div class=\"heads\">Action:</div><div class=\"details\">Computer " + action + "</div></div>";

            return htmlAction;
        }
        public string indexHTML()
        {
            string indexHTMLView = "<!DOCTYPE html><html lang=\"en\"><head><title>Take Screenshot</title><link rel=\"icon\" type=\"image/png\" sizes=\"16x16\" href=\"/" + faviconFileName +"\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><style>.bodybackground {background-color: #252525;font-family: Tahoma;color:white;}.head {font-size: 26px; font-weight: bold; margin: 20px 0px 20px 0px;}.center {text-align: center; display: table; margin-right: auto; margin-left: auto;}.button {background-color: #4CAF50;border-radius: 6px;border: none;color: white;padding: 10px 32px;text-align: center;text-decoration: none;display: inline-block;font-size: 17px;width: 250px;font-family:verdana; margin: 10px 0px 0px 0px;}.button:hover {background-color: #3e8e41;}.rounddiv {border-radius: 8px; border: 2px solid #73AD21; padding: 2px;}.sideside {margin: 0px 5px 0px 0px;display: inline-block; text-align: center;}</style></head><body class=\"bodybackground\"><div class=\"center\"><div class=\"rounddiv\"><img src=\"" + screenshotFileName + "\" alt=\"Screenshot\"></div><div><div class=\"sideside\"><button class=\"button\" onclick=\"history.back()\">Main Page</button></div><div class=\"sideside\"><button class=\"button\" onclick=\"location.reload();\">Refresh</button></div></div></div></body></html>";

            return indexHTMLView;
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
            Properties.Settings.Default.PortNumber = Int32.Parse(tbxPortNumber.Text);
            Properties.Settings.Default.Save();

            DialogResult dialogResult = MessageBox.Show("Start Application Manually.", "Done!", MessageBoxButtons.OK);
            if (dialogResult == DialogResult.OK)
            {
                tcpListener.Stop();
                thread.Abort();
                Application.Exit();
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tcpListener.Stop();
            thread.Abort();
            Application.Exit();
        }

        private void runAtStartupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (runAtStartupToolStripMenuItem.Checked)
            {
                runAtStartupToolStripMenuItem.Checked = false;
                Properties.Settings.Default.RunAtStartup = false;

                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.DeleteValue(Application.ProductName, false);
            }
            else
            {
                runAtStartupToolStripMenuItem.Checked = true;
                Properties.Settings.Default.RunAtStartup = true;

                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.SetValue(Application.ProductName, Application.ExecutablePath.ToString());
            }
            Properties.Settings.Default.Save();
        }
    }
}
