using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using ArduinoWebSensorsMonitor.Properties;
using System.Collections.Specialized;

namespace ArduinoWebSensorsMonitor
{
    public partial class frmSettings : Form
    {
        public frmSettings()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            string[] arrSensorNames = new string[8];
            string[] arrSensorAlarmsValues = new string[8];
            string[] arrSensorEnabled = new string[8];

            foreach (Control curr_textbox in this.Controls)
            {
                if (curr_textbox.Name.Contains("txtSensor"))
                {
                    if (curr_textbox.Name.Contains("Name"))
                    {
                        //extract textbox arr index from Tag prop
                        arrSensorNames[int.Parse((string)curr_textbox.Tag)] = curr_textbox.Text;
                    }
                    
                    if (curr_textbox.Name.Contains("Alarm"))
                    {
                        //extract textbox arr index from Tag prop
                        arrSensorAlarmsValues[int.Parse((string)curr_textbox.Tag)] = curr_textbox.Text;
                    }

                }

                if (curr_textbox.Name.Contains("chkSensor"))
                {
                    //extract textbox arr index from Tag prop
                    arrSensorEnabled[int.Parse((string)curr_textbox.Tag)] = (curr_textbox as CheckBox).Checked.ToString();
                }
            }

            Settings.Default.Reset();
            Settings.Default.arrSensorNames = new StringCollection();
            Settings.Default.arrSensorAlarms = new StringCollection();
            Settings.Default.arrSensorEnabled = new StringCollection();
            Settings.Default.strServerURL = txtServerURL.Text;
            Settings.Default.arrSensorNames.AddRange(arrSensorNames);
            Settings.Default.arrSensorAlarms.AddRange(arrSensorAlarmsValues);
            Settings.Default.arrSensorEnabled.AddRange(arrSensorEnabled);
            Settings.Default.Save();
            this.Close();
        }

        private bool ValidateInputs()
        {
            //check URL field
            if (txtServerURL.Text.Length < 1)
            {
                MessageBox.Show("Введите адрес сервера!", "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtServerURL.Focus();
                return false;
            }

            Uri server_url = null;
            try
            {
                server_url = new Uri(txtServerURL.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Введите валидный адрес сервера!", "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtServerURL.Focus();
                return false;
            }

            //check sensors Alarm values fields like "txtSensor0Alarm"
            foreach (Control curr_alarm in this.Controls)
            {
                if (curr_alarm.Name.Contains("txtSensor") && curr_alarm.Name.Contains("Alarm"))
                {
                    float curr_temp_value = 0;
                    //replace rus "," -> "."
                    if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == "," && curr_alarm.Text.Contains("."))
                    {
                        curr_alarm.Text = curr_alarm.Text.Replace(".", ",");
                    }

                    try
                    {
                        curr_temp_value = float.Parse(curr_alarm.Text, NumberStyles.Number);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Введите корректное значение порога срабатывания! " + ex.Message, "WebSensorsMonitor", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        curr_alarm.Focus();
                        return false;
                    }
                }
            }

            return true;
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            Settings.Default.Reload();
            if (Settings.Default.arrSensorAlarms != null)
            {
                txtServerURL.Text = Settings.Default.strServerURL;
                for (int i = 0; i < 8; i++)
                {
                    (this.Controls["chkSensor" + i.ToString()] as CheckBox).Checked = Settings.Default.arrSensorEnabled[i] == "True" ? true : false;
                    this.Controls["txtSensor" + i.ToString() + "Name"].Text = Settings.Default.arrSensorNames[i];
                    this.Controls["txtSensor" + i.ToString() + "Alarm"].Text = Settings.Default.arrSensorAlarms[i];
                }
            }
        }


    }
}
