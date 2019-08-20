using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace ClimaticChamberControl_Programm
{
    public partial class GUI : Form
    {
        SerialInterfaceController sic;

        public GUI()
        {
            InitializeComponent();
            sic = new SerialInterfaceController(this);
        }

        private void Disconnect_Click(object sender, EventArgs e)
        {
            sic.Disconnect();
            Close();
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

        public string Temperature
        {
            get
            {
                return temp.Text;
            }
            set
            {
                if (temp.InvokeRequired)
                    this.Invoke(new MethodInvoker(() => temp.Text = value));
                else
                    this.temp.Text = value;
            }
        }

        public string Humidity
        {
            get
            {
                return rhumi.Text;
            }
            set
            {
                if (rhumi.InvokeRequired)
                    this.Invoke(new MethodInvoker(() => rhumi.Text = value));
                else
                    this.rhumi.Text = value;
            }
        }

        public string AbsoluteHumidity
        {
            get
            {
                return abshumi.Text;
            }
            set
            {
                if (abshumi.InvokeRequired)
                    this.Invoke(new MethodInvoker(() => abshumi.Text = value));
                else
                    this.abshumi.Text = value;
            }
        }

    }
}
