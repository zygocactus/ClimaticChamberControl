using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClimaticChamberControl_GUI
{
    class PIDcontroller
    {
        private DataStore _ds;

        public SerialInterfaceUSB Siusb
        {
            get;
            set;
        }
        public PIDcontroller(DataStore ds)//for object updating
        {
            _ds = ds;
        }

       private System.Windows.Threading.DispatcherTimer CCTimer = new System.Windows.Threading.DispatcherTimer();

        public double SOLLtemp;       //from CCC_MainWindow
        public double SOLLhumi;        //from CCC_MainWindow
        public double ISTtempChiller;   //from SerialInterfaceModbus
        public double SOLLtempChiller = 18.0;//from SerialInterfaceModbus
        public double ISTtemp = 0;          //from SerialInterfaceUSB
        public double ISTabshumi = 0;       //from SerialInterfaceUSB
        //public double ISTrelhumi;         //from SerialInterfaceUSB

        public string Status = "shutdown";//shutdown, fill, on, off
        string StatusOld;
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
        double Kpt = 1;         //P coefficient
        double Kit = 0;         //I coefficient
        double Kdt = 0;         //D coefficient
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
        double Kph = 1;         //P coefficient
        double Kih = 0;         //I coefficient
        double Kdh = 0;         //D coefficient
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
            xh = SOLLhumi;

            //Humidity(absolute) PID controller
            eh = wh - xh;
            ehsum = ehsum + eh;
            yh = (Kph * eh) + (Kiha * ehsum) + (Kdha * (eh - ehold));
            ehold = eh;

        }

        public void ClimaticControl()
        {
            Kita = Kit * Tat;
            Kdta = Kdt / Tat;
            Kiha = Kih * Tah;
            Kdha = Kdh / Tah;

            if (i == 0)//first Session Start -> fill up the cylinder
            {
                Status = "fill";
                Siusb.Send(Status);
                //wait for "_ready" from MCU
                while (Siusb.fillStatus != true)
                {
                    System.Threading.Thread.Sleep(10);
                }
                Status = "off";
                Siusb.Send(Status);
                i = 1;
            }
            Siusb.fillStatus = false;
            
            CCTimer.Interval = new TimeSpan(0,0,1);//intervall in (h,min,s)
            CCTimer.Tick += new EventHandler(CCTimer_Tick);
            CCTimer.Start();        
        }

        void CCTimer_Tick(object sender, EventArgs e)
        {
            if (_ds.InOperation == true)
            {
                if (i == 60)//fill up the cylinder all int FillInterval * 5
                {
                    Status = "fill";
                    Siusb.Send(Status);
                    //wait for "_ready" from MCU
                    while (Siusb.fillStatus != true)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    Siusb.Send(StatusOld);
                    i = 1;
                }

                //double chillerOffset = 10;//in Grad Celsius
                //TemperatureControl();
                //if (yh > 0)//cooling
                //{
                //    SOLLtempChiller = yh - chillerOffset;
                //}
                //if (yh < 0)//heating
                //{
                //    SOLLtempChiller = yh + chillerOffset;
                //}
                //if (yh == 0)//noting
                //{
                //    SOLLtempChiller = yh;
                //}

                double humiOffset = -2;//in g/m^3, because high downtime
                HumidityControl();
                if (yh >= humiOffset)
                {
                    Status = "off";
                    StatusOld = Status;
                    Siusb.Send(Status);
                    System.Threading.Thread.Sleep(10);
                }
                if (yh < humiOffset)
                {
                    Status = "on";
                    StatusOld = Status;
                    Siusb.Send(Status);
                    System.Threading.Thread.Sleep(10);
                }
                i++;
            }
        }
        
    }
}
