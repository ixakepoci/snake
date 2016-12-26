using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace ObjectMoving
{
    public partial class FormView : Form
    {
        public bool gameOver = false;       
        static Random rnd = new Random();
        public static GameArea gameArea = new GameArea(32, 32);
        public Snake snake = new Snake(gameArea);
        public Snake snake1 = new Snake(gameArea);
        public enum Direction
        {
            Left, Right, Up, Down
        }
        public static int directionsCount = Enum.GetNames(typeof(Direction)).Length;
        public abstract class Brain<T>
        {
            protected int width;
            protected int height;
            protected Dictionary<Tuple<Point, Direction>, T> dict = new Dictionary<Tuple<Point, Direction>, T>();
            public void Set(Point state, Direction direction, T reward)
            {
                dict[Tuple.Create(state, direction)] = reward;
            }
            public T Get(Point state, Direction direction)
            {
                return dict[Tuple.Create(state, direction)];
            }
        }

        public class Snake
        {
            private static decimal Gamma;
            private bool gameOver = false;
            public Cell head = new Cell(1, 1);
            private bool intersects;
            private List<Cell> tail = new List<Cell>();
            public List<Cell> body = new List<Cell>();
            private Point previousPosition = new Point();
            private Quantity Q = new Quantity(gameArea);
            public class Quantity : Brain<decimal>
            {
                public Quantity(GameArea gameArea)
                {
                    width = gameArea.Width;
                    height = gameArea.Height;
                    int direction;
                    for (int i = 0; i < width; i++)
                        for (int j = 0; j < height; j++)
                            for (direction = 0; direction < directionsCount; direction++)
                            {
                                Point state = new Point(i, j);
                                Set(state, (Direction)direction, 0);
                            }
                }

                public void Normalize()
                {
                    decimal Qmax = dict.Values.Max();
                    if (Qmax != 0) foreach (var key in dict.Keys.ToList())
                        {
                            dict[key] = dict[key] / Qmax * 100;
                        }
                }
            }

            private Direction directionToExclude(Point currentState, Point lastState)
            {
                if (currentState.X < lastState.X) return Direction.Right;
                if (currentState.X > lastState.X) return Direction.Left;
                if (currentState.Y < lastState.Y) return Direction.Down;
                if (currentState.Y > lastState.Y) return Direction.Up;
                return Direction.Up;
            }

            private List<Direction> PosibleActionsForState(Point currentState)
            {
                List<Direction> directions = new List<Direction>();

                int direction;
                if (currentState == default(Point))
                {
                    directions.Add(Direction.Down);
                    directions.Add(Direction.Right);
                }
                else if (currentState.X >= gameArea.Width - 1 && currentState.Y >= gameArea.Height - 1)
                {
                    directions.Add(Direction.Up);
                    directions.Add(Direction.Left);
                }
                else if (currentState.X == 0 && currentState.Y >= gameArea.Height - 1)
                {
                    directions.Add(Direction.Up);
                    directions.Add(Direction.Right);
                }
                else if (currentState.X >= gameArea.Width - 1 && currentState.Y == 0)
                {
                    directions.Add(Direction.Left);
                    directions.Add(Direction.Down);
                }
                else if (currentState.X == 0)
                {
                    directions.Add(Direction.Up);
                    directions.Add(Direction.Right);
                    directions.Add(Direction.Down);
                }
                else if (currentState.Y == 0)
                {
                    directions.Add(Direction.Left);
                    directions.Add(Direction.Right);
                    directions.Add(Direction.Down);
                }
                else if (currentState.X >= gameArea.Width - 1)
                {
                    directions.Add(Direction.Up);
                    directions.Add(Direction.Left);
                    directions.Add(Direction.Down);
                }
                else if (currentState.Y >= gameArea.Height - 1)
                {
                    directions.Add(Direction.Left);
                    directions.Add(Direction.Right);
                    directions.Add(Direction.Up);
                }

                else
                    for (direction = 0; direction < directionsCount; direction++)
                        directions.Add((Direction)direction);
                return directions;
            }

            private List<Direction> PosibleActionsForState(Point currentState, Point previousState)
            {
                List<Direction> directions = new List<Direction>();
                var directionToExclude = new Direction();
                if (currentState != previousState)
                {
                    if (currentState.X < previousState.X) directionToExclude = Direction.Right;
                    if (currentState.X > previousState.X) directionToExclude = Direction.Left;
                    if (currentState.Y < previousState.Y) directionToExclude = Direction.Down;
                    if (currentState.Y > previousState.Y) directionToExclude = Direction.Up;
                }
                return PosibleActionsForState(currentState).Where(d => d != directionToExclude).ToList();

            }

            private Point nextState(Point state, Direction action)
            {
                Point nextState = new Point();
                if (action == Direction.Up) { nextState = new Point(state.X, state.Y - 1); }
                if (action == Direction.Down) { nextState = new Point(state.X, state.Y + 1); }
                if (action == Direction.Left) { nextState = new Point(state.X - 1, state.Y); }
                if (action == Direction.Right) { nextState = new Point(state.X + 1, state.Y); }
                return nextState;
            }

            private decimal MaxQaroundState(Point currentState, Point previousState)
            {
                var Qaround = new List<decimal>();
                Point nextPosition = new Point();
                int direction;
                foreach (Direction action in PosibleActionsForState(currentState, previousState))
                {
                    for (direction = 0; direction < directionsCount; direction++)
                    {
                        nextPosition = nextState(currentState, (Direction)direction);
                        if (action == (Direction)direction) Qaround.Add(Q.Get(nextPosition, (Direction)direction));
                    }
                }
                return Qaround.Max();
            }

            private Direction HighestQactionForState(Point currentPosition, Point previousPosition)
            {
                var Qaround = new List<decimal>();

                Point nextPosition = new Point();
                int direction;
                foreach (Direction action in PosibleActionsForState(currentPosition, previousPosition))
                {
                    for (direction = 0; direction < directionsCount; direction++)
                    {
                        nextPosition = nextState(currentPosition, (Direction)direction);
                        if (action == (Direction)direction) Qaround.Add(Q.Get(nextPosition, (Direction)direction));
                    }
                }
                Decimal maxQ = Qaround.Max();
                var actionsWithMax = new List<Direction>();
                foreach (Direction action in PosibleActionsForState(currentPosition, previousPosition))
                {
                    for (direction = 0; direction < directionsCount; direction++)
                    {
                        nextPosition = nextState(currentPosition, (Direction)direction);
                        if (action == (Direction)direction && (double)Math.Abs(Q.Get(nextPosition, action) - maxQ) < 0.000000000000000000000001) actionsWithMax.Add(action);
                    }
                }
                return actionsWithMax.ElementAt(rnd.Next(actionsWithMax.Count));
            }

            private void Training(int number)
            {
                Point foodPosition = new Point(gameArea.food.PosX, gameArea.food.PosY);
                Gamma = 0.3M;
                for (int i = 0; i < number; i++)
                {
                    int currentX = rnd.Next(gameArea.Width);
                    int currentY = rnd.Next(gameArea.Height);
                    Point currentPosition = new Point(rnd.Next(gameArea.Width), rnd.Next(gameArea.Height));
                    Point previousPosition = new Point();
                    do
                    {
                        Point currentState = currentPosition;
                        Direction action;
                        List<Direction> possibleActions = PosibleActionsForState(currentState);
                        action = possibleActions[rnd.Next(possibleActions.Count)];
                        var nextStateQs = new List<decimal>();
                        Point nextPosition = nextState(currentState, action);
                        Point tempPosition = currentPosition;
                        int direction;
                        decimal maxNextQ = MaxQaroundState(nextPosition, currentState);
                        Q.Set(currentState, action, gameArea.R.Get(currentState, action) + Gamma * maxNextQ);
                        if (currentPosition == foodPosition)
                            for (direction = 0; direction < directionsCount; direction++)
                                Q.Set(currentState, (Direction)direction, gameArea.R.Get(currentState, (Direction)direction) + Gamma * maxNextQ);
                        previousPosition = tempPosition;
                        currentPosition = nextPosition;
                        currentState = nextPosition;
                    } while (currentPosition == foodPosition);
                }
            }

            public void SetSnakeNegative()
            {
                foreach (Cell cell in body)
                {
                    for (int direction = 0; direction < directionsCount; direction++)
                    {
                        Point state = new Point(cell.PosX, cell.PosY);
                        decimal currentReward = gameArea.R.Get(state,(Direction)direction);
                        gameArea.R.Set(new Point(cell.PosX, cell.PosY), (Direction)direction, -120);
                    }
                }
            }
            public void SetSnakePositive()
            {
                foreach (Cell cell in body)
                {
                    for (int direction = 0; direction < directionsCount; direction++)
                    {
                        Point state = new Point(cell.PosX, cell.PosY);
                        gameArea.R.Set(new Point(body.Last().PosX, body.Last().PosY), (Direction)direction, 0);
                    }
                }
            }
            public void moveTo()
            {
                if (body.Count() == 0) body.Add(head);
                Point foodPosition = new Point(gameArea.food.PosX, gameArea.food.PosY);
                Direction directionOfMaxForCurrentState = new Direction();
                Point currentPosition = new Point(head.PosX, head.PosY);
                do
                {
                    Point tempPosition = currentPosition;
                    SetSnakeNegative();
                    Training(10000);
                    directionOfMaxForCurrentState = HighestQactionForState(currentPosition, previousPosition);
                    if (directionOfMaxForCurrentState == Direction.Up) head.PosY--;
                    if (directionOfMaxForCurrentState == Direction.Down) head.PosY++;
                    if (directionOfMaxForCurrentState == Direction.Left) head.PosX--;
                    if (directionOfMaxForCurrentState == Direction.Right) head.PosX++;
                    SetSnakePositive(); 
                    currentPosition = new Point(head.PosX, head.PosY);
                    previousPosition = tempPosition;
                    for (int i = 0; i < tail.Count; i++)
                    {
                        Point tmPosition = new Point(tail[i].PosX, tail[i].PosY);
                        tail[i].PosX = previousPosition.X;
                        tail[i].PosY = previousPosition.Y;
                        previousPosition = tmPosition;
                    }
                    foreach (Cell cell in tail) if (head.PosX == cell.PosX && head.PosY == cell.PosY) intersects = true;
                    if (intersects) { gameOver = true; intersects = false; }
                    if (currentPosition == foodPosition)
                    {
                        tail.Add(gameArea.food);
                        body = body.Concat(tail).ToList();
                        gameArea.NewFood();
                        gameArea.R.Initialize();
                        Training(5000);
                    }                  
                    foreach (Cell brick in gameArea.wall) if (head.PosX == brick.PosX && head.PosY == brick.PosY) gameOver = true;
                    if (gameOver)
                    {
                        gameOver = false;
                        body.Clear();
                        tail.Clear();
                        head = new Cell(1, 1);
                        body.Add(head);
                        gameArea.NewFood();
                        gameArea.R.Initialize();
                    }                  

                } while (currentPosition == foodPosition); //Qnormalize();      
            }
            public Snake(GameArea gameArea)
            {
                gameArea.R.Initialize();
            }
        }

        public class Cell
        {
            public int Width = 20;
            public int Height = 20;
            private int posX;
            private int posY;
            public Brush CellColor = Brushes.Black;
            public int PosX { get { return posX; } set { posX = value; } }
            public int PosY { get { return posY; } set { posY = value; } }
            public Cell(int posx, int posy)
            {
                PosX = posx;
                PosY = posy;
            }
            public Cell(int posx,int posy, Brush cellColor)
            {
                PosX = posx;
                PosY = posy;
                CellColor = cellColor;
            }
            public Cell(GameArea gameArea)
            {
                PosX = rnd.Next(1, gameArea.Width-1);
                PosY = rnd.Next(1, gameArea.Height-1);
            }
        }

        public class GameArea
        {
            private int height;
            private int width;
            public Cell food = new Cell(5, 5);
            public List<Cell> wall = new List<Cell>();
            public class Reward : Brain<sbyte>
            {
                public Reward(GameArea gameArea)
                {
                    int direction;
                    for (int i = 0; i < width; i++)
                        for (int j = 0; j < height; j++)
                            for (direction = 0; direction < directionsCount; direction++)
                            {
                                Point state = new Point(i, j);
                                dict[Tuple.Create(state, (Direction)direction)] = 0;
                            }
                }

                public void Initialize()
                {
                    int foodX = gameArea.food.PosX; int foodY = gameArea.food.PosY;
                    int direction;
                    for (int i = 0; i < gameArea.Width; i++)
                        for (int j = 0; j < gameArea.Height; j++)
                            for (direction = 0; direction < directionsCount; direction++)
                            {
                                Point state = new Point(i, j);
                                Set(state, (Direction)direction, 0);
                            }
                    for (int i = 0; i < gameArea.Width - 1; i++)
                        for (int j = 0; j < gameArea.Height - 1; j++)
                        {
                            Point state = new Point(i, j);
                            if (j - 1 < 0) Set(state, Direction.Up, -100);
                            if (i - 1 < 0) Set(state, Direction.Left, -100);
                            if (i + 1 > gameArea.Width-2 ) Set(state, Direction.Right, -100);
                            if (j + 1 > gameArea.Height-2 ) Set(state, Direction.Down, -100);
                            if (i == foodX && j == foodY - 1) Set(state, Direction.Down, 100);
                            if (i == foodX && j == foodY + 1) Set(state, Direction.Up, 100);
                            if (i == foodX + 1 && j == foodY) Set(state, Direction.Left, 100);
                            if (i == foodX - 1 && j == foodY) Set(state, Direction.Right, 100);
                            if (i == foodX && j == foodY)
                                for (direction = 0; direction < directionsCount; direction++)
                                    Set(state, (Direction)direction, 100);
                        }
                    foreach (Cell cell in gameArea.wall)
                    {
                        for (direction = 0; direction < directionsCount; direction++)
                        {
                            Point state = new Point(cell.PosX, cell.PosY);
                            Set(state, (Direction)direction, -100);
                        }
                    }  
                }
            }
            public Reward R = new Reward(gameArea);
            public Pen gameBordersPen = new Pen(Brushes.Black);
            public GameArea(int w, int h)
            {
                width = w;
                height = h;
                for (int i = 0; i < Width; i += rnd.Next(10))
                {
                    for (int j = 0; j < Height; j += rnd.Next(10))
                    {
                        int wallBreak = rnd.Next(Width);
                        wall.Add(new Cell(i, j));
                    }
                }
            }
            public int Height { get { return height; } set { height = value; } }
            public int Width { get { return width; } set { height = value; } }

            public Rectangle GameBorder()
            {
                return new Rectangle(0, 0, Width * food.Width, Height * food.Height); 
            }
            public void NewFood() { foreach (Cell cell in wall) do food = new Cell(this); while (food.PosX == cell.PosX && food.PosY == cell.PosY); }
        }

        public FormView()
        {
            InitializeComponent();
        }

        private void FormView_Paint(object sender, PaintEventArgs e)
        {
            string pathBody = Path.Combine(Application.StartupPath, @"..\..\body.bmp");
            string pathHead = Path.Combine(Application.StartupPath, @"..\..\head.bmp");
            string pathFood = Path.Combine(Application.StartupPath, @"..\..\food.png");
            string pathWall = Path.Combine(Application.StartupPath, @"..\..\wall.png");
            Image bodyImage = Image.FromFile(pathBody);
            Image headImage = Image.FromFile(pathHead);
            Image foodImage = Image.FromFile(pathFood);
            Image wallImage = Image.FromFile(pathWall);
            Color myColor = Color.FromArgb(175, 229, 83);
            SolidBrush myBrush = new SolidBrush(myColor);
            e.Graphics.DrawRectangle(gameArea.gameBordersPen, gameArea.GameBorder());
            e.Graphics.FillRectangle(myBrush, gameArea.GameBorder());
            e.Graphics.DrawImage(foodImage, gameArea.food.PosX * gameArea.food.Width, gameArea.food.PosY * gameArea.food.Height, gameArea.food.Width, gameArea.food.Height);
            foreach (Cell bodyCell in snake.body) e.Graphics.DrawImage(bodyImage, bodyCell.PosX * bodyCell.Width, bodyCell.PosY * bodyCell.Height, bodyCell.Width, bodyCell.Height);
            foreach (Cell bodyCell in snake1.body) e.Graphics.FillRectangle(Brushes.Blue, bodyCell.PosX * bodyCell.Width, bodyCell.PosY * bodyCell.Height, bodyCell.Width, bodyCell.Height);
            foreach (Cell bodyCell in gameArea.wall) e.Graphics.DrawImage(wallImage, bodyCell.PosX * bodyCell.Width, bodyCell.PosY * bodyCell.Height, bodyCell.Width, bodyCell.Height);
        }

        private void tmrMoving_Tick(object sender, EventArgs e)
        {
            tmrMoving.Interval = 1;
            snake.moveTo();
            Invalidate();
        }

 
    }
}
