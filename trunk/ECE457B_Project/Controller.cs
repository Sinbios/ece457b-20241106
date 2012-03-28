
using System;
using System.Collections.Generic;
using AI.Fuzzy.Library;

namespace ECE457B_Project
{
	class Controller
	{
		private readonly FuzzyVariable _velocity;
		private readonly FuzzyVariable _distanceError;
		private readonly FuzzyVariable _acceleration;

		readonly MamdaniFuzzySystem _mamdani;

		private readonly List<string> _rules = new List<string>
		{
		    "if (Distance is Very_Close) and (Velocity is Very_Slow) then Acceleration is Brake",
		    "if (Distance is Very_Close) and (Velocity is Slow) then Acceleration is Brake_Hard",
		    "if (Distance is Very_Close) and (Velocity is Just_Right) then Acceleration is Brake_Hard",
		    "if (Distance is Very_Close) and (Velocity is Fast) then Acceleration is Brake_Hard",
		    "if (Distance is Very_Close) and (Velocity is Very_Fast) then Acceleration is Brake_Hard",

		    "if (Distance is Close) and (Velocity is Very_Slow) then Acceleration is Accelerate",
		    "if (Distance is Close) and (Velocity is Slow) then Acceleration is None",
		    "if (Distance is Close) and (Velocity is Just_Right) then Acceleration is Brake",
		    "if (Distance is Close) and (Velocity is Fast) then Acceleration is Brake_Hard",
		    "if (Distance is Close) and (Velocity is Very_Fast) then Acceleration is Brake_Hard",

		    "if (Distance is Just_Right) and (Velocity is Very_Slow) then Acceleration is Accelerate_Hard",
		    "if (Distance is Just_Right) and (Velocity is Slow) then Acceleration is Accelerate",
		    "if (Distance is Just_Right) and (Velocity is Just_Right) then Acceleration is None",
		    "if (Distance is Just_Right) and (Velocity is Fast) then Acceleration is Brake",
		    "if (Distance is Just_Right) and (Velocity is Very_Fast) then Acceleration is Brake_Hard",

		    "if (Distance is Far) and (Velocity is Very_Slow) then Acceleration is Accelerate_Hard",
		    "if (Distance is Far) and (Velocity is Slow) then Acceleration is Accelerate_Hard",
		    "if (Distance is Far) and (Velocity is Just_Right) then Acceleration is Accelerate",
		    "if (Distance is Far) and (Velocity is Fast) then Acceleration is None",
		    "if (Distance is Far) and (Velocity is Very_Fast) then Acceleration is Brake",

		    "if (Distance is Very_Far) and (Velocity is Very_Slow) then Acceleration is Accelerate_Hard",
		    "if (Distance is Very_Far) and (Velocity is Slow) then Acceleration is Accelerate_Hard",
		    "if (Distance is Very_Far) and (Velocity is Just_Right) then Acceleration is Accelerate_Hard",
		    "if (Distance is Very_Far) and (Velocity is Fast) then Acceleration is Accelerate",
		    "if (Distance is Very_Far) and (Velocity is Very_Fast) then Acceleration is None"
		};

		private static Controller _instance = null;

		private Controller()
		{
			_mamdani = new MamdaniFuzzySystem();

			_acceleration = new FuzzyVariable("Acceleration", Params.brake_limit, Params.acceleration_limit);
			_distanceError = new FuzzyVariable("Distance", -double.MaxValue, double.MaxValue);
			_velocity = new FuzzyVariable("Velocity", 0, double.MaxValue);

			_mamdani.Input.Add(_velocity);
			_mamdani.Input.Add(_distanceError);
			_mamdani.Output.Add(_acceleration);

			Reset();

			foreach (string rule in _rules)
			{
				_mamdani.Rules.Add(_mamdani.ParseRule(rule));
			}
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
			_mamdani.AndMethod = Params.tNorm;
			if (_mamdani.AndMethod == AndMethod.Min) { _mamdani.OrMethod = OrMethod.Max; }

			UpdateFunctions();
		}

