namespace ParkingMap
{
    using System.Collections.ObjectModel;
    using ThinkGeo.Core;

    public static class ShapeFactory
    {
        public static LineShape CreateLine(Vertex v1, Vertex v2)
        {
            return new LineShape(new Collection<Vertex> { v1, v2 });
        }

        public static LineShape CreateLine(PointShape p1, PointShape p2)
        {
            return new LineShape(new Collection<Vertex> { new Vertex(p1), new Vertex(p2) });
        }
        public static LineShape CreateLine(Vertex v1, PointShape p2)
        {
            return new LineShape(new Collection<Vertex> { v1, new Vertex(p2) });
        }
        public static LineShape CreateLine(PointShape p1, Vertex v2)
        {
            return new LineShape(new Collection<Vertex> { new Vertex(p1), v2 });
        }
    }
}
