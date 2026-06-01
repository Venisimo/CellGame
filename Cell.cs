using System;
using System.Collections.Generic;
using System.Linq;


namespace TMP_RGZ
{
    public class Cell
    {
        public List<Edge> edges { get; set; }
        public bool IsClosed { get; set; } = false;
        public double MinX { get; private set; }
        public double MaxX { get; private set; }
        public double MinY { get; private set; }
        public double MaxY { get; private set; }

        public Cell(List<Edge> edges)
        {
            this.edges = edges;
            IsClosed = false;
        }
    }
}