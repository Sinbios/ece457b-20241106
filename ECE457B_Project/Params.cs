using AI.Fuzzy.Library;

namespace ECE457B_Project
{
	public static class Params
	{
        public static int NumCars = 6;

		public static double timeStep = 0.03;
		public static double vDesired = 10;
		public static double dDesired = 5;
        public static double[] dInitials = new double[MainWindow.MaxNumCars - 1];
		public static double dInitial1 = 10;
		public static double dInitial2 = 15;
		public static double vInitial = 0;

		public static double distance_d1 { get { return dDesired / 3; } }
		public static double distance_d2 { get { return 2 * dDesired / 3; } }

		public static double acceleration_d1 = 2;
		public static double acceleration_d2 = 6;
		public static double acceleration_limit = acceleration_d2*2 - acceleration_d1;
		
		public static double brake_d1 = -2;
		public static double brake_d2 = -10;
		public static double brake_limit = brake_d2*2 - brake_d1;

		public static double velocity_d1 = -brake_d1;
		public static double velocity_d2 = -brake_d2;

		public static double convergencePercent = 0.015;

		public static FunctionType functionType = FunctionType.Trapezoidal;
		public static AndMethod tNorm = AndMethod.Production;
	}

	public enum FunctionType
	{
		Trapezoidal,
		Gaussian
	}
}
