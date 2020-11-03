using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AzFunDemo.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<SensorData> ReceivedItems { get; } = new ObservableCollection<SensorData>();

        private HubConnection _connection;

        public MainWindow()
        {
            InitializeComponent();

            listBox.ItemsSource = ReceivedItems;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:7071/api")
                .WithAutomaticReconnect()
                .Build();
            _connection.On<SensorData[]>("alert", async (data) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in data)
                    {
                        ReceivedItems.Insert(0, item);
                    }
                });
            });

            while(true)
            {
                try
                {
                    await _connection.StartAsync();
                    break;
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }
    }

    public class SensorData
    {
        [JsonProperty("value")]
        public int Value { get; set; }
        [JsonProperty("dateTime")]
        public DateTimeOffset DateTime { get; set; }
        [JsonProperty("sensor")]
        public Sensor Sensor { get; set; }

        public override string ToString() => 
            $"{Sensor.Id}: {Value} at {DateTime}.";
    }

    public class Sensor
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

}
