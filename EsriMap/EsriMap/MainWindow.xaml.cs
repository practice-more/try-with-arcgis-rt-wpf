using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
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
using System.Windows.Controls.Primitives;
using Esri.ArcGISRuntime.Http;
using System.Collections.ObjectModel;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Portal;

namespace EsriMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Create the UI, setup the control references and execute initialization 
            Initialize();
        }


        // Map initialization logic is contained in MapViewModel.cs
        // Create and hold reference to the feature layer
        private FeatureLayer _featureLayer;
        FeatureQueryResult _queryResult;

        // for license your app
        // after that, there will be no "only for develop" in the app
        private string _licenseKey = "runtimelite,1000,your license key";

        private void LicenseWithLicenseKey(string key)
        {
            try
            {
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense(key);
            }
            catch 
            {
                throw;
            }
        }

        // Indicate the url (portal) to authenticate with (ArcGIS Online)
        static private Uri ServiceUri = new Uri("http://www.arcgis.com/sharing/rest");

        public class MyChallengeHandler : IChallengeHandler
        {
            public Task<Credential> CreateCredentialAsync(CredentialRequestInfo requestInfo)
            {
                throw new NotImplementedException();
            }
        }

        private async Task LicenseWithNamedUserAccountAsync()
        {
            // Challenge the user for portal credentials (OAuth credential request for arcgis.com)
            CredentialRequestInfo loginInfo = new CredentialRequestInfo
            {

                // Use the OAuth implicit grant flow
                GenerateTokenOptions = new GenerateTokenOptions
                {
                    TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit
                },

                ServiceUri = ServiceUri

            };

            try
            {
                Esri.ArcGISRuntime.Security.AuthenticationManager.Current.ChallengeHandler = new MyChallengeHandler();

                // Call GetCredentialAsync on the AuthenticationManager to invoke the challenge handler
                Credential cred = await AuthenticationManager.Current.GetCredentialAsync(loginInfo, false);

                // Connect to the portal (ArcGIS Online) using the credential
                ArcGISPortal arcgisPortal = await ArcGISPortal.CreateAsync(loginInfo.ServiceUri, cred);

                // Get LicenseInfo from the portal
                Esri.ArcGISRuntime.LicenseInfo licenseInfo = arcgisPortal.PortalInfo.LicenseInfo;

                // ... code here to license the app immediately and/or save the license (JSON string) to take the app offline ...
                // License the app using the license info
                Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.SetLicense(licenseInfo);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private void InitializeRuntimeEnvironment()
        {
            
            try
            {
                // License with license key
                LicenseWithLicenseKey(_licenseKey);


                // License With Named User Account
                LicenseWithNamedUserAccountAsync();

                // Add product infomation header 
                System.Net.Http.Headers.ProductInfoHeaderValue infoHeader = new System.Net.Http.Headers.ProductInfoHeaderValue("MyProductName", "version");
                if (null == ArcGISHttpClientHandler.CustomUserAgentValues)
                {
                    ArcGISHttpClientHandler.CustomUserAgentValues = new ObservableCollection<System.Net.Http.Headers.ProductInfoHeaderValue>();
                }
                ArcGISHttpClientHandler.CustomUserAgentValues.Add(infoHeader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private async void Initialize()
        {
            InitializeRuntimeEnvironment();
            myPopup.Width = myPopup.Height = 0;
            // Create new Map with basemap
            var myMap = new Map(Basemap.CreateTopographic());

            // Create envelope to be used as a target extent for map's initial viewpoint
            Envelope myEnvelope = new Envelope(
                -1131596.019761, 3893114.069099, 3926705.982140, 7977912.461790,
                SpatialReferences.WebMercator);

            // Set the initial viewpoint for map
            myMap.InitialViewpoint = new Viewpoint(myEnvelope);

            // Provide used Map to the MapView
            MyMapView.Map = myMap;

            // Create Uri for the feature service
            Uri featureServiceUri = new Uri(
                "http://sampleserver6.arcgisonline.com/arcgis/rest/services/DamageAssessment/FeatureServer/0");

            // Initialize feature table using a url to feature server url
            var featureTable = new ServiceFeatureTable(featureServiceUri);

            // Initialize a new feature layer based on the feature table
            _featureLayer = new FeatureLayer(featureTable);

            // Set the selection color for feature layer
            _featureLayer.SelectionColor = Colors.Cyan;

            // Set the selection width
            _featureLayer.SelectionWidth = 3;

            // Make sure that used feature layer is loaded before we hook into the tapped event
            // This prevents us trying to do selection on the layer that isn't initialized
            await _featureLayer.LoadAsync();

            // Check for the load status. If the layer is loaded then add it to map
            if (_featureLayer.LoadStatus == LoadStatus.Loaded)
            {
                // Add the feature layer to the map
                myMap.OperationalLayers.Add(_featureLayer);

                // Add tap event handler for mapview
                MyMapView.GeoViewTapped += OnMapViewTapped;
            }
        }

        private async void OnMapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                // Define the selection tolerance (half the marker symbol size so that any click on the symbol will select the feature)
                double tolerance = 14;

                // Convert the tolerance to map units
                double mapTolerance = tolerance * MyMapView.UnitsPerPixel;

                // Define the envelope around the tap location for selecting features
                var selectionEnvelope = new Envelope(e.Location.X - mapTolerance, e.Location.Y - mapTolerance, e.Location.X + mapTolerance,
                    e.Location.Y + mapTolerance, MyMapView.Map.SpatialReference);

                // Define the query parameters for selecting features
                var queryParams = new QueryParameters();

                // Set the geometry to selection envelope for selection by geometry
                queryParams.Geometry = selectionEnvelope;

                // Select the features based on query parameters defined above
                 _queryResult = await _featureLayer.SelectFeaturesAsync(queryParams, Esri.ArcGISRuntime.Mapping.SelectionMode.New);
                bool hasResult = false;
                foreach(var r in _queryResult)
                {
                    hasResult = true;
                    Feature f = r as Feature;
                    string attribute = "";
                    foreach(var a in f.Attributes)
                    {
                        attribute = attribute + a.ToString() + "\n";
                    }

                    MapPoint p = f.Geometry as MapPoint;

                    Point sp = MyMapView.LocationToScreen(p);
                    TextBlock popupText = new TextBlock();
                    popupText.Background = Brushes.LightBlue;
                    popupText.Foreground = Brushes.Blue;
                    popupText.Text = attribute;
                    myPopup.Child = popupText;
                    myPopup.Width = 200;
                    myPopup.Height = 100;
                    var b = MyMapView.Margin;
                    
                    myPopup.Margin = new Thickness(sp.X, sp.Y, MyMapView.ActualWidth - 200 - sp.X, MyMapView.ActualHeight - 100 - sp.Y);
                }

                if(!hasResult)
                {
                    myPopup.Width = myPopup.Height = 0;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Sample error", ex.ToString());
            }
        }
    }
}
