using System;

namespace ECE457B_Project
{
    class Car
    {
        public double Velocity;
		public double Distance;
    	public double Acceleration;
    	public double Position;

    	public Car(int n)
        {
        	Acceleration = 0;
        	Velocity = Params.vInitial;

			switch(n)
			{
				case 0:
					Distance = Params.dDesired;
					Position = Params.dInitial1 + Params.dInitial2;
					break;
				case 1:
					Distance = Params.dInitial1;
					Position = Params.dInitial2;
					break;
				case 2:
					Distance = Params.dInitial2;
					Position = 0;
					break;
				default:
					throw new Exception("Car index out of range");
			}
        }
    }
}
