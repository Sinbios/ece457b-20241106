using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
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

        public static Brush[] CarBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue };
        public static Brush[] CarDistanceBrushes = new Brush[] { Brushes.Green, Brushes.Blue };

        private ObservableDataSource<Point>[] DistanceDataPoints;
        private ObservableDataSource<Point>[] VelocityDataPoints;
        private ObservableDataSource<Point>[] AccelerationDataPoints;

        private static ChartPlotter DistanceChartPlotter = null;
        private static ChartPlotter VelocityChartPlotter = null;
        private static ChartPlotter AccelerationChartPlotter = null;
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

            //Set up membership function selection combobox
            List<ComboBoxItem> memFnTypeComboBoxItems = new List<ComboBoxItem>();
            foreach (FunctionType memFnType in Enum.GetValues(typeof(FunctionType)))
            {
                ComboBoxItem memFnTypeComboBoxItem = new ComboBoxItem();
                memFnTypeComboBoxItem.Content = Enum.GetName(typeof(FunctionType), memFnType);
                
                memFnTypeComboBoxItems.Add(memFnTypeComboBoxItem);
            }
            this.MembershipFunctionTypeComboBox.ItemsSource = memFnTypeComboBoxItems;
            this.MembershipFunctionTypeComboBox.SelectedIndex = (int)Params.functionType;

            //Set up t-norm selection combobox
            List<ComboBoxItem> tNormTypeComboBoxItems = new List<ComboBoxItem>();
            foreach (AI.Fuzzy.Library.AndMethod tNormType in Enum.GetValues(typeof(AI.Fuzzy.Library.AndMethod)))
            {
                ComboBoxItem tNormTypeComboBoxItem = new ComboBoxItem();
                tNormTypeComboBoxItem.Content = Enum.GetName(typeof(AI.Fuzzy.Library.AndMethod), tNormType);

                tNormTypeComboBoxItems.Add(tNormTypeComboBoxItem);
            }
            this.TNormTypeComboBox.ItemsSource = tNormTypeComboBoxItems;
            this.TNormTypeComboBox.SelectedIndex = (int)Params.tNorm;

            //Set up distance graph
            DistanceChartPlotter = new ChartPlotter();
            DistanceChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            DistanceChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            DistanceChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            DistanceChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            DistanceDataPoints = new ObservableDataSource<Point>[NumCars - 1];
            for (int i = 0; i < NumCars - 1; i++)
            {
                DistanceDataPoints[i] = new ObservableDataSource<Point>();
                DistanceChartPlotter.AddLineGraph(DistanceDataPoints[i], new Pen(CarDistanceBrushes[i], PlotLineThickness), new PenDescription(String.Format("D_{0}", i)));
            }
            DistanceChartPlotter.LegendVisible = false;
            this.DistanceGraphGrid.Children.Add(DistanceChartPlotter);

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
            VelocityChartPlotter.LegendVisible = false;
            this.VelocityGraphGrid.Children.Add(VelocityChartPlotter);            

            //Set up acceleration graph
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
            AccelerationChartPlotter.LegendVisible = false;
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

                    this.ClearPointSources();
                });

                TimeSpan currentRuntimeFromLastUpdate = TimeSpan.FromSeconds(0);
                TimeSpan totalRuntime = TimeSpan.FromSeconds(0);
                PauseSimulation = false;

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

                        currentRuntimeFromLastUpdate = TimeSpan.FromSeconds(0);
                    }

                    if (ParameterUpdated)
                    {
                        lock (this)
                        {
                            ParameterUpdated = false;
                        }

                        currentRuntimeFromLastUpdate = TimeSpan.FromSeconds(0);
                        Dispatcher.Invoke((Action)delegate
                        {
                            fuzzyController.Reset();
                        });
                    }

                    Dispatcher.Invoke((Action)delegate
                    {
                        CarSimulationControl.GetInstance().UpdateUI(cars);
                        this.UpdateGraphPoints(totalRuntime, cars);
                        this.CurrentSystemRuntimeLabelText.Text = String.Format("{0:0.00} seconds", currentRuntimeFromLastUpdate.TotalSeconds);
                    });

                    /*
                    Car[] carsCopy = new Car[cars.Length];

                    for (int i = 0; i < cars.Length; i++)
                    {
                        carsCopy[i] = new Car(i);
                        carsCopy[i].Position = cars[i].Position;
                        carsCopy[i].Velocity = cars[i].Velocity;
                        carsCopy[i].Acceleration = cars[i].Acceleration;
                        carsCopy[i].Distance = cars[i].Distance;
                    }
                    */
                    //Thread fuzzyInferenceThread = new Thread(new ThreadStart(delegate
                    //{
                        for (int i = 0; i < cars.Length; i++)
                        {
                            cars[i].Acceleration = fuzzyController.GetOutput(cars[i].Distance - Params.dDesired, cars[i].Velocity);
                            cars[i].Position = cars[i].Position + cars[i].Velocity * Params.timeStep + 0.5 * cars[i].Acceleration * Params.timeStep * Params.timeStep;
                            if (i != 0)
                            {
                                cars[i].Distance = Math.Max(0, cars[i - 1].Position - cars[i].Position);
                            }
                            cars[i].Velocity = Math.Max(0, cars[i].Velocity + cars[i].Acceleration * Params.timeStep);
                        }
                    //}));
                    //fuzzyInferenceThread.Start();
                    
                    totalRuntime += TimeSpan.FromSeconds(Params.timeStep);
                    currentRuntimeFromLastUpdate += TimeSpan.FromSeconds(Params.timeStep);

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

            this.ClearPointSources();
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

        private void MembershipFunctionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Params.functionType = (FunctionType)Enum.Parse(typeof(FunctionType), (string)(((ComboBoxItem)this.MembershipFunctionTypeComboBox.SelectedItem).Content));

            lock (this)
            {
                ParameterUpdated = true;
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
                this.CurrentSystemRuntimeLabelText.Text = "0 seconds";

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

        private void TNormTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Params.tNorm = (AI.Fuzzy.Library.AndMethod)Enum.Parse(typeof(AI.Fuzzy.Library.AndMethod), (string)(((ComboBoxItem)this.TNormTypeComboBox.SelectedItem).Content));

            lock (this)
            {
                ParameterUpdated = true;
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

        private void ClearPointSources()
        {
            for (int i = 0; i < NumCars; i++)
            {
                if (i != NumCars - 1)
                {
                    DistanceDataPoints[i].Collection.Clear();
                }
                VelocityDataPoints[i].Collection.Clear();
                AccelerationDataPoints[i].Collection.Clear();
            }
        }

        private void UpdateGraphPoints(TimeSpan currentRuntime, Car[] cars)
        {
            for (int i = 0; i < cars.Length; i++)
            {
                if (i != cars.Length - 1)
                {
                    DistanceDataPoints[i].AppendAsync(Dispatcher, new Point(currentRuntime.TotalSeconds, cars[i + 1].Distance));
                }
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

                Params.functionType = (FunctionType)Enum.Parse(typeof(FunctionType),(string)(((ComboBoxItem)(this.MembershipFunctionTypeComboBox.SelectedItem)).Content));
                Params.tNorm = (AI.Fuzzy.Library.AndMethod)Enum.Parse(typeof(AI.Fuzzy.Library.AndMethod), (string)(((ComboBoxItem)(this.TNormTypeComboBox.SelectedItem)).Content));

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
