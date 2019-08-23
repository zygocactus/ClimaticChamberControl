using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusInterfaceLib;
using System.IO.Ports;

using NLog;

namespace ClimaticChamberControl_GUI
{
    // Singleton
    public sealed class ModbusConnectionManager
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public class ModbusItem
        {
            public ModbusItem()
            { }

            public ModbusItem(ModbusItem src)   // copy contructor
            {
                ModbusAddress = src.ModbusAddress;
                ComPortName = src.ComPortName;
                BaudRate = src.BaudRate;
                Databits = src.Databits;
                StopBits = src.StopBits;
                Parity = src.Parity;
                ReadTimeout = src.ReadTimeout;
                WriteTimeout = src.WriteTimeout;
                ModbusInterface = src.ModbusInterface;
            }
            public byte ModbusAddress { get; set; }
            public string ComPortName { get; set; }
            public int BaudRate { get; set; }
            public int Databits { get; set; }
            public StopBits StopBits { get; set; }
            public Parity Parity { get; set; }
            public int ReadTimeout { get; set; }
            public int WriteTimeout { get; set; }
            public ModbusInterface_Base ModbusInterface { get; set; }
        }

        List<ModbusItem> _modbusDevices = new List<ModbusItem>(5);

        ///////////////////// Singleton start //////////////////
        private static readonly Lazy<ModbusConnectionManager> lazy =
        new Lazy<ModbusConnectionManager>(() => new ModbusConnectionManager());

        public static ModbusConnectionManager Instance { get { return lazy.Value; } }

        private ModbusConnectionManager()
        {
        }
        ///////////////////// Singleton end //////////////////



        public ModbusInterface_Base GetModbusInterface(byte modbusAddress, string portName, int baudRate = 115200, int databits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None, int mbReadTimeout = 50, int mbWriteTimeout = 30)
        {
            string currentMethod = "GetModbusInterface";

            ModbusItem mbItemInfo = _modbusDevices.FirstOrDefault(p => p.ModbusAddress == modbusAddress && p.ComPortName.ToLower() == portName.ToLower());

            if (mbItemInfo != null)
            {
                throw new Exception(string.Format("There is already a device connected to port {0} with modbus address {1}.", portName, modbusAddress));
            }

            mbItemInfo = _modbusDevices.FirstOrDefault(p => p.ComPortName.ToLower() == portName.ToLower());
            if (mbItemInfo == null)
            {
                // this is the first modbus device for this com port
                mbItemInfo = new ModbusItem()
                {
                    ModbusAddress = modbusAddress,
                    ComPortName = portName,
                    BaudRate = baudRate,
                    Databits = databits,
                    StopBits = stopBits,
                    Parity = parity,
                    ReadTimeout = mbReadTimeout,
                    WriteTimeout = mbWriteTimeout
                };

                var mbConnection = new ModbusInterface_Base();
                mbConnection.ConnectModbusInterface(portName, baudRate, databits, stopBits, parity, mbReadTimeout, mbWriteTimeout);

                mbItemInfo.ModbusInterface = mbConnection;
                _modbusDevices.Add(mbItemInfo);
            }
            else
            {
                // there is already another modbus device connected to this com port - check connection parameters
                if (baudRate != mbItemInfo.BaudRate || databits != mbItemInfo.Databits || stopBits != mbItemInfo.StopBits || parity != mbItemInfo.Parity)
                {
                    throw new Exception(string.Format("Com port {0} already used with different connection parameters. Currently used: baud={1} databits={2} stopbits={3} parity={4}    Requested: baud={5} databits={6} stopbits={7} parity={8}", portName, mbItemInfo.BaudRate, mbItemInfo.Databits, mbItemInfo.StopBits.ToString(), mbItemInfo.Parity.ToString(), baudRate, databits, stopBits.ToString(), parity.ToString()));
                }

                if (mbReadTimeout != mbItemInfo.ReadTimeout || mbWriteTimeout != mbItemInfo.WriteTimeout)
                {
                    log.Warn("[{0}] - Different timeouts requested but modbus is still in use. Already used: readtimeout={1} writetimeout={2}   requested: readtimeout={3} writetimeout={4}", currentMethod, mbItemInfo.ReadTimeout, mbItemInfo.WriteTimeout, mbReadTimeout, mbWriteTimeout);
                }

                ModbusItem mbNewItemInfo = new ModbusItem(mbItemInfo);
                mbNewItemInfo.ModbusAddress = modbusAddress;
                _modbusDevices.Add(mbNewItemInfo);
            }

            return mbItemInfo.ModbusInterface;
        }



        public void CloseModbusInterface(byte modbusAddress, ModbusInterface_Base mbInterface)
        {
            string currentMethod = "CloseModbusInterface";

            if (mbInterface == null)
            {
                return;
            }

            ModbusItem mbItemInfo = _modbusDevices.FirstOrDefault(p => p.ModbusAddress == modbusAddress && p.ComPortName.ToLower() == mbInterface.ComPortName.ToLower());

            if (mbItemInfo == null)
            {
                // no modbus device found to close
                log.Warn("[{0}] - No modbus device found which could be closed. (modbusaddress={1}  comport={2})", currentMethod, modbusAddress, mbInterface.ComPortName);
            }
            else
            {
                string comPort = mbItemInfo.ComPortName;

                // remove found modbus device from list
                _modbusDevices.Remove(mbItemInfo);

                // check if there are other devices uses the same com port
                mbItemInfo = _modbusDevices.FirstOrDefault(p => p.ComPortName.ToLower() == mbInterface.ComPortName.ToLower());

                if (mbItemInfo == null)
                {
                    // there is no other modbus device uses the same com port --> close com port
                    log.Debug("[{0}] - Modbus interface on comport={1} will be closed.", currentMethod, comPort);
                    mbInterface.CloseModbusInterface();
                }
                else
                {
                    log.Debug("[{0}] - Modbus interface on comport={1} will not be closed because it's used by other modbus devices.", currentMethod, comPort);
                }
            }
        }        
    }
}
