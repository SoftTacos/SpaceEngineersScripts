/* Using this script as a learning base for thrust management and learning to "fly"
 * Script will calculate the height at which to ensurethrusters must be used to slow down and not hit the ground
 * currently uses lazy shortcuts and does not actually touch down, that's TODO. Just wanted something that works and has the fundamental math done right
 * TODO: manage gyros to orient craft straight down
 * TODO: manage thrusters in all directions to maneuver
 * TODO: actually land.
 * 
 * /


using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class SuicideBurn : MyGridProgram
    {
        //COPY FROM HERE
        //float targetAlt = 100.0f;
        float functionalMass = 0.0f;
        Vector3D currentNaturalGravity;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            //Runtime.UpdateFrequency = UpdateFrequency.Update1;//update once every game tick. options are non,Update1,Update10,Update100, once
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;//once run, this script needs to control the craft
            //grid is assumed perfectly normal to current gravity vector so don't need rotation or lateral motion. One step at a time
            //thrust up until X altitude
            //shut off thrust and IDs
            //turn on IDs when at minimum safe suicide burn height
            //if anything goes wrong, parachutes??

            //get remote control 
            List<IMyRemoteControl> controllers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(controllers);
            //lazily assuming one RC for now
            IMyRemoteControl controller = controllers[0];
            
            //testing to see all the data I can collect with a remote control block
            //Echo($"{controller.GetShipVelocities()}\n");//returns linear and angular velocity of the ship
            //Echo($"{controller.GetNaturalGravity()}\n");//Gets the vector pointing in the direction of planetary gravity, not normalized
            //Echo($"{controller.GetPosition()}\n");//this gets my world position. Handy
            double currentAlt = 0.0;//current altitude relative to the ground
            bool valid = controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out currentAlt);
            //Echo($"{elevation}\n");//this gets my world position. Handy
            //Echo($"{controller.CalculateShipMass().PhysicalMass}");//can get base mass(no cargo), total mass, and physical mass(ship mass+cargo adjusted by inv mult)

            //get thruster(s), all pointing down atm
            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
            float totalThrust = 0.0f;
            int numThrusters=0;
            foreach (var thruster in thrusters)
            {
                numThrusters++;
                totalThrust += thruster.MaxEffectiveThrust;
                //thruster.ThrustOverride = thruster.MaxThrust;
            }
            //calculate thrust needed to hover?
            functionalMass = controller.CalculateShipMass().PhysicalMass;
            currentNaturalGravity = controller.GetNaturalGravity();
            double gravityMagnitude = currentNaturalGravity.Length();//what a weird thing to call magnitude

            double thrustNeededToHover = functionalMass / gravityMagnitude;
            //I'm gonna mess up converting floats and doubles and someone's gonna die
           
            //this method won't work, but just want to do it for practice
            /*
            //thrust function: N = (x-(targetAlt-CurrentAlt))^3 + thrustNeededToHover//bound that by max thrust and min thrust
            double currentTargetThrust = targetAlt-currentAlt + thrustNeededToHover;
            if (currentTargetThrust < 0)
                currentTargetThrust = 0.0;
            Echo($"current total thrust output: {currentTargetThrust}");
            //divide the total needed thrust by total functional thrusters, set each thruster to that amount of thrust
            foreach (var thruster in thrusters)
            {
                thruster.ThrustOverride = (float)currentTargetThrust / numThrusters;
            }
            */

            //make craft lift off(from a hover) and then suicide burn?

            //what distance to stop == alt+Buffer => turn on thrust
            /*
            X = time to hit v=0
            then integrate from t=0 -> X
            that's stopping distance
            Calc1 coming back to haunt me
            */
            //manually solving for time being just to get something working
            double a = totalThrust / functionalMass - currentNaturalGravity.Length();
            double b = controller.GetShipVelocities().LinearVelocity.Length();
            double c = 0;// currentAlt;
            //Echo($"{a}:{b}:{c}");
            double thrustTime = b/(2*a);//y=ax^2+bx -> take derivative -> dy = 2ax+b | 0 = 2*a*x+b -> -b/2a=x//dropping the - on b because calculated B is absolute value and they would cancel irl

            //i need to calc time to stop the ship at current speed, compare that to the alt, then integrate

            Echo($"ThrustTime: {thrustTime}");
            Func<double, double> altitude = delegate (double x)
            {
                return a * x * x + b * x + c;
            };
            int slices = 10;
            double brakingHeight = integrate(0, thrustTime, altitude, slices);
            Echo($"ALT: {currentAlt}");
            Echo($"BRAKE: {brakingHeight}");
            
            double safetyBuffer = b/10 ;
            Echo($"safety buffer: {safetyBuffer}");

            //there are 60 "ticks" of ingame simulation per second

            if (currentAlt <= brakingHeight + safetyBuffer)
            {
                controller.DampenersOverride = true;
                
            }
            Echo($"DAMPENERS: {controller.DampenersOverride}");
        }

        public double integrate(double a, double b, Func<double, double> function, int n)//integrating manually because it's been 4 years
        {
            double area = 0.0;
            for(int i = 0; i < n; i++)
            {
                double start = ((b - a) / n) * i;
                double end = ((b - a) / n) * (i+1);
                area += (end - start) * (function(end) + function(start)) / 2;//( x2 - x1 ) * ( y2 - y1 )/2
            }
            return area;
        }

        //COPY FROM HERE
    }
}