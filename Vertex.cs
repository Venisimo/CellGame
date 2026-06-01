using System;

namespace TMP_RGZ
{
    public class Vertex
    {
        public Vector V_Position { get; set; }
        public double ToleranceRadius { get; set; }  // Радиус для ИНТЕРФЕЙСА (прилипание мыши)
        public bool IsClosed { get; set; } = false;
        public bool IsCollidable { get; set; } = true;

        // Новый параметр: радиус столкновения с шариками (маленький)
        public double CollisionRadius { get; set; } = 5;  // Всего 5 пикселей вокруг центра

        public event Action<Vertex> OnDeleted;
        public event Action<Vertex, Vector> OnPositionChanged;

        public Vertex(Vector position, double toleranceRadius, bool isClosed)
        {
            V_Position = position;
            ToleranceRadius = toleranceRadius;
            IsClosed = isClosed;
        }

        public void Delete()
        {
            OnDeleted?.Invoke(this);
        }

        public void MoveTo(Vector newPosition)
        {
            V_Position = newPosition;
            OnPositionChanged?.Invoke(this, newPosition);
        }
    }
}