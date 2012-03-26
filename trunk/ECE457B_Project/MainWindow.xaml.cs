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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Thread SimulationThread = null;
        private static bool PauseSimulation = false;
        private static bool ParameterUpdated = false;

        private static string PerformanceControlStartString = "Start Simulation";
        private static string PerformanceControlPauseString = "Pause Simulation";
        private static string PerformanceControlResumeString = "Resume Simulation";

        private static double AllowableDeviationForConvergenceInPercent = 0.015;

        public MainWindow()
        {
            InitializeComponent();

            this.Width = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 10;
            this.Height = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - 30;

            this.SimulationPerformanceRow.Height = new GridLength(this.Height - this.SimulationInfoRow.Height.Value - this.TitleRow.Height.Value);

            this.SystemVisualizationColumn.Width = new GridLength(this.Width - this.ParameterColumn.Width.Value);

            CarSimulationControl carSimInstance = CarSimulationControl.CreateInstance(this.SystemVisualizationColumn.Width.Value, this.SimulationPerformanceRow.Height.Value);
            carSimInstance.SetCurrentValue(Grid.ColumnProperty, 1);

            this.SimulationParameterAndVisualiationGrid.Children.Add(carSimInstance);

            this.InitialVelocityTextBox.Text = String.Format("{0:0.00}", Params.vInitial);
            this.DesiredVelocityTextBox.Text = String.Format("{0:0.00}", Params.vDesired);

            this.InitialDistance1TextBox.Text = String.Format("{0:0.00}", Params.dInitial1);
            this.InitialDistance2TextBox.Text = String.Format("{0:0.00}", Params.dInitial2);
            this.DesiredDistanceTextBox.Text = String.Format("{0:0.00}", Params.dDesired);

            this.PerformanceControlButtonText.Text = PerformanceControlStartString;
        }

        private void PerformanceControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.PerformanceControlButtonText.Text.Equals(PerformanceControlStartString))
            {
                this.ReadInSystemParameters();
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

        private void EndSimulationButton_Click(object sender, RoutedEventArgs e)
        {
            if (SimulationThread != null && SimulationThread.IsAlive)
            {
                SimulationThread.Abort();
                CarSimulationControl.GetInstance().InitializeVisualization(new Car[] { new Car(0), new Car(1), new Car(2) });
            }

            this.PerformanceControlButtonText.Text = PerformanceControlStartString;
            this.EndSimulationButton.Visibility = System.Windows.Visibility.Hidden;
        }

        private void SimulationMainLoop()
        {
            try
            {
                Car[] cars = new Car[] { new Car(0), new Car(1), new Car(2) };

                Controller fuzzyController = Controller.GetInstance();

                ParameterUpdated = false;

                Dispatcher.Invoke((Action)delegate
                {
                    CarSimulationControl.GetInstance().InitializeVisualization(cars);
                    fuzzyController.Reset();
                });

                TimeSpan totalRuntime = TimeSpan.FromSeconds(0);
                PauseSimulation = false;

                while (true)
                {
                    if (PauseSimulation)
                    {
                        continue;
                    }

                    if (ParameterUpdated)
                    {
                        lock (this)
                        {
                            ParameterUpdated = false;
                        }

                        Dispatcher.Invoke(new Action(fuzzyController.Reset));
                    }

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

                    //Check convergence
                    if (ApproximatelyEqual(cars[1].Distance, Params.dDesired, AllowableDeviationForConvergenceInPercent)
                        && ApproximatelyEqual(cars[2].Distance, Params.dDesired, AllowableDeviationForConvergenceInPercent)
                        && ApproximatelyEqual(cars[0].Velocity, Params.vDesired, AllowableDeviationForConvergenceInPercent)
                        && ApproximatelyEqual(cars[1].Velocity, Params.vDesired, AllowableDeviationForConvergenceInPercent)
                        && ApproximatelyEqual(cars[2].Velocity, Params.vDesired, AllowableDeviationForConvergenceInPercent))
                    {
                        Console.WriteLine("The system converged in {0}", totalRuntime);
                        Dispatcher.Invoke(new Action<RoutedEventArgs>(this.PerformanceControlButton.RaiseEvent), new RoutedEventArgs(Button.ClickEvent)); //Pause the simulation
                    }

                    Dispatcher.Invoke((Action)delegate
                    {
                        CarSimulationControl.GetInstance().UpdateUI(cars);
                    });

                    totalRuntime += TimeSpan.FromSeconds(Params.timeStep);

                    Thread.Sleep(TimeSpan.FromSeconds(Params.timeStep));
                }
            }
            catch (ThreadAbortException)
            {

            }
        }

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
        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CarSimulationControl.GetInstance().InitializeVisualization(new Car[] { new Car(0), new Car(1), new Car(2) });
        }

        private void ReadInSystemParameters()
        {
            Params.vInitial = Double.Parse(this.InitialVelocityTextBox.Text);
            Params.vDesired = Double.Parse(this.DesiredVelocityTextBox.Text);

            Params.dInitial1 = Double.Parse(this.InitialDistance1TextBox.Text);
            Params.dInitial2 = Double.Parse(this.InitialDistance2TextBox.Text);
            Params.dDesired = Double.Parse(this.DesiredDistanceTextBox.Text);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SimulationThread != null && SimulationThread.IsAlive)
            {
                SimulationThread.Abort();
            }
        }
    }
}
