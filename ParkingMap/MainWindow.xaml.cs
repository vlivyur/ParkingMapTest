namespace ParkingMap
{
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using ThinkGeo.Core;
    using ThinkGeo.UI.Wpf;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        InMemoryFeatureLayer parkinglotslayer = new();
        OpenStreetMapOverlay openStreetMapOverlay = new();

        private void mapView_Loaded(object sender, RoutedEventArgs e)
        {
            this.mapView.MapUnit = GeographyUnit.Meter;
            this.mapView.CurrentExtent = new RectangleShape(-132661, 6985947, -128915, 6983984);

            LayerOverlay backLO = new();
            backLO.Layers.Add(new BackgroundLayer(new GeoSolidBrush(GeoColor.FromHtml("#09eee8"))));
            this.mapView.Overlays.Add(backLO);

            this.mapView.Overlays.Add(openStreetMapOverlay);

            this.parkinglotslayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = new PointStyle(PointSymbolType.Circle, 1, GeoBrushes.Blue);
            this.parkinglotslayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = new LineStyle(GeoPens.PaleRed);
            this.parkinglotslayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            LayerOverlay parkinglotsoverlay = new();
            parkinglotsoverlay.Layers.Add(LayerTypes.ParkingLotsLayer, this.parkinglotslayer);
            this.mapView.Overlays.Add(LayerTypes.ParkingLotsOverlay, parkinglotsoverlay);

            // TODO: overlay with selected lots

            this.mapView.TrackOverlay.TrackEnded += this.TrackOverlay_OnPolygonDrawn;
        }

        private async void TrackOverlay_OnPolygonDrawn(object? sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            AreaBaseShape shape = (AreaBaseShape)e.TrackShape;
            double square = shape.GetArea(GeographyUnit.Meter, AreaUnit.SquareMeters);
            this.txtSquare.Text = square.ToString();
            this.DrawLots(shape);
        }

        private async void DrawLots(AreaBaseShape where)
        {
            RectangleShape box = where.GetBoundingBox();

            double width;
            if (!double.TryParse(this.txtWidth.Text, out width))
                return;
            double length;
            if (!double.TryParse(this.txtLength.Text, out length))
                return;
            if (!this.parkinglotslayer.FeatureSource.IsOpen)
                this.parkinglotslayer.FeatureSource.Open();
            this.parkinglotslayer.Clear();
            this.parkinglotslayer.FeatureSource.BeginTransaction();
            for (double x = box.MinX; x < box.MaxX; x += width)
            {
                Vertex p1 = new(x, box.MinY);
                Vertex p2 = new(x, box.MaxY);
                LineShape line = new LineShape(new Collection<Vertex> { p1, p2});
                this.parkinglotslayer.FeatureSource.AddFeature(line);
            }
            this.parkinglotslayer.FeatureSource.CommitTransaction();
            this.parkinglotslayer.FeatureSource.Close();
            await this.mapView.RefreshAsync();
            return;
        }

        private void mapView_MapClick(object sender, MapClickMapViewEventArgs e)
        {
            //this.txtX.Text = e.WorldX.ToString();
            //this.txtY.Text = e.WorldY.ToString();
        }

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void btnClear_Click(object sender, RoutedEventArgs e)
        {
            this.mapView.TrackOverlay.TrackMode = TrackMode.None;
            this.lblDrawAction.Content = String.Empty;
            mapView.TrackOverlay.TrackShapeLayer.InternalFeatures.Clear();
            await mapView.TrackOverlay.RefreshAsync();
        }

        private void btnShowMap_Click(object sender, RoutedEventArgs e)
        {
            this.openStreetMapOverlay.IsVisible = !this.openStreetMapOverlay.IsVisible;
            this.btnShowMap.Content = this.openStreetMapOverlay.IsVisible ? "Hide map" : "Show map";
        }

        private void btnDraw_Checked(object sender, RoutedEventArgs e)
        {
            this.mapView.TrackOverlay.TrackMode = TrackMode.Polygon;
            this.lblDrawAction.Content = "Draw parking space...";
        }

        private void btnDraw_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mapView.TrackOverlay.TrackMode = TrackMode.None;
            this.lblDrawAction.Content = String.Empty;
        }
    }
}