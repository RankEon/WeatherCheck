using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
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
using System.Windows.Threading;
using WeatherCheck.Models;

namespace WeatherCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<WeatherCheck.Models.List> allWeatherData;
        private ObservableCollection<WeatherCheck.Models.List> currentWeatherData;
        private List<string> cityList;
        private DispatcherTimer UpdateTimer;
        private DateTime LastUpdatedTime;
        private string SelectedCity;
        private string ApiKey;

        /// <summary>
        /// Application main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            GetApiKey();

            // Initialize timer for weather updates. Weather data is updated automatically
            // every 12m 20sec, otherwise a cache is used. This is to avoid excessive calls
            // OpenWeatherMap API.
            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Tick += new EventHandler(UpdateTimer_Tick);
            UpdateTimer.Interval = new TimeSpan(0, 12, 20);
            UpdateTimer.Start();

            // Initialize collections
            allWeatherData = new ObservableCollection<WeatherCheck.Models.List>();
            currentWeatherData = new ObservableCollection<WeatherCheck.Models.List>();
            cityList = new List<string>();

            // Keeps track of time elapsed since the last API -call
            LastUpdatedTime = OpenWeatherMapFacade.GetLastUpdatedTime();

            // Load the list of Finland cities/towns
            // and get currently selected city.
            LoadCityList();
            GetSelectedCity();

            if (!String.IsNullOrEmpty(SelectedCity))
            {
                if (IsWeatherUpdateAllowed())
                {
                    UpdateWeatherDataAsync();
                }
                else
                {
                    OpenWeatherMapFacade.LoadWeatherDataFromFileCache(SelectedCity, allWeatherData);
                    SetCurrentWeatherData();
                }
            }

            // Initialize databindings
            icCurrentWeatherData.ItemsSource = currentWeatherData;
            icWeatherData.ItemsSource = allWeatherData;
            tbSelectedCityName.Text = SelectedCity;
        }

        /// <summary>
        /// Retrieves the API -key and stores it to internal (private) variable.
        /// </summary>
        private void GetApiKey()
        {
            try
            {
                ApiKey = File.ReadAllText(@"./Resources/ApiKey.txt");
                
                if(ApiKey.Contains("00000000000000000000000000000000"))
                {
                    AskForApiKeyDialog();
                }
            }
            catch(FileNotFoundException fe)
            {
                AskForApiKeyDialog();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AskForApiKeyDialog()
        {
            ModalApiKeyDialog dialog = new ModalApiKeyDialog();
            dialog.ShowDialog();

            if (!String.IsNullOrEmpty(dialog.ApiKey))
            {
                ApiKey = dialog.ApiKey;
                File.WriteAllText(@"./Resources/ApiKey.txt", ApiKey);
            }
            else
            {
                throw new Exception("API Key is not set correctly, please enter valid API -key or modify ApiKey.txt");
            }
        }

        /// <summary>
        /// Updates the current weather data bindings.
        /// </summary>
        private void SetCurrentWeatherData()
        {
            if(currentWeatherData.Count > 0)
            {
                currentWeatherData.Clear();
            }

            if (allWeatherData.Count > 0)
            {
                currentWeatherData.Add(allWeatherData[0]);
            }
        }

        /// <summary>
        /// Loads the list of cities/towns.
        /// The list is based on https://fi.wikipedia.org/wiki/Luettelo_Suomen_kunnista
        /// </summary>
        private void LoadCityList()
        {
            string csvList = System.IO.File.ReadAllText(@"./Resources/Kunnat.csv", Encoding.Unicode);

            foreach (string item in (csvList.Split(';')).ToList())
            {
                cityList.Add(item);
            }
        }

        /// <summary>
        /// Gets the currently selected city and stores it into internal (private) variable.
        /// </summary>
        private void GetSelectedCity()
        {
            try
            {
                SelectedCity = System.IO.File.ReadAllText(@"./Cache/SelectedCity.txt");
            }
            catch(Exception e)
            {
                SelectedCity = String.Empty;
            }
        }

        /// <summary>
        /// Triggered, when the timer expires (every 12m 20sec) and initiates weather data update.
        /// </summary>
        private void UpdateTimer_Tick(Object sender, EventArgs e)
        {
            UpdateWeatherDataAsync();
        }

        /// <summary>
        /// Updates the weather data asynchronously.
        /// </summary>
        public async void UpdateWeatherDataAsync()
        {
            try
            {
                imgLoading.Visibility = Visibility.Visible;

                // Clear the current data, call and await OpenWeatherMap API to return data
                // and cache the data 
                // and update the last update time.
                allWeatherData.Clear();
                await OpenWeatherMapFacade.GetWeatherDataAsync(SelectedCity, allWeatherData, ApiKey);
                OpenWeatherMapFacade.SaveWeatherDataToFileCache(SelectedCity, allWeatherData);
                OpenWeatherMapFacade.SaveLastUpdatedTime();
                SetCurrentWeatherData();
                imgLoading.Visibility = Visibility.Hidden;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error Occurred:\n{e.Message}\n\nStack:\n{e.StackTrace}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                imgLoading.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Check whether weather data updat is allowed from OpenWeatherMap API.
        /// </summary>
        /// <returns>
        /// True  - weather data update is allowed.
        /// False - weather data update is not allowed (last update less than 12m 20 sec)
        /// </returns>
        public bool IsWeatherUpdateAllowed()
        {
            TimeSpan timeSpan = DateTime.Now.Subtract(LastUpdatedTime);

            if(timeSpan.TotalMinutes > 15)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles the keypress, when a city selection button is pressed. It has two
        /// functions depending on the application state:
        ///   1) From weather view, when a city is selected, opens city selection view
        ///   2) If in city selection view, sets and applies the selection and returns to
        ///      weather view.
        /// </summary>
        private void btnSelectCity_Click(object sender, RoutedEventArgs e)
        {
            if(borderCityName.Visibility == Visibility.Collapsed)
            {
                borderSelectedCityName.Visibility = Visibility.Collapsed;
                borderCityName.Visibility = Visibility.Visible;
                lbCitySuggestions.Visibility = Visibility.Visible;
                icCurrentWeatherData.Visibility = Visibility.Collapsed;
                btnGetCityWeather.Content = " Set ";
            }
            else if(borderSelectedCityName.Visibility == Visibility.Collapsed)
            {
                if(!String.IsNullOrEmpty(tbCityName.Text) && 
                   !SelectedCity.Equals(tbCityName.Text))
                {
                    SelectedCity = tbCityName.Text;
                    tbSelectedCityName.Text = SelectedCity;
                    System.IO.File.WriteAllText(@"./Cache/SelectedCity.txt", SelectedCity);
                    UpdateWeatherDataAsync();
                }

                borderSelectedCityName.Visibility = Visibility.Visible;
                borderCityName.Visibility = Visibility.Collapsed;
                lbCitySuggestions.Visibility = Visibility.Collapsed;
                icCurrentWeatherData.Visibility = Visibility.Visible;
                btnGetCityWeather.Content = " Select City ";
            }
        }

        /// <summary>
        /// Triggered when the user has selected a city from the list of city name suggestions
        /// </summary>
        private void lbCitySuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lbCitySuggestions.SelectedIndex != -1)
            {
                tbCityName.Text = lbCitySuggestions.SelectedItem.ToString();
            }
        }

        /// <summary>
        /// Show city name suggestions, which is updated based on what the user types in the city selection view.
        /// Inspiration from Stackoverflow and http://www.c-sharpcorner.com/uploadfile/dpatra/autocomplete-textbox-in-wpf/
        /// </summary>
        private void tbCityName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string userTypedStr = tbCityName.Text;
            List<string> matchedCities = new List<string>();

            // If the user has typed something to the textbox, check whether it matches to any
            // cities in the suggestion list and if yes, show those suggestions.
            if (!String.IsNullOrEmpty(userTypedStr))
            {
                foreach (string city in cityList)
                {
                    if (city.StartsWith(userTypedStr, StringComparison.CurrentCultureIgnoreCase))
                    {
                        matchedCities.Add(city);
                    }
                }
            }

            if (matchedCities.Count > 0)
            {
                lbCitySuggestions.ItemsSource = matchedCities;
            }
            else
            {
                lbCitySuggestions.ItemsSource = null;
            }
        }
    }
}
