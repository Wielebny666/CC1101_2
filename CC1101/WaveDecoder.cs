using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace CC1101
{
    class WaveDecoder
    {
        private GpioPin rxPin;
        private GpioChangeReader changeReader;

        private Stopwatch stopwatch;
        private TimeSpan Timeout = TimeSpan.FromMilliseconds(10000);

        public WaveDecoder()
        {
            //if (LightningProvider.IsLightningEnabled)
            //{
            //    LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            //}
            InitRxGpio();
        }

        public void InitRxGpio()
        {
            try
            {
                // Get default controller
                GpioController gpioController = GpioController.GetDefault();
                if (gpioController == null)
                {
                    return;
                }
                rxPin = gpioController.OpenPin(CCRegister.CC1101_GDO2);
                rxPin.SetDriveMode(GpioPinDriveMode.Input);

                //changeReader = new GpioChangeReader(rxPin);
                //changeReader.Polarity = GpioChangePolarity.Both; // one measurement results in one rising and one falling edge

                //// we use the stopwatch to time the trigger pulse
                //stopwatch = Stopwatch.StartNew();
                //stopwatch.Start();
            }
            catch (Exception ex)
            {
                throw new Exception($"GPIO_{CCRegister.CC1101_GDO2} initialization failed", ex);
            }
        }

        public bool Measure()
        {
            return rxPin.Read() == GpioPinValue.High ? true : false;
        }

        private void RxPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            var result = args.Edge == GpioPinEdge.FallingEdge ? false : true;

        }

        public async Task MeasureAsync()
        {
            try
            {
                changeReader.Clear();
                changeReader.Start();
                CancellationTokenSource source = new CancellationTokenSource(20000);
                await changeReader.WaitForItemsAsync(100).AsTask(source.Token);
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                changeReader.Stop();
            }


            // ***
            // *** Get all of the change records.
            // ***
            List<GpioChangeRecord> changeRecords = changeReader.GetAllItems().ToList();

            Decode(changeRecords);
        }

        private void Decode(List<GpioChangeRecord> wave)
        {
            List<(bool state, double time)> waveMasure = new List<(bool state, double time)>();

            var cnt = wave.Count;
            for (int i = 0; i < cnt - 1; i++)
            {
                var tick_n = wave[i].RelativeTime.Ticks;
                var tick_n_plus_one = wave[i + 1].RelativeTime.Ticks;
                waveMasure.Add((state: wave[i].Edge == GpioPinEdge.FallingEdge ? false : true, time: tick_n_plus_one - tick_n));
            }
        }
    }
}
