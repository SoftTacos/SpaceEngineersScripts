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
        List<IMySensorBlock> sensors;
        IMyRadioAntenna antenna;

        public Program(){
            gyros = new List<IMyGyro>();
            controllers = new List<IMyRemoteControl>();
            sensors = new List<IMySensorBlock>();
            List<IMyRadioAntenna> antennas = new List<IMyRadioAntenna>();
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
            antenna = antennas[0];

            Echo("COMPILED");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save(){

        }

        public void Main(string argument, UpdateType updateSource){
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(controllers);
            GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors);

            if (controllers.Count < 1 || gyros.Count < 1)
            {
                Echo("ERROR: either no controller or gyro");
                return;
            }
            IMyRemoteControl controller = controllers[0];//
            
            //Vector2 yawAndRoll = controller.RotationIndicator;
            //Vector3 inputRotationVector = new Vector3(-yawAndRoll.X, yawAndRoll.Y, controller.RollIndicator);//pitch, yaw, roll. What?? ok.

            IMySensorBlock sensor = sensors[0];
            //Echo($"{sensor.LastDetectedEntity.Position}\n{sensor.LastDetectedEntity.Orientation}\n{sensor.LastDetectedEntity.Velocity}");
            //direction to the target
            Vector3D p1 = controller.GetPosition();
            Vector3D p2 = sensor.LastDetectedEntity.Position;
            Vector3D p3 = p2 - p1;
            //rotation to the target
            p3.Normalize();
            Vector3D heading = controller.WorldMatrix.Forward;
            heading.Normalize();
            double angle = Math.Acos(heading.Dot(p3));//this doesn't account for up vector for now
            Echo($"ANGLE:{angle}");
            Echo($"p3:{p3}");
            antenna.HudText = angle.ToString();
            //axis of rotation
            Vector3D cross = heading.Cross(p3);
            //var relativeRotation = Vector3D.Transform(cross, controller.WorldMatrix);

            double yaw; double pitch;
            GetRotationAngles(p3, controller.WorldMatrix.Forward, controller.WorldMatrix.Left, controller.WorldMatrix.Up, out yaw, out pitch);

            

            applyRotation(VECTOR@, gyros, controller);
            //turn axis into ship coordinates

            //calc the new matrix after that rotation

            //IF there is a gravity
            //calc angle to rotate by and axis of rotation, add that rotation to the rotation already underway
            //align the grav and up vector into the same plane...?
            //calc what my desired forward vector is(p3?), then 

            //need to rotate and project up vector in line with art/nat gravity
            //Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
            //applyRotation(inputRotationVector, gyros, controller);

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

        Vector3D VectorProjection(Vector3D a, Vector3D b) //proj a on b    
        {
            Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
            return projection;
        }

        Vector3D VectorReflection(Vector3D a, Vector3D b, double rejectionFactor = 1) //reflect a over b    
        {
            Vector3D project_a = VectorProjection(a, b);
            Vector3D reject_a = a - project_a;
            Vector3D reflect_a = project_a - reject_a * rejectionFactor;
            return reflect_a;
        }

        double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
        {
            if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
                return 0;
            else
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
        }

        void GetRotationAngles(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double yaw, out double pitch)
        {
            //Dependencies: VectorProjection() | VectorAngleBetween()
            var projectTargetUp = VectorProjection(v_target, v_up);
            var projTargetFrontLeft = v_target - projectTargetUp;

            yaw = VectorAngleBetween(v_front, projTargetFrontLeft);
            pitch = VectorAngleBetween(v_target, projTargetFrontLeft);

            //---Check if yaw angle is left or right  
            //multiplied by -1 to convert from right hand rule to left hand rule
            yaw = -1 * Math.Sign(v_left.Dot(v_target)) * yaw;

            //---Check if pitch angle is up or down    
            pitch = Math.Sign(v_up.Dot(v_target)) * pitch;

            //---Check if target vector is pointing opposite the front vector
            if (pitch == 0 && yaw == 0 && v_target.Dot(v_front) < 0)
            {
                yaw = Math.PI;
            }
        }

        //COPY
    }
}