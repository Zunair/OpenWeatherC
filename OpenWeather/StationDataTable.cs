﻿using System;
using OpenWeather.Properties;
using System.Data;
using System.IO;
using System.Linq;

#if !ANDROID

using System.Device.Location;

#else

using Android.Locations;

#endif

namespace OpenWeather
{
    /// <summary>
    /// Class to hold data table of all METAR compliant weather stations
    /// </summary>
    internal sealed class StationDataTable : IDisposable
    {
        /// <summary>
        /// Data table of stations
        /// </summary>
        private DataTable Stations { get; set; }

        /// <summary>
        /// Constructor, builds the Stations data table from official_stations.csv resource
        /// </summary>
        public StationDataTable()
        {
            Stations = new DataTable();

            Stations.Columns.Add("ICAO", typeof(string));
            Stations.Columns.Add("Latitude", typeof(double));
            Stations.Columns.Add("Longitude", typeof(double));
            Stations.Columns.Add("Elevation", typeof(double));
            Stations.Columns.Add("Country", typeof(string));
            Stations.Columns.Add("Region", typeof(string));
            Stations.Columns.Add("City", typeof(string));

            using (var reader = new StringReader(Resources.official_stations))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("#") || string.IsNullOrEmpty(line)) continue;

                    var fields = line.Split(',');
                    Stations.Rows.Add(fields[0], Convert.ToDouble(fields[1]), Convert.ToDouble(fields[2]),
                        Convert.ToDouble(fields[3]), fields[4], fields[5], fields[6]);
                }
            }
        }

        /// <summary>
        /// Gets the station (if any) matching an ICAO code
        /// </summary>
        /// <param name="icao">Station's ICAO code</param>
        /// <returns>A MetarStation matching the ICAO code</returns>
        public StationInfo GetStationInfo(string icao)
        {
            var row = Stations.Rows.Cast<DataRow>().ToList().SingleOrDefault(r => (string)r["ICAO"] == icao.ToUpper());

            var info = new StationInfo
            {
                ICAO = (string)row["ICAO"],
                Location = new GeoCoordinate((double)row["Latitude"], (double)row["Longitude"]),
                Elevation = (double)row["Elevation"],
                Country = (string)row["Country"],
                Region = (string)row["Region"],
                City = (string)row["City"],
                Name = (string)row["City"]
            };

            return info;
        }

        /// <summary>
        /// Gets the nearest station to a given coordinate
        /// </summary>
        /// <param name="coordinate">Coorodinate of location</param>
        /// <returns>The Station closest to the provided coorodinate</returns>
        public StationInfo GetClosestStationInfo(GeoCoordinate coordinate)
            => GetClosestStationInfo(coordinate.Latitude, coordinate.Longitude);

        /// <summary>
        /// Gets the nearest station to a given latitude and longitude
        /// </summary>
        /// <param name="latitude">Latitude of location</param>
        /// <param name="longitude">Longitude of location</param>
        /// <returns>The Station closest to the provided coorodinate</returns>
        public StationInfo GetClosestStationInfo(double latitude, double longitude)
        {
            var rows = Stations.Rows.Cast<DataRow>().ToList();
            var closestStation = new StationInfo
            {
                ICAO = (string)rows[0]["ICAO"],
                Location = new GeoCoordinate((double)rows[0]["Latitude"], (double)rows[0]["Longitude"]),
                Elevation = (double)rows[0]["Elevation"],
                Country = (string)rows[0]["Country"],
                Region = (string)rows[0]["Region"],
                City = (string)rows[0]["City"],
                Name = (string)rows[0]["City"]
            };

#if !ANDROID
            var location = new GeoCoordinate(latitude, longitude);

            foreach (var row in from row in rows.Skip(1)
                                let dest = new GeoCoordinate((double)row["Latitude"], (double)row["Longitude"])
                                where location.GetDistanceTo(dest) < location.GetDistanceTo(closestStation.Location)
                                select row)
                closestStation = new StationInfo
                {
                    ICAO = (string)row["ICAO"],
                    Location = new GeoCoordinate((double)row["Latitude"], (double)row["Longitude"]),
                    Elevation = (double)row["Elevation"],
                    Country = (string)row["Country"],
                    Region = (string)row["Region"],
                    City = (string)row["City"],
                    Name = (string)row["City"]
                };
#else
            var location = new Location("ORGIN")
            {
                Latitude = latitude,
                Longitude = longitude
            };

            foreach (var row in from row in rows
                                let possibleEndPoint = new Location("DEST_P")
                                {
                                    Latitude = (double)row["Latitude"],
                                    Longitude = (double)row["Longitude"]
                                }
                                let currentEndPoint = new Location("DEST")
                                {
                                    Latitude = closestStation.Location.Latitude,
                                    Longitude = closestStation.Location.Longitude
                                }
                                where location.DistanceTo(possibleEndPoint) < location.DistanceTo(currentEndPoint)
                                select row)
                closestStation = new StationInfo
            {
                ICAO = (string) row["ICAO"],
                Location = new GeoCoordinate((double)row["Latitude"], (double)row["Longitude"]),
                Elevation = (double) row["Elevation"],
                Country = (string) row["Country"],
                Region = (string) row["Region"],
                City = (string) row["City"],
                Name = (string)row["City"]
            };
#endif
            return closestStation;
        }

        #region IDisposable Support

        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
                Stations.Dispose();

            Stations = null;
            disposedValue = true;
        }

        public void Dispose() => Dispose(true);

        #endregion IDisposable Support
    }
}