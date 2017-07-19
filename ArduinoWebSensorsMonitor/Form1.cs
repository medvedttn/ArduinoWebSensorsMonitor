using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Media;
using System.Net.Cache;
using ArduinoWebSensorsMonitor.Properties;
using System.Diagnostics;

namespace ArduinoWebSensorsMonitor
{
    public partial class Form1 : Form
    {
        static DateTime last_write_log_time = new DateTime(1983,10,1);

        public Form1()
        {
            InitializeComponent();
        }

        private void ParseWebPage(Uri server_url)
        {
            //get web-server HTML page
            HttpWebResponse myHttpWebResponse = null;
            //debug only
            //FileWebResponse myFileWebResponse = null;
            StreamReader resp_stream = null;
            string html_text = "";
            
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            HttpWebRequest.DefaultCachePolicy = noCachePolicy;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(server_url);
            //debug only
            //FileWebRequest myHttpWebRequest = (FileWebRequest)FileWebRequest.Create(server_url);

            myHttpWebRequest.Method = "GET";
            myHttpWebRequest.ContentType = "text/html";
            myHttpWebRequest.Timeout = 5000;
            myHttpWebRequest.KeepAlive = true;
            myHttpWebRequest.CachePolicy = noCachePolicy;
            
            //release only
            myHttpWebRequest.ReadWriteTimeout = 5000;
            myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:13.0) Gecko/20100101 Firefox/13.0.1";
            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                //debug only
                //myFileWebResponse = (FileWebResponse)myHttpWebRequest.GetResponse();
                //resp_stream = new StreamReader(myFileWebResponse.GetResponseStream(), true);
                resp_stream = new StreamReader(myHttpWebResponse.GetResponseStream(), true);
                html_text = resp_stream.ReadToEnd();
            }
            catch (Exception ex)
            {
                tmrTimer.Stop();
                MessageBox.Show("HTTP server read error!" + ex.Message, "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                resp_stream.Close();
                myHttpWebResponse.Close();
                //debug only
                //myFileWebResponse.Close();
            }

            //parse HTML page
            int curr_sensor_word_pos = 0, curr_sensor_index_pos=0;
            int sensor_index = 0;
            //temperature HTML line example
            //<font size=4 color=#C0C7C1><b>Sensor<b></font> 0: 23.31C - OLD
            //<b>Sensor<b>0: 23.50C
            int word_det_len = @"Sensor<b>".Length;
            string curr_temp = "";

            //Sensor[0..8].Text : xx.xxC
            //clear prev text data value for Sensor0..7 fields
            for (int i = 0; i < 8; i++)
            {
                grpSensors.Controls["txtSensor" + i.ToString()].Text = "";
            }
            
            //fill Sersors text data
            while (curr_sensor_word_pos >= 0)
            {
                if (sensor_index >= 7) break;    //max sensors 8 - indexes [0;7]
                curr_sensor_word_pos = html_text.IndexOf("Sensor", curr_sensor_word_pos + word_det_len, StringComparison.InvariantCultureIgnoreCase);
                curr_sensor_index_pos = curr_sensor_word_pos + word_det_len + 0;
                if (curr_sensor_word_pos > 0)
                {
                    // 0 chars are "0: "
                    // 5 chars are "23.50"
                    curr_temp = html_text.Substring(curr_sensor_word_pos + word_det_len + 3, 5);
                    sensor_index = int.Parse(html_text.Substring(curr_sensor_index_pos, 1));
                    grpSensors.Controls["txtSensor" + sensor_index.ToString()].Text = curr_temp;
                }
            }
        }


