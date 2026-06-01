using System;

namespace TMP_RGZ
{
    public class Edge
    {
        public Vertex Start { get; set; }
        public Vertex End { get; set; }
        public bool IsHorizontal { get; private set; }
        public bool IsVertical { get; private set; }

        public event Action<Edge> OnDeleted;

        public Action<Vertex> OnVertexDeletedHandler { get; private set; }
        public Action<Vertex, Vector> OnVertexMovedHandler { get; private set; }

        public bool IsCollidable { get; set; } = true;
        public bool IsRemovableOnTouch { get; set; } = true;

        public Edge(Vertex start, Vertex end)
        {
            Start = start;
            End = end;

            OnVertexDeletedHandler = OnVertexDeleted;
            OnVertexMovedHandler = OnVertexMoved;

            UpdateOrientation();

            Start.OnDeleted += OnVertexDeletedHandler;
            End.OnDeleted += OnVertexDeletedHandler;
            Start.OnPositionChanged += OnVertexMovedHandler;
            End.OnPositionChanged += OnVertexMovedHandler;

            Logger.LogEdgeCreation(this);
        }

        private void UpdateOrientation()
        {
            double yDiff = Math.Abs(Start.V_Position.Y - End.V_Position.Y);
            double xDiff = Math.Abs(Start.V_Position.X - End.V_Position.X);

            IsHorizontal = yDiff <= xDiff && yDiff < 5.0;
            IsVertical = xDiff < yDiff && xDiff < 5.0;

            if (!IsHorizontal && !IsVertical)
            {
                // Диагональное ребро - не горизонтальное и не вертикальное
                IsHorizontal = false;
                IsVertical = false;
            }
        }

        private void OnVertexDeleted(Vertex deletedVertex)
        {
            Logger.Log($"Ребро получило уведомление об удалении вершины ({deletedVertex.V_Position.X:F0},{deletedVertex.V_Position.Y:F0})", "OBSERVER");

            if (Start != null)
            {
                Start.OnDeleted -= OnVertexDeletedHandler;
                Start.OnPositionChanged -= OnVertexMovedHandler;
            }
            if (End != null)
            {
                End.OnDeleted -= OnVertexDeletedHandler;
                End.OnPositionChanged -= OnVertexMovedHandler;
            }

            OnDeleted?.Invoke(this);
            Logger.LogEdgeDeletion(this, "удаление инцидентной вершины");
        }

        private void OnVertexMoved(Vertex movedVertex, Vector newPosition)
        {
            UpdateOrientation();
        }

        public void Delete()
        {
            Logger.Log($"Вызван Edge.Delete() для ребра", "EDGE");

            if (Start != null)
            {
                Start.OnDeleted -= OnVertexDeletedHandler;
                Start.OnPositionChanged -= OnVertexMovedHandler;
            }
            if (End != null)
            {
                End.OnDeleted -= OnVertexDeletedHandler;
                End.OnPositionChanged -= OnVertexMovedHandler;
            }

            OnDeleted?.Invoke(this);
        }

        public double Length
        {
            get
            {
                if (Start == null || End == null) return 0;
                double dx = End.V_Position.X - Start.V_Position.X;
                double dy = End.V_Position.Y - Start.V_Position.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        // Нормаль к ребру (перпендикуляр)
        public Vector GetNormal()
        {
            Vector direction = new Vector(
                End.V_Position.X - Start.V_Position.X,
                End.V_Position.Y - Start.V_Position.Y
            ).Normalize();

            // Поворачиваем на 90 градусов (перпендикуляр)
            // Для границ поля нормаль должна быть направлена внутрь
            return new Vector(-direction.Y, direction.X).Normalize();
        }

        // Ближайшая точка на отрезке к заданной позиции
        public Vector GetClosestPoint(Vector position)
        {
            Vector a = Start.V_Position;
            Vector b = End.V_Position;

            Vector ab = b - a;
            Vector ap = position - a;

            double t = (ap.X * ab.X + ap.Y * ab.Y) / (ab.X * ab.X + ab.Y * ab.Y);
            t = Math.Max(0, Math.Min(1, t));

            return a + ab * t;
        }

        // Расстояние от точки до ребра
        public double DistanceToPoint(Vector point)
        {
            Vector closest = GetClosestPoint(point);
            double dx = point.X - closest.X;
            double dy = point.Y - closest.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}