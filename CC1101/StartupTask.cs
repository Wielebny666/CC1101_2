using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Microsoft.IoT.Lightning.Providers;
using Windows.Devices;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace CC1101
{
    public sealed class StartupTask : IBackgroundTask
    {
        private CC1101 rf = new CC1101();
        //private WaveDecoder wave = new WaveDecoder();

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //

            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            rf.InitAll(); //433.92MHz
            //rf.InitGPIOAsyncTransmit();
            
            rf.SetCarrierFrequency(434.5);
            rf.SetDeviationFrequencySetting(20);
            rf.SetRxBandWidth(102);
            rf.SetBaudRate(1);
            rf.SetSyncMode(0);
            rf.SetManchesterEncoding(false);
            rf.SetPreambleNum(0);
            rf.SetCCAMode(0);

            rf.SetWhiteData(false);
            rf.SetPKTFormat(0);
            rf.SetCrcEnable(false);
            rf.SetPacketLengthCfg(1);
            //rf.SetCarrierSense(0);
            //rf.SetFilterLength(3);

            rf.SetIdleState();
            //rf.SetRxState();
            //rf.SetTxState();
            //wave.Measure();

            while (true)
            {
                //var rssi = rf.GetRSSIdBm();
                //var reslt = wave.Measure();

                //rf.ShortWait(100);
                //wave.MeasureAsync().GetAwaiter().GetResult();
                rf.SendData(new byte[] {
                    0b1111_1111 });

                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                //0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xac, 0xb5, 0x55,
                //0x35, 0x53, 0x35, 0x4d, 0x29, 0x83, 0x54, 0xac,
                //0xb3, 0x2c, 0xcc, 0xb3, 0x4a, 0xb3, 0x4a, 0xab,
                //0x4d, 0x34, 0xd4, 0xb5, 0x4a, 0xcd, 0x04 });
            }
            deferral.Complete();
        }
    }
}
