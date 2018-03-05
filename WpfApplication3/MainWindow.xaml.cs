using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;
using System.Net;
using System.IO;
using log4net;
using Microsoft.Win32;

namespace IndigoPlugin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            Logger.Info("MainWindow Called.");
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void ClickConnect(object sender, RoutedEventArgs e)
        {

            Logger.Info("ClickConnect Run.");
            var box = sender as System.Windows.Controls.TextBox;
            var Hostname = System.Net.Dns.GetHostName();
            try
            {
                string api = @"http://" + ServerIP.Text +"/StartupConnect/";
                IndigoPlugin.Properties.Settings.Default.ipaddress = ServerIP.Text;
              
                connectInfo.Text = "Trying to connect....";
                connectInfo.Text += " to   " + ServerIP.Text + System.Environment.NewLine;
                Logger.Info("Connecting to:" + ServerIP + " and :" + api);

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
                string responsestring = ((HttpWebResponse)response).StatusDescription.ToString();
                var headers = ((HttpWebResponse)response).Headers.ToString();
                // Logger.Debug(statusdescription);
                Logger.Debug("--------Response Status Received:" + responsestring);
                Logger.Debug("--------Headers Recevied:" + headers);


                var versionheader = ((HttpWebResponse)response).Headers["IndigoPluginVersion"].ToString();
                Logger.Debug("-------- Version Received:" + versionheader);
                Logger.Debug("-------- Current App Version:" + App.currentVersion);
                // Grab Last characters seperated by . character

                var lastversiondigit = versionheader.Substring(versionheader.LastIndexOf(".") + 1);
                Logger.Debug("---------- Last Digit Version Equals:" + lastversiondigit);

                // Test for update alert.
               // lastversiondigit = "4";

                try
                {
                    if (int.Parse(lastversiondigit) > int.Parse(App.currentVersion))
                    {
                        Logger.Info("---------------Updated Required.  Please download and Update your PC's Software-------------");
                        connectInfo.Text = "Application Updated Required.  Please download and Update this Software.  "+ System.Environment.NewLine + System.Environment.NewLine;
                        App.updateNeeded = true;
                    }
                }
                catch (Exception exc)
                {
                    Logger.Error("Exception in ClickConnect Parsing Version Numbers:" + exc);
                }


                Logger.Info("Received this response from server:"+ responsestring);
            // Get the stream containing content returned by the server.
                dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
            // Read the content.
                string responseFromServer = reader.ReadToEnd();
            // Display the content.
            //Console.WriteLine(responseFromServer);
               // connectInfo.Text += responseFromServer.ToString();

                Logger.Info("Received Response:" + responseFromServer.ToString());

            // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();

                if (App.updateNeeded == true)
                {
                    App.nIcon.ShowBalloonTip(15000, "Indigo Plugin Communicator", "Update Needed for PC Software; Please download and install to ensure proper functioning", System.Windows.Forms.ToolTipIcon.Error);
                    App.nIcon.Text = "Indigo Plugin Communicator.  Connected.  Updated Needed.";
                    connectInfo.Text += "Connection Successful.  Update recommended." + System.Environment.NewLine+ System.Environment.NewLine+ "Select Debug options and Save to continue.";
                    IndigoPlugin.Properties.Settings.Default.setupcomplete = true;
                    Properties.Settings.Default.Save();
                    Logger.Info("Connection Successful");
                    return;
                }

                App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Connected", System.Windows.Forms.ToolTipIcon.Info);
                App.nIcon.Text = "Indigo Plugin Communicator.  Connected";
                connectInfo.Text += "Connection Successful.  Select options and Save to continue.";
                IndigoPlugin.Properties.Settings.Default.setupcomplete = true;
                Properties.Settings.Default.Save();
                Logger.Info("Connection Successful");
                return;
            }
            catch (Exception exc)
            {
                connectInfo.Text = "------- Error. Failed to Connect.  ----------";
                connectInfo.Text += exc.ToString();
                App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Failed to Connect", System.Windows.Forms.ToolTipIcon.Error);
                App.nIcon.Text = "Indigo Plugin Communicator.  Failed to Connect.";
                Logger.Info("Failed Connection with exc:" + exc.ToString());
                return;

            }
      
            
        }
        private void OnExit(object sender, ExitEventArgs e)
        {
            Properties.Settings.Default.Save();
            Logger.Info("OnExit called");

        }

        private void debug_CheckedChanged(object sender, RoutedEventArgs e)
        {

            Logger.Info("debug_CheckChanged Called");
            if (checkboxdebug.IsChecked == true)
            {
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = log4net.Core.Level.Debug;
                ((log4net.Repository.Hierarchy.Logger)Logger.Logger).Level = log4net.Core.Level.Debug;
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                Logger.Info("Setting Logging to Debug Level");
            }
     
            if (checkboxdebug.IsChecked == false)
            {
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = log4net.Core.Level.Info;
                ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                Logger.Info("Setting Logging to Info Level");
                
            }
            Properties.Settings.Default.Save();

        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {

            Logger.Info("SaveSettings Called");
            Properties.Settings.Default.setupcomplete = true;
            Properties.Settings.Default.Save();
            this.WindowState = WindowState.Minimized;
            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
           // App.nIcon.ShowBalloonTip(5000, "Indigo Plugin Communicator", "Communicating...", System.Windows.Forms.ToolTipIcon.Info);


        }
    }
}
