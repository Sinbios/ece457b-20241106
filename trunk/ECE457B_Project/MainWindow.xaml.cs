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

        private List<Control[]> InitialDistanceParameterEntryRows = new List<Control[]>();

        private ObservableDataSource<Point>[] DistanceMemFnDataPoints;
        private ObservableDataSource<Point>[] VelocityMemFnDataPoints;
        private ObservableDataSource<Point>[] AccelerationMemFnDataPoints;

        private ObservableDataSource<Point> DistanceDesiredDataPoints;
        private ObservableDataSource<Point>[] DistanceDataPoints;

        private ObservableDataSource<Point> VelocityDesiredDataPoints;
        private ObservableDataSource<Point>[] VelocityDataPoints;

        private ObservableDataSource<Point>[] AccelerationDataPoints;

        private static Brush[] MemFnBrushes = new Brush[] { Brushes.Purple, Brushes.Blue, Brushes.Green, Brushes.Orange, Brushes.Red };
        private static int NumMemFnsPerParameter = MemFnBrushes.Length;
        private static double MemFnGraphPointsPadding = 5.0;
        private static double MemFnGraphPointsSpacing = 0.1;

        private static ChartPlotter DistanceChartPlotter = null;
        private static ChartPlotter VelocityChartPlotter = null;
        private static ChartPlotter AccelerationChartPlotter = null;

        private static ChartPlotter DistanceMemFnChartPlotter = null;
        private static ChartPlotter VelocityMemFnChartPlotter = null;
        private static ChartPlotter AccelerationMemFnChartPlotter = null;

        public static Brush[] CarBrushes = new Brush[] { Brushes.DarkRed, Brushes.OrangeRed, Brushes.Goldenrod, Brushes.Green, Brushes.Blue, Brushes.Indigo, Brushes.Violet, Brushes.DeepPink };

        private static double PlotLineThickness = 2;
        private static int TimestepsPerChartUpdate = 3;

        private static Thread SimulationThread = null;
        private static bool PauseSimulation = false;
        private static bool ParameterUpdated = false;
        private static bool Converged = false;

        private static string PerformanceControlStartString = "Start Simulation";
        private static string PerformanceControlPauseString = "Pause Simulation";
        private static string PerformanceControlResumeString = "Resume Simulation";

        private static int ParameterTextFontSize = 15;

        public static int MinNumCars = 2;
        public static int MaxNumCars = CarBrushes.Length;

        #endregion

        #region Member Variables

        private CarSimulationControl CarSimulationControlInstance = null;

        #endregion


        #region Constructor

        public MainWindow()
        {
            InitializeComponent();

            Controller.GetInstance().Reset();

            this.WindowState = System.Windows.WindowState.Maximized;
            this.ResizeMode = System.Windows.ResizeMode.CanResize;
            this.Width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width;
            this.MinWidth = 640;
            this.Height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - 20;
            this.MinHeight = 600;
            this.SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);
            
            this.SimulationInfoRow.Height = new GridLength((this.Height - this.TitleRow.Height.Value) * (4.5 / 8));
            this.SimulationPerformanceRow.Height = new GridLength(this.Height - this.SimulationInfoRow.Height.Value - this.TitleRow.Height.Value);

            this.SystemVisualizationColumn.Width = new GridLength(this.Width - this.ParameterColumn.Width.Value);

            this.CarSimulationControlInstance = CarSimulationControl.CreateInstance(this.SystemVisualizationColumn.Width.Value, this.SimulationPerformanceRow.Height.Value);
            this.CarSimulationControlInstance.SetCurrentValue(Grid.ColumnProperty, 1);
            this.CarSimulationControlInstance.SetCurrentValue(Grid.RowProperty, 1);
            this.CarSimulationControlInstance.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            this.CarSimulationControlInstance.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.SimulationParameterAndVisualiationGrid.Children.Add(this.CarSimulationControlInstance);

            //Add parameter setters and combo box items for all cars
            List<ComboBoxItem> carNumComboBoxItems = new List<ComboBoxItem>();
            ComboBoxItem toSelect = null;
            int gridRowToAddInto = (int)this.DesiredDistanceTextBox.GetValue(Grid.RowProperty);
            for (int i = 0; i < MaxNumCars; i++)
            {
                ComboBoxItem carNumComboBoxItem = new ComboBoxItem();
                carNumComboBoxItem.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                carNumComboBoxItem.Content = (i + 1).ToString();
                carNumComboBoxItems.Add(carNumComboBoxItem);

                if (i == Params.NumCars - 1)
                {
                    toSelect = carNumComboBoxItem;
                }

                gridRowToAddInto++;

                RowDefinition newRow = new RowDefinition();
                newRow.Height = GridLength.Auto;
                this.SystemParameterGrid.RowDefinitions.Add(newRow);

                Params.dInitials[i] = Params.dDesired * (i + 1);

                if (i > 0)
                {
                    Control[] rowControls = new Control[3];

                    Label initialDistanceLabel = new Label();
                    initialDistanceLabel.FontFamily = new FontFamily("Arial");
                    initialDistanceLabel.FontSize = ParameterTextFontSize;
                    initialDistanceLabel.SetCurrentValue(Grid.RowProperty, gridRowToAddInto);
                    initialDistanceLabel.SetCurrentValue(Grid.ColumnProperty, 0);
                    initialDistanceLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    initialDistanceLabel.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    initialDistanceLabel.Height = 0;
                    initialDistanceLabel.Visibility = System.Windows.Visibility.Hidden;
                    initialDistanceLabel.Content = String.Format("D_initial_{0}", (i));
                    this.SystemParameterGrid.Children.Add(initialDistanceLabel);
                    rowControls[0] = initialDistanceLabel;

                    TextBox initialDistanceTextBox = new TextBox();
                    initialDistanceTextBox.FontFamily = new FontFamily("Arial");
                    initialDistanceTextBox.FontSize = ParameterTextFontSize;
                    initialDistanceTextBox.SetCurrentValue(Grid.RowProperty, gridRowToAddInto);
                    initialDistanceTextBox.SetCurrentValue(Grid.ColumnProperty, 1);
                    initialDistanceTextBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    initialDistanceTextBox.TextAlignment = TextAlignment.Right;
                    initialDistanceTextBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    initialDistanceTextBox.Height = 0;
                    initialDistanceTextBox.Visibility = System.Windows.Visibility.Hidden;
                    initialDistanceTextBox.Text = String.Format("{0:0.00}", Params.dInitials[i]);
                    initialDistanceTextBox.TextChanged += new TextChangedEventHandler(InitialDistanceTextBox_TextChanged);
                    this.SystemParameterGrid.Children.Add(initialDistanceTextBox);
                    rowControls[1] = initialDistanceTextBox;

                    Label initialDistanceUnitsLabel = new Label();
                    initialDistanceUnitsLabel.FontFamily = new FontFamily("Arial");
                    initialDistanceUnitsLabel.FontSize = ParameterTextFontSize;
                    initialDistanceUnitsLabel.SetCurrentValue(Grid.RowProperty, gridRowToAddInto);
                    initialDistanceUnitsLabel.SetCurrentValue(Grid.ColumnProperty, 2);
                    initialDistanceUnitsLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    initialDistanceUnitsLabel.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    initialDistanceUnitsLabel.Height = 0;
                    initialDistanceUnitsLabel.Visibility = System.Windows.Visibility.Hidden;
                    initialDistanceUnitsLabel.Content = "m";
                    this.SystemParameterGrid.Children.Add(initialDistanceUnitsLabel);
                    rowControls[2] = initialDistanceUnitsLabel;

                    this.InitialDistanceParameterEntryRows.Add(rowControls);

                    if (i < Params.NumCars)
                    {
                        initialDistanceLabel.Visibility = System.Windows.Visibility.Visible;
                        initialDistanceLabel.Height = Double.NaN;

                        initialDistanceTextBox.Visibility = System.Windows.Visibility.Visible;
                        initialDistanceTextBox.Height = Double.NaN;

                        initialDistanceUnitsLabel.Visibility = System.Windows.Visibility.Visible;
                        initialDistanceUnitsLabel.Height = Double.NaN;
                    }
                }
                
            }

            this.NumCarComboBox.ItemsSource = carNumComboBoxItems;
            this.NumCarComboBox.SelectedItem = toSelect;

            this.PerformanceControlButtonText.Text = PerformanceControlStartString;

            //Set up distance graph
            DistanceChartPlotter = new ChartPlotter();
            DistanceChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            DistanceChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            DistanceChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            DistanceChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            DistanceDataPoints = new ObservableDataSource<Point>[MaxNumCars - 1];
            DistanceDesiredDataPoints = new ObservableDataSource<Point>();
            DistanceChartPlotter.AddLineGraph(DistanceDesiredDataPoints, new Pen(Brushes.Black, Math.Min(PlotLineThickness / 2.0, 1)), new PenDescription(String.Format("D_desired")));
            for (int i = 0; i < MaxNumCars - 1; i++)
            {
                DistanceDataPoints[i] = new ObservableDataSource<Point>();
                DistanceChartPlotter.AddLineGraph(DistanceDataPoints[i], new Pen(CarBrushes[i + 1], PlotLineThickness), new PenDescription(String.Format("D_{0}", i)));
            }
            DistanceChartPlotter.LegendVisible = false;
            this.DistanceGraphGrid.Children.Add(DistanceChartPlotter);

            //Set up velocity graph
            VelocityChartPlotter = new ChartPlotter();
            VelocityChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            VelocityChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            VelocityChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            VelocityChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            VelocityDataPoints = new ObservableDataSource<Point>[MaxNumCars];
            VelocityDesiredDataPoints = new ObservableDataSource<Point>();
            VelocityChartPlotter.AddLineGraph(VelocityDesiredDataPoints, new Pen(Brushes.Black, Math.Min(PlotLineThickness / 2.0, 1)), new PenDescription(String.Format("V_desired")));
            for (int i = 0; i < MaxNumCars; i++)
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
            AccelerationDataPoints = new ObservableDataSource<Point>[MaxNumCars];
            for (int i = 0; i < MaxNumCars; i++)
            {
                AccelerationDataPoints[i] = new ObservableDataSource<Point>();
                AccelerationChartPlotter.AddLineGraph(AccelerationDataPoints[i], new Pen(CarBrushes[i], PlotLineThickness), new PenDescription(String.Format("A_{0}", i)));
            }
            AccelerationChartPlotter.LegendVisible = false;
            this.AccelerationGraphGrid.Children.Add(AccelerationChartPlotter);

            DistanceMemFnChartPlotter = new ChartPlotter();
            DistanceMemFnChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            DistanceMemFnChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            DistanceMemFnChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            DistanceMemFnChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            DistanceMemFnDataPoints = new ObservableDataSource<Point>[NumMemFnsPerParameter];
            for (int i = 0; i < DistanceMemFnDataPoints.Length; i++)
            {
                DistanceMemFnDataPoints[i] = new ObservableDataSource<Point>();
                DistanceMemFnChartPlotter.AddLineGraph(DistanceMemFnDataPoints[i], new Pen(MemFnBrushes[i], PlotLineThickness), new PenDescription(String.Format("Mem fn #{0}", i)));
            }
            DistanceMemFnChartPlotter.LegendVisible = false;
            this.DistanceMemFnGrid.Children.Add(DistanceMemFnChartPlotter);

            VelocityMemFnChartPlotter = new ChartPlotter();
            VelocityMemFnChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            VelocityMemFnChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            VelocityMemFnChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            VelocityMemFnChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            VelocityMemFnDataPoints = new ObservableDataSource<Point>[NumMemFnsPerParameter];
            for (int i = 0; i < VelocityMemFnDataPoints.Length; i++)
            {
                VelocityMemFnDataPoints[i] = new ObservableDataSource<Point>();
                VelocityMemFnChartPlotter.AddLineGraph(VelocityMemFnDataPoints[i], new Pen(MemFnBrushes[i], PlotLineThickness), new PenDescription(String.Format("Mem fn #{0}", i)));
            }
            VelocityMemFnChartPlotter.LegendVisible = false;
            this.VelocityMemFnGrid.Children.Add(VelocityMemFnChartPlotter);

            AccelerationMemFnChartPlotter = new ChartPlotter();
            AccelerationMemFnChartPlotter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            AccelerationMemFnChartPlotter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            AccelerationMemFnChartPlotter.SetCurrentValue(Grid.RowProperty, 0);
            AccelerationMemFnChartPlotter.Margin = new Thickness(10, 0, 10, 0);
            AccelerationMemFnDataPoints = new ObservableDataSource<Point>[NumMemFnsPerParameter];
            for (int i = 0; i < AccelerationMemFnDataPoints.Length; i++)
            {
                AccelerationMemFnDataPoints[i] = new ObservableDataSource<Point>();
                AccelerationMemFnChartPlotter.AddLineGraph(AccelerationMemFnDataPoints[i], new Pen(MemFnBrushes[i], PlotLineThickness), new PenDescription(String.Format("Mem fn #{0}", i)));
            }
            AccelerationMemFnChartPlotter.LegendVisible = false;
            this.AccelerationMemFnGrid.Children.Add(AccelerationMemFnChartPlotter);

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

            this.InitialVelocityTextBox.Text = String.Format("{0:0.00}", Params.vInitial);
            this.DesiredVelocityTextBox.Text = String.Format("{0:0.00}", Params.vDesired);
            this.DesiredDistanceTextBox.Text = String.Format("{0:0.00}", Params.dDesired);
            this.ConvergencePercentTextBox.Text = String.Format("{0:0.00}", Params.convergencePercent * 100.0);

            this.UpdateMemFnPoints();
        }

        #endregion

        #region Simulation Mainloop

        private void SimulationMainLoop()
        {
            try
            {
                Car[] cars = Car.CreateCars();

                Controller fuzzyController = Controller.GetInstance();

                ParameterUpdated = false;
                Converged = false;
                int timestepToChartUpdate = 0;

                Dispatcher.Invoke((Action)delegate
                {
                    CarSimulationControl.GetInstance().InitializeVisualization(cars);
                    fuzzyController.Reset();
                    this.UpdateMemFnPoints();

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
                        ParameterUpdated = false;

                        currentRuntimeFromLastUpdate = TimeSpan.FromSeconds(0);
                        Dispatcher.Invoke((Action)delegate
                        {
                            fuzzyController.Reset();
                            this.UpdateMemFnPoints();
                        });
                    }

                    Dispatcher.Invoke((Action)delegate
                    {
                        CarSimulationControl.GetInstance().UpdateUI(cars);

                        if (timestepToChartUpdate == 0)
                        {
                            this.UpdateSystemGraphPoints(totalRuntime, cars);
                        }

                        timestepToChartUpdate = (timestepToChartUpdate + 1) % TimestepsPerChartUpdate;

                        this.CurrentSystemRuntimeLabelText.Text = String.Format("{0:0.00} seconds", currentRuntimeFromLastUpdate.TotalSeconds);
                    });

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
                    
                    totalRuntime += TimeSpan.FromSeconds(Params.timeStep);
                    currentRuntimeFromLastUpdate += TimeSpan.FromSeconds(Params.timeStep);

                    //Check convergence
                    if (CheckConvergence(cars))
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
                    this.ConvergencePercentTextBox.Foreground = Brushes.Black;
                    Params.convergencePercent = newConvergencePercent / 100.0;

                    if (SimulationThread != null && SimulationThread.IsAlive)
                    {
                        ParameterUpdated = true;
                    }
                }
                else
                {
                    this.ConvergencePercentTextBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                this.ConvergencePercentTextBox.Foreground = Brushes.Red;
            }
        }

        private void DesiredVelocityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newDesiredVelocity;
            if (Double.TryParse(this.DesiredVelocityTextBox.Text, out newDesiredVelocity))
            {
                if (newDesiredVelocity > 0)
                {
                    this.DesiredVelocityTextBox.Foreground = Brushes.Black;
                    Params.vDesired = newDesiredVelocity;

                    if (SimulationThread == null || !SimulationThread.IsAlive)
                    {
                        Controller.GetInstance().Reset();
                        this.UpdateMemFnPoints();
                    }
                    else
                    {
                        ParameterUpdated = true;
                    }
                }
                else
                {
                    this.DesiredVelocityTextBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                this.DesiredVelocityTextBox.Foreground = Brushes.Red;
            }
        }

        private void DesiredDistanceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newDesiredDistance;
            if (Double.TryParse(this.DesiredDistanceTextBox.Text, out newDesiredDistance))
            {
                if (newDesiredDistance > 0)
                {
                    this.DesiredDistanceTextBox.Foreground = Brushes.Black;
                    Params.dDesired = newDesiredDistance;

                    if (SimulationThread == null || !SimulationThread.IsAlive)
                    {
                        Controller.GetInstance().Reset();
                        this.UpdateMemFnPoints();
                    }
                    else
                    {
                        ParameterUpdated = true;
                    }
                }
                else
                {
                    this.DesiredDistanceTextBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                this.DesiredDistanceTextBox.Foreground = Brushes.Red;
            }
        }

        private void EndSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (SimulationThread != null && SimulationThread.IsAlive)
            {
                SimulationThread.Abort();
                SimulationThread.Join();
            }

            Controller.GetInstance().Reset();

            this.NumCarComboBox.IsEnabled = true;
            foreach (Control[] controls in this.InitialDistanceParameterEntryRows)
            {
                controls[1].IsEnabled = true;
            }

            this.InitialVelocityTextBox.IsEnabled = true;

            this.PerformanceControlButtonText.Text = PerformanceControlStartString;
            this.EndSimulationButton.Visibility = System.Windows.Visibility.Hidden;
            this.TotalSystemRuntimeLabel.Visibility = System.Windows.Visibility.Hidden;
            this.SystemConvergedText.Visibility = System.Windows.Visibility.Hidden;

            this.ClearPointSources();

            Dispatcher.Invoke((Action)delegate
            {
                CarSimulationControl.GetInstance().InitializeVisualization(Car.CreateCars());
            });
        }

        void InitialDistanceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox sourceTextBox = (TextBox)e.Source;

            double newInitialDistance;
            if (Double.TryParse(sourceTextBox.Text, out newInitialDistance))
            {
                if (newInitialDistance > 0)
                {
                    sourceTextBox.Foreground = Brushes.Black;

                    int carNum = (int)sourceTextBox.GetValue(Grid.RowProperty) - (int)this.DesiredDistanceTextBox.GetValue(Grid.RowProperty);
                    Params.dInitials[carNum - 1] = newInitialDistance;

                    CarSimulationControl.GetInstance().InitializeVisualization(Car.CreateCars());
                }
                else
                {
                    sourceTextBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                sourceTextBox.Foreground = Brushes.Red;
            }
        }

        private void InitialVelocityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double newInitialVelocity;
            if (Double.TryParse(this.InitialVelocityTextBox.Text, out newInitialVelocity))
            {
                if (newInitialVelocity >= 0)
                {
                    this.InitialVelocityTextBox.Foreground = Brushes.Black;
                    Params.vInitial = newInitialVelocity;
                }
                else
                {
                    this.InitialVelocityTextBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                this.InitialVelocityTextBox.Foreground = Brushes.Red;
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Console.WriteLine("Current height is: {0}", this.Height);
            if (e.PreviousSize.Width != 0 && e.WidthChanged)
            {
                double sizeChange = e.PreviousSize.Width - e.NewSize.Width;

                this.CarSimulationControlInstance.Width -= sizeChange;
                this.SystemVisualizationColumn.Width = new GridLength(this.SystemVisualizationColumn.Width.Value - sizeChange);

                if (SimulationThread == null || !SimulationThread.IsAlive)
                {
                    this.CarSimulationControlInstance.InitializeVisualization(Car.CreateCars());
                }
            }

            if (e.PreviousSize.Height != 0 && e.HeightChanged)
            {
                double sizeChange = e.PreviousSize.Height - e.NewSize.Height;

                this.SimulationPerformanceRow.Height = new GridLength(this.SimulationPerformanceRow.Height.Value - (sizeChange / 2.0));
                this.SimulationInfoRow.Height = new GridLength(this.SimulationInfoRow.Height.Value - (sizeChange / 2.0));
                this.CarSimulationControlInstance.Height = this.SimulationPerformanceRow.Height.Value;

                if (SimulationThread == null || !SimulationThread.IsAlive)
                {
                    this.CarSimulationControlInstance.InitializeVisualization(Car.CreateCars());
                }
            }
        }

        private void MembershipFunctionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Params.functionType = (FunctionType)Enum.Parse(typeof(FunctionType), (string)(((ComboBoxItem)this.MembershipFunctionTypeComboBox.SelectedItem).Content));

            if (SimulationThread == null || !SimulationThread.IsAlive)
            {
                Controller.GetInstance().Reset();
                this.UpdateMemFnPoints();
            }
            else
            {
                ParameterUpdated = true;
            }
        }

        private void NumCarComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Params.NumCars = int.Parse((string)(((ComboBoxItem)this.NumCarComboBox.SelectedItem).Content));

            CarSimulationControl.GetInstance().InitializeVisualization(Car.CreateCars());

            for (int i = 0; i < MaxNumCars - 1; i++)
            {
                Control[] rowControls = this.InitialDistanceParameterEntryRows[i];

                foreach (Control rowControl in rowControls)
                {
                    if (i < Params.NumCars - 1)
                    {
                        rowControl.Visibility = System.Windows.Visibility.Visible;
                        rowControl.Height = Double.NaN;
                    }
                    else
                    {
                        rowControl.Visibility = System.Windows.Visibility.Hidden;
                        rowControl.Height = 0;
                    }
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

                this.NumCarComboBox.IsEnabled = false;
                foreach (Control[] controls in this.InitialDistanceParameterEntryRows)
                {
                    controls[1].IsEnabled = false;
                }

                this.InitialVelocityTextBox.IsEnabled = false;

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

            if (SimulationThread == null || !SimulationThread.IsAlive)
            {
                Controller.GetInstance().Reset();
                this.UpdateMemFnPoints();
            }
            else
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
            CarSimulationControl.GetInstance().InitializeVisualization(Car.CreateCars());
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

        private bool CheckConvergence(Car[] cars)
        {
            for (int i = 0; i < Params.NumCars; i++)
            {
                if (!ApproximatelyEqual(cars[i].Distance, Params.dDesired, Params.convergencePercent)
                    || !ApproximatelyEqual(cars[i].Velocity, Params.vDesired, Params.convergencePercent))
                {
                    return false;
                }
            }

            return true;
        }

        private void ClearPointSources()
        {
            for (int i = 0; i < Params.NumCars; i++)
            {
                if (i != Params.NumCars - 1)
                {
                    DistanceDataPoints[i].Collection.Clear();
                }
                VelocityDataPoints[i].Collection.Clear();
                AccelerationDataPoints[i].Collection.Clear();
            }

            DistanceDesiredDataPoints.Collection.Clear();
            VelocityDesiredDataPoints.Collection.Clear();
        }

        private void ClearMemFnPointSources()
        {
            for (int i = 0; i < NumMemFnsPerParameter; i++)
            {
                DistanceMemFnDataPoints[i].Collection.Clear();
                VelocityMemFnDataPoints[i].Collection.Clear();
                AccelerationMemFnDataPoints[i].Collection.Clear();
            }
        }

        private void UpdateSystemGraphPoints(TimeSpan currentRuntime, Car[] cars)
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

            DistanceDesiredDataPoints.AppendAsync(Dispatcher, new Point(currentRuntime.TotalSeconds, Params.dDesired));
            VelocityDesiredDataPoints.AppendAsync(Dispatcher, new Point(currentRuntime.TotalSeconds, Params.vDesired));
        }

        private void UpdateMemFnPoints()
        {
            ClearMemFnPointSources();

            for (int i = 0; i < 3; i++)
            {
                UpdateMemFnGraph(i);
            }
        }

        private void UpdateMemFnGraph(int parameterType)
        {
            ObservableDataSource<Point>[] pointsToUpdate;

            if (parameterType == 0)
            {
                pointsToUpdate = DistanceMemFnDataPoints;
            }
            else if (parameterType == 1)
            {
                pointsToUpdate = VelocityMemFnDataPoints;
            }
            else
            {
                pointsToUpdate = AccelerationMemFnDataPoints;
            }

            List<Point>[] listsOfPointsToAdd = new List<Point>[NumMemFnsPerParameter];

            for (int i = 0; i < NumMemFnsPerParameter; i++)
            {
                listsOfPointsToAdd[i] = new List<Point>();
            }

            //Note: We obtain the points and then batch graph them for performance reasons (the graphing library is not very performant)

            Tuple<double, double> memFnBounds = Controller.GetInstance().GetMemFnBounds(parameterType);
            for (double pointX = memFnBounds.Item1 - MemFnGraphPointsPadding; pointX <= memFnBounds.Item2 + MemFnGraphPointsPadding; pointX += MemFnGraphPointsSpacing)
            {
                double[] memFnValues = Controller.GetInstance().GetMemFnValuesAt(pointX, parameterType);

                for (int i = 0; i < NumMemFnsPerParameter; i++)
                {
                    listsOfPointsToAdd[i].Add(new Point(pointX, memFnValues[i]));
                }
            }

            for (int i = 0; i < NumMemFnsPerParameter; i++)
            {
                pointsToUpdate[i].AppendMany(listsOfPointsToAdd[i]);
            }
        }

        private bool ReadInSystemParameters()
        {
            try
            {
                Params.vInitial = Double.Parse(this.InitialVelocityTextBox.Text);
                Params.vDesired = Double.Parse(this.DesiredVelocityTextBox.Text);

                Params.dDesired = Double.Parse(this.DesiredDistanceTextBox.Text);

                Params.convergencePercent = Double.Parse(this.ConvergencePercentTextBox.Text) / 100.0;

                Params.functionType = (FunctionType)Enum.Parse(typeof(FunctionType),(string)(((ComboBoxItem)(this.MembershipFunctionTypeComboBox.SelectedItem)).Content));
                Params.tNorm = (AI.Fuzzy.Library.AndMethod)Enum.Parse(typeof(AI.Fuzzy.Library.AndMethod), (string)(((ComboBoxItem)(this.TNormTypeComboBox.SelectedItem)).Content));

                if (Params.vInitial >= 0 && Params.vDesired > 0 && Params.dDesired > 0 && Params.convergencePercent > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        #endregion
    }
}
