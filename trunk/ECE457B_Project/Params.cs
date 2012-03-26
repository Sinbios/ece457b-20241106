using AI.Fuzzy.Library;

namespace ECE457B_Project
{
	public static class Params
	{
		public static double timeStep = 0.04;
		public static double vDesired = 30;
		public static double dDesired = 5;
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

		public static double velocity_d1 = acceleration_d1;
		public static double velocity_d2 = acceleration_d2;

		public static double convergencePercent = 0.015;

		public static FunctionType functionType = FunctionType.Gaussian;
		public static AndMethod tNorm = AndMethod.Min;
	}

	public enum FunctionType
	{
		Trapezoidal,
		Gaussian
	}
}