        private void CheckSensorAlarms()
        {
            float curr_temp=0, curr_temp_alarm=0;
            //sure that Settings already loaded
            if (Settings.Default.arrSensorAlarms == null) return;

            try
            {
                foreach (Control curr_control in grpSensors.Controls)
                {
                    if (curr_control.Name.Contains("txtSensor") && 
                        curr_control.Text.Length > 0)
                    {
                        if (Boolean.Parse(Settings.Default.arrSensorEnabled[int.Parse((string)curr_control.Tag)]) == true)
                        {
                            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "," && curr_control.Text.Contains("."))
                            {
                                curr_control.Text = curr_control.Text.Replace(".", ",");
                            }

                            curr_temp = float.Parse(curr_control.Text);
                            curr_temp_alarm = float.Parse(Settings.Default.arrSensorAlarms[int.Parse((string)curr_control.Tag)]);

                            //compare temps
                            if (curr_temp > curr_temp_alarm)
                            {
                                curr_control.BackColor = Color.Red;
                                //Console.Beep();
                                SystemSounds.Hand.Play();
                            }
                            else
                            {
                                curr_control.BackColor = SystemColors.Control;
                            }
                        }
                        else
                        {
                            curr_control.BackColor = SystemColors.Control;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in check sensors for alarms! " + ex.Message, "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
        }


        private void btnUpdate_Click(object sender, EventArgs e)
        {
            Uri server_url = new Uri(Settings.Default.strServerURL);
            ParseWebPage(server_url);
            CheckSensorAlarms();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //debug ONLY
            //txtServerURL.Text = @"http://medvedttn.github.io/server_page.html";

            Version curr_exe_ver = Assembly.GetExecutingAssembly().GetName().Version;
            string app_v = " v" + curr_exe_ver.Major.ToString() + "." + curr_exe_ver.Minor.ToString();
            Text += app_v;
            WriteLog(this.Text + " started-----");

            Settings.Default.Reload();
            if (Settings.Default.arrSensorNames != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (Settings.Default.arrSensorNames[i] != null)
                    {
                        grpSensors.Controls["lblSensor" + i.ToString()].Text = Settings.Default.arrSensorNames[i];
                    }
                }

            }
        }

        private void btnStartMonitoring_Click(object sender, EventArgs e)
        {
            if (Settings.Default.arrSensorAlarms==null)
            {
                tmrTimer.Stop();
                btnUpdate.Enabled = true;
                btnStartMonitoring.Enabled = true;
                btnSettings.Enabled = true;
                MessageBox.Show("Введите настройки приложения!", "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                btnSettings.Focus();
                return;
            }

            //all data valid, start timer
            btnStartMonitoring.Enabled = false;
            btnUpdate.Enabled = false;
            btnSettings.Enabled = false;
            tmrTimer.Start();
        }

        private void btnStopMonitoring_Click(object sender, EventArgs e)
        {
            btnStartMonitoring.Enabled = true;
            btnUpdate.Enabled = true;
            btnSettings.Enabled = true;
            tmrTimer.Stop();
        }

        private void tmrTimer_Tick(object sender, EventArgs e)
        {
            if (Settings.Default.arrSensorAlarms == null)
            {
                tmrTimer.Stop();
                MessageBox.Show("Настройки не заданы(сервер, пороги срабатывания...)", "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                btnSettings.Focus();
                return;
            }

            Uri server_url = new Uri(Settings.Default.strServerURL);
            ParseWebPage(server_url);
            CheckSensorAlarms();
            WriteLog();
        }

        private void WriteLog()
        {
            string log_file_name = @"Sensors_log.txt";
            string sensors_ids = ReturnSensorAlarms();
            
            TimeSpan alarms_log_interval = new TimeSpan(0, 5, 0);
            if ((DateTime.Now - last_write_log_time) >= alarms_log_interval)
            {
                if (sensors_ids.Length > 1)
                {
                    using (StreamWriter curr_log = new StreamWriter(log_file_name, true))
                    {
                        curr_log.WriteLine(DateTime.Now.ToString() + " Sensors: " + sensors_ids + " Warning:Temperature Alarm");
                        curr_log.Flush();
                        last_write_log_time = DateTime.Now;
                    }
                }
            }
        }

        private void WriteLog(string text_log)
        {
            string log_file_name = @"Sensors_log.txt";
            
            using (StreamWriter curr_log = new StreamWriter(log_file_name, true))
            {
                curr_log.WriteLine(DateTime.Now.ToString() + " " + text_log);
                curr_log.Flush();
            }
        }

        private string ReturnSensorAlarms()
        {
            string res = "";
            Settings.Default.Reload();

            for (int i=0;i<8;i++)
            {
                Control curr_temp = grpSensors.Controls["txtSensor" + i.ToString()];
                if (curr_temp.Text.Length>1)
                {
                    string curr_control_tag = (string)curr_temp.Tag;
                    if (Boolean.Parse(Settings.Default.arrSensorEnabled[int.Parse(curr_control_tag)]) == true)
                    {
                        if (float.Parse(curr_temp.Text) > float.Parse(Settings.Default.arrSensorAlarms[int.Parse(curr_control_tag)]))
                        {
                            res += curr_control_tag + "/";
                        }
                    }
                }
            }

            return res;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            frmSettings frm_Settings = new frmSettings();
            frm_Settings.ShowDialog();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            WriteLog("ArduinoWebSensorsMonitor closed.-----");
        }

        private void btnLogs_Click(object sender, EventArgs e)
        {
            string curr_exe_folder = "";
            curr_exe_folder = AppDomain.CurrentDomain.BaseDirectory;
            Process.Start("explorer.exe", curr_exe_folder);
        }

       
    }
}
