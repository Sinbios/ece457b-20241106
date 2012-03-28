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
using System.Threading;

namespace ECE457B_Project
{
    /// <summary>
    /// Interaction logic for CarSimulationControl.xaml
    /// </summary>
    public partial class CarSimulationControl : UserControl
    {
        #region Static Variables

        private List<UIElement> CarElementsToDraw;

        private static CarSimulationControl Instance;

        /* Environment Dimensions */
        private static readonly double HeightOfRoadsideInPixels = 10;
        private static readonly double TreeHeightInMeters = 3.5;
        private static readonly double TreeWidthInMeters = (1.0 / 6) * TreeHeightInMeters;
        private static readonly double TreeFoliageHeightInMeters = 0.75 * TreeHeightInMeters;
        private static readonly double TreeFoliageWidthInMeters = (2.0 / 3) * TreeFoliageHeightInMeters;
        private static readonly double RoadHeightInMeters = 6.7;
        private static readonly double RoadLineHeightInMeters = (1.0 / 8) * RoadHeightInMeters;
        private static readonly double RoadLineWidthInMeters = 3 * RoadLineHeightInMeters;

        /* Car Dimensions */
        private static readonly double LowerCarWidthInMeters = 1.812 * RoadHeightInMeters;
        private static readonly double LowerCarHeightInMeters = 0.130 * LowerCarWidthInMeters;
        private static readonly double UpperCarHeightInMeters = LowerCarHeightInMeters;
        private static readonly double CarDoorWidthInMeters = 0.18 * LowerCarWidthInMeters;
        private static readonly double CarRearWindshieldWidthInMeters = 0.137 * LowerCarWidthInMeters;
        private static readonly double CarFrontWindshieldWidthInMeters = 0.172 * LowerCarWidthInMeters;
        private static readonly double CarDoorHandleWidthInMeters = 0.07 * LowerCarWidthInMeters;
        private static readonly double CarDoorHandleHeightInMeters = 0.15 * LowerCarHeightInMeters;
        private static readonly double CarRearLightWidthInMeters = 0.035 * LowerCarWidthInMeters;
        private static readonly double CarRearLightHeightInMeters = 0.55 * LowerCarHeightInMeters;
        private static readonly double CarFrontLightWidthInMeters = 0.055 * LowerCarWidthInMeters;
        private static readonly double CarFrontLightHeightInMeters = CarRearLightHeightInMeters;
        private static readonly double CarTireDiameterInMeters = 1.3 * LowerCarHeightInMeters;
        private static readonly double CarTireLineStrokeThicknessInMeters = 0.13 * CarTireDiameterInMeters;
        private static readonly double CarHubCapDiameterInMeters = 0.545 * CarTireDiameterInMeters;
        private static readonly double CarWindowStrokeThicknessInMeters = 0.2 * UpperCarHeightInMeters;

        /* Car Component Positions */
        private static readonly double CarBackTireDistanceFromLeftEndOfCarInMeters = 0.103 * LowerCarWidthInMeters; //Distance to the left edge of the tire, NOT the center of the tire
        private static readonly double CarFrontTireDistanceFromLeftEndOfCarInMeters = 0.655 * LowerCarWidthInMeters; //Distance to the left edge of the tire
        private static readonly double CarBackDoorDistanceFromLeftEndOfCarInMeters = 0.241 * LowerCarWidthInMeters;
        private static readonly double CarFrontDoorDistanceFromLeftEndOfCarInMeters = CarBackDoorDistanceFromLeftEndOfCarInMeters + CarDoorWidthInMeters;
        private static readonly double CarDoorHandleDistanceFromLeftEndOfDoorInMeters = 0.167 * CarDoorWidthInMeters;
        private static readonly double CarDoorHandleDistanceFromBottomOfLowerCarInMeters = 0.7 * LowerCarHeightInMeters;

        /* Environment Component Positions */
        private static readonly double DistanceBetweenRoadLinesInMeters = 12;
        private static readonly double MinimumMetersBehindLastCar = 2 * LowerCarWidthInMeters;
        private static readonly double MaximumMetersBehindLastCar = 4 * LowerCarWidthInMeters;
        private static readonly double EmptyRoadLengthInFrontOfPilotCarInMeters = 2 * LowerCarWidthInMeters;
        private static readonly double MinEmptyRoadLengthBehindLastCarInMeters = 25;

        #endregion

        #region Member Variables

