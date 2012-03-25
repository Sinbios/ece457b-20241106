using System;
using System.Collections.Generic;
using System.Linq;
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

namespace ECE457B_Project
{
    /// <summary>
    /// Interaction logic for CarSimulationControl.xaml
    /// </summary>
    public partial class CarSimulationControl : UserControl
    {
        private double PixelsPerMeter;

        private List<UIElement[]> Cars = null;
        private List<UIElement[]> Trees = null; //Arrays of length 2 -- one UIElement for the sign post, another for the sign
        private List<Rectangle> RoadLines = null;

        private static double EmptyRoadLengthAtEachEndInPixels = 150;

        private static double DistanceBetweenRoadLinesInMeters = 9;
        private static double DistanceBetweenTreesInMeters = 25;

        private static double HeightOfRoadsideInPixels = 10;

        private static double TreeHeightInMeters = 3.5;
        private static double TreeWidthInMeters = (1.0 / 6) * TreeHeightInMeters;
        private static double TreeFoliageHeightInMeters = 0.75 * TreeHeightInMeters;
        private static double TreeFoliageWidthInMeters = (2.0 / 3) * TreeFoliageHeightInMeters;

        private static double LowerCarLengthInMeters = 4.1;
        private static double LowerCarHeightInMeters = 0.137 * LowerCarLengthInMeters;
        private static double CarDoorLengthInMeters = 0.207 * LowerCarLengthInMeters;

        private static double RoadLineLengthInMeters = (1.0 / 3) * LowerCarLengthInMeters;
        private static double RoadLineHeightInMeters = (1.0 / 3) * RoadLineLengthInMeters;

        private static double RoadHeightInMeters = 6.7;

        private static CarSimulationControl Instance;

        private CarSimulationControl(double width, double height)
        {
            InitializeComponent();

            this.Width = width;
            this.Height = height;
        }

        public static CarSimulationControl GetInstance()
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("Must call CreateInstance before trying to get instance");
            }

