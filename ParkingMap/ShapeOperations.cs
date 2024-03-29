﻿namespace ParkingMap
{
    using System.Collections.ObjectModel;
    using ThinkGeo.Core;

    public static class ShapeOperations
    {
        private static LineShape LongestSideOfShape(PolygonShape shape)
        {
            Vertex v = shape.OuterRing.Vertices[0];
            LineShape? longestline = null;
            double maxlength = 0;
            for (int i = 1; i < shape.OuterRing.Vertices.Count; i++)
            {
                Vertex e = shape.OuterRing.Vertices[i];
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

        private static double InclinationOfLine(LineShape line)
        {
            return ShapeOperations.InclinationOfLine(line.Vertices[0], line.Vertices[1]);
        }

        public static double InclinationOfLine(Vertex from, Vertex to)
        {
            double angle;

            if (from.X != to.X)
            {
                double tangentangle = (to.Y - from.Y) / (to.X - from.X);
                angle = Math.Atan(tangentangle) * 180 / Math.PI;
                if (to.X < from.X)
                    angle += 180;
                if (angle < 0)
                {
                    angle += 360;
                }
            }
            else
            {
                angle = (from.Y > to.Y) ? 270 : 90;
            }

            return angle % 360;
        }

        private static PointShape? ProjectionPointOnLine(Vertex point, LineShape crossingline, double angle, bool returncrossing = false)
        {
            PointShape p1 = new(point);
            angle = (360 - angle) % 360;
            p1.TranslateByDegree(1000, angle);
            PointShape p2 = new(point);
            p2.TranslateByDegree(1000, (angle + 180) % 360);
            LineShape line = ShapeFactory.CreateLine(p1, p2);
            MultipointShape crossing = line.GetCrossing(crossingline);
            if (crossing.Points.Count > 0)
                return returncrossing ? crossing.Points[0] : new PointShape(point);
            return null;
        }

        /// <summary>Get longest line between two parallel lines, that could be used for rectangle between these two lines</summary>
        /// <returns>Subline of line1</returns>
        public static LineShape? ProjectionLineOnLine(LineShape line1, LineShape line2)
        {
            double angleL1 = ShapeOperations.InclinationOfLine(line1);
            double angleL2 = ShapeOperations.InclinationOfLine(line2);
            PointShape? p1 = ShapeOperations.ProjectionPointOnLine(line1.Vertices[0], line2, angleL1);
            if (p1 is null)
                p1 = ShapeOperations.ProjectionPointOnLine(line2.Vertices[0], line1, angleL2, true);
            if (p1 is null)
                return null;

            PointShape? p2 = ShapeOperations.ProjectionPointOnLine(line1.Vertices[1], line2, angleL1);
            if (p2 is null)
                p2 = ShapeOperations.ProjectionPointOnLine(line2.Vertices[1], line1, angleL2, true);
            if (p2 is null)
                return null;
            return ShapeFactory.CreateLine(p1, p2);
        }

        private static PolygonShape ShapeOfParkingLot(PointShape point1, PointShape point2, double distance, double angle)
        {
            List<Vertex> points = new() { new Vertex(point1), new Vertex(point2) };
            points.Add(ShapeOperations.PointByAngleAndDistance(point2, distance, angle));
            points.Add(ShapeOperations.PointByAngleAndDistance(point1, distance, angle));
            points.Add(new Vertex(point1));
            RingShape shape = new(points);
            return new PolygonShape(shape);
        }

        private static Vertex PointByAngleAndDistance(PointShape point, double distance, double angle)
        {
            PointShape newpoint = (PointShape)point.CloneDeep();
            newpoint.TranslateByDegree(distance, angle);
            return new(newpoint);
        }

        public static ParkingLotsInfo GenerateLots(PolygonShape where, double width, double length, bool includeSupportingLines = false)
        {
            ParkingLotsInfo result = new();

            // draw lots along the longest side
            LineShape longestLine = ShapeOperations.LongestSideOfShape(where);
            longestLine.ScaleUp(100);
            if (includeSupportingLines)
                result.AuxiliaryLines.Add(longestLine);
            double angle = (360 - ShapeOperations.InclinationOfLine(longestLine.Vertices[0], longestLine.Vertices[1])) % 360;

            // in which side of longest line our figure is
            LineShape line2 = (LineShape)longestLine.CloneDeep();
            line2.TranslateByDegree(length, angle);
            MultilineShape intersection = line2.GetIntersection(where);
            if (includeSupportingLines)
                result.AuxiliaryLines.Add(line2);
            if (intersection.Lines.Count == 0)
            {
                angle = (angle + 180) % 360;
                line2 = (LineShape)longestLine.CloneDeep();
                line2.TranslateByDegree(length, angle);
                intersection = line2.GetIntersection(where);
                if (includeSupportingLines)
                    result.AuxiliaryLines.Add(line2);
            }
            // draw parallel lines of lots
            if (intersection.Lines.Count > 0) // shape isn't too narrow
            {
                LineShape line1 = ShapeOperations.LongestSideOfShape(where);
                intersection = line1.GetIntersection(where);
                line1.ScaleUp(100);
                int countOfLots = 0;
                double lotsArea = 0;
                int row = 1;
                while (intersection.Lines.Count > 0)
                {
                    line1 = intersection.Lines[0];
                    line2 = (LineShape)longestLine.CloneDeep();
                    line2.TranslateByDegree(row * length, angle);
                    intersection = line2.GetIntersection(where);
                    if (intersection.Lines.Count <= 0)
                        break;
                    line2 = intersection.Lines[0];
                    if (includeSupportingLines)
                        result.AuxiliaryLines.Add(line2);
                    // got two lines, draw lots in the middle of them
                    LineShape? shortline = ShapeOperations.ProjectionLineOnLine(line1, line2);
                    if (shortline != null)
                    {
                        double shortlinelength = shortline.GetLength(GeographyUnit.Meter, DistanceUnit.Meter);
                        PointShape point1 = new PointShape(shortline.Vertices[0]);
                        for (double l = width; l <= shortlinelength; l += width)
                        {
                            PointShape point2 = shortline.GetPointOnALine(StartingPoint.FirstPoint, l, GeographyUnit.Meter, DistanceUnit.Meter);
                            PolygonShape r = ShapeOperations.ShapeOfParkingLot(point1, point2, length, angle);
                            result.ParkingLots.Add(r);
                            point1 = point2;
                            countOfLots++;
                            lotsArea += r.GetArea(GeographyUnit.Meter, AreaUnit.SquareMeters);
                        }
                    }
                    row++;
                }
                result.QtyLots = countOfLots;
                result.ShapeArea = where.GetArea(GeographyUnit.Meter, AreaUnit.SquareMeters);
                result.LotsArea = lotsArea;
            }
            return result;
        }
    }
}