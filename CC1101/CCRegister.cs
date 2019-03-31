using System.Collections.Generic;

namespace CC1101
{
    /// <summary>
    /// Description of CCRegister.
    /// </summary>
    public static class CCRegister
    {
        /**
		 * Type of transfers
		 */
        public static byte WRITE_BURST => 0x40;
        public static byte READ_SINGLE => 0x80;
        public static byte READ_BURST => 0xC0;

        /**
		 * PATABLE & FIFO's
		 */
        public static byte CC1101_PATABLE => 0x3E;        // PATABLE address
        public static byte CC1101_TXFIFO => 0x3F;        // TX FIFO address
        public static byte CC1101_RXFIFO => 0x3F;        // RX FIFO address

        /**
		 * Command strobes
		 */
        public static byte CC1101_SRES => 0x30;        // Reset CC1101 chip
        public static byte CC1101_SFSTXON => 0x31;        // Enable and calibrate frequency synthesizer (if MCSM0.FS_AUTOCAL=1). If in RX (with CCA):
                                                          // Go to a wait state where only the synthesizer is running (for quick RX / TX turnaround).
        public static byte CC1101_SXOFF => 0x32;        // Turn off crystal oscillator
        public static byte CC1101_SCAL => 0x33;        // Calibrate frequency synthesizer and turn it off. SCAL can be strobed from IDLE mode without
                                                       // setting manual calibration mode (MCSM0.FS_AUTOCAL=0)
        public static byte CC1101_SRX => 0x34;        // Enable RX. Perform calibration first if coming from IDLE and MCSM0.FS_AUTOCAL=1
        public static byte CC1101_STX => 0x35;        // In IDLE state: Enable TX. Perform calibration first if MCSM0.FS_AUTOCAL=1.
                                                      // If in RX state and CCA is enabled: Only go to TX if channel is clear
        public static byte CC1101_SIDLE => 0x36;        // Exit RX / TX, turn off frequency synthesizer and exit Wake-On-Radio mode if applicable
        public static byte CC1101_SWOR => 0x38;        // Start automatic RX polling sequence (Wake-on-Radio) as described in Section 19.5 if
                                                       // WORCTRL.RC_PD=0
        public static byte CC1101_SPWD => 0x39;        // Enter power down mode when CSn goes high
        public static byte CC1101_SFRX => 0x3A;        // Flush the RX FIFO buffer. Only issue SFRX in IDLE or RXFIFO_OVERFLOW states
        public static byte CC1101_SFTX => 0x3B;        // Flush the TX FIFO buffer. Only issue SFTX in IDLE or TXFIFO_UNDERFLOW states
        public static byte CC1101_SWORRST => 0x3C;        // Reset real time clock to Event1 value
        public static byte CC1101_SNOP => 0x3D;        // No operation. May be used to get access to the chip status byte