		private void UpdateFunctions()
		{
			if (Params.functionType == FunctionType.Trapezoidal)
			{
				AddTerm(_velocity, "Very_Slow", new TrapezoidMembershipFunction(0, 0, Params.vDesired - Params.velocity_d2, Params.vDesired - Params.velocity_d1));
				AddTerm(_velocity, "Slow", new TriangularMembershipFunction(Params.vDesired - Params.velocity_d2, Params.vDesired - Params.velocity_d1, Params.vDesired));
				AddTerm(_velocity, "Just_Right", new TriangularMembershipFunction(Params.vDesired - Params.velocity_d1, Params.vDesired, Params.vDesired + Params.velocity_d1));
				AddTerm(_velocity, "Fast", new TriangularMembershipFunction(Params.vDesired, Params.vDesired + Params.velocity_d1, Params.vDesired + Params.velocity_d2));
				AddTerm(_velocity, "Very_Fast", new TrapezoidMembershipFunction(Params.vDesired + Params.velocity_d1, Params.vDesired + Params.velocity_d2, double.MaxValue, double.MaxValue));

				AddTerm(_distanceError, "Very_Close", new TrapezoidMembershipFunction(-double.MaxValue, -double.MaxValue, -Params.distance_d2, -Params.distance_d1));
				AddTerm(_distanceError, "Close", new TriangularMembershipFunction(-Params.distance_d2, -Params.distance_d1, 0));
				AddTerm(_distanceError, "Just_Right", new TriangularMembershipFunction(-Params.distance_d1, 0, Params.distance_d1));
				AddTerm(_distanceError, "Far", new TriangularMembershipFunction(0, Params.distance_d1, Params.distance_d2));
				AddTerm(_distanceError, "Very_Far", new TrapezoidMembershipFunction(Params.distance_d1, Params.distance_d2, double.MaxValue, double.MaxValue));

				AddTerm(_acceleration, "Brake_Hard", new TriangularMembershipFunction(Params.brake_limit, Params.brake_d2, Params.brake_d1));
				AddTerm(_acceleration, "Brake", new TriangularMembershipFunction(Params.brake_d2, Params.brake_d1, 0));
				AddTerm(_acceleration, "None", new TriangularMembershipFunction(Params.brake_d1, 0, Params.acceleration_d1));
				AddTerm(_acceleration, "Accelerate", new TriangularMembershipFunction(0, Params.acceleration_d1, Params.acceleration_d2));
				AddTerm(_acceleration, "Accelerate_Hard", new TriangularMembershipFunction(Params.acceleration_d1, Params.acceleration_d2, Params.acceleration_limit));
			}
			else if(Params.functionType == FunctionType.Gaussian)
			{
				var verySlowMFConst = new TrapezoidMembershipFunction(-double.MaxValue, -double.MaxValue,
																	  Params.vDesired - Params.velocity_d2,
																	  Params.vDesired - Params.velocity_d2);
				var verySlowMFGauss = new NormalMembershipFunction(Params.vDesired - Params.velocity_d2, (Params.velocity_d2 - Params.velocity_d1) / 2);

				var veryFastMFConst = new TrapezoidMembershipFunction(Params.vDesired + Params.velocity_d2, Params.vDesired + Params.velocity_d2, double.MaxValue, double.MaxValue);
				var veryFastMFGauss = new NormalMembershipFunction(Params.vDesired + Params.velocity_d2, (Params.velocity_d2 - Params.velocity_d1) / 2);

				AddTerm(_velocity, "Very_Slow", new CompositeMembershipFunction(MfCompositionType.Max, verySlowMFConst, verySlowMFGauss));
				AddTerm(_velocity, "Slow", new NormalMembershipFunction(Params.vDesired - Params.velocity_d1, Params.velocity_d1 / 2));
				AddTerm(_velocity, "Just_Right", new NormalMembershipFunction(Params.vDesired, Params.velocity_d1 / 2));
				AddTerm(_velocity, "Fast", new NormalMembershipFunction(Params.vDesired + Params.velocity_d1, Params.velocity_d1 / 2));
				AddTerm(_velocity, "Very_Fast", new CompositeMembershipFunction(MfCompositionType.Max, veryFastMFConst, veryFastMFGauss));

				var veryCloseMFConst = new TrapezoidMembershipFunction(-double.MaxValue, -double.MaxValue, -Params.distance_d2, -Params.distance_d2);
				var veryCloseMFGauss = new NormalMembershipFunction(-Params.distance_d2, (Params.distance_d2 - Params.distance_d1) / 2);

				var veryFarMFConst = new TrapezoidMembershipFunction(Params.distance_d2, Params.distance_d2, double.MaxValue, double.MaxValue);
				var veryFarMFGauss = new NormalMembershipFunction(Params.distance_d2, (Params.distance_d2 - Params.distance_d1) / 2);

				AddTerm(_distanceError, "Very_Close", new CompositeMembershipFunction(MfCompositionType.Max, veryCloseMFConst, veryCloseMFGauss));
				AddTerm(_distanceError, "Close", new NormalMembershipFunction(-Params.distance_d1, Params.distance_d1 / 2));
				AddTerm(_distanceError, "Just_Right", new NormalMembershipFunction(0, Params.distance_d1 / 2));
				AddTerm(_distanceError, "Far", new NormalMembershipFunction(Params.distance_d1, Params.distance_d1 / 2));
				AddTerm(_distanceError, "Very_Far", new CompositeMembershipFunction(MfCompositionType.Max, veryFarMFConst, veryFarMFGauss));

				AddTerm(_acceleration, "Brake_Hard", new NormalMembershipFunction(Params.brake_d2, (Params.brake_d2 - Params.brake_d1) / 2));
				AddTerm(_acceleration, "Brake", new NormalMembershipFunction(-Params.acceleration_d1, Params.acceleration_d1 / 2));
				AddTerm(_acceleration, "None", new NormalMembershipFunction(0, Params.acceleration_d1 / 2));
				AddTerm(_acceleration, "Accelerate", new NormalMembershipFunction(Params.acceleration_d1, Params.acceleration_d1 / 2));
				AddTerm(_acceleration, "Accelerate_Hard", new NormalMembershipFunction(Params.acceleration_d2, (Params.acceleration_d2 - Params.acceleration_d1) / 2));
			}
			else
			{
				throw new Exception("Invalid membership function type");
			}
		}

		private void AddTerm(FuzzyVariable var, string name, IMembershipFunction function)
		{
			FuzzyTerm term;
			try
			{
				term = var.GetTermByName(name);
			}
			catch (KeyNotFoundException)
			{
				term = null;
			}
			if (term == null)
			{
				var.Terms.Add(new FuzzyTerm(name, function));
			}
			else
			{
				term.MembershipFunction = function;
			}
		}

		public double GetOutput(double distanceDiff, double velocity)
		{
			var output = _mamdani.Calculate(new Dictionary<FuzzyVariable, double> { { _distanceError, distanceDiff }, { _velocity, velocity } });
			return output[_acceleration];
		}
	}
}
