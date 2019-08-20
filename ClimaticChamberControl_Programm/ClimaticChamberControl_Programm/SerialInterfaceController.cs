using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace ClimaticChamberControl_Programm
{
    class SerialInterfaceController//steam generation controll and sensordata
    {
        string[] dataTH;
        string[] da;
        string temp;
        string rhumi;
        string absHumi;

        GUI GUILink
        {
            get;
        }

        public SerialInterfaceController(GUI guiLink)
        {
            GUILink = guiLink;
        }

        SerialPort ComPortUSB = new SerialPort("COM7", 115200, Parity.None, 8, StopBits.One); // ComCort generating


        public void actDA()// updating GUI Label
        {
            
            while (true)
            {
                string dataraw = "";
                while (dataraw == "")
                {
                    ComPortUSB.NewLine = "DA";
                    dataraw = ComPortUSB.ReadLine();
                }
                dataTH = dataraw.Split(' ');


                //ComPortUSB.WriteLine("on");//Sendetest:an jeden Befehl wird als Ende 'DA' angefügt

                for (int i = 0; i < 2; i++)
                {
                    if (dataTH[i].Substring(0, 1) == "T")
                    {
                        da = dataTH[i].Split('T');
                        temp = da[1];
                        GUILink.Temperature = temp;
                    }
                    if (dataTH[i].Substring(0, 1) == "F")
                    {
                        da = dataTH[i].Split('F');
                        rhumi = da[1];
                        GUILink.Humidity = rhumi;
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

                double SDDt = 6.1078 * Math.Pow(10, ((a * Convert.ToDouble(temp)) / (b + Convert.ToDouble(temp))));
                double DD = Convert.ToDouble(rhumi) / 100 * SDDt;
                double SDDtd = Convert.ToDouble(rhumi) * SDDt / 100;
                double v = Math.Log10(DD / 6.1078);
                double TD = b * v / (a - v);
                double AF = Math.Pow(10, 5) * mw / rd * DD / (Convert.ToDouble(temp) + 273.15);
                double roundOff_AF = Math.Round(AF * 10.0) / 10.0;
                GUILink.AbsoluteHumidity = roundOff_AF.ToString(absHumi);
            }
        }

        public void Disconnect()
        {
            ComPortUSB.Close();
        }

        public void Connect()
        {
            try
            {
                ComPortUSB.ReadTimeout = 500;     //Wartezeit (Timeout Zeit) (in ms) auf Antwort bevor der Lesevorgang abgebrochen wird
                ComPortUSB.Open();
            }
            catch (Exception ex)
            {
                ComPortUSB.Close();
                throw ex;
            }
        }

        

    }
}