        /**
		 * CC1101 configuration registers
		 */
        public static IReadOnlyDictionary<string, byte> ConfigRegisters => new Dictionary<string, byte>()
        {
            {"IOCFG2" , 0x00},
            {"IOCFG1" , 0x01},
            {"IOCFG0" , 0x02},
            {"FIFOTHR" , 0x03},
            {"SYNC1" , 0x04},
            {"SYNC0" , 0x05},
            {"PKTLEN" , 0x06},
            {"PKTCTRL1" , 0x07},
            {"PKTCTRL0" , 0x08},
            {"ADDR" , 0x09},
            {"CHANNR" , 0x0A},
            {"FSCTRL1" , 0x0B},
            {"FSCTRL0" , 0x0C},
            {"FREQ2" , 0x0D},
            {"FREQ1" , 0x0E},
            {"FREQ0" , 0x0F},
            {"MDMCFG4" , 0x10},
            {"MDMCFG3" , 0x11},
            {"MDMCFG2" , 0x12},
            {"MDMCFG1" , 0x13},
            {"MDMCFG0" , 0x14},
            {"DEVIATN" , 0x15},
            {"MCSM2" , 0x16},
            {"MCSM1" , 0x17},
            {"MCSM0" , 0x18},
            {"FOCCFG" , 0x19},
            {"BSCFG" , 0x1A},
            {"AGCCTRL2" , 0x1B},
            {"AGCCTRL1" , 0x1C},
            {"AGCCTRL0" , 0x1D},
            {"WOREVT1" , 0x1E},
            {"WOREVT0" , 0x1F},
            {"WORCTRL" , 0x20},
            {"FREND1" , 0x21},
            {"FREND0" , 0x22},
            {"FSCAL3" , 0x23},
            {"FSCAL2" , 0x24},
            {"FSCAL1" , 0x25},
            {"FSCAL0" , 0x26},
            {"RCCTRL1" , 0x27},
            {"RCCTRL0" , 0x28},
            {"FSTEST" , 0x29},
            {"PTEST" , 0x2A},
            {"AGCTEST" , 0x2B},
            {"TEST2" , 0x2C},
            {"TEST1" , 0x2D},
            {"TEST0" , 0x2E}
        };

        // Defaults to 315 MHz, 4.15325 KBaud OOK, Max TX (10dBm)
        //public static Dictionary<string, byte> ConfigurationRegisterValues => new Dictionary<string, byte>()
        //{
        //    {"IOCFG0", 0x06},
        //    {"FIFOTHR", 0x47},
        //    {"PKTCTRL0", 0x01},
        //    {"FSCTRL1", 0x06},
        //    {"MDMCFG2", 0x30},
        //    {"MDMCFG1", 0x02},
        //    {"DEVIATN", 0x15},
        //    {"MCSM1", 0x00},
        //    {"MCSM0", 0x18},
        //    {"FOCCFG", 0x14},
        //    {"AGCCTRL0", 0x92},
        //    {"WORCTRL", 0xFB},
        //    {"FREND0", 0x11},
        //    {"FSCAL3", 0xE9},
        //    {"FSCAL2", 0x2A},
        //    {"FSCAL1", 0x00},
        //    {"FSCAL0", 0x1F},
        //    {"TEST2", 0x81},
        //    {"TEST1", 0x35},
        //    {"SYNC0", 0xFF},
        //    {"SYNC1", 0xFF},
        //    {"FREQ2", 0x0C},
        //    {"FREQ1", 0x1D},
        //    {"FREQ0", 0x89},
        //    {"MDMCFG4", 0xF7},
        //    {"MDMCFG3", 0x4F}
        //};

        // Address Config => No address check 
        // Base Frequency => 433.919830 
        // CRC Autoflush => false 
        // CRC Enable => false 
        // Carrier Frequency => 433.919830 
        // Channel Number => 0 
        // Channel Spacing => 49.987793 
        // Data Format => Normal mode 
        // Data Rate => 2.00224 
        // Deviation => 5.157471 
        // Device Address => 0 
        // Manchester Enable => false 
        // Modulation Format => ASK/OOK 
        // PA Ramping => false 
        // Packet Length => 255 
        // Packet Length Mode => Variable packet length mode. Packet length configured by the first byte after sync word 
        // Preamble Count => 2 
        // RX Filter BW => 58.035714 
        // Sync Word Qualifier Mode => No preamble/sync 
        // TX Power => 0 
        // Whitening => false 

