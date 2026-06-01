using System;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TMP_RGZ
{
    public class Circle : IGameObject
    {
        public Vector V_Position { get; set; }
        public Vector V_Velocity { get; set; }
        public double Radius { get; set; } = 15;

        public Brush Brush { get; set; }        
        public Ellipse EllipseShape { get; set; }

        public Shape Shape => EllipseShape;

        // Коэффициент упругости при отскоке (1 = абсолютно упругий, 0 = без отскока)
        public double Bounciness { get; set; } = 1;

        public Circle(Vector v_position, Vector v_Velocity, double radius, Brush color)
        {
            V_Position = v_position;
            V_Velocity = v_Velocity;
            Radius = radius;
            Brush = color;

            EllipseShape = new Ellipse
            {
                Width = Radius * 2,
                Height = Radius * 2,
                Fill = Brush
            };

            System.Windows.Controls.Canvas.SetLeft(EllipseShape, V_Position.X - Radius);
            System.Windows.Controls.Canvas.SetTop(EllipseShape, V_Position.Y - Radius);
        }

        public void Update(double deltaTime)
        {
            V_Position += V_Velocity * deltaTime;

            if (EllipseShape != null)
            {
                System.Windows.Controls.Canvas.SetLeft(EllipseShape, V_Position.X - Radius);
                System.Windows.Controls.Canvas.SetTop(EllipseShape, V_Position.Y - Radius);
            }
        }

        public void ReflectX()
        {
            V_Velocity = new Vector(-V_Velocity.X * Bounciness, V_Velocity.Y);
        }

        public void ReflectY()
        {
            V_Velocity = new Vector(V_Velocity.X, -V_Velocity.Y * Bounciness);
        }

        // Отскок от ребра с учётом нормали
        public void ReflectAgainstEdge(Edge edge)
        {
            if (!edge.IsCollidable) return;

            Vector normal = edge.GetNormal();

            // Вычисляем отражённую скорость
            double dot = V_Velocity.X * normal.X + V_Velocity.Y * normal.Y;
            Vector reflectedVelocity = new Vector(
                V_Velocity.X - 2 * dot * normal.X * Bounciness,
                V_Velocity.Y - 2 * dot * normal.Y * Bounciness
            );

            V_Velocity = reflectedVelocity;
        }
    }
}