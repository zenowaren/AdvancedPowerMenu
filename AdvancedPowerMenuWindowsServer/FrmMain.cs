using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace AdvancedPowerMenu
{
    public partial class FrmMain : Form
    {
        //Lock Computer
        [DllImport("user32")]
        public static extern void LockWorkStation();

        public FrmMain()
        {
            InitializeComponent();
            runAtStartupToolStripMenuItem.Checked = (bool)Properties.Settings.Default.RunAtStartup;
            tbxPortNumber.Text = Convert.ToString(Properties.Settings.Default.PortNumber);
            RunWebServer();
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

        public async void RunWebServer()
        {
            TcpListener tcpListener = null;

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, Properties.Settings.Default.PortNumber);
                tcpListener.Start();
                
                byte[] request = new byte[1024];

                String data = null;
                await Task.Run(() =>
                {
                    while (true)
                    {
                        notifyIcon.Icon = Properties.Resources.trayIconRunnig;

                        Console.WriteLine("Listening for browser clients...");

                        TcpClient tcpClient = tcpListener.AcceptTcpClient();

                        data = null;

                        NetworkStream networkStream = tcpClient.GetStream();

                        int i;

                        do
                        {
                            i = networkStream.Read(request, 0, request.Length);
                            data = Encoding.ASCII.GetString(request, 0, i);

                            Console.WriteLine("--------Begin of request--------");
                            Console.WriteLine(data);

                            if (data.Contains("inquiry_is=Way"))
                            {
                                Console.WriteLine("Host Name is Requested.");

                                byte[] sendData = Encoding.UTF8.GetBytes(Dns.GetHostName());
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
                            else
                            {
                                Console.WriteLine("Advanced Power Menu was Requested.");

                                byte[] sendData = Encoding.UTF8.GetBytes(htmlPage(htmlActionPost()));
                                networkStream.Write(sendData, 0, sendData.Length);
                            }

                            Console.WriteLine("--------End of request---------");
                            Console.Write("\n");

                        } while (networkStream.DataAvailable);

                        tcpClient.Close();
                    }
                });
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: ", e);
            }
            finally
            {
                tcpListener.Stop();
            }
            notifyIcon.Icon = Properties.Resources.trayIconNotRunning;
        }

        public static String htmlPage(String htmlResponseContent)
        {
            string htmlResponseTop = "<!DOCTYPE html><html><head><title>Advanced Power Menu</title><meta name=\"viewport\" content=\"width=width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no\"><style>.bodybackground {background-color: #252525;font-family: Tahoma;color:white;}.centeroutside {display: flex;justify-content: center;}.center {text-align: center;width:320px;}.button {background-color: #4CAF50;border-radius: 6px;border: none;color: white;padding: 10px 32px;text-align: center;text-decoration: none;display: inline-block;font-size: 17px;width: 310px;font-family:verdana; margin: 10px 0px 0px 0px;}.button:hover {background-color: #3e8e41;}.head {font-size: 26px; font-weight: bold; margin: 20px 0px 20px 0px;}.form{margin: 20px 0px 0px 0px;}.actionisdone{font-size: 23px; font-weight: bold; color: #a6caff; margin: 20px 0px 0px 0px;}.version {font-size: 10px; text-align: right; margin: 5px 6px 0px 0px;}.hr{margin: 10px 5px 0px 5px;}.table{border-radius: 8px;border: 2px solid #4CAF50;padding: 15px;margin: 0px 5px 0px 5px;}.tableresult{border-radius: 8px;background: #1fa3ec;padding: 15px;margin: 0px 5px 0px 5px;}.heads {font-size: 12px;color: #fff;}.details {font-size: 20px;font-weight: bold;color: #a6caff;margin: 0px 0px 00px 0px;}</style></head><body class=\"bodybackground\"><div class=\"centeroutside\"><div class=\"center\"><div class=\"head\">Advanced Power Menu</div>";
            string htmlResponseBottom = "<hr class=\"hr\"><div class=\"version\">APM v1.0 by Venovar</div></div></div></body></html>";

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("HTTP/1.1 200 OK");
            stringBuilder.AppendLine("Content-type: text/html; charset=utf-8");
            stringBuilder.AppendLine("Content-Disposition: inline");
            stringBuilder.AppendLine("Content-Length: " + (htmlResponseTop.Length + htmlResponseContent.Length + htmlResponseBottom.Length));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(htmlResponseTop);
            stringBuilder.AppendLine(htmlResponseContent);
            stringBuilder.AppendLine(htmlResponseBottom);

            String response = stringBuilder.ToString();

            return response;
        }

        public static String htmlComputerDetails()
        {
            string computerDetails = "<div class=\"table\"><div class=\"heads\">Computer Name:</div><div class=\"details\">" + Dns.GetHostName() + "</div><br><div class=\"heads\">Logged User Name:</div><div class=\"details\">" + Environment.UserName + "</div><br><div class=\"heads\">IP Address:</div><div class=\"details\">" + Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString() + "</div></div>";

            return computerDetails;
        }

        public static String htmlActionPost()
        {
            string htmlWelcome = htmlComputerDetails() + "<div class=\"form\"><form method=\"POST\"><input type=\"submit\" class=\"button\" id=\"shutdown\" name=\"action_is\" value=\"Shutdown\"><input type=\"submit\" class=\"button\" id=\"restart\" name=\"action_is\" value=\"Restart\"><input type=\"submit\" class=\"button\" id=\"lock\" name=\"action_is\" value=\"Lock\"></form></div>";

            return htmlWelcome;
        }
        
        public static String htmlActionDone(String action)
        {
            string htmlAction = htmlComputerDetails() + "<br><div class=\"tableresult\"><div class=\"heads\">Action:</div><div class=\"details\">Computer " + action + "</div></div>";

            return htmlAction;
        }

        private void tbxPortNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.PortNumber = Int32.Parse(tbxPortNumber.Text);
            Properties.Settings.Default.Save();

            DialogResult dialogResult = MessageBox.Show("Start Application Manually.", "Done!", MessageBoxButtons.OK);
            if (dialogResult == DialogResult.OK)
            {
                Application.Exit();
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
