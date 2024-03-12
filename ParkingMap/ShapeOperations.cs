namespace ParkingMap
{
    using System.Collections.ObjectModel;
    using System.Windows.Shapes;
    using ThinkGeo.Core;

    public static class ShapeOperations
    {
        public static LineShape LongestSideOfShape(PolygonShape shape)
        {
            Vertex v = shape.OuterRing.Vertices[0];
            LineShape longestline = null;
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
            int pointindex = 0;
            if (dist > nearestto.GetDistanceTo(new PointShape(line.Vertices[1]), GeographyUnit.Meter, DistanceUnit.Meter))
                pointindex = 1;
            return line.Vertices[pointindex];
        }

        public static LineShape ProjectionLineOnLineGet(LineShape line1, LineShape line2)
        {
            PointShape res1, res2;
            PointShape p1 = new(line1.Vertices[0]);
            double angleL1 = ShapeOperations.InclineOfTheLine(line1.Vertices[0], line1.Vertices[1]);
            Vertex v2 = ShapeOperations.NearestPointOfLine(p1, line2);
            double angleP1L2 = ShapeOperations.InclineOfTheLine(line1.Vertices[0], v2);
            double angle = (angleP1L2 - angleL1 + 360) % 360;
            if (angle <= 90 && angle >= -90)
            {
                // perpendicular angle from line2 to line1
                angle = ((angle >= 0 ? angleL1 + 180 : angleL1 - 180) + 360) % 360;
                p1 = new(v2);
                p1.TranslateByDegree(1000, angle);
                LineShape line = ShapeFactory.CreateLine(v2, p1);
                MultipointShape crossing = line.GetCrossing(line1);
                res1 = crossing.Points[0];
            }
            else
            {
                // perpendicular angle from line1 to line2
                angle = ((angle >= 180 ? angleL1 + 180 : angleL1 - 180) + 360) % 360;
                p1.TranslateByDegree(1000, angle);
                LineShape line = ShapeFactory.CreateLine(line1.Vertices[0], p1);
                MultipointShape crossing = line.GetCrossing(line2);
                res1 = crossing.Points[0];
            }
            // second point
            p1 = new(line1.Vertices[1]);
            angleL1 = (angleL1 + 180) % 360;
            v2 = ShapeOperations.NearestPointOfLine(p1, line2);
            angleP1L2 = ShapeOperations.InclineOfTheLine(line1.Vertices[1], v2);
            angle = (angleL1 - angleP1L2 + 360) % 360;
            if (angle <= 90 && angle >= -90)
            {
                // perpendicular angle from line2 to line1
                angle = ((angle >= 0 ? angleL1 + 360 : angleL1 - 360) + 360) % 360;
                p1 = new(v2);
                p1.TranslateByDegree(1000, angle);
                LineShape line = ShapeFactory.CreateLine(v2, p1);
                MultipointShape crossing = line.GetCrossing(line1);
                res2 = crossing.Points[0];
            }
            else
            {
                // perpendicular angle from line1 to line2
                angle = ((angle >= 180 ? angleL1 + 180 : angleL1 - 180) + 360) % 360;
                p1.TranslateByDegree(1000, angle);
                LineShape line = ShapeFactory.CreateLine(line1.Vertices[0], p1);
                MultipointShape crossing = line.GetCrossing(line2);
                res2 = crossing.Points[0];
            }


            return ShapeFactory.CreateLine(res1, res2);
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