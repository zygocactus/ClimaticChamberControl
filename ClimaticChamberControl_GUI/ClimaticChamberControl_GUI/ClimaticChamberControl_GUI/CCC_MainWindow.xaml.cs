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

namespace ClimaticChamberControl_GUI
{
    /// <summary>
    /// Interaktionslogik für CCC_MainWindow.xaml
    /// </summary>
    public partial class CCC_MainWindow : Window
    {
        SerialInterfaceUSB sic;

        public CCC_MainWindow()
        {
            InitializeComponent();
            sic = new SerialInterfaceUSB(this);
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            try
            {
                sic.Connect();
                Thread thr = new Thread(new ThreadStart(sic.actDA));
                thr.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Start_Click(object sender, EventArgs e)
        {

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

        public string Humidity
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
                //if (abshumi.InvokeRequired)
                //    this.Invoke(new MethodInvoker(() => abshumi.Content = value));
                //else
                //    this.abshumi.Content = value;
            }
        }
    }
}
