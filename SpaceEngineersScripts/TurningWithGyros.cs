//most important part of this is the applyRotation method, it successfully translates "requested" rotation of the 
//  grid relative to some block, and transforms the rotation into the needed X,Y,Z rotations on each gyro, since gyros 
//  can be oriented in any direction
//script captures the input from the user input and directs the ApplyRottion method to do so. Functional parity!-sih
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
    partial class TurningWithGyros : MyGridProgram
    {
        //copy
        
        MyShipVelocities previousShipVelicities;
        List<IMyGyro> gyros;
        List<IMyRemoteControl> controllers;
        public Program(){
            gyros = new List<IMyGyro>();
            controllers = new List<IMyRemoteControl>();
            Echo("COMPILED");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save(){

        }

        public void Main(string argument, UpdateType updateSource){
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(controllers);

            if (controllers.Count < 1 || gyros.Count < 1)
            {
                Echo("ERROR: either no controller or gyro");
                return;
            }
            IMyRemoteControl controller = controllers[0];//
            controller.GetNaturalGravity();
            Quaternion conQuat = new Quaternion();
            controller.Orientation.GetQuaternion(out conQuat);
            Echo($"{conQuat}");

            
            Vector2 yawAndRoll = controller.RotationIndicator;
            Vector3 inputRotationVector = new Vector3(-yawAndRoll.X, yawAndRoll.Y, controller.RollIndicator);//pitch, yaw, roll. What?? ok.


            applyRotation(inputRotationVector, gyros, controller);

            //keep this at the end
            previousShipVelicities = controllers[0].GetShipVelocities();
        }
        
        private void applyRotation(Vector3D rotationInput, List<IMyGyro> gyros, IMyTerminalBlock reference) {//converting everything to world space because that's conceptually simple, might redo in ship's space later
            //ship in world
            var shipMatrix = reference.WorldMatrix;//matrix of the reference block in world space. used as the orientation of the overall ship
            //input -> world
            var relativeRotationVec = Vector3D.TransformNormal(rotationInput, shipMatrix);//rotation input in worldspace 

            foreach (IMyGyro gyro in gyros) {
                gyro.GyroOverride = true;
                //gyro in world 
                var gyroMatrix = gyro.WorldMatrix;//matrix of the gryo's orientation
                //input in world -> ship. take inverse to go back
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));

                gyro.Pitch = -(float)transformedRotationVec.X;
                gyro.Yaw = (float)transformedRotationVec.Y;
                gyro.Roll = (float)transformedRotationVec.Z;

            }
        }

        //COPY
    }
}