            return Instance;
        }

        public static CarSimulationControl CreateInstance(double width, double height)
        {
            Instance = new CarSimulationControl(width, height);

            Instance.PixelsPerMeter = Instance.Width / ((3 * LowerCarLengthInMeters) + (2 * 10) /*10m between each car*/ + (2 * (20)) /*20m of space at each end*/);
            Console.WriteLine("Pixels per meter = {0:0.00}", Instance.PixelsPerMeter);
            Instance.DrawBackground();

            return Instance;
        }

        public void DrawBackground()
        {
            this.DrawingCanvas.Children.Clear();

            Rectangle skyRectangle = new Rectangle();
            skyRectangle.Stroke = Brushes.Black;
            skyRectangle.StrokeThickness = 1;
            skyRectangle.Fill = Brushes.SkyBlue;
            skyRectangle.Width = this.Width;
            skyRectangle.Height = this.Height;
            this.DrawingCanvas.Children.Add(skyRectangle);

            Rectangle grassRectangle = new Rectangle();
            grassRectangle.Stroke = Brushes.Black;
            grassRectangle.StrokeThickness = 1;
            grassRectangle.Fill = Brushes.ForestGreen;
            grassRectangle.Width = this.Width;
            grassRectangle.Height = (2.0 / 3.0) * this.Height;
            grassRectangle.SetCurrentValue(Canvas.BottomProperty, 0.0);
            this.DrawingCanvas.Children.Add(grassRectangle);

            Rectangle roadRectangle = new Rectangle();
            roadRectangle.Stroke = Brushes.Black;
            roadRectangle.StrokeThickness = 1;
            roadRectangle.Fill = Brushes.LightGray;
            roadRectangle.Width = this.Width;
            roadRectangle.Height = RoadHeightInMeters * PixelsPerMeter;
            roadRectangle.SetCurrentValue(Canvas.TopProperty, this.Height - grassRectangle.Height + HeightOfRoadsideInPixels);
            this.DrawingCanvas.Children.Add(roadRectangle);

            Trees = new List<UIElement[]>();
            double curTreeXPosition = this.Width - ((DistanceBetweenTreesInMeters * 0.5) * PixelsPerMeter); //From left
            double treeYPosition = grassRectangle.Height - (HeightOfRoadsideInPixels / 2.0); //From bottom

            while (curTreeXPosition + ((TreeFoliageWidthInMeters * PixelsPerMeter) / 2.0) > 0)
            {
                Trees.Insert(0, this.CreateTree(curTreeXPosition, treeYPosition));

                curTreeXPosition -= DistanceBetweenTreesInMeters * PixelsPerMeter;
            }

            RoadLines = new List<Rectangle>();
            double curRoadLineXPosition = this.Width - ((DistanceBetweenRoadLinesInMeters * 0.5) * PixelsPerMeter); //From left
            double roadLineYPosition = grassRectangle.Height - HeightOfRoadsideInPixels - ((RoadHeightInMeters * PixelsPerMeter) / 2.0);

            while (curRoadLineXPosition + ((RoadLineLengthInMeters * PixelsPerMeter) / 2.0) > 0)
            {
                RoadLines.Insert(0, this.CreateRoadLine(curRoadLineXPosition, roadLineYPosition));

                curRoadLineXPosition -= DistanceBetweenRoadLinesInMeters * PixelsPerMeter;
            }
        }

        private Rectangle CreateRoadLine(double roadLineCenterXPos, double roadLineCenterYPos)
        {
            Rectangle roadLine = new Rectangle();
            roadLine.Stroke = Brushes.Black;
            roadLine.StrokeThickness = 1;
            roadLine.Fill = Brushes.White;
            roadLine.Width = RoadLineLengthInMeters * PixelsPerMeter;
            roadLine.Height = RoadLineHeightInMeters * PixelsPerMeter;
            roadLine.SetCurrentValue(Canvas.LeftProperty, roadLineCenterXPos - (roadLine.Width / 2.0));
            roadLine.SetCurrentValue(Canvas.BottomProperty, roadLineCenterYPos - (roadLine.Height / 2.0));

            roadLine.RenderTransform = new SkewTransform(-20, 0);

            this.DrawingCanvas.Children.Add(roadLine);

            return roadLine;
        }

        private UIElement[] CreateTree(double signBaseCenterXPos, double signBaseYPos)
        {
            UIElement[] treeElements = new UIElement[2];

            Rectangle treeTrunk = new Rectangle();
            treeTrunk.Stroke = Brushes.Black;
            treeTrunk.StrokeThickness = 1;
            treeTrunk.Fill = Brushes.Brown;
            treeTrunk.Width = TreeWidthInMeters * PixelsPerMeter;
            treeTrunk.Height = TreeHeightInMeters * PixelsPerMeter;
            treeTrunk.SetCurrentValue(Canvas.BottomProperty, signBaseYPos);
            treeTrunk.SetCurrentValue(Canvas.LeftProperty, signBaseCenterXPos - (treeTrunk.Width / 2.0));
            treeElements[0] = treeTrunk;
            this.DrawingCanvas.Children.Add(treeTrunk);

            Ellipse treeFoliage = new Ellipse();
            treeFoliage.Stroke = Brushes.Black;
            treeFoliage.StrokeThickness = 1;
            treeFoliage.Fill = Brushes.ForestGreen;
            treeFoliage.Width = TreeFoliageWidthInMeters * PixelsPerMeter;
            treeFoliage.Height = TreeFoliageHeightInMeters * PixelsPerMeter;
            treeFoliage.SetCurrentValue(Canvas.BottomProperty, signBaseYPos + (TreeHeightInMeters * PixelsPerMeter) - ((TreeFoliageHeightInMeters * PixelsPerMeter) / 2.0));
            treeFoliage.SetCurrentValue(Canvas.LeftProperty, signBaseCenterXPos - ((TreeFoliageWidthInMeters * PixelsPerMeter) / 2.0));
            treeElements[1] = treeFoliage;
            this.DrawingCanvas.Children.Add(treeFoliage);

            return treeElements;
        }
    }
}
