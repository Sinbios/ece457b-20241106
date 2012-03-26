using System;
using System.Threading;
using System.Windows;
using System.IO;
using System.Windows.Threading;

namespace ECE457B_Project
{
	public partial class Sample : Window
	{
		string logFile = @"C:\carlog.txt";
		Car[] cars = new[] { new Car(0), new Car(1), new Car(2) };

		public Sample()
		{
			InitializeComponent();
			this.Loaded += OnLoaded;
			File.Delete(logFile);
			textBox1.Text = "";

		}

		private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
		{
			var thread = new Thread(mainLoop);
			thread.Start();
		}

		void mainLoop()
		{
			double t = 0;

			Output("t\tv0\td0\ta0\tv1\td1\ta1\tv2\td2\ta2\n");
			while (true)
			{
				t += Params.timeStep;
                var controller = Controller.GetInstance();

				Output(String.Format("{0}\t", t));
				for (int i = 0; i < 3; i++)
				{
					cars[i].Acceleration = controller.GetOutput(cars[i].Distance - Params.dDesired, cars[i].Velocity);
					Output(String.Format("{0:N2}\t{1:N2}\t{2:E4}\t", cars[i].Velocity, cars[i].Distance, cars[i].Acceleration));

					cars[i].Position = cars[i].Position + cars[i].Velocity * Params.timeStep + 0.5 * cars[i].Acceleration * Params.timeStep * Params.timeStep;
					if (i != 0)
					{
						cars[i].Distance = Math.Max(0, cars[i - 1].Position - cars[i].Position);
					}
					cars[i].Velocity = cars[i].Velocity + cars[i].Acceleration * Params.timeStep;
				}
				Output("\n");
			}
		}

		public void Output(string s)
		{
			using (var sw = new StreamWriter(logFile, true))
			{
				sw.Write(s);
				textBox1.Dispatcher.Invoke(DispatcherPriority.Normal,
										   new Action(delegate
										   {
											   textBox1.Text += s;
											   textBox1.ScrollToEnd();
										   }));
				sw.Close();
			}
		}
	}
}
