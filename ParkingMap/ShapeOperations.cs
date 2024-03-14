namespace ParkingMap
{
    using System.Collections.ObjectModel;
    using ThinkGeo.Core;

    public static class ShapeOperations
    {
        public static LineShape LongestSideOfShape(PolygonShape shape)
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

        public static double InclineOfLine(LineShape line)
        {
            return ShapeOperations.InclineOfTheLine(line.Vertices[0], line.Vertices[1]);
        }

        public static double InclineOfTheLine(Vertex from, Vertex to)
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

        public static Vertex NearestPointOfLine(PointShape nearestto, LineShape line)
        {
            double dist = nearestto.GetDistanceTo(new PointShape(line.Vertices[0]), GeographyUnit.Meter, DistanceUnit.Meter);
            if (dist > nearestto.GetDistanceTo(new PointShape(line.Vertices[1]), GeographyUnit.Meter, DistanceUnit.Meter))
                return line.Vertices[1];
            return line.Vertices[0];
        }

        public static PointShape? ProjectionPointOnLine(Vertex point, LineShape crossingline, double angle, bool returncrossing = false)
        {
            PointShape p1 = new(point);
            angle = (360 - angle) % 360;
            p1.TranslateByDegree(1000, angle);
            PointShape p2 = new(point);
            p2.TranslateByDegree(1000, (angle + 180) % 360);
            LineShape line = ShapeFactory.CreateLine(p1, p2);
            MultipointShape crossing = line.GetCrossing(crossingline);
            if (crossing.Points.Count > 0)
                return returncrossing? crossing.Points[0] : new PointShape(point);
            return null;
        }

        public static LineShape? ProjectionLineOnLine(LineShape line1, LineShape line2)
        {
            double angleL1 = ShapeOperations.InclineOfLine(line1);
            double angleL2 = ShapeOperations.InclineOfLine(line2);
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

        public static PolygonShape ParkingLotDraw(PointShape point, double angle, double length, double width)
        {
            RingShape shape;
            List<Vertex> points = new() { new Vertex(point) };
            point = (PointShape)point.CloneDeep();
            point.TranslateByDegree(length, angle);
            points.Add(new Vertex(point));
            point = (PointShape)point.CloneDeep();
            point.TranslateByDegree(width, (angle + 90) % 360);
            points.Add(new Vertex(point));
            point = new(points[0]);
            point.TranslateByDegree(width, (angle + 90) % 360);
            points.Add(new Vertex(point));
            points.Add(points[0]);
            shape = new(points);
            return new PolygonShape(shape);
        }

    }
}