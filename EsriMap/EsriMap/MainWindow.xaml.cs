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

        private async void Initialize()
        {
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

                ShowLabel();
            }
        }

        private void ShowLabel()
        {
            //{
            //  "labelExpressionInfo": 
            //  {
            //                    "expression": "return $feature.address;"
            //  },
            //  "labelPlacement": "esriServerPolygonPlacementAlwaysHorizontal",
            //  "symbol": 
            //  {
            //                    "color": [255,0,255,123],
            //    "font": { "size": 16 },
            //    "type": "esriTS"
            //  }
            //}
            //https://developers.arcgis.com/net/latest/wpf/guide/add-labels.htm
            // Create a StringBuilder to create the label definition JSON string
            StringBuilder addressLabelsBuilder = new StringBuilder();
            addressLabelsBuilder.AppendLine("{");
            //     Define a labeling expression that will show the address attribute value
            addressLabelsBuilder.AppendLine("\"labelExpressionInfo\": {");
            addressLabelsBuilder.AppendLine("\"expression\": \"return 'id:' + $feature.objectid;\"},");
            //     Align labels horizontally
            addressLabelsBuilder.AppendLine("\"labelPlacement\": \"esriServerPolygonPlacementAlwaysHorizontal\",");
            //     Use a green bold text symbol
            addressLabelsBuilder.AppendLine("\"symbol\": {");
            addressLabelsBuilder.AppendLine("\"color\": [255,0,0,255],");
            addressLabelsBuilder.AppendLine("\"font\": {\"size\": 18, \"weight\": \"bold\"},");
            addressLabelsBuilder.AppendLine("\"type\": \"esriTS\"}");
            addressLabelsBuilder.AppendLine("}");


            // Get the label definition string
            var addressLabelsJson = addressLabelsBuilder.ToString();


            // Create a new LabelDefintion object using the static FromJson method
            LabelDefinition labelDef = LabelDefinition.FromJson(addressLabelsJson);


            // Clear the current collection of label definitions (if any)
            _featureLayer.LabelDefinitions.Clear();


            // Add this label definition to the collection
            _featureLayer.LabelDefinitions.Add(labelDef);


            // Make sure labeling is enabled for the layer
            _featureLayer.LabelsEnabled = true;
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
