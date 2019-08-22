using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace ClimaticChamberControl_GUI
{
    class DataStore//write text document with temprature and humitity
    {
        public string Path
        {
            get;
            set;
        }
        public bool InOperation
        {
            get;
            set;
        }
        
        string Time;
        string Date;
        string filename;
        UInt32 i = 1;
        bool FirstTimerEvent = true;

        public string DewPoint;
        public string Temperature;
        public string relHumidity;
        public string absHumidity;

        public Timer writeTimer = new Timer();       


        public void GenerateFile()
        {
            i = 1;
            try
            {
                Date = DateTime.Now.ToString("yyyyMMdd");
                filename = Path + "/" + Date + "_CCC_Parameters.txt";
                if (File.Exists(filename))
                {
                    if (MessageBox.Show("Datei ersetzen?", "Datei exsitiert bereits!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        File.Delete(filename);
                        using (StreamWriter sw = File.CreateText(filename))
                        {
                            sw.WriteLine(Date + ",Time,Celsius(°C),Humidity(%rh),Dew Point(°C),Absolute Humidity(g/m^3)");
                        }
                    }
                    else
                    {
                        InOperation = false;
                    }
                }
                else
                {
                    //generate file and write first line
                    using (StreamWriter sw = File.CreateText(filename))
                    {
                        sw.WriteLine(Date + ",Time,Celsius(°C),Humidity(%rh),Dew Point(°C),Absolute Humidity(g/m^3)");
                    }
                }
                
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }


        }
        
        public void StoreDATA()//write parameters in new Line 1 time in a minute
        {
            writeTimer.Interval = 6000; //intervall in ms
            if (FirstTimerEvent == true)
            {
                writeTimer.Tick += new EventHandler(writeTimer_Tick);
                FirstTimerEvent = false;
            }

            if (InOperation == true)
            {
                Time = DateTime.Now.ToString("HH:mm:ss");
                Date = DateTime.Now.ToString("yyyy-MM-dd");
                using (StreamWriter sw = File.AppendText(filename))
                {
                    sw.WriteLine("1," + Date + " " + Time + "," + Temperature + "," + relHumidity + "," + DewPoint + "," + absHumidity);
                }
                
                writeTimer.Start();
            }
        }
        void writeTimer_Tick(object sender, EventArgs e)
        {
            FileInfo fi = new FileInfo(filename);
            
            i++;
            Time = DateTime.Now.ToString("HH:mm:ss");
            Date = DateTime.Now.ToString("yyyy-MM-dd");

            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine(i.ToString() + "," + Date + " " + Time + "," + Temperature + "," + relHumidity + "," + DewPoint + "," + absHumidity);
            }

        }

    }
}

