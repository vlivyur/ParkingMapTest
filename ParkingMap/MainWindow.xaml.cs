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
            PolygonShape shape = (PolygonShape)e.TrackShape;
            double square = shape.GetArea(GeographyUnit.Meter, AreaUnit.SquareMeters);
            this.txtSquare.Text = square.ToString();
            this.DrawLots(shape);
        }

        private async void DrawLots(PolygonShape where)
        {
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

            // draw lots along the longest side
            LineShape longestLine = MainWindow.LongestLineGet(where);
            longestLine.ScaleUp(100);
            double angle = this.AngleOfTheLineGet(longestLine.Vertices[0], longestLine.Vertices[1]);
            // in which side of longest line our figure is
            LineShape nextline = (LineShape)longestLine.CloneDeep();
            nextline.TranslateByDegree(length, angle);
            MultilineShape tmp = nextline.GetIntersection(where);
            if (tmp.Lines.Count == 0)
            {
                angle = (angle + 180) % 360;
            }
            // ready to draw parallel lines
            nextline = (LineShape)longestLine.CloneDeep();
            tmp = nextline.GetIntersection(where);
            if (tmp.Lines.Count > 0) // shape isn't too narrow
            {
                while (tmp.Lines.Count > 0)
                {
                    this.parkinglotslayer.FeatureSource.AddFeature(tmp.Lines[0]);
                    nextline.TranslateByDegree(length, angle);
                    tmp = nextline.GetIntersection(where);
                }
            }
            // TODO: draw perpendicular lines


            this.parkinglotslayer.FeatureSource.CommitTransaction();
            this.parkinglotslayer.FeatureSource.Close();
            await this.mapView.RefreshAsync();
            return;
        }

        private double AngleOfTheLineGet(Vertex from, Vertex to)
        {
            double angle;

            if (from.X != to.X)
            {
                double tangentangle = (from.Y - to.Y) / (to.X - from.X);
                angle = Math.Atan(tangentangle) * 180 / Math.PI;
                if (angle < 0)
                {
                    angle += 360;
                }
            }
            else
            {
                angle = (from.Y > to.Y) ? 90 : 270;
            }

            return angle % 360;
        }

        private void mapView_MapClick(object sender, MapClickMapViewEventArgs e)
        {
        }

        private async void btnClear_Click(object sender, RoutedEventArgs e)
        {
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

        private static LineShape LongestLineGet(PolygonShape where)
        {
            Vertex v = where.OuterRing.Vertices[0];
            LineShape longestline = null;
            double maxlength = 0;
            for (int i = 1; i < where.OuterRing.Vertices.Count; i++)
            {
                Vertex e = where.OuterRing.Vertices[i];
                LineShape line = new LineShape(new Collection<Vertex> { v, e });
                double tmp = line.GetLength(GeographyUnit.Meter, DistanceUnit.Meter);
                if (tmp > maxlength)
                {
                    maxlength = tmp;
                    longestline = line;
                }
                v = e;
            }
            return longestline;
        }
    }
}