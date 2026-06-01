using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TMP_RGZ
{
    public partial class GameWindow : Window
    {
        private GameField gameField;
        private List<IGameObject> objects;
        private Random random = new Random();
        private DispatcherTimer gameTimer;
        private DateTime lastUpdate;

        private DrawingVisualizer _visualizer;
        private CellDrawingManager _cellDrawingManager;

        private int _frameCounter = 0;
        private int _edgeDeletionCounter = 0;

        public GameWindow()
        {
            InitializeComponent();

            // ✅ СНАЧАЛА создаём игровое поле
            gameField = new GameField();

            // ✅ ПОТОМ используем его свойства
            Logger.Log("=== НАЧАЛО ИГРЫ ===", "GAME");
            Logger.Log($"Размер поля: {gameField.Width}x{gameField.Height}", "GAME");

            // Отрисовка поля
            DrawGameField();

            // Создание игровых объектов
            InitializeGameObjects();

            // Запуск игрового цикла
            StartGameLoop();

            // Инициализация визуализатора и менеджера рисования
            _visualizer = new DrawingVisualizer(GameCanvas);
            _cellDrawingManager = new CellDrawingManager(_visualizer);

            // Настройка поведения игры
            _cellDrawingManager.EnableVertexCollisions = true;
            _cellDrawingManager.EnableEdgeBounce = true;
            _cellDrawingManager.EnableEdgeRemovalOnTouch = true;

            Logger.Log("Настройки коллизий: VertexCollisions=ON, EdgeBounce=ON, EdgeRemoval=ON", "GAME");

            // Подписка на события мыши
            GameCanvas.MouseDown += GameCanvas_MouseDown;
            GameCanvas.MouseUp += GameCanvas_MouseUp;
            GameCanvas.MouseMove += GameCanvas_MouseMove;
            GameCanvas.LostMouseCapture += GameCanvas_LostMouseCapture;

            this.Closing += MainWindow_Closing;
        }

        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(GameCanvas);

            if (e.ChangedButton == MouseButton.Left)
            {
                _cellDrawingManager.HandleMouseLeftButtonDown(clickPoint);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                _cellDrawingManager.HandleMouseRightButtonDown(clickPoint);
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                _cellDrawingManager.HandleMouseMiddleButtonDown(clickPoint);
                e.Handled = true;
            }
        }

        private void GameCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point upPoint = e.GetPosition(GameCanvas);

            if (e.ChangedButton == MouseButton.Left)
            {
                _cellDrawingManager.HandleMouseLeftButtonUp(upPoint);
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                _cellDrawingManager.HandleMouseMiddleButtonUp(upPoint);
                e.Handled = true;
            }
        }

        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPoint = e.GetPosition(GameCanvas);
            _cellDrawingManager.HandleMouseMove(currentPoint);
        }

        private void GameCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _cellDrawingManager.CancelDraggingVertex();
        }

        private void InitializeGameObjects()
        {
            objects = new List<IGameObject>();

            int objectCount = 5;

            for (int i = 0; i < objectCount; i++)
            {
                double radius = 15;
                double x = random.Next((int)radius + 10, gameField.Width - (int)radius - 10);
                double y = random.Next((int)radius + 10, gameField.Height - (int)radius - 10);
                Vector position = new Vector(x, y);

                double angle = random.NextDouble() * 2 * Math.PI;
                double speed = 250;
                Vector velocity = new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed);

                Brush color = new SolidColorBrush(Color.FromRgb(
                    (byte)random.Next(100, 255),
                    (byte)random.Next(100, 255),
                    (byte)random.Next(100, 255)));

                Circle obj = new Circle(position, velocity, radius, color);
                objects.Add(obj);
                GameCanvas.Children.Add(obj.Shape);
            }

            Logger.Log($"Создано {objectCount} шариков", "GAME");
        }

        private void DrawGameField()
        {
            var border = new Rectangle
            {
                Width = gameField.Width,
                Height = gameField.Height,
                Stroke = gameField.BorderBrush,
                StrokeThickness = 3,
                Fill = gameField.Background
            };
            GameCanvas.Children.Add(border);
        }

        private void HandleCollisions()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                for (int j = i + 1; j < objects.Count; j++)
                {
                    IGameObject a = objects[i];
                    IGameObject b = objects[j];

                    Vector delta = a.V_Position - b.V_Position;
                    double distance = delta.Length;
                    double minDistance = a.Radius + b.Radius;

                    if (distance < minDistance)
                    {
                        Vector overlap = delta.Normalize() * (minDistance - distance);
                        a.V_Position += overlap * 0.5;
                        b.V_Position -= overlap * 0.5;

                        UpdateShapePosition(a);
                        UpdateShapePosition(b);

                        Vector normal = delta.Normalize();
                        Vector tangent = new Vector(-normal.Y, normal.X);

                        double v1n = a.V_Velocity.X * normal.X + a.V_Velocity.Y * normal.Y;
                        double v1t = a.V_Velocity.X * tangent.X + a.V_Velocity.Y * tangent.Y;
                        double v2n = b.V_Velocity.X * normal.X + b.V_Velocity.Y * normal.Y;
                        double v2t = b.V_Velocity.X * tangent.X + b.V_Velocity.Y * tangent.Y;

                        double v1n_after = v2n;
                        double v2n_after = v1n;

                        a.V_Velocity = (normal * v1n_after) + (tangent * v1t);
                        b.V_Velocity = (normal * v2n_after) + (tangent * v2t);
                    }
                }
            }
        }

        private void UpdateShapePosition(IGameObject obj)
        {
            if (obj.Shape != null)
            {
                Canvas.SetLeft(obj.Shape, obj.V_Position.X - obj.Radius);
                Canvas.SetTop(obj.Shape, obj.V_Position.Y - obj.Radius);
            }
        }

        private void StartGameLoop()
        {
            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(12);
            gameTimer.Tick += GameLoop;
            lastUpdate = DateTime.Now;
            gameTimer.Start();
            Logger.Log("Игровой цикл запущен", "GAME");
        }

        private void GameLoop(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            double deltaTime = (now - lastUpdate).TotalSeconds;
            lastUpdate = now;

            if (deltaTime > 0.05) deltaTime = 0.05;

            // Обновление позиций шариков
            foreach (var obj in objects)
            {
                obj.Update(deltaTime);
            }

            // ========== 1. Коллизии с границами поля ==========
            foreach (var obj in objects)
            {
                bool bounced = false;

                if (obj.V_Position.X - obj.Radius <= 0)
                {
                    obj.ReflectX();
                    obj.V_Position = new Vector(obj.Radius, obj.V_Position.Y);
                    bounced = true;
                }
                else if (obj.V_Position.X + obj.Radius >= gameField.Width)
                {
                    obj.ReflectX();
                    obj.V_Position = new Vector(gameField.Width - obj.Radius, obj.V_Position.Y);
                    bounced = true;
                }

                if (obj.V_Position.Y - obj.Radius <= 0)
                {
                    obj.ReflectY();
                    obj.V_Position = new Vector(obj.V_Position.X, obj.Radius);
                    bounced = true;
                }
                else if (obj.V_Position.Y + obj.Radius >= gameField.Height)
                {
                    obj.ReflectY();
                    obj.V_Position = new Vector(obj.V_Position.X, gameField.Height - obj.Radius);
                    bounced = true;
                }

                if (bounced)
                {
                    Logger.Log($"Шарик отскочил от границы поля. Новая позиция: ({obj.V_Position.X:F0},{obj.V_Position.Y:F0})", "COLLISION");
                }

                UpdateShapePosition(obj);
            }

            // ========== 2. Коллизии шариков друг с другом ==========
            HandleCollisions();

            // ========== 3. Коллизии шариков с рёбрами (отскок) ==========
            _cellDrawingManager.CheckEdgeCollisions(objects);

            // Небольшое дополнительное обновление для выхода из залипания
            foreach (var obj in objects)
            {
                obj.Update(0.001);
            }

            // ========== 4. Коллизии шариков с вершинами ==========
            _cellDrawingManager.CheckVertexCollisions(objects);

            // ========== 5. Проверка касания рёбер (исчезновение) ==========
            int beforeCount = _cellDrawingManager.EdgeCount;
            _cellDrawingManager.CheckEdgesTouchedByObjects(objects);
            int afterCount = _cellDrawingManager.EdgeCount;

            if (beforeCount != afterCount)
            {
                _edgeDeletionCounter += (beforeCount - afterCount);
                Logger.Log($"Игровой цикл: удалено {beforeCount - afterCount} рёбер. Всего удалено за игру: {_edgeDeletionCounter}", "GAMELOOP");
            }

            // ========== 6. Периодическое логирование состояния (каждые 100 кадров) ==========
            _frameCounter++;
            if (_frameCounter % 100 == 0)
            {
                Logger.Log($"Состояние игры: Вершин={_cellDrawingManager.VertexCount}, Рёбер={_cellDrawingManager.EdgeCount}, Шариков={objects.Count}", "STATE");
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("=== ЗАВЕРШЕНИЕ ИГРЫ ===", "GAME");

            if (gameTimer != null)
            {
                gameTimer.Stop();
            }

            MainWindow newWindow = new MainWindow();
            newWindow.Show();
        }
    }
}