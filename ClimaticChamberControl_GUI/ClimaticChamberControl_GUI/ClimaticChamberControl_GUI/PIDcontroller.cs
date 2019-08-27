using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ClimaticChamberControl_GUI
{
    class PIDcontroller
    {
        DataStore _ds;
        SerialInterfaceUSB _siusb;

        public double SOLLtemp;         //from CCC_MainWindow
        public double SOLLhumi;         //from CCC_MainWindow
        public double ISTtempChiller;   //from SerialInterfaceModbus
        public double SOLLtempChiller = 18.0;  //from SerialInterfaceModbus
        public double ISTtemp;          //from SerialInterfaceUSB
        public double ISTabshumi;       //from SerialInterfaceUSB
        //public double ISTrelhumi;       //from SerialInterfaceUSB

        public string Status = "shutdown";//shutdown, fill, on, off
        int i = 0;//

        //general equation
        //Kp = Proportionalbeiwert; Ki = Integrierbeiwert; Kd = Differenzierbeiwert 
        //w -> Istwert
        //x -> Sollwert
        //e -> Regelabweichung
        //y -> Stellgröße
        //Ta -> Abtastzeit

        //for temperature controll
        double et = 0;      //system deviation
        double etsum = 0;   //system deviation summe
        double etold = 0;   //old system deviation
        double wt;          //actual value
        double xt;          //set value
        double yt;          //actuating value
        double Tat = 1;     //sampling time in s
        double Kpt;         //P coefficient
        double Kit;         //I coefficient
        double Kdt;         //D coefficient
        double Kita;
        double Kdta;
        //for humidity controll
        double eh = 0;      //system deviation
        double ehsum = 0;   //system deviation summe
        double ehold = 0;   //old system deviation
        double wh;          //actual value
        double xh;          //set value
        double yh;          //actuating value
        double Tah = 1;     //sampling time in s
        double Kph;         //P coefficient
        double Kih;         //I coefficient
        double Kdh;         //D coefficient
        double Kiha;
        double Kdha;

        private void TemperatureControl()
        {            
            wt = ISTtemp;
            xt = SOLLtemp;

            //Temperature PID controller
            et = wt - xt;
            etsum = etsum + et;
            yt = (Kpt* et) + (Kita * etsum) + (Kdta * (et - etold));
            etold = et;


        }

        private void HumidityControl()
        {            
            wh = ISTabshumi;
            xt = SOLLhumi;

            //Humidity(absolute) PID controller
            eh = wh - xh;
            ehsum = ehsum + eh;
            yh = (Kph * eh) + (Kiha * ehsum) + (Kdha * (eh - ehold));
            ehold = eh;

        }

        public void ClimaticControl()
        {
            _ds = new DataStore();

            Kita = Kit * Tat;
            Kdta = Kdt / Tat;
            Kiha = Kih * Tah;
            Kdha = Kdh / Tah;

            if (i == 0)//first Session Start -> fill up the cylinder
            {
                Status = "fill";
                _siusb.Send(Status);
                i = 1;
                //wait for "_ready" from MCU
                while (_siusb.fillStatus == false)
                {
                    //stop waiting
                    System.Threading.Thread.Sleep(100);
                }
                _siusb.fillStatus = false;
            }

            int CCInterval = 6000 * 1;//1min * x
            System.Windows.Forms.Timer CCTimer = new System.Windows.Forms.Timer();
            CCTimer.Interval = CCInterval;
            CCTimer.Tick += new EventHandler(CCTimer_Tick);
            CCTimer.Start();            
        }

        void CCTimer_Tick(object sender, EventArgs e)
        {
            if (i == 5)//fill up the cylinder all int FillInterval min * 5
            {
                Status = "fill";
                _siusb.Send(Status);
                System.Threading.Thread.Sleep(20);
                i = 1;
            } 

            while (_ds.InOperation == true)
            {
                double chillerOffset = 5;//in Grad Celsius
                TemperatureControl();
                if (yh > 0)//cooling
                {
                    SOLLtempChiller = yh - chillerOffset;
                }
                if (yh < 0)//heating
                {
                    SOLLtempChiller = yh + chillerOffset;
                }
                if (yh == 0)//noting
                {
                    SOLLtempChiller = yh;
                }

                double humiOffset = 2;//in g/m^3, because high downtime

                HumidityControl();
                if (yh >= humiOffset)
                {
                    Status = "off";
                    System.Threading.Thread.Sleep(20);
                }
                if (yh < humiOffset)
                {
                    Status = "on";
                    System.Threading.Thread.Sleep(20);
                }
                _siusb.Send(Status);
                i++;
            }
        }
        
    }
}