        public static IReadOnlyDictionary<string, byte> ConfigRegisterValues => new Dictionary<string, byte>()
        {
  //{"IOCFG0",      0x06},
  //{"IOCFG2",      0x06},
  //{"PKTCTRL0",    0x01},
  //{"FSCTRL1",     0x06},
  //{"FREQ2",       0x10},
  //{"FREQ1",       0xB0},
  //{"FREQ0",       0x71},
  //{"MDMCFG4",     0xF8},
  //{"MDMCFG3",     0x93},
  //{"MDMCFG2",     0x30},
  //{"MCSM0",       0x18},
  //{"FOCCFG",      0x14},
  //{"AGCCTRL0",    0x92},
  //{"WORCTRL",     0xFB},
  //{"FREND0",      0x11},
  //{"FSCAL3",      0xE9},
  //{"FSCAL2",      0x2A},
  //{"FSCAL1",      0x00},
  //{"FSCAL0",      0x1F},
  //{"TEST2",       0x81},
  //{"TEST1",       0x35},
  //{"TEST0",       0x09}

  {"IOCFG2",      0x06}, //0x0D - Data output in async rx
  {"IOCFG0",      0x06}, //Data input if set async tx
  {"FIFOTHR",     0x47},
  {"PKTCTRL1",    0x03},
  {"PKTCTRL0",    0x31},
  {"FREQ2",       0x10},
  {"FREQ1",       0xB0},
  {"FREQ0",       0x71},
  {"MDMCFG4",     0xC9},
  {"MDMCFG2",     0x30},
  {"MCSM1",       0x00},
  {"MCSM0",       0x18},
  {"FOCCFG",      0x14},
  {"AGCCTRL0",    0x92},
  {"WORCTRL",     0xFB},
  {"FREND0",      0x17},
  {"FSCAL3",      0xE9},
  {"FSCAL2",      0x2A},
  {"FSCAL1",      0x00},
  {"FSCAL0",      0x1F},
  {"TEST2",       0x81},
  {"TEST1",       0x35},
  {"TEST0",       0x09}


              //{"IOCFG2",      0x06},
              //{"IOCFG0",      0x06},
              //{"FIFOTHR",     0x47},
              //{"PKTCTRL0",    0x01},
              //{"FSCTRL1",     0x06},
              //{"FREQ2",       0x10},
              //{"FREQ1",       0xB0},
              //{"FREQ0",       0x71},
              //{"MDMCFG4",     0xF6},
              //{"MDMCFG3",     0x43},
              //{"MDMCFG2",     0x30},
              //{"MDMCFG1",     0x20},
              //{"MCSM1",       0x00},
              //{"MCSM0",       0x14},
              //{"FOCCFG",      0x16},
              //{"AGCCTRL0",    0x92},
              //{"FREND0",      0x11},
              //{"FSCAL3",      0xE9},
              //{"FSCAL2",      0x2A},
              //{"FSCAL1",      0x00},
              //{"FSCAL0",      0x1F},
              //{"TEST2",       0x81},
              //{"TEST1",       0x35},
              //{"TEST0",       0x09}
        };


        // Status registers
        public static byte CC1101_PARTNUM => 0x30;        // Chip ID
        public static byte CC1101_VERSION => 0x31;        // Chip ID
        public static byte CC1101_FREQEST => 0x32;        // Frequency Offset Estimate from Demodulator
        public static byte CC1101_LQI => 0x33;        // Demodulator Estimate for Link Quality
        public static byte CC1101_RSSI => 0x34;        // Received Signal Strength Indication
        public static byte CC1101_MARCSTATE => 0x35;        // Main Radio Control State Machine State
        public static byte CC1101_WORTIME1 => 0x36;        // High public static byte of WOR Time
        public static byte CC1101_WORTIME0 => 0x37;        // Low public static byte of WOR Time
        public static byte CC1101_PKTSTATUS => 0x38;        // Current GDOx Status and Packet Status
        public static byte CC1101_VCO_VC_DAC => 0x39;        // Current Setting from PLL Calibration Module
        public static byte CC1101_TXBYTES => 0x3A;        // Underflow and Number of public static bytes
        public static byte CC1101_RXBYTES => 0x3B;        // Overflow and Number of public static bytes
        public static byte CC1101_RCCTRL1_STATUS => 0x3C;        // Last RC Oscillator Calibration Result
        public static byte CC1101_RCCTRL0_STATUS => 0x3D;        // Last RC Oscillator Calibration Result 

