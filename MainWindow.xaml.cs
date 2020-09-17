using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace SnakeGameWPFTutorial
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer gameTickTimer = new DispatcherTimer();
        private int currentScore = 0;
        const int MaxHighScoreEntry = 5;

        private Random rnd = new Random();

        const int snakeSquareSize = 20;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;

        private enum SnakeDirection { Up, Down, Left, Right}
        private SnakeDirection snakeDirection = SnakeDirection.Right;
        private int snakeLength;

        UIElement snakeFood = null;
        private SolidColorBrush snakeBodyBrush = Brushes.Green;
        private SolidColorBrush snakeHeadBrush = Brushes.GreenYellow;
        private List<SnakePart> snakeParts = new List<SnakePart>();
        private SolidColorBrush foodBrush = Brushes.Red;

        public ObservableCollection<SnakeHighScore> HighScores { get; set; } = new ObservableCollection<SnakeHighScore>();
        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GamerTickTimer_Tick;
            LoadHighScores();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
        }

        private void StartNewGame()
        {
            borWelcomeMessage.Visibility = Visibility.Collapsed;
            borEndGame.Visibility = Visibility.Collapsed;
            borHighScoreList.Visibility = Visibility.Collapsed;

            //checks for any remaining snake parts from the previous game
            //and removes those parts
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.uiElement != null)
                    GameArea.Children.Remove(snakePart.uiElement);
            }
            snakeParts.Clear();

            //checks if there are any leftover food parts from the previous game and removes them
            if (snakeFood != null)
                GameArea.Children.Remove(snakeFood);

            //Resetting values
            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Down;
            snakeParts.Add(new SnakePart() { Position = new Point(snakeSquareSize * 5, snakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            DrawSnake();
            DrawFood();
            UpdateGameStatus();

            gameTickTimer.IsEnabled = true;
        }

        private void GamerTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY =0;
            bool nextIsOdd = false;

            //loop for drawing checkered background
            while (doneDrawingBackground == false)
            {
                //creates a new recretangle each iteration of the loop
                Rectangle rect = new Rectangle
                {
                    Height = snakeSquareSize,
                    Width = snakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.Black
                };

                //adds the new rectangle to the canvas
                GameArea.Children.Add(rect);
                //sets the postion of the canvas each iteration of the loop
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                //toggles the nextisodd bool value to allow alternation between white and black squares
                nextIsOdd = !nextIsOdd;
                nextX += snakeSquareSize;
                //if the nextX variable reaches the width of the game area we reset the 
                //next variable to create a new row
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += snakeSquareSize;
                    //toggles values so that the color alternates instead of a board with black and white lines
                    nextIsOdd = !nextIsOdd;
                }

                //stops drawing once the nextY int value reaches height of canvas
                if (nextY >= GameArea.ActualHeight)
                    doneDrawingBackground = true;
            }
        }

        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.uiElement == null)
                {
                    snakePart.uiElement = new Rectangle
                    {
                        Height = snakeSquareSize,
                        Width = snakeSquareSize,
                        Fill = snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush
                    };

                    GameArea.Children.Add(snakePart.uiElement);
                    Canvas.SetTop(snakePart.uiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.uiElement, snakePart.Position.X);
                }
            }
        }

        private void MoveSnake()
        {
            //Remove the last part of the snake, in preparation for the next part to be added
            //Removes the head of the snake to color the body
            if (snakeParts.Count >= snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].uiElement);
                snakeParts.RemoveAt(0);
            }

            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.uiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            //setting the position of the snake head
            //and then setting the direction in which the snake can go
            SnakePart snakeHead = snakeParts[^1];//snakeparts.Count -1 == ^1
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;

            switch (snakeDirection)
            {
                case SnakeDirection.Up:
                    nextY -= snakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += snakeSquareSize;
                    break;
                case SnakeDirection.Left:
                    nextX -= snakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += snakeSquareSize;
                    break;
            }

            //now adds the head to the body of the snake
            snakeParts.Add(new SnakePart
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            DrawSnake();
            CheckForCollision();
        }

        private Point GetNextFoodPosition()
        {
            int maxX = (int)GameArea.ActualWidth / snakeSquareSize;
            int maxY = (int)GameArea.ActualHeight / snakeSquareSize;
            int posX = rnd.Next(0, maxX) * snakeSquareSize;
            int posY = rnd.Next(0, maxY) * snakeSquareSize;

            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.Position.X == posX && snakePart.Position.Y == posY)
                    return GetNextFoodPosition();   
            }

            return new Point(posX, posY);
        }

        private void DrawFood()
        {
            Point foodPos = GetNextFoodPosition();

            snakeFood = new Ellipse
            {
                Height = snakeSquareSize,
                Width = snakeSquareSize,
                Fill = foodBrush
            };

            GameArea.Children.Add(snakeFood);
            Canvas.SetLeft(snakeFood, foodPos.X);
            Canvas.SetTop(snakeFood, foodPos.Y);
            
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                    if(snakeDirection != SnakeDirection.Down)
                        snakeDirection = SnakeDirection.Up;
                    break;
                case Key.Down:
                    if(snakeDirection != SnakeDirection.Up)
                        snakeDirection = SnakeDirection.Down;
                    break;
                case Key.Left:
                    if(snakeDirection != SnakeDirection.Right)
                        snakeDirection = SnakeDirection.Left;
                    break;
                case Key.Right:
                    if(snakeDirection != SnakeDirection.Left)
                        snakeDirection = SnakeDirection.Right;
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
            }
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }

        private void CheckForCollision()
        {
            SnakePart snakeHead = snakeParts[^1];

            if (snakeHead.Position.X == Canvas.GetLeft(snakeFood) && snakeHead.Position.Y == Canvas.GetTop(snakeFood))
            {
                EatTheFood();
                return;
            }

            if (snakeHead.Position.X < 0 || snakeHead.Position.X == GameArea.ActualWidth ||
                snakeHead.Position.Y < 0 || snakeHead.Position.Y == GameArea.ActualHeight )
                    GameOver();

            foreach (SnakePart snakePart in snakeParts.Take(snakeParts.Count - 1))
            {
                if (snakePart.Position.X == snakeHead.Position.X && snakePart.Position.Y == snakeHead.Position.Y)
                    GameOver();
            }
        }

        private void EatTheFood()
        {
            currentScore++;
            snakeLength++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(snakeFood);
            DrawFood();
            UpdateGameStatus();
        }

        private void UpdateGameStatus() 
        {
            this.txtStatusScore.Text = currentScore.ToString();
            this.txtStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        } 

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnHighList_Click(object sender, RoutedEventArgs e)
        {
            borHighScoreList.Visibility = Visibility.Visible;
            borWelcomeMessage.Visibility = Visibility.Collapsed;
        }

        private void BtnAddToHighScoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;

            this.HighScores.Insert(newIndex,new SnakeHighScore()
            {
                PlayerName = txtPlayerName.Text,
                Score = currentScore
            });

            while (this.HighScores.Count > MaxHighScoreEntry)
                this.HighScores.RemoveAt(MaxHighScoreEntry);

            SaveHighScores();
            txtPlayerName.Text = null;
            borNewHighScore.Visibility = Visibility.Collapsed;
            borHighScoreList.Visibility = Visibility.Visible;

        }

        private void LoadHighScores()
        {
            if (File.Exists("snake_highscores.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighScore>));
                using (Stream reader = new FileStream("snake_highscores.xml", FileMode.Open))
                {
                    List<SnakeHighScore> tempList = (List<SnakeHighScore>)serializer.Deserialize(reader);
                    this.HighScores.Clear();
                    foreach (var item in tempList.OrderByDescending(x => x.Score))
                        this.HighScores.Add(item);
                }
  
            }
        }

        private void SaveHighScores()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighScore>));
            using (Stream writer = new FileStream("snake_highscores.xml", FileMode.Create))
            {
                serializer.Serialize(writer, this.HighScores);
            }
        }

        private void GameOver()
        {
            bool isNewHighScore = false;
            if (currentScore > 0)
            {
                int lowestHighScore = (this.HighScores.Count > 0 ? this.HighScores.Min(x => x.Score) : 0);
                if (currentScore > lowestHighScore || this.HighScores.Count < MaxHighScoreEntry)
                {
                    borNewHighScore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighScore = true;
                }
            }

            if (isNewHighScore == false)
            {
                borEndGame.Visibility = Visibility.Visible;
                txtFinalScore.Text = currentScore.ToString();
                
            }
            gameTickTimer.IsEnabled = false;
        }
    }
}
