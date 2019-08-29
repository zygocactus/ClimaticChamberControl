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
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;// for FileExplorer_Click

using NLog;
using ModbusInterfaceLib;

namespace ClimaticChamberControl_GUI
{
    /// <summary>
    /// Interaktionslogik für CCC_MainWindow.xaml
    /// </summary>
    public partial class CCC_MainWindow : Window
    {
        SerialInterfaceUSB _siusb;
        ModbusInterface_Base _mbInterface;
        DataStore _ds;
        PIDcontroller _pid;

        bool startThreadactDA = false;
        bool startThreadClimaticControl = false;

        public CCC_MainWindow()
        {
            InitializeComponent();
            _ds = new DataStore();
            _pid = new PIDcontroller(_ds);
            _siusb = new SerialInterfaceUSB(this, _ds);
            _siusb.PIDLink = _pid;
            _pid.Siusb = _siusb;
            _ds.InOperation = false;
        }

        string comPortNameModbus;

        private void Connect_Click(object sender, EventArgs e)
        {

            _siusb.comPortNameUSB = "COM7";
            comPortNameModbus = "COM9";
            try
            {
                _siusb.connect = true;
                _siusb.Connect();
                if (startThreadactDA == false)
                {
                    Thread thrUSB = new Thread(new ThreadStart(_siusb.actDA));
                    thrUSB.Start();
                    startThreadactDA = true;
                }
                //InitChiller();
                Connect.IsEnabled = false;
                Start.IsEnabled = true;
                Stop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Start_Click(object sender, EventArgs e)
        {
            if (SollTemp.Text.ToString() == "")
            {
                MessageBox.Show(string.Format("Es müssen noch SOLL-Werte gesetzt werden."), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            if (SaveLocation.Text.ToString() == "")
            {
                MessageBox.Show(string.Format("Es muss noch ein Pfad gewählt werden."), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                _ds.Path = SaveLocation.Text;
                _ds.InOperation = true;
                Stop.IsEnabled = true;
                Start.IsEnabled = false;
                SollTemp.IsEnabled = false;
                SollabsHumi.IsEnabled = false;
                _ds.GenerateFile();
                _ds.StoreDATA();
                _pid.SOLLtemp = Convert.ToDouble(SollTemp.Text);
                _pid.SOLLhumi = Convert.ToDouble(SollabsHumi.Text);
                
                if (startThreadClimaticControl == false)
                {
                    _pid.ClimaticControl();
                    startThreadClimaticControl = true;
                }
            }
        }
        private void Stop_Click(object sender, EventArgs e)
        {
            _siusb.connect = false;
            _ds.InOperation = false;
            SollTemp.IsEnabled = true;
            SollabsHumi.IsEnabled = true;
            _ds.writeTimer.Stop();//stop writing in parameter text file
            Connect.IsEnabled = true;
            Start.IsEnabled = false;
            Stop.IsEnabled = false;
            _siusb.Disconnect();
        }
        public void FileExplorer_Click(object sender, EventArgs e)
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                _ds.Path = folderDialog.SelectedPath;
                SaveLocation.Text = _ds.Path;
            }
        }
        private static void OpenExplorer(string path)
        {
            if (Directory.Exists(path))
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
        }

        public string Temperature
        {
            get
            {
                return temp.Content.ToString();
            }
            set
            {
                if (temp.Dispatcher.CheckAccess())
                {
                    this.temp.Content = value;
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(() => this.temp.Content = value));
                }
            }
        }
        public string RelativeHumidity
        {
            get
            {
                return rhumi.Content.ToString();
            }
            set
            {
                if (rhumi.Dispatcher.CheckAccess())
                {
                    this.rhumi.Content = value;
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.rhumi.Content = value));
                }
            }
        }
        public string AbsoluteHumidity
        {
            get
            {
                return abshumi.Content.ToString();
            }
            set
            {
                if (abshumi.Dispatcher.CheckAccess())
                {  
                this.abshumi.Content = value;
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.abshumi.Content = value));
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _siusb.connect = false;
            _ds.InOperation = false;
            _siusb.Disconnect();
            //e.Cancel = true;
        }


        ////////////NatinalLabChiller////////////
        private static Logger log = LogManager.GetCurrentClassLogger();

        byte _modbusAddress = 0;

