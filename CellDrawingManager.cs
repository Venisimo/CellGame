using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TMP_RGZ
{
    public class CellDrawingManager
    {
        // Публичные поля
        public List<Vertex> Vertices;
        public List<Edge> Edges;
        public DrawingVisualizer Visualizer;

        public const double DEFAULT_TOLERANCE_RADIUS = 15;
        public double MinEdgeLength => DEFAULT_TOLERANCE_RADIUS * 2;

        // Флаги для коллизий
        public bool EnableVertexCollisions { get; set; } = true;
        public bool EnableEdgeBounce { get; set; } = true;
        public bool EnableEdgeRemovalOnTouch { get; set; } = true;

        // Свойства
        public int VertexCount => Vertices.Count;
        public int EdgeCount => Edges.Count;
        public bool IsDrawingEdge => _isDrawingEdge;
        public bool IsDraggingVertex => _isDraggingVertex;

        // Состояние рисования ребра
        private Vertex _drawingStartVertex;
        private bool _isDrawingEdge;
        private bool _isHorizontal;
        private double _currentEdgeLength;

        // Состояние перетаскивания вершины
        private Vertex _draggingVertex;
        private bool _isDraggingVertex;
        private Point _dragStartPoint;
        private Vector _dragStartPosition;

        // Оптимизация движения мыши
        private Point _lastMousePosition;
        private double _lastDrawnLength = -1;

        public CellDrawingManager(DrawingVisualizer visualizer)
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
            Visualizer = visualizer;
        }

        // Проверка, будет ли ребро от вершины к новой позиции пересекать существующие рёбра
        private bool WouldEdgeFromVertexIntersectExistingEdges(Vertex vertex, Vector newPosition, Edge excludeEdge = null)
        {
            // Получаем все рёбра, связанные с этой вершиной
            var attachedEdges = Edges.Where(e => e.Start == vertex || e.End == vertex).ToList();

            if (attachedEdges.Count == 0) return false;

            // Для каждого прикреплённого ребра проверяем, не будет ли оно пересекать другие рёбра
            foreach (var edge in attachedEdges)
            {
                // Пропускаем ребро, которое мы исключаем
                if (excludeEdge != null && edge == excludeEdge)
                    continue;

                // Определяем другой конец ребра (не перемещаемую вершину)
                Vertex otherEnd = (edge.Start == vertex) ? edge.End : edge.Start;

                // Создаём временное ребро от другого конца к новой позиции
                Vertex tempStart = new Vertex(otherEnd.V_Position, DEFAULT_TOLERANCE_RADIUS, false);
                Vertex tempEnd = new Vertex(newPosition, DEFAULT_TOLERANCE_RADIUS, false);

                foreach (var existingEdge in Edges)
                {
                    // Пропускаем рёбра, которые используют перемещаемую вершину
                    if (existingEdge.Start == vertex || existingEdge.End == vertex)
                        continue;

                    // Пропускаем само ребро, которое мы проверяем
                    if (existingEdge == edge)
                        continue;

                    // Проверяем пересечение
                    if (DoEdgesIntersectTest(tempStart, tempEnd, existingEdge.Start, existingEdge.End))
                    {
                        Logger.Log($"Перетаскивание создаст пересечение ребра ({otherEnd.V_Position.X:F0},{otherEnd.V_Position.Y:F0})->({newPosition.X:F0},{newPosition.Y:F0}) с ребром ({existingEdge.Start.V_Position.X:F0},{existingEdge.Start.V_Position.Y:F0})->({existingEdge.End.V_Position.X:F0},{existingEdge.End.V_Position.Y:F0})", "DRAG");
                        return true;
                    }
                }
            }

            return false;
        }
        // Проверка, не слишком ли короткое ребро после перемещения
        private bool WouldEdgeBecomeTooShort(Vertex vertex, Vector newPosition)
        {
            var attachedEdges = Edges.Where(e => e.Start == vertex || e.End == vertex).ToList();

            foreach (var edge in attachedEdges)
            {
                Vertex otherEnd = (edge.Start == vertex) ? edge.End : edge.Start;
                double newLength = Math.Sqrt(
                    Math.Pow(newPosition.X - otherEnd.V_Position.X, 2) +
                    Math.Pow(newPosition.Y - otherEnd.V_Position.Y, 2));

                if (newLength < MinEdgeLength)
                {
                    Logger.Log($"Ребро станет слишком коротким ({newLength:F1} < {MinEdgeLength})", "DRAG");
                    return true;
                }
            }

            return false;
        }


        // Проверка, не заденет ли вершина в новой позиции существующие рёбра (с учётом радиуса)
        private bool WouldVertexTouchAnyEdgeInNewPosition(Vertex vertex, Vector newPosition, double toleranceRadius)
        {
            foreach (var edge in Edges)
            {
                // Пропускаем рёбра, которые содержат эту вершину
                if (edge.Start == vertex || edge.End == vertex)
                    continue;

                if (IsVertexTooCloseToEdge(newPosition, toleranceRadius, edge))
                {
                    Logger.Log($"Перетаскивание: вершина слишком близко к ребру ({edge.Start.V_Position.X:F0},{edge.Start.V_Position.Y:F0})->({edge.End.V_Position.X:F0},{edge.End.V_Position.Y:F0})", "DRAG");
                    return true;
                }
            }
            return false;
        }

        // Проверка, не будет ли ребро при перетаскивании задевать другие рёбра (с учётом радиуса)
        private bool WouldEdgeTouchAnyOtherEdge(Vertex vertex, Vector newPosition)
        {
            // Получаем все рёбра, связанные с этой вершиной
            var attachedEdges = Edges.Where(e => e.Start == vertex || e.End == vertex).ToList();

            foreach (var edge in attachedEdges)
            {
                // Определяем другой конец ребра (не перемещаемую вершину)
                Vertex otherEnd = (edge.Start == vertex) ? edge.End : edge.Start;

                // Для каждого существующего ребра проверяем, не задевает ли его перемещаемое ребро
                foreach (var existingEdge in Edges)
                {
                    // Пропускаем рёбра, которые используют перемещаемую вершину
                    if (existingEdge.Start == vertex || existingEdge.End == vertex)
                        continue;

                    // Пропускаем само ребро
                    if (existingEdge == edge)
                        continue;

                    // Проверяем, не задевает ли ребро (otherEnd -> newPosition) существующее ребро
                    if (DoEdgesTouch(otherEnd.V_Position, newPosition, existingEdge))
                    {
                        Logger.Log($"Перетаскивание: ребро ({otherEnd.V_Position.X:F0},{otherEnd.V_Position.Y:F0})->({newPosition.X:F0},{newPosition.Y:F0}) задевает ребро ({existingEdge.Start.V_Position.X:F0},{existingEdge.Start.V_Position.Y:F0})->({existingEdge.End.V_Position.X:F0},{existingEdge.End.V_Position.Y:F0})", "DRAG");
                        return true;
                    }
                }
            }

            return false;
        }

        // Проверка, задевает ли отрезок (p1-p2) существующее ребро с учётом радиуса
        private bool DoEdgesTouch(Vector p1, Vector p2, Edge existingEdge)
        {
            // Получаем точки существующего ребра
            Vector e1 = existingEdge.Start.V_Position;
            Vector e2 = existingEdge.End.V_Position;

            // Проверяем расстояние от каждого конца нового ребра до существующего ребра
            double dist1 = PointToSegmentDistance(new Point(p1.X, p1.Y), new Point(e1.X, e1.Y), new Point(e2.X, e2.Y));
            double dist2 = PointToSegmentDistance(new Point(p2.X, p2.Y), new Point(e1.X, e1.Y), new Point(e2.X, e2.Y));

            // Проверяем расстояние от каждого конца существующего ребра до нового ребра
            double dist3 = PointToSegmentDistance(new Point(e1.X, e1.Y), new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
            double dist4 = PointToSegmentDistance(new Point(e2.X, e2.Y), new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));

            // Если любое расстояние меньше радиуса погрешности - считаем задеванием
            double tolerance = DEFAULT_TOLERANCE_RADIUS;

            return dist1 <= tolerance || dist2 <= tolerance || dist3 <= tolerance || dist4 <= tolerance;
        }

        // Добавьте этот метод в класс CellDrawingManager
        // Добавьте этот метод в класс CellDrawingManager
        private bool WouldEdgeIntersectExistingEdges(Vertex start, Vertex end, bool isHorizontal)
        {
            foreach (var existingEdge in Edges)
            {
                // Пропускаем рёбра, которые используют те же вершины (это допустимо)
                if ((existingEdge.Start == start && existingEdge.End == end) ||
                    (existingEdge.Start == end && existingEdge.End == start))
                    continue;

                // Пропускаем рёбра, которые имеют общую вершину с создаваемым ребром
                if (existingEdge.Start == start || existingEdge.Start == end ||
                    existingEdge.End == start || existingEdge.End == end)
                    continue;

                // ========== НОВАЯ ПРОВЕРКА ==========
                // Проверяем, не заканчивается ли новое ребро на существующей вершине,
                // которая лежит на existingEdge (но не является его концом)
                // Если да, то это разрешено — мы просто присоединяемся к существующему ребру
                if (IsEndPointLyingOnEdge(end, existingEdge) || IsEndPointLyingOnEdge(start, existingEdge))
                {
                    continue; // Разрешаем — это присоединение к существующему ребру в его середине
                }

                // Проверяем пересечение
                if (DoEdgesIntersectTest(start, end, existingEdge.Start, existingEdge.End))
                {
                    Logger.Log($"Обнаружено пересечение с существующим ребром", "DRAW");
                    return true;
                }
            }

            return false;
        }

        // Новый метод для проверки, лежит ли вершина на существующем ребре (включая середину)
        private bool IsEndPointLyingOnEdge(Vertex vertex, Edge edge)
        {
            // Проверяем, не является ли вершина одним из концов ребра (это уже проверено выше)
            if (edge.Start == vertex || edge.End == vertex)
                return true;

            // Проверяем расстояние от вершины до ребра
            double distance = edge.DistanceToPoint(vertex.V_Position);

            // Если расстояние меньше погрешности (например, 1 пиксель), считаем, что вершина лежит на ребре
            return distance < 1.0;
        }

        // Новый метод для проверки пересечения двух отрезков
        private bool DoEdgesIntersectTest(Vertex a1, Vertex a2, Vertex b1, Vertex b2)
        {
            Point p1 = new Point(a1.V_Position.X, a1.V_Position.Y);
            Point p2 = new Point(a2.V_Position.X, a2.V_Position.Y);
            Point p3 = new Point(b1.V_Position.X, b1.V_Position.Y);
            Point p4 = new Point(b2.V_Position.X, b2.V_Position.Y);

            // Проверяем пересечение отрезков (без учёта общих вершин)
            return SegmentsIntersect(p1, p2, p3, p4);
        }

        // Алгоритм проверки пересечения двух отрезков
        private bool SegmentsIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double o1 = Orientation(p1, p2, p3);
            double o2 = Orientation(p1, p2, p4);
            double o3 = Orientation(p3, p4, p1);
            double o4 = Orientation(p3, p4, p2);

            // Общий случай
            if (o1 != o2 && o3 != o4)
                return true;

            // Проверка на коллинеарность и наложение
            if (o1 == 0 && OnSegment(p1, p3, p2)) return true;
            if (o2 == 0 && OnSegment(p1, p4, p2)) return true;
            if (o3 == 0 && OnSegment(p3, p1, p4)) return true;
            if (o4 == 0 && OnSegment(p3, p2, p4)) return true;

            return false;
        }

        // Проверка, не находится ли вершина слишком близко к существующему ребру
        private bool WouldVertexTouchAnyEdge(Vector position, double toleranceRadius)
        {
            foreach (var edge in Edges)
            {
                if (IsVertexTooCloseToEdge(position, toleranceRadius, edge))
                {
                    Logger.Log($"Вершина слишком близко к ребру: ({edge.Start.V_Position.X:F0},{edge.Start.V_Position.Y:F0}) -> ({edge.End.V_Position.X:F0},{edge.End.V_Position.Y:F0})", "VERTEX");
                    return true;
                }
            }
            return false;
        }

        // Проверка расстояния от точки до ребра
        private bool IsVertexTooCloseToEdge(Vector vertexPos, double toleranceRadius, Edge edge)
        {
            return edge.DistanceToPoint(vertexPos) <= toleranceRadius;
        }

        // Скопируйте этот метод из вашего кода (он уже есть в классе)
        // private double PointToSegmentDistance(Point p, Point a, Point b) - уже существует

        // Вычисление ориентации трёх точек
        private double Orientation(Point p, Point q, Point r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (Math.Abs(val) < 1e-10) return 0; // Коллинеарны
            return (val > 0) ? 1 : 2; // По часовой или против
        }

        // Проверка, лежит ли точка q на отрезке pr
        private bool OnSegment(Point p, Point q, Point r)
        {
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }

        // ========== Рисование ребра ==========

        public void StartDrawingEdge(Vertex startVertex)
        {
            _drawingStartVertex = startVertex;
            _isDrawingEdge = true;
            _currentEdgeLength = 0;
            _lastDrawnLength = -1;
            _lastMousePosition = new Point(-1, -1);
            Logger.LogDrawingState("НАЧАЛО", $"от вершины ({startVertex.V_Position.X:F0},{startVertex.V_Position.Y:F0})");
        }

        private bool CanCreateEdge(double length) => length >= MinEdgeLength;

        private bool CanCreateVertexAtPosition(Vector position)
        {
            double fieldWidth = Visualizer.Canvas.Width;
            double fieldHeight = Visualizer.Canvas.Height;

            // Проверка границ поля
            if (position.X - DEFAULT_TOLERANCE_RADIUS <= 0 ||
                position.X + DEFAULT_TOLERANCE_RADIUS >= fieldWidth ||
                position.Y - DEFAULT_TOLERANCE_RADIUS <= 0 ||
                position.Y + DEFAULT_TOLERANCE_RADIUS >= fieldHeight)
            {
                return false;
            }

            // Проверка близости к другим вершинам
            if (Vertices.Any(v =>
                Math.Sqrt(Math.Pow(v.V_Position.X - position.X, 2) +
                          Math.Pow(v.V_Position.Y - position.Y, 2)) < MinEdgeLength))
            {
                return false;
            }

            // Новая проверка: не слишком ли близко к существующим рёбрам
            if (WouldVertexTouchAnyEdge(position, DEFAULT_TOLERANCE_RADIUS))
            {
                return false;
            }

            return true;
        }

        public void UpdateDrawingEdge(Point currentPoint)
        {
            if (!_isDrawingEdge || _drawingStartVertex == null) return;
            if (_lastMousePosition == currentPoint) return;

            _lastMousePosition = currentPoint;
            Point startPoint = new Point(_drawingStartVertex.V_Position.X, _drawingStartVertex.V_Position.Y);

            double dx = Math.Abs(currentPoint.X - startPoint.X);
            double dy = Math.Abs(currentPoint.Y - startPoint.Y);

            Vector endVector;
            if (dx > dy)
            {
                _isHorizontal = true;
                endVector = new Vector(currentPoint.X, _drawingStartVertex.V_Position.Y);
            }
            else
            {
                _isHorizontal = false;
                endVector = new Vector(_drawingStartVertex.V_Position.X, currentPoint.Y);
            }

            _currentEdgeLength = _isHorizontal
                ? Math.Abs(endVector.X - _drawingStartVertex.V_Position.X)
                : Math.Abs(endVector.Y - _drawingStartVertex.V_Position.Y);

            double roundedLength = Math.Round(_currentEdgeLength, 0);
            if (Math.Abs(roundedLength - _lastDrawnLength) < 1 && _lastDrawnLength >= 0) return;
            _lastDrawnLength = roundedLength;

            Point endPoint = new Point(endVector.X, endVector.Y);

            // Проверяем условия для создания ребра
            bool canCreate = CanCreateEdge(_currentEdgeLength);

            // Временная проверка для вершины
            bool wouldTouchEdge = WouldVertexTouchAnyEdge(endVector, DEFAULT_TOLERANCE_RADIUS);
            bool wouldIntersect = false;

            // Создаём временную вершину для проверки пересечения
            Vertex tempEndVertex = new Vertex(endVector, DEFAULT_TOLERANCE_RADIUS, false);

            // Проверяем пересечение только если вершина не касается рёбер
            if (!wouldTouchEdge)
            {
                wouldIntersect = WouldEdgeIntersectExistingEdges(_drawingStartVertex, tempEndVertex, _isHorizontal);
            }

            // Цвет: красный если слишком короткое ИЛИ пересекается ИЛИ касается ребра
            Brush lineColor = (canCreate && !wouldIntersect && !wouldTouchEdge) ? Brushes.Gray : Brushes.Red;

            Visualizer.DrawTemporaryEdge(startPoint, endPoint, _isHorizontal, lineColor);
        }

        public Vertex FinishDrawingEdge(Point endPoint)
        {
            if (!_isDrawingEdge || _drawingStartVertex == null) return null;

            Vector endVector = _isHorizontal
                ? new Vector(endPoint.X, _drawingStartVertex.V_Position.Y)
                : new Vector(_drawingStartVertex.V_Position.X, endPoint.Y);

            // ВЫЧИСЛЯЕМ длину ДО того, как искать существующую вершину
            double length = _isHorizontal
                ? Math.Abs(endVector.X - _drawingStartVertex.V_Position.X)
                : Math.Abs(endVector.Y - _drawingStartVertex.V_Position.Y);

            if (!CanCreateEdge(length))
            {
                Logger.LogWarning($"Ребро слишком короткое ({length:F1} < {MinEdgeLength})");
                CancelDrawingEdge();
                return null;
            }

            // ИЩЕМ существующую вершину - НО проверяем не только конечную точку,
            // но и проекцию на линию!
            Vertex endVertex = FindNearbyVertexOnLine(_drawingStartVertex, endVector, _isHorizontal);

            // Если нашли существующую вершину на линии
            if (endVertex != null)
            {
                // Корректируем endVector до позиции существующей вершины
                endVector = endVertex.V_Position;

                // Пересчитываем длину
                length = _isHorizontal
                    ? Math.Abs(endVector.X - _drawingStartVertex.V_Position.X)
                    : Math.Abs(endVector.Y - _drawingStartVertex.V_Position.Y);

                if (!CanCreateEdge(length))
                {
                    Logger.LogWarning($"Ребро слишком короткое после привязки к вершине ({length:F1} < {MinEdgeLength})");
                    CancelDrawingEdge();
                    return null;
                }
            }
            else
            {
                // Если вершины нет на линии, ищем рядом с конечной точкой
                endVertex = FindNearbyVertex(endVector);

                if (endVertex != null)
                {
                    // Привязываемся к существующей вершине
                    endVector = endVertex.V_Position;
                }
                else
                {
                    // Создаём новую вершину только если место свободно
                    if (!CanCreateVertexAtPosition(endVector))
                    {
                        Logger.LogWarning($"Нельзя создать вершину слишком близко к границе или другой вершине");
                        CancelDrawingEdge();
                        return null;
                    }
                    endVertex = new Vertex(endVector, DEFAULT_TOLERANCE_RADIUS, false);
                    Vertices.Add(endVertex);
                    Visualizer.DrawVertex(endVertex, new Point(endVector.X, endVector.Y));
                    Logger.LogVertexCreation(endVertex);
                }
            }

            // Проверка на соединение с самим собой
            if (_drawingStartVertex == endVertex)
            {
                Logger.LogWarning("Нельзя соединить вершину саму с собой");
                CancelDrawingEdge();
                return null;
            }

            // Проверка на существующее ребро
            Edge newEdge = new Edge(_drawingStartVertex, endVertex);
            if (!IsEdgeExists(newEdge))
            {
                Edges.Add(newEdge);
                Visualizer.DrawEdge(newEdge, Brushes.Black, 3);
                Logger.LogEdgeCreation(newEdge);
            }
            else
            {
                Logger.LogWarning($"Попытка создать дублирующееся ребро между ({_drawingStartVertex.V_Position.X:F0},{_drawingStartVertex.V_Position.Y:F0}) и ({endVertex.V_Position.X:F0},{endVertex.V_Position.Y:F0})");
            }

            Vertex result = endVertex;
            CancelDrawingEdge();
            return result;
        }

        // НОВЫЙ МЕТОД: поиск вершины на линии рисования
        private Vertex FindNearbyVertexOnLine(Vertex startVertex, Vector endVector, bool isHorizontal)
        {
            foreach (var vertex in Vertices)
            {
                if (vertex == startVertex) continue;

                if (isHorizontal)
                {
                    // Для горизонтального ребра Y должен совпадать (с допуском)
                    if (Math.Abs(vertex.V_Position.Y - startVertex.V_Position.Y) <= DEFAULT_TOLERANCE_RADIUS)
                    {
                        // Проверяем, что X вершины находится между start и end (с допуском)
                        double minX = Math.Min(startVertex.V_Position.X, endVector.X);
                        double maxX = Math.Max(startVertex.V_Position.X, endVector.X);

                        if (vertex.V_Position.X >= minX - DEFAULT_TOLERANCE_RADIUS &&
                            vertex.V_Position.X <= maxX + DEFAULT_TOLERANCE_RADIUS)
                        {
                            return vertex;
                        }
                    }
                }
                else
                {
                    // Для вертикального ребра X должен совпадать (с допуском)
                    if (Math.Abs(vertex.V_Position.X - startVertex.V_Position.X) <= DEFAULT_TOLERANCE_RADIUS)
                    {
                        // Проверяем, что Y вершины находится между start и end (с допуском)
                        double minY = Math.Min(startVertex.V_Position.Y, endVector.Y);
                        double maxY = Math.Max(startVertex.V_Position.Y, endVector.Y);

                        if (vertex.V_Position.Y >= minY - DEFAULT_TOLERANCE_RADIUS &&
                            vertex.V_Position.Y <= maxY + DEFAULT_TOLERANCE_RADIUS)
                        {
                            return vertex;
                        }
                    }
                }
            }

            return null;
        }

        // Новый метод для поиска ребра, содержащего точку
        private Edge FindEdgeThatContainsPoint(Vector point)
        {
            foreach (var edge in Edges)
            {
                double distance = edge.DistanceToPoint(point);
                if (distance < 3.0) // Небольшая погрешность
                {
                    // Проверяем, не является ли точка концом ребра
                    if (edge.Start.V_Position.DistanceTo(point) < 1.0 ||
                        edge.End.V_Position.DistanceTo(point) < 1.0)
                        continue;
                    return edge;
                }
            }
            return null;
        }

        // Новый метод для разделения ребра
        private Vertex SplitEdge(Edge existingEdge, Vector splitPoint)
        {
            // Создаём новую вершину
            Vertex newVertex = new Vertex(splitPoint, DEFAULT_TOLERANCE_RADIUS, false);
            Vertices.Add(newVertex);
            Visualizer.DrawVertex(newVertex, new Point(splitPoint.X, splitPoint.Y));
            Logger.LogVertexCreation(newVertex);

            // Удаляем существующее ребро
            Edges.Remove(existingEdge);
            existingEdge.Delete();

            // Создаём два новых ребра
            Edge edge1 = new Edge(existingEdge.Start, newVertex);
            Edge edge2 = new Edge(newVertex, existingEdge.End);

            // Проверяем, что рёбра не слишком короткие
            if (edge1.Length < MinEdgeLength || edge2.Length < MinEdgeLength)
            {
                Logger.LogWarning("Разделённые рёбра слишком короткие — отмена разделения");
                Vertices.Remove(newVertex);
                Edges.Add(existingEdge); // Восстанавливаем исходное ребро
                return null;
            }

            Edges.Add(edge1);
            Edges.Add(edge2);
            Visualizer.DrawEdge(edge1, Brushes.Black, 3);
            Visualizer.DrawEdge(edge2, Brushes.Black, 3);
            Logger.LogEdgeCreation(edge1);
            Logger.LogEdgeCreation(edge2);

            return newVertex;
        }

        public void CancelDrawingEdge()
        {
            if (_isDrawingEdge)
            {
                Logger.LogDrawingState("ОТМЕНА", $"ребро не было завершено");
            }
            _isDrawingEdge = false;
            _drawingStartVertex = null;
            _currentEdgeLength = 0;
            _lastDrawnLength = -1;
            Visualizer.ClearTemporaryEdge();
        }

        // ========== Перетаскивание вершины ==========

        public void StartDraggingVertex(Point clickPoint)
        {
            Vertex vertexToDrag = FindNearbyVertex(new Vector(clickPoint.X, clickPoint.Y));
            if (vertexToDrag == null) return;

            _draggingVertex = vertexToDrag;
            _isDraggingVertex = true;
            _dragStartPoint = clickPoint;
            _dragStartPosition = vertexToDrag.V_Position;

            Visualizer.Canvas.CaptureMouse();
            Visualizer.HighlightVertex(vertexToDrag, true);
            Visualizer.ClearDragConflict();
            Logger.LogDraggingState("НАЧАЛО", vertexToDrag);
        }

        public void UpdateDraggingVertex(Point currentPoint)
        {
            if (!_isDraggingVertex || _draggingVertex == null) return;

            Vector newPosition = new Vector(
                _dragStartPosition.X + currentPoint.X - _dragStartPoint.X,
                _dragStartPosition.Y + currentPoint.Y - _dragStartPoint.Y);

            double min = DEFAULT_TOLERANCE_RADIUS;
            double maxX = Visualizer.Canvas.Width - min;
            double maxY = Visualizer.Canvas.Height - min;
            newPosition.X = Math.Max(min, Math.Min(maxX, newPosition.X));
            newPosition.Y = Math.Max(min, Math.Min(maxY, newPosition.Y));

            // Проверка близости к другим вершинам
            bool tooCloseToVertex = Vertices.Any(v => v != _draggingVertex &&
                Math.Sqrt(Math.Pow(v.V_Position.X - newPosition.X, 2) +
                          Math.Pow(v.V_Position.Y - newPosition.Y, 2)) < MinEdgeLength);

            if (tooCloseToVertex)
            {
                Visualizer.ShowDragConflict(currentPoint);
                return;
            }

            // Проверка, не станут ли рёбра слишком короткими
            if (WouldEdgeBecomeTooShort(_draggingVertex, newPosition))
            {
                Visualizer.ShowDragConflict(currentPoint);
                return;
            }

            // ========== ИСПРАВЛЕНИЕ: проверяем только пересечения с рёбрами, 
            // которые НЕ содержат перемещаемую вершину ==========
            bool wouldCreateIntersection = WouldEdgeFromVertexIntersectExistingEdges(_draggingVertex, newPosition);

            // Проверка, не заденет ли вершина другие рёбра (исключая рёбра, связанные с вершиной)
            bool wouldTouchEdge = WouldVertexTouchAnyEdgeInNewPosition(_draggingVertex, newPosition, DEFAULT_TOLERANCE_RADIUS);

            // НОВАЯ ПРОВЕРКА: не будут ли рёбра задевать другие рёбра
            bool wouldEdgeTouchOtherEdge = WouldEdgeTouchAnyOtherEdge(_draggingVertex, newPosition);

            if (wouldCreateIntersection || wouldTouchEdge || wouldEdgeTouchOtherEdge)
            {
                Visualizer.ShowDragConflict(currentPoint);
                return;
            }

            Visualizer.ClearDragConflict();
            Vector oldPos = _draggingVertex.V_Position;
            _draggingVertex.MoveTo(newPosition);
            Logger.LogVertexMoved(_draggingVertex, oldPos, newPosition);
            RedrawAll();
            Visualizer.HighlightVertex(_draggingVertex, true);
        }

        public void FinishDraggingVertex()
        {
            if (_isDraggingVertex && _draggingVertex != null)
            {
                Vector finalPosition = _draggingVertex.V_Position;

                // Финальная проверка перед завершением
                bool hasIntersection = WouldEdgeFromVertexIntersectExistingEdges(_draggingVertex, finalPosition);
                bool wouldTouchEdge = WouldVertexTouchAnyEdgeInNewPosition(_draggingVertex, finalPosition, DEFAULT_TOLERANCE_RADIUS);
                bool tooShort = WouldEdgeBecomeTooShort(_draggingVertex, finalPosition);
                bool edgeTouchesOther = WouldEdgeTouchAnyOtherEdge(_draggingVertex, finalPosition);

                if (hasIntersection || wouldTouchEdge || tooShort || edgeTouchesOther)
                {
                    Logger.LogWarning($"Перетаскивание завершено с конфликтом - откат позиции");
                    _draggingVertex.V_Position = _dragStartPosition;
                    RedrawAll();
                }

                Visualizer.HighlightVertex(_draggingVertex, false);
                Logger.LogDraggingState("ЗАВЕРШЕНИЕ", _draggingVertex);
                TryMergeVertex(_draggingVertex);
            }
            _isDraggingVertex = false;
            _draggingVertex = null;
            Visualizer.ClearDragConflict();
            Visualizer.Canvas.ReleaseMouseCapture();
        }

        public void CancelDraggingVertex()
        {
            if (_isDraggingVertex && _draggingVertex != null)
            {
                Logger.LogDraggingState("ОТМЕНА", _draggingVertex);
                _draggingVertex.V_Position = _dragStartPosition;
                RedrawAll();
                Visualizer.HighlightVertex(_draggingVertex, false);
            }
            _isDraggingVertex = false;
            _draggingVertex = null;
            Visualizer.ClearDragConflict();
            Visualizer.Canvas.ReleaseMouseCapture();
        }

        // ========== Коллизии шариков с вершинами ==========

        public void CheckVertexCollisions(List<IGameObject> objects)
        {
            if (!EnableVertexCollisions) return;
            if (Vertices.Count == 0) return;

            foreach (var obj in objects)
            {
                foreach (var vertex in Vertices)
                {
                    if (!vertex.IsCollidable) continue;

                    double dx = obj.V_Position.X - vertex.V_Position.X;
                    double dy = obj.V_Position.Y - vertex.V_Position.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    double minDistance = obj.Radius + vertex.CollisionRadius;

                    if (distance < minDistance)
                    {
                        Logger.LogBallVertexCollision(obj, vertex);

                        Vector normal = new Vector(dx, dy).Normalize();

                        if (double.IsNaN(normal.X) || double.IsNaN(normal.Y))
                        {
                            normal = new Vector(1, 0);
                        }

                        double overlap = minDistance - distance;
                        obj.V_Position += normal * (overlap + 1);

                        double dot = obj.V_Velocity.X * normal.X + obj.V_Velocity.Y * normal.Y;

                        if (obj is Circle circle)
                        {
                            obj.V_Velocity = new Vector(
                                obj.V_Velocity.X - 2 * dot * normal.X * circle.Bounciness,
                                obj.V_Velocity.Y - 2 * dot * normal.Y * circle.Bounciness
                            );
                        }
                        else
                        {
                            obj.V_Velocity = new Vector(
                                obj.V_Velocity.X - 2 * dot * normal.X,
                                obj.V_Velocity.Y - 2 * dot * normal.Y
                            );
                        }

                        UpdateShapePosition(obj);
                    }
                }
            }
        }

        // ========== Коллизии шариков с рёбрами (отскок) ==========

        public void CheckEdgeCollisions(List<IGameObject> objects)
        {
            if (!EnableEdgeBounce) return;
            if (Edges.Count == 0) return;

            foreach (var obj in objects)
            {
                foreach (var edge in Edges)
                {
                    if (!edge.IsCollidable) continue;

                    // Находим ближайшую точку на ребре к центру шарика
                    Vector closestPoint = edge.GetClosestPoint(obj.V_Position);

                    // Вычисляем расстояние от центра шарика до ближайшей точки
                    double dx = obj.V_Position.X - closestPoint.X;
                    double dy = obj.V_Position.Y - closestPoint.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    // Если шарик касается или пересекает ребро
                    if (distance <= obj.Radius)
                    {
                        Logger.LogBallEdgeCollision(obj, edge);

                        // Нормаль от ближайшей точки к центру шарика
                        Vector normal = new Vector(dx, dy).Normalize();

                        // Если нормаль не определена (центр точно на ребре), используем нормаль ребра
                        if (double.IsNaN(normal.X) || double.IsNaN(normal.Y))
                        {
                            normal = edge.GetNormal();
                        }

                        // Корректируем позицию, чтобы шарик не застревал в ребре
                        double penetration = obj.Radius - distance;
                        obj.V_Position += normal * (penetration + 0.1);

                        // Обновляем позицию визуального элемента
                        UpdateShapePosition(obj);

                        // Вычисляем отражённую скорость
                        if (obj is Circle circle)
                        {
                            double dot = obj.V_Velocity.X * normal.X + obj.V_Velocity.Y * normal.Y;
                            obj.V_Velocity = new Vector(
                                obj.V_Velocity.X - 2 * dot * normal.X * circle.Bounciness,
                                obj.V_Velocity.Y - 2 * dot * normal.Y * circle.Bounciness
                            );
                        }
                        else
                        {
                            double dot = obj.V_Velocity.X * normal.X + obj.V_Velocity.Y * normal.Y;
                            obj.V_Velocity = new Vector(
                                obj.V_Velocity.X - 2 * dot * normal.X,
                                obj.V_Velocity.Y - 2 * dot * normal.Y
                            );
                        }

                        // Небольшое смещение для предотвращения залипания
                        obj.Update(0.001);
                    }
                }
            }
        }

        // ========== Проверка касания рёбер объектами (исчезновение) ==========

        public void CheckEdgesTouchedByObjects(List<IGameObject> objects)
        {
            if (!EnableEdgeRemovalOnTouch) return;
            if (Edges.Count == 0) return;

            // Изменяем условие: удаляем рёбра, у которых IsRemovableOnTouch == false
            var edgesToRemove = Edges.Where(edge => !edge.IsRemovableOnTouch &&  // <-- ИЗМЕНЕНО: !edge.IsRemovableOnTouch
                objects.Any(obj => IsEdgeTouchedByObject(edge, obj))).ToList();

            if (edgesToRemove.Count == 0) return;

            Logger.Log($"Обнаружено {edgesToRemove.Count} рёбер, задетых шариками", "COLLISION");

            foreach (var edge in edgesToRemove)
            {
                var touchingBall = objects.FirstOrDefault(obj => IsEdgeTouchedByObject(edge, obj));
                if (touchingBall != null)
                {
                    Logger.LogEdgeTouchedByBall(edge, touchingBall);
                }
            }

            var affectedVertices = new HashSet<Vertex>();
            foreach (var edge in edgesToRemove)
            {
                if (edge.Start != null) affectedVertices.Add(edge.Start);
                if (edge.End != null) affectedVertices.Add(edge.End);
            }

            if (_isDrawingEdge && _drawingStartVertex != null && affectedVertices.Contains(_drawingStartVertex))
            {
                Logger.LogDrawingState("ОТМЕНА", "начальная вершина ребра была затронута");
                CancelDrawingEdge();
            }
            if (_isDraggingVertex && _draggingVertex != null && affectedVertices.Contains(_draggingVertex))
            {
                Logger.LogDraggingState("ОТМЕНА", _draggingVertex);
                CancelDraggingVertex();
            }

            foreach (var edge in edgesToRemove)
            {
                edge.Delete();
                Edges.Remove(edge);
            }

            RemoveOrphanedVertices(affectedVertices);
            RedrawAll();
        }

        private bool IsEdgeTouchedByObject(Edge edge, IGameObject obj)
        {
            return edge.DistanceToPoint(obj.V_Position) <= obj.Radius;
        }

        // Можно оставить для обратной совместимости, но лучше использовать edge.DistanceToPoint()
        private double PointToSegmentDistance(Point p, Point a, Point b)
        {
            Vector ap = new Vector(p.X - a.X, p.Y - a.Y);
            Vector ab = new Vector(b.X - a.X, b.Y - a.Y);

            double t = (ap.X * ab.X + ap.Y * ab.Y) / (ab.X * ab.X + ab.Y * ab.Y);
            t = Math.Max(0, Math.Min(1, t));

            double closestX = a.X + t * ab.X;
            double closestY = a.Y + t * ab.Y;

            double dx = p.X - closestX;
            double dy = p.Y - closestY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // ========== Управление вершинами и рёбрами ==========

        private void TryMergeVertex(Vertex vertex)
        {
            foreach (var other in Vertices.ToList())
            {
                if (other == vertex) continue;
                if (Math.Sqrt(Math.Pow(other.V_Position.X - vertex.V_Position.X, 2) +
                              Math.Pow(other.V_Position.Y - vertex.V_Position.Y, 2)) >= MinEdgeLength * 0.5) continue;

                Logger.Log($"Обнаружено слияние вершин: ({vertex.V_Position.X:F0},{vertex.V_Position.Y:F0}) и ({other.V_Position.X:F0},{other.V_Position.Y:F0})", "VERTEX");

                if (_isDrawingEdge && (_drawingStartVertex == vertex || _drawingStartVertex == other))
                    CancelDrawingEdge();
                if (_isDraggingVertex && (_draggingVertex == vertex || _draggingVertex == other))
                    CancelDraggingVertex();

                foreach (var edge in Edges.ToList())
                {
                    if (edge.Start == vertex)
                    {
                        ReassignEdgeVertex(edge, edge.Start, other);
                    }
                    if (edge.End == vertex)
                    {
                        ReassignEdgeVertex(edge, edge.End, other);
                    }
                }

                RemoveDuplicateEdges();
                RemoveSelfLoops();
                Vertices.Remove(vertex);
                RedrawAll();
                break;
            }
        }

        private void ReassignEdgeVertex(Edge edge, Vertex oldVertex, Vertex newVertex)
        {
            if (oldVertex != null)
            {
                oldVertex.OnDeleted -= edge.OnVertexDeletedHandler;
                oldVertex.OnPositionChanged -= edge.OnVertexMovedHandler;
            }
            if (edge.Start == oldVertex) edge.Start = newVertex;
            if (edge.End == oldVertex) edge.End = newVertex;
            newVertex.OnDeleted += edge.OnVertexDeletedHandler;
            newVertex.OnPositionChanged += edge.OnVertexMovedHandler;
        }

        private void RemoveOrphanedVertices(HashSet<Vertex> verticesToCheck)
        {
            var usedVertices = new HashSet<Vertex>();
            foreach (var edge in Edges)
            {
                if (edge.Start != null) usedVertices.Add(edge.Start);
                if (edge.End != null) usedVertices.Add(edge.End);
            }

            foreach (var vertex in verticesToCheck.Where(v => !usedVertices.Contains(v) && Vertices.Contains(v)).ToList())
            {
                Logger.LogVertexDeletion(vertex, "осиротевшая вершина (нет инцидентных рёбер)");
                Vertices.Remove(vertex);
            }
        }

        private void RemoveDuplicateEdges()
        {
            var uniqueEdges = new List<Edge>();
            foreach (var edge in Edges)
            {
                bool isDuplicate = uniqueEdges.Any(e =>
                    (e.Start == edge.Start && e.End == edge.End) ||
                    (e.Start == edge.End && e.End == edge.Start));

                if (!isDuplicate && edge.Start != edge.End && edge.Start != null && edge.End != null)
                    uniqueEdges.Add(edge);
                else if (isDuplicate)
                {
                    Logger.LogWarning($"Удалено дублирующееся ребро между ({edge.Start?.V_Position.X:F0},{edge.Start?.V_Position.Y:F0}) и ({edge.End?.V_Position.X:F0},{edge.End?.V_Position.Y:F0})");
                }
            }
            Edges.Clear();
            Edges.AddRange(uniqueEdges);
        }

        private void RemoveSelfLoops()
        {
            var selfLoops = Edges.Where(e => e.Start == e.End).ToList();
            if (selfLoops.Any())
            {
                Logger.Log($"Удалено {selfLoops.Count} петель (ребро из вершины в саму себя)", "EDGE");
                Edges.RemoveAll(e => e.Start == e.End);
            }
        }

        private bool IsEdgeExists(Edge edge)
        {
            return Edges.Any(e => (e.Start == edge.Start && e.End == edge.End) ||
                                  (e.Start == edge.End && e.End == edge.Start));
        }

        private Vertex FindNearbyVertex(Vector position)
        {
            return Vertices.FirstOrDefault(v =>
                Math.Sqrt(Math.Pow(v.V_Position.X - position.X, 2) +
                          Math.Pow(v.V_Position.Y - position.Y, 2)) <= v.ToleranceRadius);
        }

        public bool DeleteVertex(Vertex vertex)
        {
            if (!Vertices.Contains(vertex)) return false;

            Logger.LogVertexDeletion(vertex, "вызов DeleteVertex()");
            vertex.Delete();
            Vertices.Remove(vertex);

            int removedEdges = Edges.RemoveAll(e => e.Start == null || e.End == null ||
                                                     !Vertices.Contains(e.Start) || !Vertices.Contains(e.End));

            if (removedEdges > 0)
            {
                Logger.Log($"При удалении вершины удалено {removedEdges} осиротевших рёбер", "EDGE");
            }

            RedrawAll();
            return true;
        }

        public bool DeleteVertexAt(Point clickPoint)
        {
            var vertex = FindNearbyVertex(new Vector(clickPoint.X, clickPoint.Y));
            return vertex != null && DeleteVertex(vertex);
        }

        public void RedrawAll()
        {
            Visualizer.ClearAll();
            foreach (var edge in Edges.Where(e => e.Start != null && e.End != null))
                Visualizer.DrawEdge(edge, Brushes.Black, 3);
            foreach (var vertex in Vertices)
                Visualizer.DrawVertex(vertex, new Point(vertex.V_Position.X, vertex.V_Position.Y));
        }

        // ========== Вспомогательные методы ==========

        private void UpdateShapePosition(IGameObject obj)
        {
            if (obj.Shape != null)
            {
                System.Windows.Controls.Canvas.SetLeft(obj.Shape, obj.V_Position.X - obj.Radius);
                System.Windows.Controls.Canvas.SetTop(obj.Shape, obj.V_Position.Y - obj.Radius);
            }
        }

        // ========== Обработчики событий мыши ==========

        public void HandleMouseLeftButtonDown(Point clickPoint)
        {
            if (_isDraggingVertex) return;

            var nearbyVertex = FindNearbyVertex(new Vector(clickPoint.X, clickPoint.Y));
            if (nearbyVertex != null)
            {
                StartDrawingEdge(nearbyVertex);
            }
            else if (CanCreateVertexAtPosition(new Vector(clickPoint.X, clickPoint.Y)))
            {
                var newVertex = new Vertex(new Vector(clickPoint.X, clickPoint.Y), DEFAULT_TOLERANCE_RADIUS, false);
                Vertices.Add(newVertex);
                Visualizer.DrawVertex(newVertex, clickPoint);
                Logger.LogVertexCreation(newVertex);
            }
            else
            {
                Logger.LogWarning($"Не удалось создать вершину в позиции ({clickPoint.X:F0},{clickPoint.Y:F0})");
            }
        }

        public void HandleMouseRightButtonDown(Point clickPoint)
        {
            if (!_isDraggingVertex) DeleteVertexAt(clickPoint);
        }

        public void HandleMouseMiddleButtonDown(Point clickPoint)
        {
            if (_isDrawingEdge) CancelDrawingEdge();
            StartDraggingVertex(clickPoint);
        }

        public void HandleMouseMiddleButtonUp(Point clickPoint) => FinishDraggingVertex();

        public void HandleMouseMove(Point currentPoint)
        {
            if (_isDraggingVertex) UpdateDraggingVertex(currentPoint);
            else if (_isDrawingEdge) UpdateDrawingEdge(currentPoint);
        }

        public void HandleMouseLeftButtonUp(Point upPoint)
        {
            if (_isDrawingEdge) FinishDrawingEdge(upPoint);
        }

        public void ClearAll()
        {
            Logger.Log("Очистка всех данных (ClearAll)", "SYSTEM");

            foreach (var edge in Edges)
            {
                if (edge.Start != null)
                {
                    edge.Start.OnDeleted -= edge.OnVertexDeletedHandler;
                    edge.Start.OnPositionChanged -= edge.OnVertexMovedHandler;
                }
                if (edge.End != null)
                {
                    edge.End.OnDeleted -= edge.OnVertexDeletedHandler;
                    edge.End.OnPositionChanged -= edge.OnVertexMovedHandler;
                }
            }
            Vertices.Clear();
            Edges.Clear();
            Visualizer.ClearAll();
            CancelDrawingEdge();
            CancelDraggingVertex();
        }
    }
}