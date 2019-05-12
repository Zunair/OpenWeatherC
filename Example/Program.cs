extern alias winUni;

using OpenWeather;
using System;
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;
//using Windows.Devices.Geolocation;
//using winGeo = Windows.Devices.Geolocation;
using winUni.Windows.Devices.Geolocation;

namespace Example
{
    internal class Program
    {
        static GeoCoordinateWatcher Watcher = null;

        private static void Main(string[] args)
        {
            //Optional, build the StationDataTable without any actions, otherwise it will be built upon first loopkup like below.
            //On average, increases the first lookup time by 5 times. Obviously it's of no use in this application, but for an
            //application that runs lookups at a later time, you could build the table at the start and have it ready for later (assuming you even want persistant lookup).
            //StationLookup.ZeroActionInitialize();
            //Console.WriteLine("Don't wait more than 10 seconds...");

            Task t = new Task(() => {
                //GeoPositionPermission

                Watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                // Catch the StatusChanged event.

                Watcher.MovementThreshold = 0;
                CancellationTokenSource source = new CancellationTokenSource();

                Task t3NonBlocking = new Task(() =>
                {

                    Task t2 = new Task(() =>
                    {

                        try
                        {
                            Console.Write("Locating Device...");
                            var accessStatus = Geolocator.RequestAccessAsync();

                            while (accessStatus.Status == Windows.Foundation.AsyncStatus.Completed)
                            {
                                Console.Write(".");
                                System.Threading.Thread.Sleep(1);
                                //t2.Status
                                if (source.IsCancellationRequested)
                                    break;
                            }
                            Console.WriteLine();
                        }
                        catch (TimeoutException ex)
                        {
                            Console.WriteLine("Error: " + ex.Message);
                        }
                    });
                    t2.Start();
                    t2.Wait();
                });

                t3NonBlocking.Start();
                t3NonBlocking.Wait(TimeSpan.FromSeconds(15));
                source.Cancel();

                if (t3NonBlocking.IsCompleted)
                {
                    // Subscribe to StatusChanged event to get updates of location status changes
                    //_geolocator.StatusChanged += OnStatusChanged;

                    if (Watcher.Permission != GeoPositionPermission.Granted) Console.WriteLine("Make sure Windows 10 location privacy settings is enabled in settings.");

                    Watcher.StatusChanged += Watcher_StatusChanged;
                    // Start the watcher.

                    Watcher.Start();
                }
                else
                {
                    Console.WriteLine("Failed to get location.");
                }
                //ResolveAddressSync();
            });

            t.Start();
            Console.ReadLine();
        }

        static bool locationFound = false;

        static void Watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Ready)
            {
                // Display the latitude and longitude.
                if (Watcher.Position.Location.IsUnknown)
                {
                    Console.WriteLine( "Cannot find location data");
                }
                else if (!locationFound)
                {
                    Console.WriteLine("lat: " + Watcher.Position.Location.Latitude.ToString());
                    Console.WriteLine("lon: " + Watcher.Position.Location.Longitude.ToString());
                    locationFound = true;

                    //var station = MetarStationLookup.Instance.Lookup(Watcher.Position.Location.Latitude, Watcher.Position.Location.Longitude);
                    var station = MetarStationLookup.Instance.Lookup(33.6, -102.0333333333333); //0 results
                    station.Updated += Station_Updated;
                    station.Update();
                    //ResolveAddressSync();             
                }
            }
        }

        private static void Station_Updated(object source, LocationUpdateEventArgs e)
        {
            var station = source as MetarStation;
            Console.WriteLine();
            Console.WriteLine($"Station: {station.GetStationInfo.Name}\n" +
                             $"ICAO: {station.GetStationInfo.ICAO}\n" +
                             $"Temperature: {station.Weather.Temperature} {station.Units.TemperatureUnit}\n" +
                             $"Pressure: {station.Weather.Pressure} {station.Units.PressureUnit}\n" +
                             $"Wind Speed: {station.Weather.WindSpeed} {station.Units.WindSpeedUnit}");
        }

        //static void ResolveAddressSync()
        //{
        //    GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
        //    watcher.MovementThreshold = 1.0; // set to one meter
        //    watcher.TryStart(false, TimeSpan.FromMilliseconds(1000));

        //    CivicAddressResolver resolver = new CivicAddressResolver();

        //    if (watcher.Position.Location.IsUnknown == false)
        //    {
        //        CivicAddress address = resolver.ResolveAddress(watcher.Position.Location);

        //        if (!address.IsUnknown)
        //        {
        //            Console.WriteLine("Country: {0}, Zip: {1}",
        //                    address.CountryRegion,
        //                    address.PostalCode);
        //        }
        //        else
        //        {
        //            Console.WriteLine("Address unknown.");
        //        }
        //    }
        //}

    }
}