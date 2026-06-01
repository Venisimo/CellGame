using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace TMP_RGZ
{
    public interface IGameObject
    {
        public Vector V_Position { get; set; }
        public Vector V_Velocity { get; set; }
        public Shape Shape { get; }
        public double Radius { get; }
        public void Update(double deltaTime);
        public void ReflectX();
        public void ReflectY();
    }
}
