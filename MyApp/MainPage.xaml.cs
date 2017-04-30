using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

// Add using statements to the GrovePi libraries
using GrovePi;
using GrovePi.Sensors;
using GrovePi.I2CDevices;
using Windows.Graphics.Display;
using System.Reflection;

namespace MyApp
{
    public sealed partial class MainPage : Page
    {
        IDHTTemperatureAndHumiditySensor sensor = DeviceFactory.Build.DHTTemperatureAndHumiditySensor(Pin.DigitalPin4, DHTModel.Dht11);
        IRgbLcdDisplay display = DeviceFactory.Build.RgbLcdDisplay();

        DispatcherTimer dispatcherTimer;
        DateTimeOffset startTime;
        DateTimeOffset lastTime;
        
        public MainPage()
        {
            this.InitializeComponent();
            this.DispatcherTimerSetup();
           // this.textBox.Text = GetCurrentDisplaySize().ToString();
            System.Diagnostics.Debug.WriteLine("MainPage()");
        }

        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            //IsEnabled defaults to false
            System.Diagnostics.Debug.WriteLine("dispatcherTimer.IsEnabled = " + dispatcherTimer.IsEnabled);
            startTime = DateTimeOffset.Now;
            lastTime = startTime;
            System.Diagnostics.Debug.WriteLine("Calling dispatcherTimer.Start()\n");
            dispatcherTimer.Start();
            //IsEnabled should now be true after calling start
            System.Diagnostics.Debug.WriteLine("dispatcherTimer.IsEnabled = " + dispatcherTimer.IsEnabled);
        }

        void dispatcherTimer_Tick(object sender, object e)
        {

            System.Diagnostics.Debug.WriteLine("dispatcherTimer_Tick()");
            DateTimeOffset time = DateTimeOffset.Now;
            TimeSpan span = time - lastTime;
            lastTime = time;

            try
            {

                TelemtryData data = MeasureTelemetry();

                //sensor.Measure();
                //string sensorTemperatureInFahrenheit = sensor.TemperatureInFahrenheit.ToString();
                //string sensorHumidity = sensor.Humidity.ToString();

                string sensorTemperatureInFahrenheit = data.Temperature.ToString();
                string sensorHumidity = data.Humidity.ToString();

                this.txtT.Text = sensorTemperatureInFahrenheit + " F";
                this.txtH.Text = sensorHumidity + " %";

                display.SetText("T:" + sensorTemperatureInFahrenheit + " F.\nH:" + sensorHumidity + " %. ").SetBacklightRgb(127, 127, 127); ;

                string msgString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                System.Diagnostics.Debug.WriteLine("json:" + msgString);

                Task.Run(async () =>
                {

                    //await AzureIoTHub.SendDeviceToCloudMessageAsync(msgString);
                    await AzureIoTHub.SendDeviceToCloudMessageAsync(msgString);

                });

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

        }

        public static Size GetCurrentDisplaySize()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            TypeInfo t = typeof(DisplayInformation).GetTypeInfo();
            var props = t.DeclaredProperties.Where(x => x.Name.StartsWith("Screen") && x.Name.EndsWith("InRawPixels")).ToArray();
            var w = props.Where(x => x.Name.Contains("Width")).First().GetValue(displayInformation);
            var h = props.Where(x => x.Name.Contains("Height")).First().GetValue(displayInformation);
            var size = new Size(System.Convert.ToDouble(w), System.Convert.ToDouble(h));
            switch (displayInformation.CurrentOrientation)
            {
                case DisplayOrientations.Landscape:
                case DisplayOrientations.LandscapeFlipped:
                    size = new Size(Math.Max(size.Width, size.Height), Math.Min(size.Width, size.Height));
                    break;
                case DisplayOrientations.Portrait:
                case DisplayOrientations.PortraitFlipped:
                    size = new Size(Math.Min(size.Width, size.Height), Math.Max(size.Width, size.Height));
                    break;
            }
            return size;
        }

        private class TelemtryData
        {
            public string DeviceId;
            public double Temperature;
            public double Humidity;
            public bool LiveData;

            public TelemtryData()
            {
                // Make up some default random data for Azure lab if there is no live data
                Random random = new Random();
                DeviceId = "myPi";
                LiveData = false;

                // Some random number with +/- ranges
                Temperature = Math.Ceiling((90 + random.NextDouble() * 4 - 2) * 10) / 10;
                Humidity = Math.Ceiling((70 + random.NextDouble() * 4 - 2) * 10) / 10;
            }
        }

        private TelemtryData MeasureTelemetry()
        {
            TelemtryData data = new TelemtryData();
            
            if (sensor != null)
            {
                try
                {
                    sensor.Measure();
                    data.Temperature = sensor.TemperatureInFahrenheit;
                    data.Humidity = sensor.Humidity;
                    data.LiveData = true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
            }
            return data;
        }


    }
}
