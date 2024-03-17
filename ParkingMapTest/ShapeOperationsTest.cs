namespace ParkingMapTest
{
    using ParkingMap;
    using ThinkGeo.Core;

    [TestClass]
    public class ShapeOperationsTest
    {
        #region InclineOfLine
        [TestMethod]
        public void InclineOfLine_Right()
        {
            Vertex from = new(0, 0);
            Vertex to = new(10, 0);
            double expected = 0;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_UpRight()
        {
            Vertex from = new(0, 0);
            Vertex to = new(10, 10);
            double expected = 45;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_Up()
        {
            Vertex from = new(0, 0);
            Vertex to = new(0, 10);
            double expected = 90;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_UpLeft()
        {
            Vertex from = new(0, 0);
            Vertex to = new(-10, 10);
            double expected = 135;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_Left()
        {
            Vertex from = new(0, 0);
            Vertex to = new(-10, 0);
            double expected = 180;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_DownLeft()
        {
            Vertex from = new(0, 0);
            Vertex to = new(-10, -10);
            double expected = 225;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_Down()
        {
            Vertex from = new(0, 0);
            Vertex to = new(0, -10);
            double expected = 270;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }

        [TestMethod]
        public void InclineOfLine_DownRight()
        {
            Vertex from = new(0, 0);
            Vertex to = new(10, -10);
            double expected = 315;
            Assert.AreEqual(expected, ShapeOperations.InclinationOfLine(from, to));
        }
        #endregion

        #region ProjectionLineOnLine
        [TestMethod]
        public void ProjectionLineOnLine_VerticalRight()
        {
            LineShape line1 = ShapeFactory.CreateLine(new Vertex(-10, 10), new Vertex(-10, -10));
            LineShape line2 = ShapeFactory.CreateLine(new Vertex(10, 1), new Vertex(10, -1));
            LineShape expected = ShapeFactory.CreateLine(new Vertex(-10, 1), new Vertex(-10, -1));
            this.ProjectionLineOnLine_Check(expected, line1, line2);
        }

        [TestMethod]
        public void ProjectionLineOnLine_HorizontalUp()
        {
            LineShape line1 = ShapeFactory.CreateLine(new Vertex(-10, -10), new Vertex(10, -10));
            LineShape line2 = ShapeFactory.CreateLine(new Vertex(-1, 10), new Vertex(1, 10));
            LineShape expected = ShapeFactory.CreateLine(new Vertex(-1, -10), new Vertex(1, -10));
            this.ProjectionLineOnLine_Check(expected, line1, line2);
        }

        [TestMethod]
        public void ProjectionLineOnLine_VerticalLeft()
        {
            LineShape line1 = ShapeFactory.CreateLine(new Vertex(10, 10), new Vertex(10, -10));
            LineShape line2 = ShapeFactory.CreateLine(new Vertex(-10, 1), new Vertex(-10, -1));
            LineShape expected = ShapeFactory.CreateLine(new Vertex(10, 1), new Vertex(10, -1));
            this.ProjectionLineOnLine_Check(expected, line1, line2);
        }

        [TestMethod]
        public void ProjectionLineOnLine_HorizontalDown()
        {
            LineShape line1 = ShapeFactory.CreateLine(new Vertex(-10, 10), new Vertex(10, 10));
            LineShape line2 = ShapeFactory.CreateLine(new Vertex(-1, -10), new Vertex(1, -10));
            LineShape expected = ShapeFactory.CreateLine(new Vertex(-1, 10), new Vertex(1, 10));
            this.ProjectionLineOnLine_Check(expected, line1, line2);
        }

        private void ProjectionLineOnLine_Check(LineShape expected, LineShape line1, LineShape line2)
        {
            LineShape res = ShapeOperations.ProjectionLineOnLine(line1, line2);
            Assert.AreEqual(expected.Vertices[0].ToString(), res.Vertices[0].ToString());
            Assert.AreEqual(expected.Vertices[1].ToString(), res.Vertices[1].ToString());
        }
        #endregion
    }
}
