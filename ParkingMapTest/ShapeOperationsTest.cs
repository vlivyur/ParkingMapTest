namespace ParkingMaprTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NetTopologySuite.Triangulate;
    using ParkingMap;
    using ThinkGeo.Core;

    [TestClass]
    public class ShapeOperationsTest
    {
        [TestMethod]
        public void InclineOfTheLine_Right()
        {
            Vertex from = new(0, 0);
            Vertex to = new(10, 0);
            double expected = 0;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_UpRight()
        {
            Vertex from = new(0, 0);
            Vertex to = new(10, 10);
            double expected = 45;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_Up()
        {
            Vertex from = new(0, 0);
            Vertex to = new(0, 10);
            double expected = 90;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_UpLeft()
        {
            Vertex from = new(0, 0);
            Vertex to = new(-10, 10);
            double expected = 135;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_Left()
        {
            Vertex from = new(0, 0);
            Vertex to = new(-10, 0);
            double expected = 180;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_DownLeft()
        {
            Vertex from = new(0, 0);
            Vertex to = new(-10, -10);
            double expected = 225;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_Down()
        {
            Vertex from = new(0, 0);
            Vertex to = new(0, -10);
            double expected = 270;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }

        [TestMethod]
        public void InclineOfTheLine_DownRight()
        {
            Vertex from = new(0, 0);
            Vertex to = new(10, -10);
            double expected = 315;
            Assert.AreEqual(expected, ShapeOperations.InclineOfTheLine(from, to));
        }
    }
}
