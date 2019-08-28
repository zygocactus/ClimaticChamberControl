using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;

namespace ClimaticChamberControl_GUI
{
    class SerialInterfaceUSB
    {
        public bool connect = false;
        public bool fillStatus = false;
        string[] dataTH;
        string[] da;
        public string temp;
        public string rhumi;
        public string absHumi;
        public string comPortNameUSB;

        CCC_MainWindow GUILink
        {
            get;
        }
        DataStore DATALink
        {
            get;
        }
        public PIDcontroller PIDLink
        {
            get;
            set;
        }

        public SerialInterfaceUSB(CCC_MainWindow _guiLink, DataStore _ds)//for object updating
        {
            GUILink = _guiLink;
            DATALink = _ds;
        }
        
        SerialPort ComPortUSB = new SerialPort(); // ComCort generating
        

        public void actDA()//updating GUI Label
        {
            while (connect == true)
            {
                begin:

                string dataraw = "";
                while (dataraw == "")
                {
                    try
                    {
                        try
                        {
                            ComPortUSB.NewLine = "DA";
                            dataraw = ComPortUSB.ReadLine();
                        }
                        catch (InvalidOperationException)
                        {

                        }
                    }
                    catch(IOException)
                    {
                        //for Close ComPort, because ReadLine is blocking 
                    }
                   
                }
                if (dataraw == "_ready")
                {
                    fillStatus = true;
                    goto begin;
                }
                dataTH = dataraw.Split(' ');

                for (int i = 0; i < 2; i++)
                {
                    if (dataTH[i].Substring(0, 1) == "T")
                    {
                        da = dataTH[i].Split('T');
                        temp = da[1];
                        temp = temp.Substring(0, 4);
                        GUILink.Temperature = temp;
                        if (DATALink != null)
                        {
                            DATALink.Temperature = temp.Replace(',', '.');
                            PIDLink.ISTtemp = Convert.ToDouble(temp.Replace(',', '.'));
                        }
                    }
                    if (dataTH[i].Substring(0, 1) == "F")
                    {
                        da = dataTH[i].Split('F');
                        rhumi = da[1];
                        rhumi = rhumi.Substring(0, 4);
                        GUILink.RelativeHumidity = rhumi;
                        if (DATALink != null)
                        {
                            DATALink.relHumidity = rhumi.Replace(',', '.');
                            //PIDLink.ISTrelhumi = Convert.ToDouble(rhumi.Replace(',', '.'));
                        }
                    }
                }
                //calculate the absolute humidity
                //TK = temperature in kelvin
                //TD = dew point in °C
                //DD = vopour pressure in hPa
                //SDD = saturation vapour pressure in hPa

                double mw = 18.016;//universal gas constant
                double rd = 8314.3;//molecular weight of water vapour
                double a = 7.5;//for steam T>=0
                double b = 237.3;//for steam T>=0

                double SDD = 6.1078 * Math.Pow(10, ((a * Convert.ToDouble(temp)) / (b + Convert.ToDouble(temp))));
                double DD = Convert.ToDouble(rhumi) / 100 * SDD;
                double v = Math.Log10(DD / 6.1078);
                double TD = b * v / (a - v);                
                double AF = Math.Pow(10, 5) * mw / rd * DD / (Convert.ToDouble(temp) + 273.15);
                double roundOff_AF = Math.Round(AF, 1, MidpointRounding.AwayFromZero);
                GUILink.AbsoluteHumidity = roundOff_AF.ToString(absHumi);
                PIDLink.ISTabshumi = Convert.ToDouble(roundOff_AF.ToString(absHumi));
                if (DATALink != null)
                {
                    DATALink.absHumidity = roundOff_AF.ToString(absHumi).Replace(',', '.');
                    DATALink.DewPoint = Math.Round(TD, 1, MidpointRounding.AwayFromZero).ToString().Replace(',', '.');
                }

                //if realtive Humidity over 90% -> shut down
                if (Convert.ToDouble(rhumi) >= 90)
                {
                    DATALink.InOperation = false;
                    DATALink.writeTimer.Stop();//stop writing in parameter text file
                    System.Windows.MessageBox.Show("System nahe des Taupunktes!\nRegelung wurde abgeschaltet.");
                }

                fillStatus = false;
            }
        }

        public void Disconnect()//nicht implementiert
        {
            ComPortUSB.WriteLine("shutdown");
            ComPortUSB.Close();
        }

        public void Connect()
        {
            //ComPort properties
            ComPortUSB.PortName = comPortNameUSB;
            ComPortUSB.BaudRate = 115200;
            ComPortUSB.Parity = Parity.None;
            ComPortUSB.DataBits = 8;
            ComPortUSB.StopBits = StopBits.One;

            try
            {
                ComPortUSB.ReadTimeout = 1000;     //Wartezeit (Timeout Zeit) (in ms) auf Antwort bevor der Lesevorgang abgebrochen wird
                ComPortUSB.Open();
            }
            catch (Exception ex)
            {
                ComPortUSB.Close();
                throw ex;
            }
        }

        public void Send(string Status)
        {
        
            ComPortUSB.WriteLine(Status);//Sendetest:an jeden Befehl wird als Ende 'DA' angefügt


        }
    }
}
