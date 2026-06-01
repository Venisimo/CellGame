using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Diagnostics;

namespace TMP_RGZ
{
    public static class Logger
    {
        private static string _logFilePath = "game_log.txt";
        private static object _lockObject = new object();
        private static bool _enabled = true;

        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        static Logger()
        {
            // Очищаем файл лога при запуске
            try
            {
                if (File.Exists(_logFilePath))
                    File.Delete(_logFilePath);
            }
            catch { }
        }

        public static void Log(string message, string category = "INFO")
        {
            if (!_enabled) return;

            lock (_lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{category}] {message}";

                    // Записываем в файл
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);

                    // Также выводим в Debug Output (Visual Studio)
                    Debug.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка записи лога: {ex.Message}");
                }
            }
        }

        public static void LogEdgeCreation(Edge edge)
        {
            Log($"Создано ребро: ({edge.Start.V_Position.X:F0},{edge.Start.V_Position.Y:F0}) -> ({edge.End.V_Position.X:F0},{edge.End.V_Position.Y:F0}), IsHorizontal={edge.IsHorizontal}", "EDGE");
        }

        public static void LogEdgeDeletion(Edge edge, string reason)
        {
            string edgeInfo = edge.Start != null && edge.End != null
                ? $"({edge.Start.V_Position.X:F0},{edge.Start.V_Position.Y:F0}) -> ({edge.End.V_Position.X:F0},{edge.End.V_Position.Y:F0})"
                : "ребро с null-вершинами";
            Log($"Удалено ребро: {edgeInfo}. Причина: {reason}", "EDGE");
        }

        public static void LogEdgeTouchedByBall(Edge edge, IGameObject ball)
        {
            Log($"Ребро задето шариком! Позиция шарика: ({ball.V_Position.X:F0},{ball.V_Position.Y:F0}), Радиус: {ball.Radius}", "COLLISION");
        }

        public static void LogVertexCreation(Vertex vertex)
        {
            Log($"Создана вершина: ({vertex.V_Position.X:F0},{vertex.V_Position.Y:F0}), ToleranceRadius={vertex.ToleranceRadius}", "VERTEX");
        }

        public static void LogVertexDeletion(Vertex vertex, string reason)
        {
            Log($"Удалена вершина: ({vertex.V_Position.X:F0},{vertex.V_Position.Y:F0}). Причина: {reason}", "VERTEX");
        }

        public static void LogVertexMoved(Vertex vertex, Vector oldPos, Vector newPos)
        {
            Log($"Перемещена вершина: ({oldPos.X:F0},{oldPos.Y:F0}) -> ({newPos.X:F0},{newPos.Y:F0})", "VERTEX");
        }

        public static void LogBallVertexCollision(IGameObject ball, Vertex vertex)
        {
            Log($"СТОЛКНОВЕНИЕ шарика с вершиной! Шарик: ({ball.V_Position.X:F0},{ball.V_Position.Y:F0}), Вершина: ({vertex.V_Position.X:F0},{vertex.V_Position.Y:F0}), Расстояние: {Math.Sqrt(Math.Pow(ball.V_Position.X - vertex.V_Position.X, 2) + Math.Pow(ball.V_Position.Y - vertex.V_Position.Y, 2)):F1}", "COLLISION");
        }

        public static void LogBallEdgeCollision(IGameObject ball, Edge edge)
        {
            Log($"ОТСКОК шарика от ребра! Шарик: ({ball.V_Position.X:F0},{ball.V_Position.Y:F0}), Ребро: ({edge.Start.V_Position.X:F0},{edge.Start.V_Position.Y:F0}) -> ({edge.End.V_Position.X:F0},{edge.End.V_Position.Y:F0})", "COLLISION");
        }

        public static void LogDrawingState(string action, string details = "")
        {
            Log($"Рисование: {action}. {details}", "DRAW");
        }

        public static void LogDraggingState(string action, Vertex vertex)
        {
            Log($"Перетаскивание: {action}. Вершина: ({vertex.V_Position.X:F0},{vertex.V_Position.Y:F0})", "DRAG");
        }

        public static void LogCellCreated(Cell cell)
        {
            int edgeCount = cell.edges?.Count ?? 0;
            Log($"Создана клетка! Количество рёбер: {edgeCount}, Замкнута: {cell.IsClosed}", "CELL");
        }

        public static void LogCellClosed(Cell cell)
        {
            Log($"Клетка ЗАМКНУТА! MinX={cell.MinX:F0}, MaxX={cell.MaxX:F0}, MinY={cell.MinY:F0}, MaxY={cell.MaxY:F0}", "CELL");
        }

        public static void LogError(string message, Exception ex = null)
        {
            string errorMsg = message;
            if (ex != null)
                errorMsg += $" | Ошибка: {ex.Message}";
            Log(errorMsg, "ERROR");
        }

        public static void LogWarning(string message)
        {
            Log(message, "WARNING");
        }
    }
}