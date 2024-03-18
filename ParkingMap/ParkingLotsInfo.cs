namespace ParkingMap
{
    using System.Collections.Generic;
    using ThinkGeo.Core;

    /// <summary>Calculated parking lots</summary>
    public class ParkingLotsInfo
    {
        /// <summary>Quantity of parking lots</summary>
        public int QtyLots { get; set; }
        /// <summary>Source shape area</summary>
        public double ShapeArea { get; set; }
        /// <summary>Area under calculated parking lots</summary>
        public double LotsArea { get; set; }
        /// <summary>List of all suitable parking lots</summary>
        public List<BaseShape> ParkingLots { get; set; } = new();
        /// <summary>Auxillary shapes used in calculations</summary>
        public List<BaseShape> AuxiliaryLines { get; set; } = new();
    }
}
