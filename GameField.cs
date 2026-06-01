using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TMP_RGZ
{
    public class GameField
    {
        public int Width { get; set; } = 750;
        public int Height { get; set; } = 700;
        public Brush Background { get; set; } = Brushes.LightGray;
        public Brush BorderBrush { get; set; } = Brushes.Black;
    }
}
