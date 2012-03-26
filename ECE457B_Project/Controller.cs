
using DotFuzzy;

namespace ECE457B_Project
{
	class Controller
	{
		private LinguisticVariable _velocity;
		private LinguisticVariable _distanceDiff;
		private LinguisticVariable _acceleration;
		private readonly FuzzyEngine _engine;

        private static Controller _instance = null;

		private Controller()
		{
			_engine = new FuzzyEngine { Consequent = "Acceleration" };
			Reset();

			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Velocity IS Very_Slow) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Velocity IS Slow) THEN Acceleration IS Accelerate"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Velocity IS Just_Right) THEN Acceleration IS None"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Velocity IS Fast) THEN Acceleration IS Brake"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Velocity IS Very_Fast) THEN Acceleration IS Brake_Hard"));

			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Close) AND (Velocity IS Very_Slow) THEN Acceleration IS None"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Close) AND (Velocity IS Slow) THEN Acceleration IS Brake"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Close) AND (Velocity IS Just_Right) THEN Acceleration IS Brake_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Close) AND (Velocity IS Fast) THEN Acceleration IS Brake_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Close) AND (Velocity IS Very_Fast) THEN Acceleration IS Brake_Hard"));

			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Close) AND (Velocity IS Very_Slow) THEN Acceleration IS Accelerate"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Close) AND (Velocity IS Slow) THEN Acceleration IS None"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Close) AND (Velocity IS Just_Right) THEN Acceleration IS Brake"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Close) AND (Velocity IS Fast) THEN Acceleration IS Brake_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Close) AND (Velocity IS Very_Fast) THEN Acceleration IS Brake_Hard"));

			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Just_Right) AND (Velocity IS Very_Slow) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Just_Right) AND (Velocity IS Slow) THEN Acceleration IS Accelerate"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Just_Right) AND (Velocity IS Just_Right) THEN Acceleration IS None"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Just_Right) AND (Velocity IS Fast) THEN Acceleration IS Brake"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Just_Right) AND (Velocity IS Very_Fast) THEN Acceleration IS Brake_Hard"));

			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Far) AND (Velocity IS Very_Slow) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Far) AND (Velocity IS Slow) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Far) AND (Velocity IS Just_Right) THEN Acceleration IS Accelerate"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Far) AND (Velocity IS Fast) THEN Acceleration IS None"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Far) AND (Velocity IS Very_Fast) THEN Acceleration IS Brake"));

			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Far) AND (Velocity IS Very_Slow) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Far) AND (Velocity IS Slow) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Far) AND (Velocity IS Just_Right) THEN Acceleration IS Accelerate_Hard"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Far) AND (Velocity IS Fast) THEN Acceleration IS Accelerate"));
			_engine.FuzzyRuleCollection.Add(new FuzzyRule("IF (Distance IS Very_Far) AND (Velocity IS Very_Fast) THEN Acceleration IS None"));
		}

        public static Controller GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Controller();
            }

            return _instance;
        }

		public void Reset()
		{
			_velocity = new LinguisticVariable("Velocity");
			_distanceDiff = new LinguisticVariable("Distance");
			_acceleration = new LinguisticVariable("Acceleration");
			_engine.LinguisticVariableCollection.Clear();
			_engine.LinguisticVariableCollection.Add(_velocity);
			_engine.LinguisticVariableCollection.Add(_distanceDiff);
			_engine.LinguisticVariableCollection.Add(_acceleration);
			UpdateFunctions();
		}

		private void UpdateFunctions()
		{
			_velocity.MembershipFunctionCollection.Clear();
			_velocity.MembershipFunctionCollection.Add(new MembershipFunction("Very_Slow", 0, 0, Params.vDesired - Params.velocity_d2, Params.vDesired - Params.velocity_d1));
			_velocity.MembershipFunctionCollection.Add(new MembershipFunction("Slow", Params.vDesired - Params.velocity_d2, Params.vDesired - Params.velocity_d1, Params.vDesired - Params.velocity_d1, Params.vDesired));
			_velocity.MembershipFunctionCollection.Add(new MembershipFunction("Just_Right", Params.vDesired - Params.velocity_d1, Params.vDesired, Params.vDesired, Params.vDesired + Params.velocity_d1));
			_velocity.MembershipFunctionCollection.Add(new MembershipFunction("Fast", Params.vDesired, Params.vDesired + Params.velocity_d1, Params.vDesired + Params.velocity_d1, Params.vDesired + Params.velocity_d2));
			_velocity.MembershipFunctionCollection.Add(new MembershipFunction("Very_Fast", Params.vDesired + Params.velocity_d1, Params.vDesired + Params.velocity_d2, 300, 300));

			_distanceDiff.MembershipFunctionCollection.Clear();
            _distanceDiff.MembershipFunctionCollection.Add(new MembershipFunction("Very_Close", -double.MaxValue, -double.MaxValue, -Params.distance_d2, -Params.distance_d1));
			_distanceDiff.MembershipFunctionCollection.Add(new MembershipFunction("Close", -Params.distance_d2, -Params.distance_d1, -Params.distance_d1, 0));
			_distanceDiff.MembershipFunctionCollection.Add(new MembershipFunction("Just_Right", -Params.distance_d1, 0, 0, Params.distance_d1));
			_distanceDiff.MembershipFunctionCollection.Add(new MembershipFunction("Far", 0, Params.distance_d1, Params.distance_d1, Params.distance_d2));
			_distanceDiff.MembershipFunctionCollection.Add(new MembershipFunction("Very_Far", Params.distance_d1, Params.distance_d2, double.MaxValue, double.MaxValue));

			_acceleration.MembershipFunctionCollection.Clear();
			_acceleration.MembershipFunctionCollection.Add(new MembershipFunction("Brake_Hard", Params.brake_limit, Params.brake_limit, Params.brake_d2, Params.brake_d1));
			_acceleration.MembershipFunctionCollection.Add(new MembershipFunction("Brake", Params.brake_d2, Params.brake_d1, Params.brake_d1, 0));
			_acceleration.MembershipFunctionCollection.Add(new MembershipFunction("None", Params.brake_d1, 0, 0, Params.acceleration_d1));
			_acceleration.MembershipFunctionCollection.Add(new MembershipFunction("Accelerate", 0, Params.acceleration_d1, Params.acceleration_d1, Params.acceleration_d2));
			_acceleration.MembershipFunctionCollection.Add(new MembershipFunction("Accelerate_Hard", Params.acceleration_d1, Params.acceleration_d2, Params.acceleration_limit, Params.acceleration_limit));
		}

		public double GetOutput(double distanceDiff, double velocity)
		{
			_distanceDiff.InputValue = distanceDiff;
			_velocity.InputValue = velocity;
			return _engine.Defuzzify();
		}
	}
}