        /**
          MARCSTATES
          */
        public static byte MARCSTATE_SLEEP => 0x00;
        public static byte MARCSTATE_IDLE => 0x01;
        public static byte MARCSTATE_XOFF => 0x02;
        public static byte MARCSTATE_VCOON_MC => 0x03;
        public static byte MARCSTATE_REGON_MC => 0x04;
        public static byte MARCSTATE_MANCAL => 0x05;
        public static byte MARCSTATE_VCOON => 0x06;
        public static byte MARCSTATE_REGON => 0x07;
        public static byte MARCSTATE_STARTCAL => 0x08;
        public static byte MARCSTATE_BWBOOST => 0x09;
        public static byte MARCSTATE_FS_LOCK => 0x0A;
        public static byte MARCSTATE_IFADCON => 0x0B;
        public static byte MARCSTATE_ENDCAL => 0x0C;
        public static byte MARCSTATE_RX => 0x0D;
        public static byte MARCSTATE_RX_END => 0x0E;
        public static byte MARCSTATE_RX_RST => 0x0F;
        public static byte MARCSTATE_TXRX_SWITCH => 0x10;
        public static byte MARCSTATE_RXFIFO_OVERFLOW => 0x11;
        public static byte MARCSTATE_FSTXON => 0x12;
        public static byte MARCSTATE_TX => 0x13;
        public static byte MARCSTATE_TX_END => 0x14;
        public static byte MARCSTATE_RXTX_SWITCH => 0x15;
        public static byte MARCSTATE_TXFIFO_UNDEFLOW => 0x16;

        public static string[] StateNames => new string[]
        {
            "SLEEP",            // 00
            "IDLE",             // 01
            "XOFF",             // 02
            "VCOON_MC",         // 03
            "REGON_MC",         // 04
            "MANCAL",           // 05
            "VCOON",            // 06
            "REGON",            // 07
            "STARTCAL",         // 08
            "BWBOOST",          // 09
            "FS_LOCK",          // 10
            "IFADCON",          // 11
            "ENDCAL",           // 12
            "RX",               // 13
            "RX_END",           // 14
            "RX_RST",           // 15
            "TXRX_SWITCH",      // 16
            "RXFIFO_OVERFLOW",  // 17
            "FSTXON",           // 18
            "TX",               // 19
            "TX_END",           // 20
            "RXTX_SWITCH",      // 21
            "TXFIFO_UNDERFLOW", // 22
            "undefined",        // 23
            "undefined",        // 24
            "undefined",        // 25
            "undefined",        // 26
            "undefined",        // 27
            "undefined",        // 28
            "undefined",        // 29
            "undefined",        // 30
            "undefined"         // 31
        };

        //Patable index: -30  -20- -15  -10   0    5    7    10 dBm
        public static byte[] PatablePower_315 => new byte[] { 0x17, 0x1D, 0x26, 0x69, 0x51, 0x86, 0xCC, 0xC3 };
        public static byte[] PatablePower_433 => new byte[] { 0x6C, 0x1C, 0x06, 0x3A, 0x51, 0x85, 0xC8, 0xC0 };
        public static byte[] PatablePower_868 => new byte[] { 0x03, 0x17, 0x1D, 0x26, 0x50, 0x86, 0xCD, 0xC0 };
        public static byte[] PatablePower_915 => new byte[] { 0x0B, 0x1B, 0x6D, 0x67, 0x50, 0x85, 0xC9, 0xC1 };

        public static byte[] PatablePower => new byte[] { 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };//

        // 4x4 channel bandwidth limits
        public static int[] ChanBW_limits => new int[] {
            812,
            650,
            541,
            464,
            406,
            325,
            270,
            232,
            203,
            162,
            135,
            116,
            102,
            81,
            68,
            58
        };

        public static double CC1101_CLOCK_FREQUENCY => 26;
        public static int CC1101_GDO0 => 24;
        public static int CC1101_GDO2 => 25;
    }
}
