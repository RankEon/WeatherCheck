using System;
using System.Threading.Tasks;
using WeatherCheck.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace WeatherCheck
{
    /// <summary>
    /// OpenWeatherMapFacade contains functionality to interact with OpenWeatherMap API.
    /// </summary>
    static class OpenWeatherMapFacade
    {
        // Constants, we are interested in Finnish weather and results in metric -format.
        const string units = "metric";
        const string country = "FI";

        /// <summary>
        /// Asynchronous function to retrieve data from OpenWeatherMap API.
        /// </summary>
        /// <param name="city">Name of the city for which the weather data is retrieved.</param>
        /// <param name="allWeatherData">Observable collection for storing the weather data</param>
        /// <param name="apiKey">API key for OpenWeatherMap</param>
        /// <returns></returns>
        public static async Task GetWeatherDataAsync(
            string city,
            ObservableCollection<WeatherCheck.Models.List> allWeatherData,
            string apiKey)
        {
            // Check that a city name is provided.
            if(String.IsNullOrEmpty(city))
            {
                throw new Exception("Please, select a city first!");
            }

            // Handle responsedata
            string weatherDataResp;
            OpenWeatherMapData WeatherData = new OpenWeatherMapData();

            // Call OpenWeatherMap API.
            using (var HttpClient = new HttpClient())
            {
                string apiCall = $"http://api.openweathermap.org/data/2.5/forecast?q={ city },{ country }&units={ units }&appid={ apiKey }";
                weatherDataResp = await HttpClient.GetStringAsync(apiCall);
            }

            // Deserialize the data from JSON response.
            WeatherData = JsonConvert.DeserializeObject<OpenWeatherMapData>(weatherDataResp);
            OpenWeatherMapData wt = new OpenWeatherMapData();

            // Format date, time and image path and copy the results to allWeatherData
            foreach (var observationPoint in WeatherData.list)
            {
                System.DateTime timestamp = System.DateTimeOffset.FromUnixTimeSeconds(observationPoint.dt).DateTime;
                observationPoint.datetime = timestamp;
                observationPoint.weather[0].icon = $"/Resources/WeatherImages/{observationPoint.weather[0].icon}.png";
                allWeatherData.Add(observationPoint);
            }
        }

        /// <summary>
        /// Save weather data to file cache.
        /// </summary>
        /// <param name="city">Name of the city for which the weather data was retrieved.</param>
        /// <param name="allWeatherData">Observable collection to store into the cache</param>
        public static void SaveWeatherDataToFileCache(string city, ObservableCollection<WeatherCheck.Models.List> allWeatherData)
        {
            string jsonData = "{\n\"list\":" +  JsonConvert.SerializeObject(allWeatherData) + "\n}";
            File.WriteAllText(@"./Cache/" + city + ".json", jsonData);
        }

        /// <summary>
        /// Loads the weather data from cache.
        /// </summary>
        /// <param name="city">Name of the city for which the weather data was retrieved.</param>
        /// <param name="allWeatherData">Observable collection to store the cached data</param>
        public static void LoadWeatherDataFromFileCache(string city, ObservableCollection<WeatherCheck.Models.List> allWeatherData)
        {
            try
            {
                string cachedFile = File.ReadAllText(@"./Cache/" + city + ".json");

                var weatherDatalist = JsonConvert.DeserializeObject<OpenWeatherMapData>(cachedFile);

                foreach (var item in weatherDatalist.list)
                {
                    allWeatherData.Add(item);
                }
            }
            catch(FileNotFoundException fe)
            {
                MessageBox.Show("Please, select a city first!",
                                "City not set",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            catch(Exception e)
            {
                MessageBox.Show($"Error Occurred:\n{e.Message}\n\nStack:\n{e.StackTrace}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves the last update time (i.e. when the API was last called).
        /// </summary>
        public static void SaveLastUpdatedTime()
        {
            DateTime currDate = DateTime.Now;            
            File.WriteAllText(@"./Cache/lastUpdate.txt", currDate.ToString());
        }

        /// <summary>
        /// Gets the last updated time (i.e. when the API was last called).
        /// </summary>
        /// <returns>Time, when the API was called last.</returns>
        public static DateTime GetLastUpdatedTime()
        {
            try
            {
                string cachedFile = File.ReadAllText(@"./Cache/lastUpdate.txt");
                DateTime cachedTime = System.DateTime.Parse(cachedFile);
                return cachedTime;
            }
            catch(Exception e)
            {
                return System.DateTime.Now;
            }
        }
    }
}
