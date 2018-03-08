﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.Net;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using log4net;
using System.Net.NetworkInformation;

namespace IndigoPlugin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    

    public partial class App : Application
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool LockWorkStation();

        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        static public string currentVersion { get; set; }
        public string ForegroundApp { get; set; }
        public string CPU { get; set; }
        public string MemLoad { get; set; }
        public string OpSystem { get; set; }
        public string Hostname { get; set; }
        public string MACaddress { get; set; }
        public string localIPaddress { get; set; }
        static public bool updateNeeded { get; set; }
        public ulong idleTime { get; set; }
        public string userName { get; set; }
        public double upTime { get; set; }
        

        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;

        static public System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0]).Value;
        private Mutex _instanceMutex = null;

        

        public void Application_Startup(object sender, StartupEventArgs e)
        {


            Logger.Info(string.Concat(Enumerable.Repeat(Environment.NewLine, 20)));
            Logger.Info("-------------------------------------------------------------------------");
            Logger.Info("-------------------------------------------------------------------------");
            Logger.InfoFormat("-------------------- Starting IndigoPlugin ------------------------------");
            Logger.Info("-------------------------------------------------------------------------");
            Logger.Info("-------------------------------------------------------------------------");
            Logger.Info(string.Concat(Enumerable.Repeat(Environment.NewLine, 5)));
            var SystemVersion = Environment.OSVersion.Version.Major;
            var is64bit = Environment.Is64BitOperatingSystem;

            Logger.InfoFormat(":: System Major Version:   "+SystemVersion.ToString());

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            string ipaddress = IndigoPlugin.Properties.Settings.Default.ipaddress;
            bool setupcomplete = IndigoPlugin.Properties.Settings.Default.setupcomplete;
            bool debuglogging = IndigoPlugin.Properties.Settings.Default.debuglogging;

            // Set variables to nil in case in accessing (can't send html blanks)
            ForegroundApp = "unknown";
            CPU = "unknown";
            MemLoad = "unknown";           
            OpSystem = "unknown";
            Hostname = "unknown";
            MACaddress = "unknown";
            localIPaddress = "";
            currentVersion = "4";
            updateNeeded = false;
            idleTime = 0;
            Logger.Info("-------------------------------------------------------------------------");
            Logger.InfoFormat(":: Application Version   :"+currentVersion.ToString());
            Logger.Info("-------------------------------------------------------------------------");
            Logger.Info("-------------------------------------------------------------------------");
            Logger.InfoFormat(":: Windows 64bit Version   :" + is64bit.ToString());
            Logger.Info("-------------------------------------------------------------------------");
           

            //# Okay Versions across two applications
            //# First Number 0 - ignore
            //# Second Number is the Mac Version -- increasing this without breaking PC app versions
            //# Third Number is the PC Version -- 
            //# e.g 0.2.2 -- 2 is current mac version, 2 is current PC version
            //# if version goes to 0.2.4  -- PC version needs to be updated if less than 4 and will organise message
            //# if version is 0.4.2 -- PC version remains on 2 - so only Mac update needed/done.


            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            Hostname = System.Net.Dns.GetHostName();

            if (debuglogging)
            {
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = log4net.Core.Level.Debug;
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                Logger.Info("Setting Debug Level Logging");
            }
            else
            {
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = log4net.Core.Level.Info;
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                Logger.Info("Setting Info Level Logging Only");
            }

            string startupmessage = "";          
     
            if (ipaddress=="please enter" || setupcomplete==false)
            {
                // Means that Application is not set IP address not entered.
                // Need to have a bool for setup
                Logger.Info("Setup Not complete, opening window for setup");
                startupmessage = "Please setup the Indigo Mac Server and Port.  WinRemote Indigo Plugin must be running";
            }
            else
            {
                startupmessage = "Attempting to connect...";
            }

            System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon();
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/IndigoPlugin;component/Icons/Win_Remote_Icon.ico")).Stream;
            icon.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Dispose();

           // nIcon.Icon = new Icon(@"indigo.ico");
            nIcon.Icon = icon.Icon;
            nIcon.Visible = true;
            nIcon.Text = "Indigo Plugin Communicator.  Startup";
            if (setupcomplete == false)
            {
                nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", startupmessage, System.Windows.Forms.ToolTipIcon.Info);
            }
            //nIcon.Click += nIcon_Click;
            nIcon.DoubleClick += nIcon_Click;

            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();

            this.contextMenu1.MenuItems.AddRange(
                    new System.Windows.Forms.MenuItem[] { this.menuItem1, this.menuItem2, this.menuItem3});
            this.menuItem1.Index = 2;
            this.menuItem1.Text = "E&xit";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);

            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Show Window";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);

            this.menuItem3.Index = 0;
            this.menuItem3.Text = "Hide Window";
            this.menuItem3.Click += new System.EventHandler(this.menuItem3_Click);

            nIcon.ContextMenu = this.contextMenu1;

            // if already setup check connection and then reset setupcomplete as needed
            if (setupcomplete == true)
            {
                // Should already be setup
                // Connect and don't open window
                if (StartupConnect()==false)
                {
                    // continue
                    setupcomplete = false;
                    IndigoPlugin.Properties.Settings.Default.setupcomplete = false;
                }
            }               
            MainWindow wnd = new IndigoPlugin.MainWindow();
            // if not complete show window
            // might need to show then minimise?
            if (setupcomplete == false)
            {
                wnd.Show();
                wnd.connectInfo.Text = "Failed to Connect.  Please check settings and try again.";
            }

            //Setup Main Loop timer
            theMainLoop();        


        }

        public string getCurrentCpuUsage()
        {
            return cpuCounter.NextValue() + "%";
        }

        public string getAvailableRAM()
        {
            return ramCounter.NextValue() + "MB";
        }

        public void theMainLoop()
        {
            //setup timer for call backs
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += Timer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 59);
            dispatcherTimer.Start();
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            //Called every 60 seconds I hope
            //Send info to Indigo

            if (IndigoPlugin.Properties.Settings.Default.setupcomplete==false)
            {
                Logger.Info("Failed to Connect.  Not checking until setup");
                return;
            }

            GetInformation();

            if (SendInfotoIndigo() == true)
            {
                nIcon.Text = "Indigo Plugin Communicator.  Connected. " + DateTime.Now.ToString("hh:mm:ss tt");
            }
                // tick

        }

        public void GetInformation()
        {
            // Get information
            // Start with Host Name as using this not IP for devices
            try
            {
                Hostname = System.Net.Dns.GetHostName();
                Logger.Debug("Hostname set to:" + Hostname);
            }
            catch { }

            // now windows version

            // now CPU load

            try
            {
                CPU = getCurrentCpuUsage();
                Logger.Debug("CPU Usage set to:" + CPU);
            }
            catch { }


            // now Memory Load

            try
            {
                MemLoad = getAvailableRAM();
                Logger.Debug("Memory Available set to:" + MemLoad);
            }
            catch { }

            // then with foreground window
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                uint procId = 0;
                GetWindowThreadProcessId(hWnd, out procId);
                var proc = Process.GetProcessById((int)procId);
                ForegroundApp = proc.ProcessName.ToString();
                Logger.Debug("Foreground App Set to:" + ForegroundApp);
            }
            catch (Exception exc)
            {
                ForegroundApp = "unknown";
                Logger.Error("ForeGround App Error:" + exc);
            }


            //Get MAC address for Wake on Lan
            try
            {
                // only check Mac if still unknown
                // defaults to unknown on restart.
                if (MACaddress == "unknown" && localIPaddress !="")
                {
                    MACaddress = GetMacByIP(localIPaddress);
                }
            }
            catch (Exception exc)
            {
                MACaddress = "unknown";
                Logger.Error("MAC address get error" + exc);
            }
            // get idle time
            try
            {
                //var idle = IdleTimeDetector.GetIdleTimeInfo();
                UInt64 idle = IdleTimeDetector.GetLastInputTime();
                idleTime = idle/60;
               // Logger.Debug("IdleTime: IdleTime SystemUptmeMilliseconds:" + idle.SystemUptimeMilliseconds.ToString());
               //  Logger.Debug("IdleTime: IdleTime LastInputTime:" + idle.LastInputTime.ToString());
                Logger.Debug("IdleTime: IdleTime Seconds:" + idle.ToString());

                Logger.Debug("IdleTime: Minutes:" + idleTime.ToString());
            }
            catch (Exception exc)
            {
                Logger.Error("Idletime exc:" + exc);
            }
            //get current username
            try
            {
                string user = Environment.UserName.ToString();
                Logger.Debug("Username Returned as:" + user);
                userName = user;
            }
            catch (Exception exc)
            {
                Logger.Error("Username exception" + exc);
            }

            try
            {
                // deal with 24.9 days uptime
                // Use GetTickCount if Vista and above.
                
                if (System.Environment.OSVersion.Version.Major < 6)
                {
                    Int32 tickcount = System.Environment.TickCount & Int32.MaxValue;
                    upTime = TimeSpan.FromMilliseconds(tickcount).TotalHours;

                    upTime = Math.Truncate(100 * upTime) / 100;

                    Logger.Debug("upTime: Environment.TickCount equals:" + tickcount.ToString());
                    Logger.Debug("upTime =" + upTime);   
                }
                else
                {
                    UInt64 tickcount = GetTickCount64();
                    var doubletickcount = Convert.ToDouble(tickcount);
                    upTime = TimeSpan.FromMilliseconds(doubletickcount).TotalHours;
                    Logger.Debug("upTime Before Truncate =" + upTime);

                    upTime = System.Math.Round(upTime, 3);
                   // upTime = Math.Truncate(100 * upTime) / 100;

                    Logger.Debug("upTime: GetTickCount64 used, equals:" + tickcount.ToString());
                    Logger.Debug("upTime =" + upTime);
                }

            }
            catch (Exception exc)
            {
                Logger.Error("Uptime Exception" + exc);
                upTime = 0;
            }


        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            
            Logger.Error("UnHandled Exception Occured.  Please let developer know and will 'Handle' " + e.Exception.Message.ToString()+ System.Environment.NewLine+  e.Exception.StackTrace.ToString());

            string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        public string GetMacByIP(string ipAddress)
        {

            try
            {
                // grab all online interfaces
                var query = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n =>
                        n.OperationalStatus == OperationalStatus.Up && // only grabbing what's online
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(_ => new
                    {
                        PhysicalAddress = _.GetPhysicalAddress(),
                        IPProperties = _.GetIPProperties(),
                    });

                // grab the first interface that has a unicast address that matches your search string
                var mac = query
                    .Where(q => q.IPProperties.UnicastAddresses
                        .Any(ua => ua.Address.ToString() == ipAddress))
                    .FirstOrDefault()
                    .PhysicalAddress;

                // return the mac address with formatting (eg "00-00-00-00-00-00")
                return String.Join("-", mac.GetAddressBytes().Select(b => b.ToString("X2")));
            }
            catch (Exception exc)
            {
                Logger.Error("GetMacbyIP error:" + exc);
                return "unknown";
            }
        }

        void nIcon_Click(object sender, EventArgs e)
        {
            //events comes here
                if (MainWindow.WindowState != WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Minimized;
                    MainWindow.Visibility = Visibility.Hidden;
                    MainWindow.ShowInTaskbar = false;
                }
                else
                {
                    MainWindow.Visibility = Visibility.Visible;
                    MainWindow.ShowInTaskbar = true;
                    SystemCommands.RestoreWindow(MainWindow);
                }
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            Logger.Debug("Exit selected.");
            nIcon.Visible = false;
            nIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
        private void menuItem2_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            Logger.Debug("Show Window selected.");
            MainWindow.Visibility = Visibility.Visible;
            MainWindow.ShowInTaskbar = true;
            SystemCommands.RestoreWindow(MainWindow); 
        }
        private void menuItem3_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            Logger.Debug("Hide Window selected.");
            MainWindow.WindowState = WindowState.Minimized;
            MainWindow.Visibility = Visibility.Hidden;
            MainWindow.ShowInTaskbar = false;
        }


        public bool SendInfotoIndigo()
        {
            Logger.Debug("SendInfotoIndigo Called.");
            string ServerIP = IndigoPlugin.Properties.Settings.Default.ipaddress;
            try
            {
                string api = @"http://" + ServerIP + "/Information?ForeGroundApp="+ForegroundApp+"&CPU="+CPU+"&MemLoad="+MemLoad+"&Hostname="+Hostname+"&MAC="+MACaddress+"&Idle="+idleTime+"&userName="+userName+"&upTime="+upTime;
                //string data = "";
                // strcontentType = "html";
                // Create a request using a URL that can receive a post. 
                WebRequest request = WebRequest.Create(api);
                // Set the Method property of the request to POST.
                request.Method = "POST";
                // Create POST data and convert it to a byte array.
                string postData = Hostname;
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";
                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;
                // Get the request stream.
                Stream dataStream = request.GetRequestStream();
                // Write the data to the request stream.
                //

                //
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                dataStream.Close();
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                var statusdescription = ((HttpWebResponse)response).StatusDescription.ToString();
                var headers = ((HttpWebResponse)response).Headers.ToString();

                var ipHeaderkey = ((HttpWebResponse)response).Headers.Keys[0].ToString();
                var ipHeaderResult = ((HttpWebResponse)response).Headers["IPAddress"].ToString();
                var versionheader = ((HttpWebResponse)response).Headers["IndigoPluginVersion"].ToString();

                // Logger.Debug(statusdescription);
                Logger.Debug("--------Response Status Received:"+statusdescription);
                //Logger.Debug("--------Headers Recevied:" + headers);
               // Logger.Debug("--------IpHeader Key[0] Recevied:" + ipHeaderkey);
               // Logger.Debug("--------IpHeader Result for key IPAddress Recevied:" + ipHeaderResult);
              //  Logger.Debug("--------IpHeader Result for key IndigoPluginVersion Recevied:" + versionheader);

                localIPaddress = ipHeaderResult;

                // Get the stream containing content returned by the server.
                dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();              
                // Display the content.
                //Console.WriteLine(responseFromServer);
                // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();
                // App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Connected", System.Windows.Forms.ToolTipIcon.Info);
                // App.nIcon.Text = "Indigo Plugin Communicator.  Connected";             
                Logger.Debug("Information sent to Indigo....");
                Logger.Debug(api);
                Logger.Debug("Response from Server:" + responseFromServer);
                // Check for Commands back from Indigo

                checkCommands(statusdescription);

                return true;
            }
            catch (Exception exc)
            {
                App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Failed to Connect", System.Windows.Forms.ToolTipIcon.Error);
                App.nIcon.Text = "Indigo Plugin Communicator.  Failed to Connect.";
                Logger.Error("Error Communicating with Indigo Server.." + exc);
                return false;
            }
        }
        public void checkCommands(string response)
        {
            Logger.Debug("checkCommand Called with data:" + response);

            if (!response.Contains("COMMAND"))
            {
                Logger.Debug("No COMMAND found exiting checkCommands");
            }

            if (response.Contains("COMMAND MESSAGE"))
            {
                Logger.Debug("COMMAND MESSAGE Found");
                var msg = "IndigoPlugin Message";
                int start_index = response.IndexOf(':') + 1;
                int end_index = response.LastIndexOf(':');
                int length = end_index - start_index;
                msg = response.Substring(start_index, length);             
                showMessage(msg);
            }

            if (response.Contains("COMMAND OFF"))
            {
                //Shut down computer
                showMessage("Now Shutting Down the Computer...  You have 10 seconds...");
                var psi = new ProcessStartInfo("shutdown", "/s /t 10");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);
            }

            if (response.Contains("COMMAND LOCK"))
            {
                // Lock the Computer
                Logger.Debug("COMMAND LOCK Found; Locking Computer");              
                try
                {
                    LockWorkStation();
                }
                catch (Exception exc)
                {
                    Logger.Debug("Exception in Lock:" + exc);
                }
                    
                    
            }
        }

        public void ShutdownCommand()
        {
            Logger.Debug("ShutdownCommand called");

        }


        public bool StartupConnect()
        {
            string ServerIP = IndigoPlugin.Properties.Settings.Default.ipaddress;
            try
            {
                string api = @"http://" + ServerIP + "/StartupConnect/"+Hostname;
                //string data = "";
                // string contentType = "html";
                // Create a request using a URL that can receive a post. 
                WebRequest request = WebRequest.Create(api);
                // Set the Method property of the request to POST.
                request.Method = "POST";
                // Create POST data and convert it to a byte array.
                string postData = Hostname;
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";
                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;
                // Get the request stream.
                Stream dataStream = request.GetRequestStream();
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                dataStream.Close();
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

                // Get the stream containing content returned by the server.
                dataStream = response.GetResponseStream();
                var headers = ((HttpWebResponse)response).Headers.ToString();
                // Logger.Debug(statusdescription);
                Logger.Debug("--------Headers Recevied:" + headers);
                //
                // Check Version here at Startup and also in Manual Connect Page
                //
                var versionheader = ((HttpWebResponse)response).Headers["IndigoPluginVersion"].ToString();
                Logger.Debug("-------- Version Received:" + versionheader);
                Logger.Debug("-------- Current App Version:" + currentVersion);
                // Grab Last characters seperated by . character
              
                var lastversiondigit = versionheader.Substring(versionheader.LastIndexOf(".")+1);
                Logger.Debug("---------- Last Digit Version Equals:" + lastversiondigit);
                //Test update system
//                lastversiondigit = "3";

                try
                {
                    if (int.Parse(lastversiondigit) > int.Parse(currentVersion))
                    {
                        Logger.Info("---------------Updated Required.  Please download and Update your PC's Software-------------");
                        updateNeeded = true;
                    }
                }
                catch (Exception exc)
                {
                    Logger.Error("Exception in StartupConnect Parsing Version Numbers:" + exc);
                }
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                if (updateNeeded==true)
                {
                    App.nIcon.ShowBalloonTip(15000, "Indigo Plugin Communicator", "Update Needed for PC Software; Please download and install to ensure proper functioning", System.Windows.Forms.ToolTipIcon.Error);
                    App.nIcon.Text = "Indigo Plugin Communicator.  Connected.  Updated Needed.";
                    return true;
                }

                App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Connected", System.Windows.Forms.ToolTipIcon.Info);
                App.nIcon.Text = "Indigo Plugin Communicator.  Connected";
                Logger.Info("Connected to Indigo....");
                return true;
            }
            catch (Exception exc)
            {
                App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Failed to Connect", System.Windows.Forms.ToolTipIcon.Error);
                App.nIcon.Text = "Indigo Plugin Communicator.  Failed to Connect.";
                Logger.Error("Error Communicating with Indigo Server.." + exc);
                return false;
            }
        }

        //Check for multiple versions of app and don't allow
        protected override void OnStartup(StartupEventArgs e)
        {
            // check that there is only one instance of the control panel running...
            bool createdNew;
            _instanceMutex = new Mutex(true, @"Global\IndigoPlugin", out createdNew);
            if (!createdNew)
            {
                _instanceMutex = null;
                MessageBox.Show("Only one IndigoPlugin should run at once.");
                Application.Current.Shutdown();
                return;
            }
            base.OnStartup(e);
        }

        public void showMessage(string msg)
        {
            App.nIcon.ShowBalloonTip(10000, "Indigo Plugin Communicator", msg, System.Windows.Forms.ToolTipIcon.Info);
            App.nIcon.Text = "Indigo Plugin Communicator.  Connected";
            Logger.Debug("Message: Sent:"+msg);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_instanceMutex != null)
                _instanceMutex.ReleaseMutex();

            nIcon.Visible = false;
            nIcon.Dispose();

            base.OnExit(e);
        }

    }
    public static class IdleTimeDetector
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        public static uint GetLastInputTime()
        {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }
    }

    //   public static class IdleTimeDetector




    //{
    //        [DllImport("user32.dll")]
    //        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    //        public static IdleTimeInfo GetIdleTimeInfo()
    //        {
    //            int systemUptime = Environment.TickCount,
    //                lastInputTicks = 0,
    //                idleTicks = 0;

    //            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
    //            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
    //            lastInputInfo.dwTime = 0;

    //            if (GetLastInputInfo(ref lastInputInfo))
    //            {
    //                lastInputTicks = (int)lastInputInfo.dwTime;

    //                idleTicks = systemUptime - lastInputTicks;
    //            }

    //            return new IdleTimeInfo
    //            {
    //                LastInputTime = DateTime.Now.AddMilliseconds(-1 * idleTicks),
    //                IdleTime = new TimeSpan(0, 0, 0, 0, idleTicks),
    //                SystemUptimeMilliseconds = systemUptime,
    //            };
    //        }
    //    }

    //    public class IdleTimeInfo
    //    {
    //        public DateTime LastInputTime { get; internal set; }

    //        public TimeSpan IdleTime { get; internal set; }

    //        public int SystemUptimeMilliseconds { get; internal set; }
    //    }

    //    internal struct LASTINPUTINFO
    //    {
    //        public uint cbSize;
    //        public uint dwTime;
    //    }

}
