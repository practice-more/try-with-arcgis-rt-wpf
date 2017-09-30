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
using Esri.ArcGISRuntime.Symbology;

namespace EsriMap
{
    //https://developers.arcgis.com/net/latest/wpf/sample-code/sketchonmap.htm
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

        // Graphics overlay to host sketch graphics
        private GraphicsOverlay _sketchOverlay;

        // Map initialization logic is contained in MapViewModel.cs
        // Create and hold reference to the feature layer
        private async void Initialize()
        {
            // Create a light gray canvas map
            Map myMap = new Map(Basemap.CreateLightGrayCanvas());

            // Create graphics overlay to display sketch geometry
            _sketchOverlay = new GraphicsOverlay();
            MyMapView.GraphicsOverlays.Add(_sketchOverlay);
            AddLabelInfo();
            // Assign the map to the MapView
            MyMapView.Map = myMap;

            // Fill the combo box with choices for the sketch modes (shapes)
            SketchModeComboBox.ItemsSource = System.Enum.GetValues(typeof(SketchCreationMode));
            SketchModeComboBox.SelectedIndex = 0;

            // Set the sketch editor configuration to allow vertex editing, resizing, and moving
            var config = MyMapView.SketchEditor.EditConfiguration;
            config.AllowVertexEditing = true;
            config.ResizeMode = SketchResizeMode.Uniform;
            config.AllowMove = true;

            // Set the sketch editor as the page's data context
            this.DataContext = MyMapView.SketchEditor;
        }

        #region Graphic and symbol helpers
        private Graphic CreateGraphic(Esri.ArcGISRuntime.Geometry.Geometry geometry, IEnumerable<KeyValuePair<string, object>> attributes = null)
        {
            // Create a graphic to display the specified geometry
            Symbol symbol = null;
            switch (geometry.GeometryType)
            {
                // Symbolize with a fill symbol
                case GeometryType.Envelope:
                case GeometryType.Polygon:
                    {
                        symbol = new SimpleFillSymbol()
                        {
                            Color = Colors.Red,
                            Style = SimpleFillSymbolStyle.Solid,
                        };
                        break;
                    }
                // Symbolize with a line symbol
                case GeometryType.Polyline:
                    {
                        symbol = new SimpleLineSymbol()
                        {
                            Color = Colors.Red,
                            Style = SimpleLineSymbolStyle.Solid,
                            Width = 5d
                        };
                        break;
                    }
                // Symbolize with a marker symbol
                case GeometryType.Point:
                case GeometryType.Multipoint:
                    {

                        symbol = new SimpleMarkerSymbol()
                        {
                            Color = Colors.Red,
                            Style = SimpleMarkerSymbolStyle.Circle,
                            Size = 15d
                        };
                        break;
                    }
            }

            // pass back a new graphic with the appropriate symbol
            return new Graphic(geometry, attributes, symbol);
        }

        private void AddLabelInfo()
        {
            //{
            //  "labelExpressionInfo": 
            //  {
            //      "expression": "return $feature.address;"
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
            //https://resources.arcgis.com/en/help/rest/apiref/index.html?label.html
            // Create a StringBuilder to create the label definition JSON string
            StringBuilder addressLabelsBuilder = new StringBuilder();
            addressLabelsBuilder.AppendLine("{");
            //     Define a labeling expression that will show the address attribute value
            addressLabelsBuilder.AppendLine("\"labelExpressionInfo\": {");
            addressLabelsBuilder.AppendLine("\"expression\": \"return $feature.type;\"},");
            //     Align labels horizontally
            addressLabelsBuilder.AppendLine("\"labelPlacement\": \"esriServerLinePlacementBelowAlong\",");
            //     Use a green bold text symbol
            addressLabelsBuilder.AppendLine("\"symbol\": {");
            addressLabelsBuilder.AppendLine("\"color\": [0,0,255,255],");
            addressLabelsBuilder.AppendLine("\"font\": {\"size\": 18, \"weight\": \"bold\"},");
            addressLabelsBuilder.AppendLine("\"type\": \"esriTS\"}");
            addressLabelsBuilder.AppendLine("}");


            // Get the label definition string
            var addressLabelsJson = addressLabelsBuilder.ToString();


            // Create a new LabelDefintion object using the static FromJson method
            LabelDefinition labelDef = LabelDefinition.FromJson(addressLabelsJson);


            // Clear the current collection of label definitions (if any)
            _sketchOverlay.LabelDefinitions.Clear();


            // Add this label definition to the collection
            _sketchOverlay.LabelDefinitions.Add(labelDef);


            // Make sure labeling is enabled for the layer
            _sketchOverlay.LabelsEnabled = true;
        }

        private async Task<Graphic> GetGraphicAsync()
        {
            // Wait for the user to click a location on the map
            var mapPoint = (MapPoint)await MyMapView.SketchEditor.StartAsync(SketchCreationMode.Point, false);

            // Convert the map point to a screen point
            var screenCoordinate = MyMapView.LocationToScreen(mapPoint);

            // Identify graphics in the graphics overlay using the point
            var results = await MyMapView.IdentifyGraphicsOverlaysAsync(screenCoordinate, 2, false);

            // If results were found, get the first graphic
            Graphic graphic = null;
            IdentifyGraphicsOverlayResult idResult = results.FirstOrDefault();
            if (idResult != null && idResult.Graphics.Count > 0)
            {
                graphic = idResult.Graphics.FirstOrDefault();
            }

            // Return the graphic (or null if none were found)
            return graphic;
        }
        #endregion

        private async void DrawButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Let the user draw on the map view using the chosen sketch mode
                SketchCreationMode creationMode = (SketchCreationMode)SketchModeComboBox.SelectedItem;
                Esri.ArcGISRuntime.Geometry.Geometry geometry = await MyMapView.SketchEditor.StartAsync(creationMode, true);

                // set attributes for geometry
                KeyValuePair<string, object> pair = new KeyValuePair<string, object>("type", creationMode.ToString());
                IEnumerable<KeyValuePair<string, object>> attributes = new KeyValuePair<string, object>[] { pair };

                // Create and add a graphic from the geometry the user drew
                Graphic graphic = CreateGraphic(geometry, attributes);



                _sketchOverlay.Graphics.Add(graphic);

                // Enable/disable the clear and edit buttons according to whether or not graphics exist in the overlay
                ClearButton.IsEnabled = _sketchOverlay.Graphics.Count > 0;
                EditButton.IsEnabled = _sketchOverlay.Graphics.Count > 0;
            }
            catch (TaskCanceledException)
            {
                // Ignore ... let the user cancel drawing
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            // Remove all graphics from the graphics overlay
            _sketchOverlay.Graphics.Clear();

            // Disable buttons that require graphics
            ClearButton.IsEnabled = false;
            EditButton.IsEnabled = false;
        }

        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Allow the user to select a graphic
                Graphic editGraphic = await GetGraphicAsync();
                if (editGraphic == null) { return; }

                // Let the user make changes to the graphic's geometry, await the result (updated geometry)
                Esri.ArcGISRuntime.Geometry.Geometry newGeometry = await MyMapView.SketchEditor.StartAsync(editGraphic.Geometry);

                // Display the updated geometry in the graphic
                editGraphic.Geometry = newGeometry;
            }
            catch (TaskCanceledException)
            {
                // Ignore ... let the user cancel editing
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error editing shape: " + ex.Message);
            }
        }
    }
}