        private void InitChiller()
        {
            string currentMethod = "Init";
            try
            {
                // close current interface if already connected
                try
                {
                    if (_mbInterface != null)
                    {
                        ModbusConnectionManager.Instance.CloseModbusInterface(_modbusAddress, _mbInterface);
                        _mbInterface = null;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("[{0}] - error closing existing interface. Proceeding anyway. ex: {1}", currentMethod, ex.Message);
                }

                _modbusAddress = 1;

                _mbInterface = ModbusConnectionManager.Instance.GetModbusInterface(_modbusAddress, comPortNameModbus, 9600, 8, System.IO.Ports.StopBits.One, System.IO.Ports.Parity.None, 150, 150);

                if (_mbInterface.IsConnected)
                {
                    var workTemp = GetWorkingTemperature();
                    _pid.ISTtempChiller = Convert.ToDouble(workTemp);
                    var bathTemp = GetBathTemperature();

                    var controller_status = GetControllerStatus();

                    log.Info("[{0}] - NationalLab: bath temperature={1}°C  working temperature={2}°C   controller_status={3}", currentMethod, bathTemp, workTemp, controller_status);

                    double settingWorkingTemperature = 18.0;//Set Temperature
                    SetWorkingTemperature(settingWorkingTemperature);

                    workTemp = GetWorkingTemperature();
                    if (Math.Abs(workTemp - settingWorkingTemperature) > 0.1)
                    {
                        throw new Exception(string.Format("NationalLab: Error setting working temperature. (value to set: {0}   current value: {1})", settingWorkingTemperature, workTemp));
                    }
                }
                else
                {
                    throw new Exception("Couldn't connect to serial port.");
                }
            }
            catch (Exception ex)
            {
                log.Error("[{0}] - ex: {1}", currentMethod, ex.Message);
                throw;
            }
            //start thread for control settingWorkingTemperature
            Thread thrChiller = new Thread(new ThreadStart(ChillerControl));
            thrChiller.Start();
        }

        void ChillerControl()
        {
            while (_ds.InOperation == true)
            {
                if (_mbInterface.IsConnected)
                {
                    var workTemp = GetWorkingTemperature();
                    _pid.ISTtempChiller = Convert.ToDouble(workTemp);
                    var bathTemp = GetBathTemperature();

                    var controller_status = GetControllerStatus();

                    double settingWorkingTemperature = _pid.SOLLtempChiller;//Set Temperature
                    SetWorkingTemperature(settingWorkingTemperature);

                    workTemp = GetWorkingTemperature();
                }
                else
                {
                    _siusb.connect = false;
                    _ds.InOperation = false;
                    SollTemp.IsEnabled = true;
                    SollabsHumi.IsEnabled = true;
                    _ds.writeTimer.Stop();//stop writing in parameter text file
                    Connect.IsEnabled = true;
                    Start.IsEnabled = false;
                    Stop.IsEnabled = false;
                    _siusb.Disconnect();
                    throw new Exception("Chillerverbindung unterbrochen.\nRegelung beendet.");
                }
                System.Threading.Thread.Sleep(100);

            }
        }

        void CheckConnectionState()
        {
            if (_mbInterface == null || _mbInterface.IsConnected == false)
            {
                throw new Exception("Connection to cooling device not available.");
            }
        }


        // NationalLab ProfiCool specific communication

        // 0 : off
        // 1 : auto control
        // 2 : tuning
        // 3 : man. control
        protected int GetControllerStatus()
        {
            CheckConnectionState();

            int status = -1;
            try
            {
                var ret = _mbInterface.ReadHoldingRegisters(_modbusAddress, 0x20F, 1);
                status = ret[0];
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("NationalLab: Error getting controller status. ex:{0}", ex.Message));
            }

            return status;
        }



        protected double GetBathTemperature()
        {
            CheckConnectionState();

            double retTemp = 0;
            try
            {
                var retData = _mbInterface.ReadHoldingRegisters(_modbusAddress, 0x200, 1);
                retTemp = retData[0] / 10.0;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("NationalLab: Error getting current bath temperature. ex:{0}", ex.Message));
            }

            return retTemp;
        }


        protected double GetWorkingTemperature()
        {
            CheckConnectionState();

            double retTemp = 0;
            try
            {
                var retData = _mbInterface.ReadHoldingRegisters(_modbusAddress, 0x208, 1);
                retTemp = retData[0] / 10.0;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("NationalLab: Error getting working temperature. ex:{0}", ex.Message));
            }

            return retTemp;
        }


        protected void SetWorkingTemperature(double workingTemperature)
        {
            CheckConnectionState();

            ushort val = Convert.ToUInt16(workingTemperature * 10.0);
            _mbInterface.WriteSingleRegister(_modbusAddress, 0x2802, val);
            RecalculateInternalChecksum();
        }



        protected void RecalculateInternalChecksum()
        {
            CheckConnectionState();
            // just write to address 0x039B to start the checksum calculation
            _mbInterface.WriteSingleRegister(_modbusAddress, 0x039B, 1);
        }

        
    }
}
