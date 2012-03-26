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

    	public static double velocity_d1 = 5;
		public static double velocity_d2 = 10;

		public static double distance_d1 = 20;
		public static double distance_d2 = 40;

		public static double acceleration_d1 = 2;
		public static double acceleration_d2 = 4;
		public static double acceleration_limit = 6;
		public static double brake_d1 = -2;
		public static double brake_d2 = -4;
		public static double brake_limit = -10;
    }
}
