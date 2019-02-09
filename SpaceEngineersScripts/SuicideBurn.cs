/* Using this script as a learning base for thrust management and learning to "fly"
 * Script will calculate the height at which to ensurethrusters must be used to slow down and not hit the ground
 * currently uses lazy shortcuts and does not actually touch down, that's TODO. Just wanted something that works and has the fundamental math done right
 * TODO: manage gyros to orient craft
 * TODO: manage thrusters in all directions to maneuver
 * TODO: actually land. DONE.
 * 
 */

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
        
        float physicalMass = 0.0f;
        Vector3D currentNaturalGravity;
        int stage = -1;//STG0: safe; STG1: freefall; STG2: Initial slowdown; STG3: slow controlled descent
        double currentAltitude = 0.0;
        IMyRemoteControl controller;
        List<IMyThrust> thrusters;
        int numThrusters = 0;
        float maxTotalEffectiveThrust = 0.0f;
        float safeVelocity = 7.0f;
        //float minSafeAltitude = 50.0f;
        IMyRadioAntenna antenna;
        //Vector3D previousVelocity;
        float freefallThreshold;
        float craftHeight;//leaving as a const until I get a calculation working
        float touchdownHeight;
        MyShipVelocities lastVelocities;//linear and angular velocities
        double deltaT = 0.0;
        //IMyGridProgramRuntimeInfo runtimeInfo = null;
        public Program()//constructor is called when you "compile" script ingame
        {
            deltaT = Runtime.TimeSinceLastRun.TotalSeconds;//runtimeInfo.TimeSinceLastRun.TotalSeconds;

            List<IMyRemoteControl> controllers = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(controllers);
            controller = controllers[0];//lazilly assuming 1 RC for now

            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
            antenna = antennas[0];

            thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
            maxTotalEffectiveThrust = 0.0f;
            numThrusters = thrusters.Count;
            freefallThreshold = 10.0f;

            craftHeight = 5.0f;
            touchdownHeight = craftHeight + 1;//adding wiggle room because craft tends to bounce and code the inputs don't always capture that
            
            stage = 0;
            Storage = stage + ";"; ;

            Echo($"Code Initialized");

            Runtime.UpdateFrequency = UpdateFrequency.Update1;//once run, this script needs to control the craft actively

        }

        public void Save()//idk when this is called but storage needs to be updated just in case
        {
            Storage = stage + ";";
            Echo($"Saved");
        }

        public void Main(string argument, UpdateType updateSource)//main is called once per pysics update, ideally 60Hz
        {
            if (argument.ToLower() == "reset")
                stage = 0;
            bool valid = controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out currentAltitude);

            var parts = Storage.Split(';');
            stage = int.Parse(parts[0]);
            
            maxTotalEffectiveThrust = 0.0f;//have to recalculate every time because it increases with alt
            foreach (var thruster in thrusters){
                maxTotalEffectiveThrust += thruster.MaxEffectiveThrust;
            }
            physicalMass = controller.CalculateShipMass().PhysicalMass;
            currentNaturalGravity = controller.GetNaturalGravity();//
            double gravityMagnitude = currentNaturalGravity.Length();//what a weird thing to call magnitude of a vector
            
            Echo($"STAGE: {stage}\nSPEED: {controller.GetShipVelocities().LinearVelocity.Length()}\nALT: {currentAltitude}");
            antenna.HudText = stage.ToString();
            
            if (stage == 0){
                if (Freefall()){//if we are in freefall. Freefall is defined as accelerating at a rate ~= gravity, this implies thrusters are not acive. Also should check if it's heading towards or away from the gravity well. Linear alg time yay
                    foreach(var thruster in thrusters){
                        thruster.ThrustOverride = 0;
                    }
                    stage++;
                    Storage = stage + ";";
                }
            }

            else if(stage == 1 ){
                if (!Freefall())
                    stage = 0;
                double a = maxTotalEffectiveThrust / physicalMass - currentNaturalGravity.Length();//acceleration with full retro burn
                double b = -controller.GetShipVelocities().LinearVelocity.Length();//velocity
                double c = currentAltitude;
                double thrustStopTime = -b / (2 * a);//y=ax^2+bx ->  dy = 2ax+b | 0 = 2*a*x+b -> -b/2a=x

                Func<double, double> altitudeFunc = delegate (double x) {//x=time, y=altitude assuming full retro thrust
                    return a * x * x + b * x + c;
                };

                double minHeight = altitudeFunc(thrustStopTime);//the lowest point of the trajectory IF thrust is engaged instantly
                double calculatedStoppingDistance = currentAltitude - minHeight;
                double safetyBuffer = Math.Abs(b)*1.25+craftHeight;//a coefficient in front of the safety buffer feels wrong
                double safetyStoppingDistance = calculatedStoppingDistance + safetyBuffer;
               
                if (!controller.DampenersOverride && currentAltitude <= safetyStoppingDistance){
                    stage++;
                    Storage = stage + ";";
                    controller.DampenersOverride = true;
                }
            }

            else if(stage == 2){//descent burn has been initiated, goto stg 3 when a safe speed has been reached
                antenna.HudText = stage.ToString();
                
                if (controller.GetShipVelocities().LinearVelocity.Length() < safeVelocity) {
                    stage++;
                    Storage = stage + ";";
                }
            }

            else if(stage == 3){//target safe descent speed has been reached, maintain low speed until touchdown to planet
                float totalThrustNeededToHover = physicalMass * (float)currentNaturalGravity.Length();
                float idealThrustPerThruster = (float)totalThrustNeededToHover / (float)numThrusters;
                float thrustRatio = thrusters[0].MaxThrust / thrusters[0].MaxEffectiveThrust;//actual output of thrust is similar to but different from the input set by code/user. 
                float adjustedThrustPerThruster = idealThrustPerThruster * thrustRatio;

                foreach (var thruster in thrusters){
                    thruster.ThrustOverride = adjustedThrustPerThruster;
                }

                if (currentAltitude < touchdownHeight){
                    stage++;
                    Storage = stage + ";";

                    foreach (var thruster in thrusters)
                    {
                        controller.DampenersOverride = false;
                        thruster.ThrustOverride = 0.0f;
                    }
                }
                
            }

            else if(stage == 4){//touchdown, turn stuff off pls
                antenna.HudText = stage.ToString();
                Echo($"TOUCHDOWN");
                stage = 0;
                Storage = stage + ";";
                Runtime.UpdateFrequency = UpdateFrequency.None;//stop running the script
            }
            lastVelocities = controller.GetShipVelocities();
        }

        public bool Freefall(){
            double tolerance = 0.5;//TODO
            Vector3D currentAcceleration = ( controller.GetShipVelocities().LinearVelocity - lastVelocities.LinearVelocity ) / Runtime.TimeSinceLastRun.TotalSeconds;
            Echo($"DIFF:{controller.GetNaturalGravity() - currentAcceleration}");
            if (Math.Abs(controller.GetNaturalGravity().X - currentAcceleration.X) < tolerance && Math.Abs(controller.GetNaturalGravity().Y - currentAcceleration.Y) < tolerance && Math.Abs(controller.GetNaturalGravity().Z - currentAcceleration.Z) < tolerance)
                return true;
            return false;
        }
        //COPY FROM HERE
    }
}