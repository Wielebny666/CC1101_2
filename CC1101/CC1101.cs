using Microsoft.IoT.Lightning.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;

namespace CC1101
{
    public sealed class CC1101
    {
        // Raspberry Pi 2
        private const string SPI_CONTROLLER_NAME = "SPI0";
        private const int SPI_CHIP_SELECT_LINE = 0;

        private List<bool> capture = new List<bool>();

        private SpiDevice spi;

        private GpioPin rcvPin;
        private GpioPin txPin;

        private bool TxFinish = false;

        public CC1101()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }
        }

        public int PartNumber { get; internal set; }
        public int Version { get; internal set; }

        /// <summary>
        /// Initialize interfaces and display
        /// </summary>
        public async void InitAll()
        {
            await InitTxRxGpio();                   //// Initialize the GPIO controller and GPIO pins
            await InitSpi();              //// Initialize the SPI controller
            Reset();
            WriteDefaultRegisters();
            SetISM(5); //433k
            SetIdleState();
            TxFinish = false;
        }

        /// <summary>
        /// Initialize GPIO interface
        /// </summary>
        private async Task InitTxRxGpio()
        {
            try
            {
                // Get default controller
                GpioController gpio = await GpioController.GetDefaultAsync();
                rcvPin = gpio.OpenPin(CCRegister.CC1101_GDO2);
                rcvPin.SetDriveMode(GpioPinDriveMode.Input);
                //rcvPin.ValueChanged += RcvPin_ValueChanged;
            }
            catch (Exception ex)
            {
                throw new Exception($"GPIO_{CCRegister.CC1101_GDO2} initialization failed", ex);
            }
        }

        public async void InitGPIOAsyncTransmit()
        {
            try
            {
                // Get default controller
                GpioController gpio = await GpioController.GetDefaultAsync();
                txPin = gpio.OpenPin(CCRegister.CC1101_GDO0, GpioSharingMode.Exclusive);
                txPin.SetDriveMode(GpioPinDriveMode.Output);
            }
            catch (Exception ex)
            {
                throw new Exception($"GPIO_{CCRegister.CC1101_GDO0} initialization failed", ex);
            }
        }

        private void RcvPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            //capture.Add(sender.Read() == GpioPinValue.High ? true : false);

            if (args.Edge == GpioPinEdge.FallingEdge)
                TxFinish = true;
        }

        /// <summary>
        /// Initialize SPI interface
        /// </summary>
        private async Task InitSpi()
        {
            try
            {
                var spiSettings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE)
                {
                    ClockFrequency = 5000000, //// 64000000 was not reliable
                    Mode = SpiMode.Mode0,
                    DataBitLength = 8,
                    SharingMode = SpiSharingMode.Exclusive
                };

                //ISpiProvider spiProvider = LightningSpiProvider.GetSpiProvider();
                //var test = await spiProvider.GetControllersAsync();
                //foreach(var t in test)
                //{

                //}

                //var controller = SpiController.GetControllersAsync(spiProvider);


                var controller = await SpiController.GetDefaultAsync();
                spi = controller.GetDevice(spiSettings);

            }
            catch (Exception ex)
            {
                throw new Exception("SPI initialization Failed", ex);
            }
        }

        public IAsyncAction InitSpiAsync()
        {
            return InitSpi().AsAsyncAction();
        }

        #region Lo lvl comunication method
        private void WriteRegister(byte registerAddress, byte dataToWrite)
        {
            spi?.Write(new byte[] { registerAddress, dataToWrite });
        }

        private void WriteBurstRegister(byte registerAddress, [ReadOnlyArray()] byte[] buffer)
        {
            byte[] frame = new byte[buffer.Count() + 1];
            frame[0] = (byte)(registerAddress | CCRegister.WRITE_BURST);
            Buffer.BlockCopy(buffer, 0, frame, 1, buffer.Length);
            spi?.Write(frame);
        }

        private byte ReadStatusRegister(byte registerAddress)
        {
            byte[] recive = new byte[1];
            spi?.TransferSequential(new byte[] { (byte)(registerAddress | CCRegister.READ_BURST) }, recive);
            return recive[0];
        }

        private byte ReadConfigRegister(byte registerAddress)
        {
            byte[] recive = new byte[1];
            spi?.TransferSequential(new byte[] { (byte)(registerAddress | CCRegister.READ_SINGLE) }, recive);
            return recive[0];
        }

        private byte[] ReadBurstRegister(byte registerAddress, int len)
        {
            byte[] recive = new byte[len];
            spi?.TransferSequential(new byte[] { (byte)(registerAddress | CCRegister.READ_BURST) }, recive);
            return recive;
        }

        private void CommandStrobe(byte strobe)
        {
            spi?.Write(new byte[] { strobe });
        }

        private byte GetMarcState()
        {
            return (byte)(ReadStatusRegister(CCRegister.CC1101_MARCSTATE) & 0x1F);
        }
        #endregion

        private static byte FlipByte(byte inByte)
        {
            uint flipThis = inByte;
            return (byte)(~flipThis & 0xff);
        }

        /// <summary>
        /// Set predef register value
        /// </summary>
        public void WriteDefaultRegisters()
        {
            Debug.WriteLine("Writing DefaultRe Registers", "Info");

            foreach (KeyValuePair<string, byte> configurationRegisterValue in CCRegister.ConfigRegisterValues)
            {
                WriteRegister(CCRegister.ConfigRegisters[configurationRegisterValue.Key], configurationRegisterValue.Value);
            }
        }

        /// <summary>
        /// Set the Carrier Frequency of the device for tx/rx
        /// </summary>
        /// <param name="frequencyInMHz">Frequency in MHz</param>
        public void SetCarrierFrequency(double frequencyInMHz)
        {
            // bounds checking for cc1101 hardware limitations
            if (!(((frequencyInMHz >= 300) && (frequencyInMHz <= 348)) || ((frequencyInMHz >= 387) && (frequencyInMHz <= 464)) || ((frequencyInMHz >= 779) && (frequencyInMHz <= 928))))
            {
                Debug.WriteLine("Frequency out of bounds! Use 300-348, 387-464, 779-928MHz only!", "Error");
                return;
            }

            // trying to avoid any floating point issues
            double secondByteOverflow = frequencyInMHz % 26;
            double firstByteValue = (frequencyInMHz - secondByteOverflow) / 26;

            double thirdByteOverflow = (secondByteOverflow * 255) % 26;
            double secondByteValue = ((secondByteOverflow * 255) - thirdByteOverflow) / 26;

            double excessOverflow = (thirdByteOverflow * 255) % 26;
            double thirdByteValue = ((thirdByteOverflow * 255) - excessOverflow) / 26;

            WriteRegister(CCRegister.ConfigRegisters["FREQ2"], (byte)firstByteValue);
            WriteRegister(CCRegister.ConfigRegisters["FREQ1"], (byte)secondByteValue);
            WriteRegister(CCRegister.ConfigRegisters["FREQ0"], (byte)thirdByteValue);
            ShortWait(50);

            Debug.WriteLine(string.Format("Carrier Frequency set to {0}. 1: {1}, 2: {2}, 3: {3}", frequencyInMHz, firstByteValue, secondByteValue, thirdByteValue), "Info");
        }

        public double GetCarrierFrequency()
        {
            double firstRegisterByte = ReadConfigRegister(CCRegister.ConfigRegisters["FREQ2"]);
            double secondRegisterByte = ReadConfigRegister(CCRegister.ConfigRegisters["FREQ1"]);
            double thirdRegisterByte = ReadConfigRegister(CCRegister.ConfigRegisters["FREQ0"]);

            firstRegisterByte = firstRegisterByte * 26;
            secondRegisterByte = secondRegisterByte / 255 * 26;
            thirdRegisterByte = thirdRegisterByte / 255 / 255 * 26;

            return Math.Round(firstRegisterByte + +secondRegisterByte + +thirdRegisterByte, 4);
        }

        /// <summary>
        /// Set the baud rate of the device for tx/rx
        /// </summary>
        /// <param name="baudRate">baudrate in kBauds</param>
        public void SetBaudRate(double baudRate)
        {
            double clockFrequencyMHz = CCRegister.CC1101_CLOCK_FREQUENCY;

            byte baudRateExponent = 0;
            byte baudRateMantissa = 0;

            for (int tempExponent = 0; tempExponent < 16; tempExponent++)
            {
                int tempMantissa = (int)((baudRate * 1000.0 * Math.Pow(2, 28) / (Math.Pow(2, tempExponent) * (clockFrequencyMHz * 1000000.0))) - 256 + .5);
                if (tempMantissa < 256)
                {
                    baudRateExponent = (byte)tempExponent;
                    baudRateMantissa = (byte)tempMantissa;
                    break;
                }
            }

            Debug.WriteLine(string.Format("Transmission Baud rate set to {0}. E: {1}, M: {2}", baudRate, baudRateExponent, baudRateMantissa), "Info");

            baudRateExponent = (byte)((ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG4"]) & 0xF0) | baudRateExponent);

            WriteRegister(CCRegister.ConfigRegisters["MDMCFG4"], baudRateExponent);
            WriteRegister(CCRegister.ConfigRegisters["MDMCFG3"], baudRateMantissa);
        }

        /// <summary>
        /// Get Baud Rate from CC1101
        /// </summary>
        /// <returns></returns>
        public double GetBaudRate()
        {
            uint baudRateExponent = (uint)ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG4"]) & 0x0F;
            uint baudRateMantissa = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG3"]);

            double baudRate = 1000000.0 * CCRegister.CC1101_CLOCK_FREQUENCY * (256 + baudRateMantissa) * Math.Pow(2, baudRateExponent) / Math.Pow(2, 28);
            return Math.Round(baudRate, 4);
        }

        /// <summary>
        /// Set Receiver Channel Filter Bandwidth
        /// </summary>
        /// <param name="value">812, 650, 541, 464, 406, 325, 270, 232, 203, 162, 135, 116, 102, 81, 68, 58</param>
        public void SetRxBandWidth([Range(58, 812)]int value)
        {
            byte chanbwExponent = 3, chanbwMantissa = 3;

            for (byte e_index = 0; e_index < 4; e_index++)
            {
                for (byte m_index = 0; m_index < 4; m_index++)
                {
                    if (value >= CCRegister.ChanBW_limits[4 * e_index + m_index])
                    {
                        chanbwExponent = e_index;
                        chanbwMantissa = m_index;
                        break;
                    }
                }
            }

            Debug.WriteLine(string.Format("Rx Band Width set to {0}. E: {1}, M: {2}", value, chanbwExponent, chanbwMantissa), "Info");

            var register = (byte)(ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG4"]) & 0b0000_1111);
            register = (byte)(register | chanbwExponent << 6 | chanbwMantissa << 4);
            WriteRegister(CCRegister.ConfigRegisters["MDMCFG4"], register);
        }

        /// <summary>
        /// Set output power level in dBm (-30 to 10)
        /// </summary>
        /// <param name="dBm">Power in dBm</param>
        public void SetOutputPowerLevel([Range(-30, 10)] int dBm)
        {
            byte pa = 0xC0;

            if (dBm <= -30) pa = 0x00;
            else if (dBm <= -20) pa = 0x01;
            else if (dBm <= -15) pa = 0x02;
            else if (dBm <= -10) pa = 0x03;
            else if (dBm <= 0) pa = 0x04;
            else if (dBm <= 5) pa = 0x05;
            else if (dBm <= 7) pa = 0x06;
            else if (dBm <= 10) pa = 0x07;

            var data = ReadConfigRegister(CCRegister.ConfigRegisters["FREND0"]);
            data = (byte)((data & 0b1111_1000) | pa);
            WriteRegister(CCRegister.ConfigRegisters["FREND0"], data);
            Debug.WriteLine(string.Format("Tx power level set to {0}.", dBm), "Info");
        }

        /// <summary>
        /// Set Modulation type 
        /// </summary>
        /// <param name="cfg">
        /// <para>0 - 2-FSK</para>
        /// <para>1 - GFSK</param>
        /// <para>3 - ASK/OOK</param>
        /// <para>4 - 4-FSK</param>
        /// <para>7 - MSK</param>
        public void SetModulationType(byte cfg)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG2"]);
            data = (byte)((data & 0x8F) | (((cfg) << 4) & 0x70));
            WriteRegister(CCRegister.ConfigRegisters["MDMCFG2"], data);
        }

        /// <summary>
        /// Get Modulation Type from CC1101
        /// </summary>
        /// <returns></returns>
        public byte GetModulationType()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG2"]);
            data = (byte)((data & 0x70) >> 4);
            return data;
        }

        /// <summary>
        /// Set Modem Deviation Setting (OOK - no effect)
        /// </summary>
        /// <param name="cfg">Frequency in kHz</param>
        public void SetDeviationFrequencySetting(double frequencyInkHz)
        {
            double clockFrequencyMHz = CCRegister.CC1101_CLOCK_FREQUENCY;

            int deviationFrequencyExponent = 0;
            int deviationFrequencyMantissa = 0;

            for (int tempExponent = 0; tempExponent < 4; tempExponent++)
            {
                int tempMantissa = (int)(frequencyInkHz * 1000.0 * Math.Pow(2, 17) / (Math.Pow(2, tempExponent) * (clockFrequencyMHz * 1000000.0)) - 8);
                if (tempMantissa < 8)
                {
                    deviationFrequencyExponent = tempExponent;
                    deviationFrequencyMantissa = tempMantissa < 0 ? 0 : tempMantissa;
                    break;
                }
            }
            byte deviationFrequency = (byte)((deviationFrequencyExponent << 4) | deviationFrequencyMantissa);

            WriteRegister(CCRegister.ConfigRegisters["DEVIATN"], deviationFrequency);

            Debug.WriteLine(string.Format("Deviation Frequency set to {0}. E: {1}, M: {2}", frequencyInkHz, deviationFrequencyExponent, deviationFrequencyMantissa), "Info");
        }

        /// <summary>
        /// Get frequency deviation from CC1101
        /// </summary>
        /// <returns>deviation</returns>
        public double GetDeviationFrequencySetting()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["DEVIATN"]);
            byte deviationExponent = (byte)((data & 0x70) >> 4);
            byte deviationMantissa = (byte)(data & 0x07);

            double deviation = 1000000.0 * CCRegister.CC1101_CLOCK_FREQUENCY * (8 + deviationMantissa) * Math.Pow(2, deviationExponent) / Math.Pow(2, 17);
            return Math.Round(deviation, 4);
        }

        /// <summary>
        /// Sets the relative change threshold for asserting carrier sense
        /// </summary>
        /// <param name="value">
        /// <para>0 (00): Relative carrier sense threshold disabled</para>
        /// <para>1 (01): 6 dB increase in RSSI value</para>
        /// <para>2 (10): 10 dB increase in RSSI value</para>
        /// <para>3 (11): 14 dB increase in RSSI value</para>
        /// </param>
        public void SetCarrierSense([Range(0, 3)] byte value)
        {
            //Debug.WriteLine(string.Format("Rx Band Width set to {0}. E: {1}, M: {2}", value, chanbwExponent, chanbwMantissa), "Info");

            var register = (byte)(ReadConfigRegister(CCRegister.ConfigRegisters["AGCCTRL1"]) & 0b1100_1111);
            register = (byte)(register | value << 4);
            WriteRegister(CCRegister.ConfigRegisters["AGCCTRL1"], register);
        }

        /// <summary>
        /// Sets the averaging length for the amplitude from the channel filter. Sets the OOK/ASK decision boundary for OOK/ASK reception.
        /// </summary>
        /// <param name="value">
        /// <para>0 (00)  8  4 dB</para>
        /// <para>1 (01) 16  8 dB</para>
        /// <para>2 (10) 32 12 dB</para>
        /// <para>3 (11) 64 16 dB</para>
        /// </param>
        public void SetFilterLength([Range(0, 3)] byte value)
        {
            //Debug.WriteLine(string.Format("Rx Band Width set to {0}. E: {1}, M: {2}", value, chanbwExponent, chanbwMantissa), "Info");

            var register = (byte)(ReadConfigRegister(CCRegister.ConfigRegisters["AGCCTRL0"]) & 0b1111_1100);
            register = (byte)(register | (value & 0b0000_0011));
            WriteRegister(CCRegister.ConfigRegisters["AGCCTRL0"], register);
        }



        /// <summary>
        /// Sets the minimum number of preamble bytes to be transmitted
        /// </summary>
        /// <param name="num">Preamble bytes</param>
        public void SetPreambleNum([Range(0, 7)] byte num)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG1"]);
            data = (byte)((data & 0b1000_1111) | (((num) << 4) & 0b0111_0000));
            WriteRegister(CCRegister.ConfigRegisters["MDMCFG1"], data);
            Debug.WriteLine(string.Format("Preamble bytes set to {0}.", num), "Info");
        }

        /// <summary>
        /// Get the  number of preamble bytes from CC1101
        /// </summary>
        /// <returns></returns>
        public byte GetPreambleNum()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG1"]);
            data = (byte)((data & 0b0111_0000) >> 4);
            return data;
        }

        /// <summary>
        /// Set manchester encoding 
        /// </summary>
        /// <param name="cfg"></param>
        public void SetManchesterEncoding(bool cfg)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG2"]);
            data = (byte)((data & 0xF7) | (((cfg ? 1 : 0) << 3) & 0x08));
            WriteRegister(CCRegister.ConfigRegisters["MDMCFG2"], data);
            Debug.WriteLine(string.Format("Manchester Encoding set to {0}.", cfg), "Info");
        }

        public bool GetManchesterEncoding()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG2"]);
            data = (byte)((data & 0x08) >> 3);
            return data == 1 ? true : false;
        }

        /// <summary>
        /// Combined sync-word qualifier mode.
        /// </summary>
        /// <param name="index">
        /// <para>0 - No preamble/sync</para>
        /// <para>1 - 15/16 sync word bits detected</para>
        /// <para>2 - 16/16 sync word bits detected</para>
        /// <para>3 - 30/32 sync word bits detected</para>
        /// <para>4 - No preamble/sync, carrier-sense above threshold</para>
        /// <para>5 - 15/16 + carrier-sense above threshold</para>
        /// <para>6 - 16/16 + carrier-sense above threshold</para>
        /// <para>7 - 30/32 + carrier-sense above thre</para>
        /// </param>
        public void SetSyncMode([Range(0, 7)] byte value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG2"])
                .SetBits(value, 3, 0);
            WriteRegister(CCRegister.ConfigRegisters["MDMCFG2"], data);
            Debug.WriteLine(string.Format("Sync Mode set to {0}.", value), "Info");
        }

        public byte GetSyncMode()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MDMCFG2"]);
            data = (byte)(data & 0x07);
            return data;
        }

        /// <summary>
        /// Set Sync Word
        /// </summary>
        /// <param name="value"></param>
        public void SetSyncWord(int value)
        {
            byte lsb = (byte)value;
            byte msb = (byte)(value >> 8);

            WriteRegister(CCRegister.ConfigRegisters["SYNC0"], lsb);
            WriteRegister(CCRegister.ConfigRegisters["SYNC1"], msb);
            Debug.WriteLine(string.Format("Sync Word set to {0}.", value), "Info");
        }

        public int GetSyncWord()
        {
            int data = ReadConfigRegister(CCRegister.ConfigRegisters["SYNC0"]);
            data |= ReadConfigRegister(CCRegister.ConfigRegisters["SYNC1"]) << 8;
            return data;
        }

        /// <summary>
        /// Set CCA (Clear channel indication) Mode
        /// </summary>
        /// <param name="value">
        /// <para>0 (00) : Always</para>
        /// <para>1 (01) : If RSSI below threshold</para>
        /// <para>2 (10) : Unless currently receiving a packet</para>
        /// <para>3 (11) : If RSSI below threshold unless currently receiving a packet</para>
        /// </param>
        public void SetCCAMode([Range(0, 3)] byte value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MCSM1"])
                .SetBits(value, 2, 4);
            //data = (byte)((data & 0x0F) | ((value << 4) & 0x30));
            WriteRegister(CCRegister.ConfigRegisters["MCSM1"], data);
            Debug.WriteLine(string.Format("CCA Mode set to {0}.", value), "Info");
        }

        /// <summary>
        /// Clear Channel Assessment
        /// </summary>
        /// <returns></returns>
        public byte GetCCAMode()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MCSM1"]);
            data = (byte)((data & 0x30) >> 4);
            return data;
        }

        /// <summary>
        /// Select what should happen when a packet has been sent 
        /// </summary>
        /// <param name="value">
        /// <para>0 (00) IDLE</para>
        /// <para>1 (01) FSTXON</para>
        /// <para>2 (10) Stay in TX(start sending preamble)</para>
        /// <para>3 (11) RX</para>
        /// </param>
        public void SetTxOffMode([Range(0, 3)] int value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MCSM1"]);
            data = (byte)((data & 0b1111_1100) | (value & 0b0000_0011));
            WriteRegister(CCRegister.ConfigRegisters["MCSM1"], data);
            Debug.WriteLine(string.Format("CCA Mode set to {0}.", value), "Info");
        }

        public byte GetTxOffMode()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["MCSM1"]);
            data &= 0b0000_0011;
            return data;
        }

        public void SetPaPowerSetting([Range(0, 7)] byte index)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["FREND0"]);
            data = (byte)((data & 0xF8) | (index & 0x07));
            WriteRegister(CCRegister.ConfigRegisters["FREND0"], data);
        }

        public byte GetSelectedPaPowerSetting()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["FREND0"]);
            data = (byte)(data & 0x07);
            return data;
        }

        /// <summary>
        /// Set ISM Band
        /// </summary>
        /// <param name="ism_freq">
        /// <para>1=315MHz</para>
        /// <para>2=433.92MHz</para
        /// <para>3=868.3MHz</para
        /// <para>4=915MHz</para
        /// </param>
        public void SetISM([Range(1, 5)] byte ism_freq)
        {
            switch (ism_freq)                                                       //loads the RF freq which is defined in cc1100_freq_select
            {
                case 0x01:
                    SetCarrierFrequency(315);//315MHz
                    WriteBurstRegister(CCRegister.CC1101_PATABLE, CCRegister.PatablePower_315);
                    break;
                case 0x02:                                                          //433.92MHz
                    SetCarrierFrequency(433.92);
                    WriteBurstRegister(CCRegister.CC1101_PATABLE, CCRegister.PatablePower_433);
                    break;
                case 0x03:                                                          //868.3MHz
                    SetCarrierFrequency(868.3);
                    WriteBurstRegister(CCRegister.CC1101_PATABLE, CCRegister.PatablePower_868);
                    break;
                case 0x04:                                                          //915MHz
                    SetCarrierFrequency(915);
                    WriteBurstRegister(CCRegister.CC1101_PATABLE, CCRegister.PatablePower_915);
                    break;
                case 0x05:                                                            //default is 433.92MHz
                    SetCarrierFrequency(433.92);
                    WriteBurstRegister(CCRegister.CC1101_PATABLE, CCRegister.PatablePower);    //sets up output power ramp register
                    break;
            }
            ShortWait(50);
            return;
        }

        /// <summary>
        /// When enabled, two status bytes will be appended to the payload of the packet. The status bytes contain RSSI and LQI values, as well as CRC OK.
        /// </summary>
        /// <param name="value"></param>
        public void SetAppendStatus(bool value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL1"]);
            data = (byte)((data & 0xFB) | ((value ? 1 : 0) << 2));
            WriteRegister(CCRegister.ConfigRegisters["PKTCTRL1"], data);
            Debug.WriteLine(string.Format("Append Status set to {0}.", value), "Info");
        }

        public void SetWhiteData(bool value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data = (byte)((data & 0b1011_1111) | ((value ? 1 : 0) << 6));
            WriteRegister(CCRegister.ConfigRegisters["PKTCTRL0"], data);
            Debug.WriteLine(string.Format("Whitedata configuration set to {0}.", value), "Info");
        }

        /// <summary>
        /// Format of RX and TX data
        /// </summary>
        /// <param name="value">
        /// <para>0 (00) Normal mode, use FIFOs for RX and TX</para>
        /// <para>1 (01) Synchronous serial mode, Data in on GDO0 and data out on either of the GDOx pins</para>
        /// <para>2 (10) Random TX mode; sends random data using PN9 generator.Used for test. Works as normal mode, setting 0 (00), in RX</para>
        /// <para>3 (11) Asynchronous serial mode, Data in on GDO0 and data out on either of the GDOx pins</para>
        /// </param>
        public void SetPKTFormat([Range(0, 3)] byte value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data = (byte)((data & 0b1100_1111) | ((value << 4) & 0b0011_0000));
            WriteRegister(CCRegister.ConfigRegisters["PKTCTRL0"], data);
            Debug.WriteLine(string.Format("Rx, Tx mode configuration set to {0}.", value), "Info");
        }

        public byte GetPKTFormat()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data = (byte)((data & 0b0011_0000) >> 4);
            return data;
        }

        /// <summary>
        /// Set CRC calculation
        /// </summary>
        /// <param name="value"></param>
        public void SetCrcEnable(bool value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data = (byte)((data & 0xFB) | (((value ? 1 : 0) << 2) & 0b0000_0100));
            WriteRegister(CCRegister.ConfigRegisters["PKTCTRL0"], data);
            Debug.WriteLine(string.Format("CRC check set to {0}.", value), "Info");

        }

        public bool GetCrcEnable()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data = (byte)((data & 0b0000_0100) >> 2);
            return data == 1 ? true : false;
        }

        /// <summary>
        /// Packet length configuration
        /// </summary>
        /// <param name="value">
        /// <para>0 (00) Fixed packet length mode. Length configured in PKTLEN register</para>
        /// <para>1 (01) Variable packet length mode.Packet length configured by the first byte after sync word</para>
        /// <para>2 (10) Infinite packet length mode</para>
        /// <para>3 (11) Reserved</para>
        /// </param>
        public void SetPacketLengthCfg([Range(0, 3)] byte value)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data = (byte)((data & 0b1111_1100) | (value & 0b0000_0011));
            WriteRegister(CCRegister.ConfigRegisters["PKTCTRL0"], data);
            Debug.WriteLine(string.Format("Packet length configuration set to {0}.", value), "Info");
        }

        public int GetPacketLengthCfg()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL0"]);
            data &= 0b0000_0011;
            return data;
        }



        /// <summary>
        /// Set Device Address
        /// </summary>
        /// <param name="address">0 and 255 are Broadcast address</param>
        public void SetDeviceAddress([Range(0, 255)] byte address)
        {
            WriteRegister(CCRegister.ConfigRegisters["ADDR"], address);
        }

        /// <summary>
        /// Controls address check configuration of received packages.
        /// </summary>
        /// <param name="cfg">
        /// <para>0 : No address check,</para>
        /// <para>1 : Address check, no broadcast,</para>
        /// <para>2 : Address check and 0 (0x00) broadcast,</para>
        /// <para>3 : Address check and 0 (0x00) and 255 (0xFF) broadcast</para>
        /// </param>
        public void SetControlsAddressCheck([Range(0, 3)] byte cfg)
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL1"]);
            data = (byte)((data & 0xFC) | (cfg & 0x03));
            WriteRegister(CCRegister.ConfigRegisters["PKTCTRL1"], data);
        }

        public byte GetControlsAddressCheck()
        {
            byte data = ReadConfigRegister(CCRegister.ConfigRegisters["PKTCTRL1"]);
            data = (byte)(data & 0x03);
            return data;
        }

        /// <summary>
        /// Set Channel Number
        /// </summary>
        /// <param name="channel">Channel</param>
        public void SetChannel([Range(0, 255)] byte channel)
        {
            WriteRegister(CCRegister.ConfigRegisters["CHANNR"], channel);
            Debug.WriteLine(string.Format("Channel bytes set to {0}.", channel), "Info");
        }

        public void GetInfo()
        {
            Debug.WriteLine("Info CC1101", "Info");
            PartNumber = ReadStatusRegister(CCRegister.CC1101_PARTNUM);
            Version = ReadStatusRegister(CCRegister.CC1101_VERSION);
        }

        public byte GetTxFifoInfo()
        {
            return ReadStatusRegister(CCRegister.CC1101_TXBYTES);
        }

        /// <summary>
        /// Received Signal Strength Indication
        /// </summary>
        /// <returns></returns>
        public byte GetRSSI()
        {
            byte data = ReadStatusRegister(CCRegister.CC1101_RSSI);
            return data;
        }

        public float GetRSSIdBm()
        {
            byte rssi = GetRSSI();
            if (rssi < 128)
            {
                return (rssi / 2F) - 74F;
            }
            else
            {
                return ((rssi - 256) / 2F) - 74F;
            }
        }

        public void GetGDOxInfo()
        {
            var test = ReadStatusRegister(CCRegister.CC1101_PKTSTATUS);
        }

        public void Reset()
        {
            Debug.WriteLine("Resetting CC1101", "Info");
            CommandStrobe(CCRegister.CC1101_SRES);
        }

        public void SetFlushTx()
        {
            CommandStrobe(CCRegister.CC1101_SFTX);
        }

        public void SetFlushRx()
        {
            CommandStrobe(CCRegister.CC1101_SFRX);
        }

        public void SetIdleState()
        {
            Debug.WriteLine("Setting CC1101 Idle state", "Info");
            CommandStrobe(CCRegister.CC1101_SIDLE);
        }

        public void SetPowerDownState()
        {
            Debug.WriteLine("Set Power Down", "Info");
            CommandStrobe(CCRegister.CC1101_SIDLE);
            CommandStrobe(CCRegister.CC1101_SPWD);
        }

        public void SetTxState()
        {
            Debug.WriteLine("Setting CC1101 Transmit state", "Info");
            CommandStrobe(CCRegister.CC1101_STX);
        }

        public void SetRxState()
        {
            Debug.WriteLine("Setting CC1101 Recieve state", "Info");
            CommandStrobe(CCRegister.CC1101_SRX);
        }

        //------------[check if Packet is received within defined time in ms]-----------
        //public bool WaitForPacket(int milliseconds)
        //{
        //    ShortWait(milliseconds);
        //    if (PacketAvailable())
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        //----------------------[check if Packet is received]---------------------------
        public void PacketAvailable()
        {
            while (!TxFinish) ;
            TxFinish = false;
            //while (rcvPin.Read() != GpioPinValue.High) ;                           //if RF package received
            //while (rcvPin.Read() != GpioPinValue.Low) ;
            //if (ReadSingleRegister(CCRegister.ConfigurationRegisters["IOCFG2"]) == 0x06)               //if sync word detect mode is used
            //{
            //wait till sync word is fully received
            //while (rcvPin.Read() == GpioPinValue.High)
            //}

        }

        /// <summary>
        /// Wait procedure
        /// </summary>
        /// <param name="delay">Miliseconds</param>
        public void ShortWait(int delay)
        {
            //Debug.WriteLine(string.Format("Waiting for CC1101 - {0} ms", delay), "Info");
            Thread.Sleep(delay);
        }

        void WaitForState(byte state, byte timeout)
        {
            byte fsm_state = 0;
            while (timeout > 0)
            {
                fsm_state = GetMarcState();

                if (fsm_state == state)
                    break;
                ShortWait(1);
                timeout--;
            }

            if (timeout == 0)
            {
                Debug.WriteLine(string.Format("Radio timeout reached in state {0} waiting for state {1}", CCRegister.StateNames[fsm_state], CCRegister.StateNames[state]), "Info");
                if (fsm_state == CCRegister.MARCSTATE_RXFIFO_OVERFLOW)
                {
                    SetFlushRx();
                    SetFlushTx();
                }
            }
        }

        /// <summary>
        /// Send byte in async mode
        /// </summary>
        /// <param name="data"></param>
        public void SentAsyncByte(byte data)
        {
            for (byte cnt = 0; cnt < 8; cnt++)
            {
                bool bit = ((data << cnt) & 0b1000_0000) == 0b1000_0000 ? true : false;
                if (bit)
                    txPin.Write(GpioPinValue.High);
                else
                    txPin.Write(GpioPinValue.Low);
                //ShortWait(1);
            }
            txPin.Write(GpioPinValue.Low);
            ShortWait(1);
        }

        public bool SendData([ReadOnlyArray()] byte[] dataToTransmit)
        {
            Debug.WriteLine("Writing TXFIFO", "Info");
            bool status = false;
            //byte marcState;

            var dataToTransmitInverted = new List<byte>();
            dataToTransmitInverted.Add((byte)dataToTransmit.Length);
            dataToTransmitInverted.AddRange(dataToTransmit);

            WriteBurstRegister(CCRegister.CC1101_TXFIFO, dataToTransmitInverted.ToArray());
            SetTxState();
            //WaitForState(CCRegister.MARCSTATE_TX, 10);
            //GetTxInfo();
            //GetGDOxInfo();

            // CCA enabled: will enter TX state only if the channel is clear
            //SetTxState();
            //ShortWait(10);

            //while (GetMarcState() != CCRegister.MARCSTATE_IDLE)
            //{
            //    ShortWait(100);
            //}

            // Check that TX state is being entered (state = RXTX_SETTLING)
            //var marcState = GetMarcState();
            //if ((marcState != CCRegister.MARCSTATE_TX) && (marcState != CCRegister.MARCSTATE_TX_END) && (marcState != CCRegister.MARCSTATE_RXTX_SWITCH))
            //{
            //    SetIdleState();
            //    SetFlushTx();
            //    //SetTxState();
            //    return false;
            //}
            //PacketAvailable();
            //byte cnt;
            //if ((cnt = GetTxFifoInfo()) == 0)
            //    status = true;
            //while (GetTxFifoInfo() != 0)
            //{
            //    var marcState = GetMarcState();
            //};

            //SetIdleState();

            //SetIdleState();
            byte marcState;
            while ((marcState = GetMarcState()) != CCRegister.MARCSTATE_IDLE)
            {
                ShortWait(10);
            }
            SetFlushTx();
            return status;
        }
    }

    public static class BitExt
    {
        public static byte SetBits(this byte self, byte parametr, byte lenght, byte offset)
        {
            if (parametr > (Math.Pow(2, lenght) - 1))
                throw new ArgumentException("Error");

            self = (byte)(self | (parametr << offset));
            return self;
        }
    }
}
