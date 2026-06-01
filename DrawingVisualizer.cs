using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TMP_RGZ
{
    public class DrawingVisualizer
    {
        // Публичные поля
        public Canvas Canvas;
        public bool ShowToleranceRadius = true;

        // Добавь эти методы в класс DrawingVisualizer:

        private Ellipse _highlightedVertexVisual;
        private Ellipse _dragConflictVisual;

        // Список всех визуальных элементов на Canvas (для очистки)
        public List<UIElement> DrawnElements;

        public DrawingVisualizer(Canvas canvas)
        {
            Canvas = canvas;
            DrawnElements = new List<UIElement>();
        }

        // Подсветка вершины (при перетаскивании)
        // Подсветка вершины (при перетаскивании) - исправленная версия
        public void HighlightVertex(Vertex vertex, bool highlight)
        {
            if (highlight)
            {
                // Снимаем подсветку с предыдущей вершины
                if (_highlightedVertexVisual != null)
                {
                    _highlightedVertexVisual.Fill = Brushes.Red;
                    _highlightedVertexVisual.Stroke = Brushes.DarkRed;
                    _highlightedVertexVisual.StrokeThickness = 2;
                    _highlightedVertexVisual = null;
                }

                // Ищем визуальный элемент вершины по позиции
                double left = vertex.V_Position.X - 4;
                double top = vertex.V_Position.Y - 4;

                foreach (var element in DrawnElements)
                {
                    if (element is Ellipse ellipse &&
                        Math.Abs(Canvas.GetLeft(ellipse) - left) < 1 &&
                        Math.Abs(Canvas.GetTop(ellipse) - top) < 1)
                    {
                        ellipse.Fill = Brushes.Yellow;
                        ellipse.Stroke = Brushes.Orange;
                        ellipse.StrokeThickness = 3;
                        _highlightedVertexVisual = ellipse;
                        break;
                    }
                }
            }
            else
            {
                if (_highlightedVertexVisual != null)
                {
                    _highlightedVertexVisual.Fill = Brushes.Red;
                    _highlightedVertexVisual.Stroke = Brushes.DarkRed;
                    _highlightedVertexVisual.StrokeThickness = 2;
                    _highlightedVertexVisual = null;
                }
            }
        }

        // Показать конфликт при перетаскивании (красный крестик или кружок)
        public void ShowDragConflict(Point position)
        {
            ClearDragConflict();

            _dragConflictVisual = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Red,
                StrokeThickness = 3,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            Canvas.SetLeft(_dragConflictVisual, position.X - 10);
            Canvas.SetTop(_dragConflictVisual, position.Y - 10);
            Canvas.Children.Add(_dragConflictVisual);
            DrawnElements.Add(_dragConflictVisual);
        }

        public void ClearDragConflict()
        {
            if (_dragConflictVisual != null)
            {
                Canvas.Children.Remove(_dragConflictVisual);
                DrawnElements.Remove(_dragConflictVisual);
                _dragConflictVisual = null;
            }
        }

        // Отрисовка вершины с радиусом погрешности
        public void DrawVertex(Vertex vertex, Point position)
        {
            // Круг погрешности
            if (ShowToleranceRadius)
            {
                Ellipse toleranceVisual = new Ellipse
                {
                    Width = vertex.ToleranceRadius * 2,
                    Height = vertex.ToleranceRadius * 2,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Orange,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 3, 3 }
                };
                Canvas.SetLeft(toleranceVisual, position.X - vertex.ToleranceRadius);
                Canvas.SetTop(toleranceVisual, position.Y - vertex.ToleranceRadius);
                Canvas.Children.Add(toleranceVisual);
                DrawnElements.Add(toleranceVisual);
            }

            // Центральная точка вершины
            Ellipse vertexVisual = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };
            Canvas.SetLeft(vertexVisual, position.X - 4);
            Canvas.SetTop(vertexVisual, position.Y - 4);
            Canvas.Children.Add(vertexVisual);
            DrawnElements.Add(vertexVisual);
        }

        // Отрисовка ребра
        public void DrawEdge(Edge edge, Brush color, double thickness)
        {
            Line line = new Line
            {
                X1 = edge.Start.V_Position.X,
                Y1 = edge.Start.V_Position.Y,
                X2 = edge.End.V_Position.X,
                Y2 = edge.End.V_Position.Y,
                Stroke = color,
                StrokeThickness = thickness
            };

            Canvas.Children.Add(line);
            DrawnElements.Add(line);
        }

        private Line _temporaryEdge;

        public void ClearTemporaryEdge()
        {
            if (_temporaryEdge != null)
            {
                Canvas.Children.Remove(_temporaryEdge);
                DrawnElements.Remove(_temporaryEdge);
                _temporaryEdge = null;
            }
        }

        // Отрисовка всех вершин из списка
        public void DrawAllVertices(List<Vertex> vertices)
        {
            foreach (var vertex in vertices)
            {
                Point position = new Point(vertex.V_Position.X, vertex.V_Position.Y);
                DrawVertex(vertex, position);
            }
        }

        // Отрисовка всех рёбер из списка
        public void DrawAllEdges(List<Edge> edges)
        {
            foreach (var edge in edges)
            {
                DrawEdge(edge, Brushes.Black, 3);
            }
        }

        // Очистка всех нарисованных элементов
        public void ClearAll()
        {
            foreach (var element in DrawnElements)
            {
                Canvas.Children.Remove(element);
            }
            DrawnElements.Clear();
            _temporaryEdge = null;
        }

        // Очистка только вершин (оставить другие элементы, если есть)
        public void ClearVertices()
        {
            var toRemove = new List<UIElement>();
            foreach (var element in DrawnElements)
            {
                if (element is Ellipse)
                {
                    Canvas.Children.Remove(element);
                    toRemove.Add(element);
                }
            }
            foreach (var item in toRemove)
            {
                DrawnElements.Remove(item);
            }
        }

        // Отрисовка временного ребра (при рисовании)
        public void DrawTemporaryEdge(Point start, Point end, bool isHorizontal, Brush lineColor)
        {
            ClearTemporaryEdge();

            Line line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = lineColor,  // Теперь цвет передаётся параметром
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 5 }
            };

            Canvas.Children.Add(line);
            DrawnElements.Add(line);
            _temporaryEdge = line;
        }

        // Отрисовка замкнутой клетки (полупрозрачная заливка для отладки)
        public void DrawClosedCell(Cell cell)
        {
            if (!cell.IsClosed) return;

            // Создаём прямоугольник для клетки
            Rectangle rect = new Rectangle
            {
                Width = cell.MaxX - cell.MinX,
                Height = cell.MaxY - cell.MinY,
                Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)), // полупрозрачный зелёный
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 2
            };

            Canvas.SetLeft(rect, cell.MinX);
            Canvas.SetTop(rect, cell.MinY);
            Canvas.Children.Add(rect);
            DrawnElements.Add(rect);
        }
    }
}