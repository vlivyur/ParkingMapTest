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
        //InMemoryFeatureLayer secondarylayer = new();


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
            this.parkinglotslayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = new AreaStyle(GeoPens.Firebrick);
            this.parkinglotslayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            LayerOverlay parkinglotsoverlay = new();
            parkinglotsoverlay.Layers.Add(LayerNames.ParkingLotsLayer, this.parkinglotslayer);
            this.mapView.Overlays.Add(LayerNames.ParkingLotsOverlay, parkinglotsoverlay);

            //this.secondarylayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = new LineStyle(GeoPens.Red);
            //this.secondarylayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            //LayerOverlay secondaryoverlay = new();
            //secondaryoverlay.Layers.Add(LayerNames.SecondaryLayer, this.secondarylayer);
            //this.mapView.Overlays.Add(LayerNames.SecondaryOverlay, secondaryoverlay);

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
            this.BuildLots(where);
            return;
        }

        private void mapView_MapClick(object sender, MapClickMapViewEventArgs e)
        {
            this.txtX.Text = e.WorldX.ToString("#.0000");
            this.txtY.Text = e.WorldY.ToString("#.0000");
        }

        private async void btnClear_Click(object sender, RoutedEventArgs e)
        {
            this.mapView.TrackOverlay.TrackShapeLayer.InternalFeatures.Clear();
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

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            PolygonShape shape = (PolygonShape)this.mapView.TrackOverlay.GetTrackingShape();
            if (shape != null)
                this.BuildLots(shape);
        }

        private async void BuildLots(PolygonShape where)
        {
            double width;
            if (!double.TryParse(this.txtWidth.Text, out width))
                return;
            double length;
            if (!double.TryParse(this.txtLength.Text, out length))
                return;

            //if (!this.secondarylayer.FeatureSource.IsOpen)
            //    this.secondarylayer.FeatureSource.Open();
            //this.secondarylayer.Clear();
            //this.secondarylayer.FeatureSource.BeginTransaction();

            // draw lots along the longest side
            LineShape longestLine = ShapeOperations.LongestSideOfShape(where);
            longestLine.ScaleUp(100);
            double angle = ShapeOperations.InclineOfTheLine(longestLine.Vertices[0], longestLine.Vertices[1]);
            // in which side of longest line our figure is
            LineShape nextline = (LineShape)longestLine.CloneDeep();
            nextline.TranslateByDegree(length, angle);
            MultilineShape intersection = nextline.GetIntersection(where);
            if (intersection.Lines.Count == 0)
            {
                angle = (angle + 180) % 360;
            }
            // ready to draw parallel lines
            nextline = (LineShape)longestLine.CloneDeep();
            intersection = nextline.GetIntersection(where);
            List<LineShape> lines = new();
            if (intersection.Lines.Count > 0) // shape isn't too narrow
            {
                while (intersection.Lines.Count > 0)
                {
                    lines.Add(intersection.Lines[0]);
                    //this.secondarylayer.FeatureSource.AddFeature(intersection.Lines[0]);
                    nextline.TranslateByDegree(length, angle);
                    intersection = nextline.GetIntersection(where);
                }
            }
            // TODO: draw parking lots
            if (!this.parkinglotslayer.FeatureSource.IsOpen)
                this.parkinglotslayer.FeatureSource.Open();
            this.parkinglotslayer.Clear();
            this.parkinglotslayer.FeatureSource.BeginTransaction();
            int countOfLots = 0;
            if (lines.Count > 0)
            {
                LineShape line1 = lines[0];
                for (int i = 1; i < lines.Count; i++)
                {
                    LineShape line2 = lines[i];
                    LineShape shortline = ShapeOperations.ProjectionLineOnLineGet(line1, line2);
                    angle = ShapeOperations.InclineOfTheLine(shortline.Vertices[0], shortline.Vertices[1]);
                    //this.parkinglotslayer.FeatureSource.AddFeature(shortline);
                    double shortlinelength = shortline.GetLength(GeographyUnit.Meter, DistanceUnit.Meter) - width;
                    for (double l = 0; l < shortlinelength; l += width)
                    {
                        PointShape point = shortline.GetPointOnALine(StartingPoint.FirstPoint, l, GeographyUnit.Meter, DistanceUnit.Meter);
                        PolygonShape r = ShapeOperations.ParkingLotDraw(point, angle, length, width);
                        this.parkinglotslayer.FeatureSource.AddFeature(r);
                        countOfLots++;
                    }
                    line1 = line2;
                }
            }
            this.parkinglotslayer.FeatureSource.CommitTransaction();
            this.parkinglotslayer.FeatureSource.Close();
            //this.secondarylayer.FeatureSource.CommitTransaction();
            //this.secondarylayer.FeatureSource.Close();
            await this.mapView.RefreshAsync();
            this.txtQtyLots.Text = countOfLots.ToString();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            PolygonShape shape = (PolygonShape)this.mapView.TrackOverlay.GetTrackingShape();
            if (shape != null)
            {
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}