        private double PixelsPerMeter;

        private double RightMostRoadLineXPos = 0;

        private double TerrainVelocity;

        private double TotalMetersInView;

        private double[] CarTireRotationAnglesInDegrees;

        #endregion

        #region Constructor

        private CarSimulationControl(double width, double height)
        {
            InitializeComponent();

            this.Width = width;
            this.Height = height;

            this.TerrainVelocity = 0;

            this.DrawingCanvas.Children.Clear();
        }

        #endregion

        #region Instance Setup Methods

        public static CarSimulationControl CreateInstance(double width, double height)
        {
            Instance = new CarSimulationControl(width, height);
            Instance.ClipToBounds = true;

            return Instance;
        }

        public static CarSimulationControl GetInstance()
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("Must call CreateInstance before trying to get instance");
            }

            return Instance;
        }

        #endregion

        #region Visualization Methods

        public void InitializeVisualization(Car[] cars)
        {
            this.DrawingCanvas.Children.Clear();

            double distanceBetweenFirstAndLast = this.GetDistanceBetweenFirstAndLastCar(cars);

            this.TotalMetersInView = (distanceBetweenFirstAndLast + (Params.NumCars * LowerCarWidthInMeters) + (EmptyRoadLengthInFrontOfPilotCarInMeters) + (2 * MinEmptyRoadLengthBehindLastCarInMeters));

            this.PixelsPerMeter = Instance.Width / this.TotalMetersInView;

            this.CreateBackground();

            this.CarTireRotationAnglesInDegrees = new double[cars.Length];

            double curCarFrontXPos = this.Width - (EmptyRoadLengthInFrontOfPilotCarInMeters * PixelsPerMeter);
            for (int i = 0; i < cars.Length; i++)
            {
                this.CreateCar(curCarFrontXPos, ((2.0 / 3.0) * this.Height) - HeightOfRoadsideInPixels - ((RoadHeightInMeters * PixelsPerMeter) / 2.0), i);

                if (i != cars.Length - 1)
                {
                    curCarFrontXPos -= (LowerCarWidthInMeters + cars[i + 1].Distance) * PixelsPerMeter;
                }
            }
        }  

        public void UpdateUI(Car[] cars)
        {
            //If we have dipped below our minimum number of meters between first and last car
            double distanceBetweenFirstAndLast = this.GetDistanceBetweenFirstAndLastCar(cars);

            double excessSpaceBehindLastCarInMeters = (this.Width / PixelsPerMeter) - ((cars.Length * LowerCarWidthInMeters) + distanceBetweenFirstAndLast + EmptyRoadLengthInFrontOfPilotCarInMeters);
            if (excessSpaceBehindLastCarInMeters <= MinimumMetersBehindLastCar)
            {
                if (cars[cars.Length - 1].Velocity < cars[0].Velocity)
                {
                    this.TotalMetersInView += (cars[0].Velocity - cars[cars.Length - 1].Velocity) * Params.timeStep;
                }
                else if (cars[cars.Length - 1].Velocity == cars[0].Velocity)
                {
                    this.TotalMetersInView += cars[0].Velocity * Params.timeStep;
                }
            }
            else if (excessSpaceBehindLastCarInMeters >= MaximumMetersBehindLastCar)
            {
                if (cars[cars.Length - 1].Velocity > cars[0].Velocity)
                {
                    this.TotalMetersInView -= (cars[cars.Length - 1].Velocity - cars[0].Velocity) * Params.timeStep;
                }
                else if (cars[cars.Length - 1].Velocity == cars[0].Velocity)
                {
                    this.TotalMetersInView -= cars[0].Velocity * Params.timeStep;
                }
            }

            this.PixelsPerMeter = Instance.Width / this.TotalMetersInView;
   
            this.TerrainVelocity = cars[0].Velocity;

            this.RedrawVisualization(cars);
        }

        #endregion

        #region Helper Methods

        private void RedrawVisualization(Car[] cars)
        {
            //Redraw the visualization
            this.DrawingCanvas.Children.Clear();

            double width = 0, height = 0;

            width = this.Width;
            height = this.Height;

            #region Update Environment

            Rectangle skyRectangle = new Rectangle();
            skyRectangle.Stroke = Brushes.Black;
            skyRectangle.StrokeThickness = 1;
            skyRectangle.Fill = Brushes.SkyBlue;
            skyRectangle.Width = width;
            skyRectangle.Height = height;
            this.DrawingCanvas.Children.Add(skyRectangle);

            Rectangle grassRectangle = new Rectangle();
            grassRectangle.Stroke = Brushes.Black;
            grassRectangle.StrokeThickness = 1;
            grassRectangle.Fill = Brushes.ForestGreen;
            grassRectangle.Width = width;
            grassRectangle.Height = (2.0 / 3.0) * height;
            grassRectangle.SetCurrentValue(Canvas.BottomProperty, 0.0);
            this.DrawingCanvas.Children.Add(grassRectangle);

            Rectangle roadRectangle = new Rectangle();
            roadRectangle.Stroke = Brushes.Black;
            roadRectangle.StrokeThickness = 1;
            roadRectangle.Fill = Brushes.LightGray;
            roadRectangle.Width = width;
            roadRectangle.Height = RoadHeightInMeters * PixelsPerMeter;
            roadRectangle.SetCurrentValue(Canvas.TopProperty, height - grassRectangle.Height + HeightOfRoadsideInPixels);
            this.DrawingCanvas.Children.Add(roadRectangle);

            #endregion

            #region Update Road Lines

            double curRoadLineXPosition = this.RightMostRoadLineXPos - (this.TerrainVelocity * Params.timeStep * PixelsPerMeter);

            if (curRoadLineXPosition - (RoadLineWidthInMeters * PixelsPerMeter / 2.0) + (DistanceBetweenRoadLinesInMeters * PixelsPerMeter) <= width)
            {
                curRoadLineXPosition += DistanceBetweenRoadLinesInMeters * PixelsPerMeter;
            }

            this.RightMostRoadLineXPos = curRoadLineXPosition;

            double roadLineYPosition = grassRectangle.Height - HeightOfRoadsideInPixels - ((RoadHeightInMeters * PixelsPerMeter) / 2.0);

            while (curRoadLineXPosition + ((RoadLineWidthInMeters * PixelsPerMeter) / 2.0) >= 0)
            {
                this.CreateRoadLine(curRoadLineXPosition, roadLineYPosition);

                curRoadLineXPosition -= DistanceBetweenRoadLinesInMeters * PixelsPerMeter;
            }

            #endregion

            #region Update Cars

            this.CreateCars(new Tuple<double, double, Car[]>(this.Width, this.Height, cars));

            #endregion
        }

        private void CreateCars(object dataObj)
        {
            Tuple<double, double, Car[]> data = (Tuple<double, double, Car[]>)dataObj;

            double curCarFrontXPos = data.Item1 - (EmptyRoadLengthInFrontOfPilotCarInMeters * PixelsPerMeter);
            for (int i = 0; i < data.Item3.Length; i++)
            {
                this.CreateCar(curCarFrontXPos, ((2.0 / 3.0) * data.Item2) - HeightOfRoadsideInPixels - ((RoadHeightInMeters * PixelsPerMeter) / 2.0), i, data.Item3[i].Velocity);

                if (i != data.Item3.Length - 1)
                {
                    curCarFrontXPos -= (LowerCarWidthInMeters + (data.Item3[i].Position - data.Item3[i + 1].Position)) * PixelsPerMeter;
                }
            }
        }

        private void CreateBackground()
        {
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

            double curRoadLineXPosition = this.Width - ((DistanceBetweenRoadLinesInMeters * 0.5) * PixelsPerMeter); //From left
            this.RightMostRoadLineXPos = curRoadLineXPosition;

            double roadLineYPosition = grassRectangle.Height - HeightOfRoadsideInPixels - ((RoadHeightInMeters * PixelsPerMeter) / 2.0);

            while (curRoadLineXPosition + ((RoadLineWidthInMeters * PixelsPerMeter) / 2.0) > 0)
            {
                this.CreateRoadLine(curRoadLineXPosition, roadLineYPosition);

                curRoadLineXPosition -= DistanceBetweenRoadLinesInMeters * PixelsPerMeter;
            }
        }

        private void CreateCar(double carFrontXPos, double carLowerBodyBottomYPos, int carIndex, double velocity = -1, List<UIElement> drawInto = null)
        {
            Rectangle lowerBody = new Rectangle();
            lowerBody.Stroke = MainWindow.CarBrushes[carIndex];
            lowerBody.Fill = MainWindow.CarBrushes[carIndex];
            lowerBody.Width = LowerCarWidthInMeters * PixelsPerMeter;
            lowerBody.Height = LowerCarHeightInMeters * PixelsPerMeter;
            lowerBody.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos);
            lowerBody.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width);
            if (drawInto != null)
            {
                drawInto.Add(lowerBody);
            }
            else
            {
                this.DrawingCanvas.Children.Add(lowerBody);
            }

            Rectangle backDoorHandle = new Rectangle();
            backDoorHandle.Stroke = Brushes.Black;
            backDoorHandle.Fill = Brushes.Black;
            backDoorHandle.Width = CarDoorHandleWidthInMeters * PixelsPerMeter;
            backDoorHandle.Height = CarDoorHandleHeightInMeters * PixelsPerMeter;
            backDoorHandle.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width + (CarBackDoorDistanceFromLeftEndOfCarInMeters + CarDoorHandleDistanceFromLeftEndOfDoorInMeters) * PixelsPerMeter);
            backDoorHandle.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos + (CarDoorHandleDistanceFromBottomOfLowerCarInMeters * PixelsPerMeter));
            if (drawInto != null)
            {
                drawInto.Add(backDoorHandle);
            }
            else
            {
                this.DrawingCanvas.Children.Add(backDoorHandle);
            }

            Rectangle frontDoorHandle = new Rectangle();
            frontDoorHandle.Stroke = Brushes.Black;
            frontDoorHandle.Fill = Brushes.Black;
            frontDoorHandle.Width = CarDoorHandleWidthInMeters * PixelsPerMeter;
            frontDoorHandle.Height = CarDoorHandleHeightInMeters * PixelsPerMeter;
            frontDoorHandle.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width + (CarFrontDoorDistanceFromLeftEndOfCarInMeters + CarDoorHandleDistanceFromLeftEndOfDoorInMeters) * PixelsPerMeter);
            frontDoorHandle.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos + (CarDoorHandleDistanceFromBottomOfLowerCarInMeters * PixelsPerMeter));
            if (drawInto != null)
            {
                drawInto.Add(frontDoorHandle);
            }
            else
            {
                this.DrawingCanvas.Children.Add(frontDoorHandle);
            }

            Rectangle rearLight = new Rectangle();
            rearLight.Stroke = Brushes.Black;
            rearLight.Fill = Brushes.Yellow;
            rearLight.Width = CarRearLightWidthInMeters * PixelsPerMeter;
            rearLight.Height = CarRearLightHeightInMeters * PixelsPerMeter;
            rearLight.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos + lowerBody.Height - rearLight.Height);
            rearLight.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width);
            if (drawInto != null)
            {
                drawInto.Add(rearLight);
            }
            else
            {
                this.DrawingCanvas.Children.Add(rearLight);
            }

            Rectangle frontLight = new Rectangle();
            frontLight.Stroke = Brushes.Black;
            frontLight.Fill = Brushes.Yellow;
            frontLight.Width = CarFrontLightWidthInMeters * PixelsPerMeter;
            frontLight.Height = CarFrontLightHeightInMeters * PixelsPerMeter;
            frontLight.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos + lowerBody.Height - frontLight.Height);
            frontLight.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - frontLight.Width);
            if (drawInto != null)
            {
                drawInto.Add(frontLight);
            }
            else
            {
                this.DrawingCanvas.Children.Add(frontLight);
            }

            Ellipse backTire = new Ellipse();
            backTire.Stroke = Brushes.Black;
            backTire.Fill = Brushes.Black;
            backTire.Width = CarTireDiameterInMeters * PixelsPerMeter;
            backTire.Height = CarTireDiameterInMeters * PixelsPerMeter;
            backTire.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos - (backTire.Height / 2.0));
            backTire.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width + (CarBackTireDistanceFromLeftEndOfCarInMeters * PixelsPerMeter));
            if (drawInto != null)
            {
                drawInto.Add(backTire);
            }
            else
            {
                this.DrawingCanvas.Children.Add(backTire);
            }            

            Ellipse backTireHubCap = new Ellipse();
            backTireHubCap.Stroke = Brushes.Black;
            backTireHubCap.Fill = Brushes.Silver;
            backTireHubCap.Width = CarHubCapDiameterInMeters * PixelsPerMeter;
            backTireHubCap.Height = CarHubCapDiameterInMeters * PixelsPerMeter;
            backTireHubCap.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos - (backTireHubCap.Height / 2.0));
            backTireHubCap.SetCurrentValue(Canvas.LeftProperty, ((double)backTire.GetValue(Canvas.LeftProperty)) + (((CarTireDiameterInMeters - CarHubCapDiameterInMeters) / 2.0) * PixelsPerMeter));

            Line backTireLine = new Line();
            backTireLine.Stroke = Brushes.White;
            backTireLine.StrokeThickness = Math.Max(CarTireLineStrokeThicknessInMeters * PixelsPerMeter, 1);
            backTireLine.Width = backTire.Width;
            backTireLine.Height = backTire.Height;
            backTireLine.SetCurrentValue(Canvas.LeftProperty, (double)backTire.GetValue(Canvas.LeftProperty));
            backTireLine.SetCurrentValue(Canvas.BottomProperty, (double)backTire.GetValue(Canvas.BottomProperty));
            backTireLine.X1 = backTire.Width / 2.0;
            backTireLine.Y1 = backTire.Height / 2.0;
            backTireLine.X2 = backTireLine.X1 + (backTire.Width / 2.0);
            backTireLine.Y2 = backTireLine.Y1;

            if (velocity != -1 && carIndex != -1)
            {
                double rotationDueToVelocity = ((velocity * Params.timeStep) / (2 * Math.PI * (backTire.Width / 2.0))) * 360;
                CarTireRotationAnglesInDegrees[carIndex] = (CarTireRotationAnglesInDegrees[carIndex] + rotationDueToVelocity) % 360;
                backTireLine.RenderTransform = new RotateTransform(CarTireRotationAnglesInDegrees[carIndex], backTireLine.X1, backTireLine.Y1);
            }
            if (drawInto != null)
            {
                drawInto.Add(backTireLine);
                drawInto.Add(backTireHubCap);
            }
            else
            {
                this.DrawingCanvas.Children.Add(backTireLine);
                this.DrawingCanvas.Children.Add(backTireHubCap);
            }            

            Ellipse frontTire = new Ellipse();
            frontTire.Stroke = Brushes.Black;
            frontTire.Fill = Brushes.Black;
            frontTire.Width = CarTireDiameterInMeters * PixelsPerMeter;
            frontTire.Height = CarTireDiameterInMeters * PixelsPerMeter;
            frontTire.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos - (frontTire.Height / 2.0));
            frontTire.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width + (CarFrontTireDistanceFromLeftEndOfCarInMeters * PixelsPerMeter));
            if (drawInto != null)
            {
                drawInto.Add(frontTire);
            }
            else
            {
                this.DrawingCanvas.Children.Add(frontTire);
            }

            Ellipse frontTireHubCap = new Ellipse();
            frontTireHubCap.Stroke = Brushes.Black;
            frontTireHubCap.Fill = Brushes.Silver;
            frontTireHubCap.Width = CarHubCapDiameterInMeters * PixelsPerMeter;
            frontTireHubCap.Height = CarHubCapDiameterInMeters * PixelsPerMeter;
            frontTireHubCap.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos - (frontTireHubCap.Height / 2.0));
            frontTireHubCap.SetCurrentValue(Canvas.LeftProperty, ((double)frontTire.GetValue(Canvas.LeftProperty)) + (((CarTireDiameterInMeters - CarHubCapDiameterInMeters) / 2.0) * PixelsPerMeter));            

            Line frontTireLine = new Line();
            frontTireLine.Stroke = Brushes.White;
            frontTireLine.StrokeThickness = Math.Max(CarTireLineStrokeThicknessInMeters * PixelsPerMeter, 1);
            frontTireLine.Width = frontTire.Width;
            frontTireLine.Height = frontTire.Height;
            frontTireLine.SetCurrentValue(Canvas.LeftProperty, (double)frontTire.GetValue(Canvas.LeftProperty));
            frontTireLine.SetCurrentValue(Canvas.BottomProperty, (double)frontTire.GetValue(Canvas.BottomProperty));
            frontTireLine.X1 = frontTire.Width / 2.0;
            frontTireLine.Y1 = frontTire.Height / 2.0;
            frontTireLine.X2 = frontTireLine.X1 + (frontTire.Width / 2.0);
            frontTireLine.Y2 = frontTireLine.Y1;

            if (velocity != -1 && carIndex != -1)
            {
                double rotationDueToVelocity = ((velocity * Params.timeStep) / (2 * Math.PI * (frontTire.Width / 2.0))) * 360;
                CarTireRotationAnglesInDegrees[carIndex] = (CarTireRotationAnglesInDegrees[carIndex] + rotationDueToVelocity) % 360;
                frontTireLine.RenderTransform = new RotateTransform(CarTireRotationAnglesInDegrees[carIndex], frontTireLine.X1, frontTireLine.Y1);
            }

            if (drawInto != null)
            {
                drawInto.Add(frontTireLine);
                drawInto.Add(frontTireHubCap);
            }
            else
            {
                this.DrawingCanvas.Children.Add(frontTireLine);
                this.DrawingCanvas.Children.Add(frontTireHubCap);
            } 

            Rectangle backWindow = new Rectangle();
            backWindow.Stroke = MainWindow.CarBrushes[carIndex];
            backWindow.StrokeThickness = Math.Max(CarWindowStrokeThicknessInMeters * PixelsPerMeter, 1.0);
            backWindow.Fill = Brushes.Black;
            backWindow.Width = CarDoorWidthInMeters * PixelsPerMeter;
            backWindow.Height = UpperCarHeightInMeters * PixelsPerMeter;
            backWindow.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos + lowerBody.Height);
            backWindow.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width + (CarBackDoorDistanceFromLeftEndOfCarInMeters * PixelsPerMeter));
            if (drawInto != null)
            {
                drawInto.Add(backWindow);
            }
            else
            {
                this.DrawingCanvas.Children.Add(backWindow);
            }

            Rectangle frontWindow = new Rectangle();
            frontWindow.Stroke = MainWindow.CarBrushes[carIndex];
            frontWindow.StrokeThickness = Math.Max(CarWindowStrokeThicknessInMeters * PixelsPerMeter, 1.0);
            frontWindow.Fill = Brushes.Black;
            frontWindow.Width = CarDoorWidthInMeters * PixelsPerMeter;
            frontWindow.Height = UpperCarHeightInMeters * PixelsPerMeter;
            frontWindow.SetCurrentValue(Canvas.BottomProperty, carLowerBodyBottomYPos + lowerBody.Height);
            frontWindow.SetCurrentValue(Canvas.LeftProperty, carFrontXPos - lowerBody.Width + (CarFrontDoorDistanceFromLeftEndOfCarInMeters * PixelsPerMeter));
            if (drawInto != null)
            {
                drawInto.Add(frontWindow);
            }
            else
            {
                this.DrawingCanvas.Children.Add(frontWindow);
            }

            Polygon rearWindshield = new Polygon();
            rearWindshield.Stroke = MainWindow.CarBrushes[carIndex];
            rearWindshield.StrokeThickness = Math.Max(CarWindowStrokeThicknessInMeters * PixelsPerMeter, 1.0);
            rearWindshield.Fill = Brushes.Black;
            rearWindshield.Points = new PointCollection()
            {
                new Point((rearWindshield.StrokeThickness / 2.0) + (CarRearWindshieldWidthInMeters * PixelsPerMeter), 0),
                new Point((rearWindshield.StrokeThickness / 2.0) + (CarRearWindshieldWidthInMeters * PixelsPerMeter), backWindow.Height - (rearWindshield.StrokeThickness / 2.0)),
                new Point((rearWindshield.StrokeThickness / 2.0), backWindow.Height - (rearWindshield.StrokeThickness / 2.0))
            };
            rearWindshield.SetCurrentValue(Canvas.LeftProperty, (double)backWindow.GetValue(Canvas.LeftProperty) - (CarRearWindshieldWidthInMeters * PixelsPerMeter));
            rearWindshield.SetCurrentValue(Canvas.BottomProperty, (double)backWindow.GetValue(Canvas.BottomProperty));
            rearWindshield.Height = UpperCarHeightInMeters * PixelsPerMeter;
            rearWindshield.Width = CarRearWindshieldWidthInMeters * PixelsPerMeter;
            rearWindshield.ClipToBounds = true;
            if (drawInto != null)
            {
                drawInto.Add(rearWindshield);
            }
            else
            {
                this.DrawingCanvas.Children.Add(rearWindshield);
            }

            Polygon frontWindshield = new Polygon();
            frontWindshield.Stroke = MainWindow.CarBrushes[carIndex];
            frontWindshield.StrokeThickness = Math.Max(CarWindowStrokeThicknessInMeters * PixelsPerMeter, 1.0);
            frontWindshield.Fill = Brushes.Black;
            frontWindshield.Points = new PointCollection()
            {
                new Point((frontWindshield.StrokeThickness / 2.0), 0),
                new Point((frontWindshield.StrokeThickness / 2.0), frontWindow.Height - (frontWindshield.StrokeThickness / 2.0)),
                new Point((frontWindshield.StrokeThickness / 2.0) + (CarFrontWindshieldWidthInMeters * PixelsPerMeter), frontWindow.Height - (frontWindshield.StrokeThickness / 2.0))
            };
            frontWindshield.SetCurrentValue(Canvas.LeftProperty, (double)frontWindow.GetValue(Canvas.LeftProperty) + (CarFrontWindshieldWidthInMeters * PixelsPerMeter));
            frontWindshield.SetCurrentValue(Canvas.BottomProperty, (double)frontWindow.GetValue(Canvas.BottomProperty));
            frontWindshield.ClipToBounds = true;
            if (drawInto != null)
            {
                drawInto.Add(frontWindshield);
            }
            else
            {
                this.DrawingCanvas.Children.Add(frontWindshield);
            }
        }

        private void CreateRoadLine(double roadLineCenterXPos, double roadLineCenterYPos, List<UIElement> drawInto = null)
        {
            Rectangle roadLine = new Rectangle();
            roadLine.Stroke = Brushes.Black;
            roadLine.StrokeThickness = 1;
            roadLine.Fill = Brushes.White;
            roadLine.Width = RoadLineWidthInMeters * PixelsPerMeter;
            roadLine.Height = RoadLineHeightInMeters * PixelsPerMeter;
            roadLine.SetCurrentValue(Canvas.LeftProperty, roadLineCenterXPos - (roadLine.Width / 2.0));
            roadLine.SetCurrentValue(Canvas.BottomProperty, roadLineCenterYPos - (roadLine.Height / 2.0));

            roadLine.RenderTransform = new SkewTransform(-20, 0);

            if (drawInto != null)
            {
                drawInto.Add(roadLine);
            }
            else
            {
                this.DrawingCanvas.Children.Add(roadLine);
            }
        }

        private void CreateTree(double signBaseCenterXPos, double signBaseYPos, List<UIElement> drawInto = null)
        {
            Rectangle treeTrunk = new Rectangle();
            treeTrunk.Stroke = Brushes.Black;
            treeTrunk.StrokeThickness = 1;
            treeTrunk.Fill = Brushes.Brown;
            treeTrunk.Width = TreeWidthInMeters * PixelsPerMeter;
            treeTrunk.Height = TreeHeightInMeters * PixelsPerMeter;
            treeTrunk.SetCurrentValue(Canvas.BottomProperty, signBaseYPos);
            treeTrunk.SetCurrentValue(Canvas.LeftProperty, signBaseCenterXPos - (treeTrunk.Width / 2.0));
            if (drawInto != null)
            {
                drawInto.Add(treeTrunk);
            }
            else
            {
                this.DrawingCanvas.Children.Add(treeTrunk);
            }

            Ellipse treeFoliage = new Ellipse();
            treeFoliage.Stroke = Brushes.Black;
            treeFoliage.StrokeThickness = 1;
            treeFoliage.Fill = Brushes.ForestGreen;
            treeFoliage.Width = TreeFoliageWidthInMeters * PixelsPerMeter;
            treeFoliage.Height = TreeFoliageHeightInMeters * PixelsPerMeter;
            treeFoliage.SetCurrentValue(Canvas.BottomProperty, signBaseYPos + (TreeHeightInMeters * PixelsPerMeter) - ((TreeFoliageHeightInMeters * PixelsPerMeter) / 2.0));
            treeFoliage.SetCurrentValue(Canvas.LeftProperty, signBaseCenterXPos - ((TreeFoliageWidthInMeters * PixelsPerMeter) / 2.0));
            if (drawInto != null)
            {
                drawInto.Add(treeFoliage);
            }
            else
            {
                this.DrawingCanvas.Children.Add(treeFoliage);
            }
        }

        private double GetDistanceBetweenFirstAndLastCar(Car[] cars)
        {
            double distanceBetweenFirstAndLast = 0;

            for (int i = cars.Length - 1; i > 0; i--)
            {

                distanceBetweenFirstAndLast += cars[i].Distance;
            }

            return distanceBetweenFirstAndLast;
        }

        #endregion
    }
}
