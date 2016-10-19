using Emmellsoft.IoT.Rpi.SenseHat;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;

namespace MeetingPolice.Windows.Models
{
    internal class MainModel : INotifyPropertyChanged
    {
        private DeviceClient deviceClient;
        private const string IotHubUri = "YOUR_KEY_HERE";
        private const string DeviceId = "meetingpolice-01";
        private const string DeviceKey = "YOUR_KEY_HERE";

        private DispatcherTimer measurementTimer = new DispatcherTimer();
        private const int measurementTimerInterval = 500;
        private DispatcherTimer flashingTimer = new DispatcherTimer();
        private const int flashingTimerInterval = 500;
        private const int messageReceiveInterval = 1000;

        private const int humidityThresholdForLocalWarning = 40;

        private ISenseHat senseHat = null;

        private bool hasSensorStartedWorking = false;

        private int currentFlashingFrame = 0;

        private double _MeasuredHumidity;
        public double MeasuredHumidity
        {
            get
            {
                return _MeasuredHumidity;
            }
            set
            {
                if (_MeasuredHumidity != value)
                {
                    _MeasuredHumidity = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainModel()
        {
            deviceClient = DeviceClient.Create(
                IotHubUri,
                new DeviceAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey),
                TransportType.Http1);

            measurementTimer.Interval = TimeSpan.FromMilliseconds(measurementTimerInterval);
            measurementTimer.Tick += MeasurementTimer_Tick;

            flashingTimer.Interval = TimeSpan.FromMilliseconds(flashingTimerInterval);
            flashingTimer.Tick += FlashingTimer_Tick;

            InitializeAsync();
        }

        public async void InitializeAsync()
        {
            // this must be separated from the constructor because await is not allowed there
            senseHat = await SenseHatFactory.GetSenseHat();
            senseHat.Display.Reset();
            senseHat.Display.Update();

            measurementTimer.Start();
            ListenForMessagesAsync();
        }

        private async void MeasurementTimer_Tick(object sender, object e)
        {
            senseHat.Sensors.HumiditySensor.Update();
            if (senseHat.Sensors.Humidity.HasValue)
            {
                hasSensorStartedWorking = true;
                MeasuredHumidity = senseHat.Sensors.Humidity.Value;

                // Display "local" (not sent from the cloud) warning
                if (!flashingTimer.IsEnabled)
                {
                    if (MeasuredHumidity >= humidityThresholdForLocalWarning)
                    {
                        senseHat.Display.Fill(Colors.Yellow);
                        senseHat.Display.Update();
                    }
                    else
                    {
                        senseHat.Display.Reset();
                        senseHat.Display.Update();
                    }
                }
            }

            // Push message to IoT Hub (Event Hubs)
            if (hasSensorStartedWorking)
            {
                await PushTemperatureToCloud(MeasuredHumidity);
            }
        }

        private async Task PushTemperatureToCloud(double measuredTemperature)
        {
            var messageString = JsonConvert.SerializeObject(new HumidityDataPoint() { PartitionKey = DeviceId, Timestamp = DateTimeOffset.Now, Humidity = MeasuredHumidity });
            var message = new Message(Encoding.UTF8.GetBytes(messageString));
            try
            {
                await deviceClient.SendEventAsync(message);
            }
            catch { }
        }

        private async void ListenForMessagesAsync()
        {
            while (true)
            {
                var message = await deviceClient.ReceiveAsync();
                if (message == null)
                {
                    await Task.Delay(messageReceiveInterval);
                    continue;
                }

                try
                {
                    var messageString = Encoding.UTF8.GetString(message.GetBytes());
                    if (messageString == "flashScreen")
                    {
                        StartFlashingScreen();
                    }
                    else
                    {
                        StopFlashingScreen();
                    }
                }
                catch
                { }
                finally
                {
                    await deviceClient.CompleteAsync(message);
                }
            }
        }

        private void StartFlashingScreen()
        {
            if (!flashingTimer.IsEnabled)
            {
                currentFlashingFrame = -1;
                flashingTimer.Start();
            }
        }

        private void StopFlashingScreen()
        {
            if (flashingTimer.IsEnabled)
            {
                flashingTimer.Stop();
                currentFlashingFrame = 0;
                senseHat.Display.Reset();
                senseHat.Display.Update();
            }
        }

        private void FlashingTimer_Tick(object sender, object e)
        {
            currentFlashingFrame++;
            if (currentFlashingFrame > 3) currentFlashingFrame = 0;

            switch (currentFlashingFrame)
            {
                case 0:
                    senseHat.Display.Fill(Colors.Red);
                    senseHat.Display.Update();
                    break;
                case 1:
                    senseHat.Display.Fill(Colors.DarkRed);
                    senseHat.Display.Update();
                    break;
                case 2:
                    senseHat.Display.Fill(Colors.Black);
                    senseHat.Display.Update();
                    break;
            }
        }

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string caller = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
        private void OnPropertyChangedExplicit(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }
        #endregion
    }
}
