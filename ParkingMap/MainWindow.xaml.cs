namespace ParkingMap
{
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Windows;
    using Microsoft.Win32;
    using ThinkGeo.Core;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool DrawSupportingLines { get; set; } = false;

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

            this.mapView.TrackOverlay.TrackEnded += this.TrackOverlay_OnPolygonDrawn;
        }

        private async void TrackOverlay_OnPolygonDrawn(object? sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            PolygonShape shape = (PolygonShape)e.TrackShape;
            double square = shape.GetArea(GeographyUnit.Meter, AreaUnit.SquareMeters);
            this.txtArea.Text = square.ToString();
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
            if (this.mapView.TrackOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
                this.mapView.TrackOverlay.TrackShapeLayer.InternalFeatures.Clear();
            else if (this.parkinglotslayer.InternalFeatures.Count > 0)
                this.parkinglotslayer.InternalFeatures.Clear();
            else
                return;
            await mapView.RefreshAsync();
        }

        private void btnShowMap_Click(object sender, RoutedEventArgs e)
        {
            this.openStreetMapOverlay.IsVisible = !this.openStreetMapOverlay.IsVisible;
            this.btnShowMap.Content = this.openStreetMapOverlay.IsVisible ? "Hide map" : "Show map";
        }

        private void btnDraw_Checked(object sender, RoutedEventArgs e)
        {
            this.mapView.TrackOverlay.TrackMode = TrackMode.Polygon;
            this.lblDrawAction.Content = "Draw parking space, last point with double click";
        }

        private void btnDraw_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mapView.TrackOverlay.TrackMode = TrackMode.None;
            this.lblDrawAction.Content = String.Empty;
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            Collection<Feature> polygons = this.mapView.TrackOverlay.TrackShapeLayer.FeatureSource.GetAllFeatures(ReturningColumnsType.NoColumns);
            this.ProcessDrawnAreas(polygons);
        }

        private async void ProcessDrawnAreas(Collection<Feature> polygons)
        {
            //if (!this.secondarylayer.FeatureSource.IsOpen)
            //    this.secondarylayer.FeatureSource.Open();
            //this.secondarylayer.Clear();
            //this.secondarylayer.FeatureSource.BeginTransaction();
            if (!this.parkinglotslayer.FeatureSource.IsOpen)
                this.parkinglotslayer.FeatureSource.Open();
            if (!this.parkinglotslayer.FeatureSource.IsInTransaction)
                this.parkinglotslayer.Clear();
            this.parkinglotslayer.FeatureSource.BeginTransaction();

            List<BaseShape> result = new();
            foreach (Feature area in polygons)
            {
                result = this.BuildLots((PolygonShape)area.GetShape());
                foreach (BaseShape resshape in result)
                    this.parkinglotslayer.FeatureSource.AddFeature(resshape);
            }

            this.parkinglotslayer.FeatureSource.CommitTransaction();
            this.parkinglotslayer.FeatureSource.Close();
            //this.secondarylayer.FeatureSource.CommitTransaction();
            //this.secondarylayer.FeatureSource.Close();
            await this.mapView.RefreshAsync();
        }

        private List<BaseShape> BuildLots(PolygonShape where)
        {
            List<BaseShape> result = new();
            double width;
            if (!double.TryParse(this.txtWidth.Text, out width))
                return result;
            double length;
            if (!double.TryParse(this.txtLength.Text, out length))
                return result;

            // draw lots along the longest side
            LineShape longestLine = ShapeOperations.LongestSideOfShape(where);
            longestLine.ScaleUp(100);
            double angle = ShapeOperations.InclineOfTheLine(longestLine.Vertices[0], longestLine.Vertices[1]);
            angle = (360 - angle) % 360;
            if (this.DrawSupportingLines)
                this.parkinglotslayer.FeatureSource.AddFeature(longestLine);
            // in which side of longest line our figure is
            LineShape nextline = (LineShape)longestLine.CloneDeep();
            nextline.TranslateByDegree(length, angle);
            if (this.DrawSupportingLines)
                this.parkinglotslayer.FeatureSource.AddFeature(nextline);
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
                    if (this.DrawSupportingLines)
                        this.parkinglotslayer.FeatureSource.AddFeature(intersection.Lines[0]);
                    nextline.TranslateByDegree(length, angle);
                    intersection = nextline.GetIntersection(where);
                }
            }
            // TODO: draw parking lots
            int countOfLots = 0;
            if (lines.Count > 0)
            {
                LineShape line1 = lines[0];
                for (int i = 1; i < lines.Count; i++)
                {
                    LineShape line2 = lines[i];
                    LineShape? shortline = ShapeOperations.ProjectionLineOnLine(line1, line2);
                    if (shortline != null)
                    {
                        //angle = ShapeOperations.InclineOfTheLine(shortline.Vertices[0], shortline.Vertices[1]);
                        //angle = 360 - angle;
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
            }
            this.txtQtyLots.Text = countOfLots.ToString();
            return result;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Collection<Feature> shapes = this.mapView.TrackOverlay.TrackShapeLayer.FeatureSource.GetAllFeatures(ReturningColumnsType.NoColumns);
            if (shapes.Count > 0)
            {
                SaveFileDialog dlg = new();
                dlg.DefaultExt = ".geojson";
                dlg.Filter = "GeoJSON (.geojson)|*.geojson";
                bool result = dlg.ShowDialog() ?? false;

                if (result)
                {
                    string filename = dlg.FileName;
                    string str = shapes.GetGeoJson();
                    using (StreamWriter sw = new(filename))
                    {
                        await sw.WriteAsync(str);
                    }
                }
            }
        }

        private async void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            bool result = true;
            if (this.mapView.TrackOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                result = MessageBox.Show("Do you want to add more drawings to the existing ones?\nIf no you can clear everything up with Clear button", "There are some drawings on the map...", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes;
            }
            if (result)
            {
                OpenFileDialog dlg = new();
                dlg.DefaultExt = ".geojson";
                dlg.Filter = "GeoJSON (.geojson)|*.geojson";
                result = dlg.ShowDialog() ?? false;
                if (result)
                {
                    string filename = dlg.FileName;
                    string str = String.Empty;
                    using (StreamReader sr = new(filename))
                    {
                        str = await sr.ReadToEndAsync();
                    }
                    if (!String.IsNullOrWhiteSpace(str))
                    {
                        bool closeIt = false, commitTransaction = false;
                        if (!this.mapView.TrackOverlay.TrackShapeLayer.IsOpen)
                        {
                            this.mapView.TrackOverlay.TrackShapeLayer.Open();
                            closeIt = true;
                        }
                        if (!this.mapView.TrackOverlay.TrackShapeLayer.FeatureSource.IsInTransaction)
                        {
                            this.mapView.TrackOverlay.TrackShapeLayer.FeatureSource.BeginTransaction();
                            commitTransaction = true;
                        }
                        foreach (Feature feature in Feature.CreateFeaturesFromGeoJson(str))
                        {
                            this.mapView.TrackOverlay.TrackShapeLayer.FeatureSource.AddFeature(feature);
                        }
                        if (commitTransaction)
                            this.mapView.TrackOverlay.TrackShapeLayer.FeatureSource.CommitTransaction();
                        if (closeIt)
                            this.mapView.TrackOverlay.TrackShapeLayer.Close();
                        await this.mapView.RefreshAsync();
                    }
                }
            }
        }
    }
}