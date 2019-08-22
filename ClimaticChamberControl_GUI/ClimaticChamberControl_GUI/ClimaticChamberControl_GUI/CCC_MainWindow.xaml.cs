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
using WinForms = System.Windows.Forms;

namespace ClimaticChamberControl_GUI
{
    /// <summary>
    /// Interaktionslogik für CCC_MainWindow.xaml
    /// </summary>
    public partial class CCC_MainWindow : Window
    {
        SerialInterfaceUSB sic;
        DataStore ds;
        PIDcontroller pid;

        public CCC_MainWindow()
        {
            InitializeComponent();
            ds = new DataStore();
            sic = new SerialInterfaceUSB(this, ds);
            ds.InOperation = false;
            pid = new PIDcontroller();
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            try
            {
                sic.Connect();
                Thread thr = new Thread(new ThreadStart(sic.actDA));
                thr.Start();
                Connect.IsEnabled = false;
                Start.IsEnabled = true;
                Stop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }
        private void Start_Click(object sender, EventArgs e)
        {
            if (SollTemp.Text.ToString() == "")
            {
                System.Windows.MessageBox.Show("Es müssen noch SOLL-Werte gesetzt werden.");
            }
            else
            {
                ds.InOperation = true;
                SollTemp.IsEnabled = false;
                SollabsHumi.IsEnabled = false;
                ds.GenerateFile();
                ds.StoreDATA();
                pid.SOLLtemp = SollTemp.Text.ToString();
                pid.SOLLhumi = SollabsHumi.Text.ToString();
                pid.ClimaticControl();
            }
        }
        private void Stop_Click(object sender, EventArgs e)
        {
            ds.InOperation = false;
            SollTemp.IsEnabled = true;
            SollabsHumi.IsEnabled = true;
            ds.writeTimer.Stop();//stop writing in parameter text file
        }
        public void FileExplorer_Click(object sender, EventArgs e)
        {
            WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
            WinForms.DialogResult result = folderDialog.ShowDialog();

            if (result == WinForms.DialogResult.OK)
            {
                ds.Path = folderDialog.SelectedPath;
                SaveLocation.Text = ds.Path;
            }
        }
        private static void OpenExplorer(string path)
        {
            if (Directory.Exists(path))
                System.Diagnostics.Process.Start("explorer.exe", path);
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
                    this.temp.Content = value;
                else
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(() => this.temp.Content = value));
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
                    this.rhumi.Content = value;
                else
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.rhumi.Content = value));
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
                    this.abshumi.Content = value;
                else
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => this.abshumi.Content = value));
            }
        }

        private void SollTemp_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
