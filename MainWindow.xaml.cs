using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TMP_RGZ
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int num = 0; 
                double speed = 0;

                if (!int.TryParse(AmountObjects.Text, out num) || num <= 0)
                {
                    throw new Exception("Неверно задано количество шариков!");
                }
                

                if (!double.TryParse(Speed.Text, out speed) || speed <= 0)
                {
                    throw new Exception("Неверно задана скорость!");
                }

                GameWindow gamewindow = new GameWindow(num, speed);
                gamewindow.Show();

                Window currentMainWindow = Window.GetWindow(this);
                currentMainWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }

        }
    }
}