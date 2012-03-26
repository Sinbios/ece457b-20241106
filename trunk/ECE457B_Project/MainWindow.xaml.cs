using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;

namespace ECE457B_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Static Variables

        ObservableDataSource<Point>[] VelocityDataPoints;
        ObservableDataSource<Point>[] AccelerationDataPoints;

        public static Brush[] CarBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue };
        public static Color[] CarColors = new Color[] { Colors.Red, Colors.Green, Colors.Blue };        

        private static ChartPlotter AccelerationChartPlotter = null;
        private static ChartPlotter VelocityChartPlotter = null;
        private static double PlotLineThickness = 2;

        private static Thread SimulationThread = null;
        private static bool PauseSimulation = false;
        private static bool ParameterUpdated = false;
        private static bool Converged = false;

        private static string PerformanceControlStartString = "Start Simulation";
        private static string PerformanceControlPauseString = "Pause Simulation";
        private static string PerformanceControlResumeString = "Resume Simulation";

        private const int NumCars = 3;

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            this.WindowState = System.Windows.WindowState.Maximized;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            this.Height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;

            this.SimulationInfoRow.Height = new GridLength((this.Height - this.TitleRow.Height.Value) * (4.75 / 8));
            this.SimulationPerformanceRow.Height = new GridLength(this.Height - this.SimulationInfoRow.Height.Value - this.TitleRow.Height.Value);

            this.SystemVisualizationColumn.Width = new GridLength(this.Width - this.ParameterColumn.Width.Value);

            CarSimulationControl carSimInstance = CarSimulationControl.CreateInstance(this.SystemVisualizationColumn.Width.Value, this.SimulationPerformanceRow.Height.Value);
            carSimInstance.SetCurrentValue(Grid.ColumnProperty, 1);
            carSimInstance.SetCurrentValue(Grid.RowProperty, 1);
            this.SimulationParameterAndVisualiationGrid.Children.Add(carSimInstance);

            this.InitialVelocityTextBox.Text = String.Format("{0:0.00}", Params.vInitial);
            this.DesiredVelocityTextBox.Text = String.Format("{0:0.00}", Params.vDesired);

            this.InitialDistance1TextBox.Text = String.Format("{0:0.00}", Params.dInitial1);
            this.InitialDistance2TextBox.Text = String.Format("{0:0.00}", Params.dInitial2);
            this.DesiredDistanceTextBox.Text = String.Format("{0:0.00}", Params.dDesired);

            this.ConvergencePercentTextBox.Text = String.Format("{0:0.00}", Params.convergencePercent * 100.0);

            this.PerformanceControlButtonText.Text = PerformanceControlStartString;

            //Set up velocity graph
            VelocityChartPlotter = new ChartPlotter();
            VelocityChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            VelocityChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            VelocityChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            VelocityChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            VelocityDataPoints = new ObservableDataSource<Point>[NumCars];
            for (int i = 0; i < NumCars; i++)
            {
                VelocityDataPoints[i] = new ObservableDataSource<Point>();
                VelocityChartPlotter.AddLineGraph(VelocityDataPoints[i], new Pen(CarBrushes[i], PlotLineThickness), new PenDescription(String.Format("V_{0}", i)));
            }
            this.VelocityGraphGrid.Children.Add(VelocityChartPlotter);            

            /*
            var testDataSourceX = new EnumerableDataSource<int>(new List<int> { 1, 2, 3 });
            testDataSourceX.SetXMapping(x => x);

            var testDataSourceY = new EnumerableDataSource<int>(new List<int> { 10, 11, 12});
            testDataSourceY.SetYMapping(y => y);

            CompositeDataSource testData = new CompositeDataSource(testDataSourceX, testDataSourceY);

            VelocityChartPlotter.AddLineGraph(testData, new Pen(Brushes.Blue, 5), new CirclePointMarker { Size = 10.0, Fill = Brushes.Red }, new PenDescription("Test data"));
            VelocityChartPlotter.Visible = new Rect(new Point(0, 0), new Size(20, 20));
            */

            AccelerationChartPlotter = new ChartPlotter();
            AccelerationChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            AccelerationChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            AccelerationChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            AccelerationChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            AccelerationDataPoints = new ObservableDataSource<Point>[NumCars];
            for (int i = 0; i < NumCars; i++)
            {
                AccelerationDataPoints[i] = new ObservableDataSource<Point>();
                AccelerationChartPlotter.AddLineGraph(AccelerationDataPoints[i], new Pen(CarBrushes[i], PlotLineThickness), new PenDescription(String.Format("A_{0}", i)));
            }
            this.AccelerationGraphGrid.Children.Add(AccelerationChartPlotter);
        }

        #endregion

        #region Simulation Mainloop

        private void SimulationMainLoop()
        {
            try
            {
                Car[] cars = new Car[] { new Car(0), new Car(1), new Car(2) };

                Controller fuzzyController = Controller.GetInstance();

                ParameterUpdated = false;
                Converged = false;

                Dispatcher.Invoke((Action)delegate
                {
                    CarSimulationControl.GetInstance().InitializeVisualization(cars);
                    fuzzyController.Reset();

                    ClearPointSources(VelocityDataPoints);
                    ClearPointSources(AccelerationDataPoints);
                });

                TimeSpan totalRuntime = TimeSpan.FromSeconds(0);
                PauseSimulation = false;

                /*
                List<double> timeStepData = new List<double>();

                List<double>[] carsVelocityData = new List<double>[cars.Length];
                InitializeLists(carsVelocityData);

                List<double>[] carsAccelerationData = new List<double>[cars.Length];
                InitializeLists(carsAccelerationData);
                 */

                while (true)
                {
                    if (PauseSimulation)
                    {
                        continue;
                    }

                    if (Converged)
                    {
                        //We only get in here if the user resumes system simulation
                        Converged = false;

                        Dispatcher.Invoke((Action)delegate
                        {
                            this.SystemConvergedText.Visibility = System.Windows.Visibility.Hidden;
                        });

                        totalRuntime = TimeSpan.FromSeconds(0);
                    }

                    if (ParameterUpdated)
                    {
                        lock (this)
                        {
                            ParameterUpdated = false;
                        }

                        totalRuntime = TimeSpan.FromSeconds(0);
                        Dispatcher.Invoke((Action)delegate
                        {
                            fuzzyController.Reset();
                        });
                    }

                    Dispatcher.Invoke((Action)delegate
                    {
                        this.UpdateGraphPoints(totalRuntime, cars);
                    });

                    for (int i = 0; i < cars.Length; i++)
                    {
                        cars[i].Acceleration = fuzzyController.GetOutput(cars[i].Distance - Params.dDesired, cars[i].Velocity);
                        cars[i].Position = cars[i].Position + cars[i].Velocity * Params.timeStep + 0.5 * cars[i].Acceleration * Params.timeStep * Params.timeStep;
                        if (i != 0)
                        {
                            cars[i].Distance = Math.Max(0, cars[i - 1].Position - cars[i].Position);
                        }
                        cars[i].Velocity = cars[i].Velocity + cars[i].Acceleration * Params.timeStep;
                    }

                    totalRuntime += TimeSpan.FromSeconds(Params.timeStep);

                    Dispatcher.Invoke((Action)delegate
                    {
                        CarSimulationControl.GetInstance().UpdateUI(cars);
                        this.TotalSystemRuntimeLabelText.Text = String.Format("{0:0.00} seconds", totalRuntime.TotalSeconds);
                    });

                    //Check convergence
                    if (ApproximatelyEqual(cars[1].Distance, Params.dDesired, Params.convergencePercent)
                        && ApproximatelyEqual(cars[2].Distance, Params.dDesired, Params.convergencePercent)
                        && ApproximatelyEqual(cars[0].Velocity, Params.vDesired, Params.convergencePercent)
                        && ApproximatelyEqual(cars[1].Velocity, Params.vDesired, Params.convergencePercent)
                        && ApproximatelyEqual(cars[2].Velocity, Params.vDesired, Params.convergencePercent))
                    {
                        Converged = true;                        
                        Dispatcher.Invoke((Action)delegate
                        {
                            this.SystemConvergedText.Visibility = System.Windows.Visibility.Visible;
                            this.PerformanceControlButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); //Pause the simulation
                        });
                    }        

                    Thread.Sleep(TimeSpan.FromSeconds(Params.timeStep));
                }
            }
            catch (ThreadAbortException)
            {

            }
        }

        #endregion

        #region UI Event Handlers

        private void ConvergencePercentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newConvergencePercent;
            if (Double.TryParse(this.ConvergencePercentTextBox.Text, out newConvergencePercent))
            {
                if (newConvergencePercent <= 100 && newConvergencePercent > 0)
                {
                    Params.convergencePercent = newConvergencePercent / 100.0;

                    lock (this)
                    {
                        ParameterUpdated = true;
                    }
                }
            }
        }

        private void DesiredVelocityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newDesiredVelocity;
            if (Double.TryParse(this.DesiredVelocityTextBox.Text, out newDesiredVelocity))
            {
                Params.vDesired = newDesiredVelocity;

                lock (this)
                {
                    ParameterUpdated = true;
                }
            }
        }

        private void DesiredDistanceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newDesiredDistance;
            if (Double.TryParse(this.DesiredDistanceTextBox.Text, out newDesiredDistance))
            {
                if (newDesiredDistance > 0)
                {
                    Params.dDesired = newDesiredDistance;

                    lock (this)
                    {
                        ParameterUpdated = true;
                    }
                }
            }
        }

        private void EndSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (SimulationThread != null && SimulationThread.IsAlive)
            {
                SimulationThread.Abort();
                CarSimulationControl.GetInstance().InitializeVisualization(new Car[] { new Car(0), new Car(1), new Car(2) });
            }

            this.InitialVelocityTextBox.IsEnabled = true;
            this.InitialDistance1TextBox.IsEnabled = true;
            this.InitialDistance2TextBox.IsEnabled = true;

            this.PerformanceControlButtonText.Text = PerformanceControlStartString;
            this.EndSimulationButton.Visibility = System.Windows.Visibility.Hidden;
            this.TotalSystemRuntimeLabel.Visibility = System.Windows.Visibility.Hidden;
            this.SystemConvergedText.Visibility = System.Windows.Visibility.Hidden;
        }

        private void InitialDistance1TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newDesiredDistance1;
            if (Double.TryParse(this.InitialDistance1TextBox.Text, out newDesiredDistance1))
            {
                if (newDesiredDistance1 > 0)
                {
                    Params.dInitial1 = newDesiredDistance1;
                    CarSimulationControl.GetInstance().InitializeVisualization(new Car[] { new Car(0), new Car(1), new Car(2) });
                }
            }
        }

        private void InitialDistance2TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newDesiredDistance2;
            if (Double.TryParse(this.InitialDistance2TextBox.Text, out newDesiredDistance2))
            {
                if (newDesiredDistance2 > 0)
                {
                    Params.dInitial2 = newDesiredDistance2;
                    CarSimulationControl.GetInstance().InitializeVisualization(new Car[] { new Car(0), new Car(1), new Car(2) });
                }
            }
        }

        private void PerformanceControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.PerformanceControlButtonText.Text.Equals(PerformanceControlStartString))
            {
                if (!this.ReadInSystemParameters())
                {
                    return;
                }

                this.InitialVelocityTextBox.IsEnabled = false;
                this.InitialDistance1TextBox.IsEnabled = false;
                this.InitialDistance2TextBox.IsEnabled = false;

                this.TotalSystemRuntimeLabel.Visibility = System.Windows.Visibility.Visible;
                this.TotalSystemRuntimeLabelText.Text = "0 seconds";

                if (SimulationThread != null && SimulationThread.IsAlive)
                {
                    //Ensure the last simulation is ended
                    SimulationThread.Abort();
                    SimulationThread.Join();
                }

                this.PerformanceControlButtonText.Text = PerformanceControlPauseString;
                this.EndSimulationButton.Visibility = System.Windows.Visibility.Visible;

                SimulationThread = new Thread(new ThreadStart(this.SimulationMainLoop));
                SimulationThread.Start();
            }
            else if (this.PerformanceControlButtonText.Text.Equals(PerformanceControlPauseString))
            {
                PauseSimulation = true;
                this.PerformanceControlButtonText.Text = PerformanceControlResumeString;
            }
            else if (this.PerformanceControlButtonText.Text.Equals(PerformanceControlResumeString))
            {
                PauseSimulation = false;
                this.PerformanceControlButtonText.Text = PerformanceControlPauseString;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SimulationThread != null && SimulationThread.IsAlive)
            {
                SimulationThread.Abort();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CarSimulationControl.GetInstance().InitializeVisualization(new Car[] { new Car(0), new Car(1), new Car(2) });
        }

        #endregion

        #region Helper Methods

        private static bool ApproximatelyEqual(double toCheck, double desiredValue, double acceptablePercentDeviation)
        {
            if (Math.Abs(desiredValue - toCheck) <= Math.Abs(desiredValue * acceptablePercentDeviation))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void ClearPointSources(ObservableDataSource<Point>[] pointSourcesToClear)
        {
            foreach (ObservableDataSource<Point> pointSource in pointSourcesToClear)
            {
                pointSource.Collection.Clear();
            }
        }

        private void UpdateGraphPoints(TimeSpan currentRuntime, Car[] cars)
        {
            for (int i = 0; i < cars.Length; i++)
            {
                VelocityDataPoints[i].AppendAsync(Dispatcher, new Point(currentRuntime.TotalSeconds, cars[i].Velocity));
                AccelerationDataPoints[i].AppendAsync(Dispatcher, new Point(currentRuntime.TotalSeconds, cars[i].Acceleration));
            }
        }

        private bool ReadInSystemParameters()
        {
            try
            {
                Params.vInitial = Double.Parse(this.InitialVelocityTextBox.Text);
                Params.vDesired = Double.Parse(this.DesiredVelocityTextBox.Text);

                Params.dInitial1 = Double.Parse(this.InitialDistance1TextBox.Text);
                Params.dInitial2 = Double.Parse(this.InitialDistance2TextBox.Text);
                Params.dDesired = Double.Parse(this.DesiredDistanceTextBox.Text);

                Params.convergencePercent = Double.Parse(this.ConvergencePercentTextBox.Text) / 100.0;

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        #endregion
    }
}
