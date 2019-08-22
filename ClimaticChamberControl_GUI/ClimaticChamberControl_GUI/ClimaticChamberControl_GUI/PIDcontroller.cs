using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimaticChamberControl_GUI
{
    class PIDcontroller
    {
        public string SOLLtemp;
        public string SOLLhumi;

        
        //general equation
        //Kp = Proportionalbeiwert; Ki = Integrierbeiwert; Kd = Differenzierbeiwert 
        //w -> Istwert
        //x -> Sollwert
        //e -> Regelabweichung
        //y -> Stellgröße
        //Ta -> Abtastzeit
        //
        //e = w - x;					                    //Vergleich
        //esum = esum + e;				                    //Integration I-Anteil
        //y = Kp* e + Ki* Ta*esum + Kd/Ta*(e – ealt);	    //Reglergleichung
        //ealt = e;

        //for temperature controll
        double et;//system deviation
        double etsum;//system deviation summe
        double etold;//old system deviation
        double wt;//actual value
        double xt;//set value
        double yt;//actuating value
        double Tat;//sampling time
        double Kpt;//P coefficient
        double Kit;//I coefficient
        double Kdt;//D coefficient
        //for humidity controll
        double eh;//system deviation
        double ehsum;//system deviation summe
        double ehold;//old system deviation
        double wh;//actual value
        double xh;//set value
        double yh;//actuating value
        double Tah;//sampling time
        double Kph;//P coefficient
        double Kih;//I coefficient
        double Kdh;//D coefficient

        private void TemperatureControl()
        {

            //Temperature PID controller
            et = wt - xt;
            etsum = etsum + et;
            yt = (Kpt* et) + (Kit* Tat * etsum) + (Kdt / Tat * (et - etold));
            etold = et;



        }

        private void HumidityControl()
        {

            //Humidity(absolute) PID controller
            eh = wh - xh;
            ehsum = ehsum + eh;
            yh = (Kph * eh) + (Kih * Tah * ehsum) + (Kdh / Tah * (eh - ehold));
            ehold = eh;

        }

        public void ClimaticControl()
        {
            xt = Convert.ToDouble(SOLLtemp);
            wt = 0;
            xh = Convert.ToDouble(SOLLhumi);
            wh = 0;

        }
    }
}
