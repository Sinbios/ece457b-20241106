using System;

namespace ECE457B_Project
{
    public class Car
    {
        public double Velocity;
        public double _distance;
        public double Distance
        {
            get
            {
                if (_index == 0) { _distance = Params.dDesired;  }
                return _distance;
            }
            set { _distance = value; }
        }
    	public double Acceleration;
    	public double Position;
        int _index;
    	public Car(int n)
        {
        	Acceleration = 0;
        	Velocity = Params.vInitial;
            _index = n;

            Position = 0;

            for (int i = Params.NumCars - 1; i > n; i--)
            {
                Position += Params.dInitials[i];
            }
            
            if (n == 0)
            {
                Distance = Params.dDesired;
            }
            else
            {
                Distance = Params.dInitials[n - 1];
            }

            /*
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
             */
        }

        public static Car[] CreateCars(int numCarsToCreate)
        {
            Car[] cars = new Car[Params.NumCars];
            for (int i = 0; i < Params.NumCars; i++)
            {
                cars[i] = new Car(i);
            }

            return cars;
        }
    }
}
