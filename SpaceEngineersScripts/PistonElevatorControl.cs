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
    partial class Program : MyGridProgram
    {
        //This is the name of the group the pistons should belong to
        string PISTON_GROUP = "ElevatorPistons";
        //character that separates the floor and it's corresponding height/ '\n' for newline
        char keyDelimiter = ':';
        //character that separates a floor+height pair. '\n' for newline
        char pairDelimiter = ';';

        //NO TOUCHY BELOW HERE
        List<IMyExtendedPistonBase> pistons;
        Dictionary<string, float> floors;//floor height will be indexed off of the pistons being at min length
        float maxHeight;
        
        public Program(){
            floors = new Dictionary<string, float>();
            floors = setupFloors(Me.CustomData);
            pistons = new List<IMyExtendedPistonBase>();
            GridTerminalSystem.GetBlockGroupWithName(PISTON_GROUP).GetBlocksOfType<IMyExtendedPistonBase>(pistons);
            maxHeight = 0.0f;
            foreach(IMyExtendedPistonBase piston in pistons) {
                maxHeight += piston.HighestPosition;
            }
            foreach(string floor in floors.Keys){
                if (floors[floor] > maxHeight)
                    Echo($"Height {floors[floor]} exceeds the maximum length of the elevator!");
            }
        }

        public void Main(string argument, UpdateType updateSource){
            if (!floors.ContainsKey(argument)){
                Echo($"Invalid Argument! Recompile if you have changed the CustomData");
                return;
            }
            float targetHeight = floors[argument];
            float currentHeight = 0.0f;
            foreach (IMyExtendedPistonBase piston in pistons){
                currentHeight += piston.CurrentPosition;
                piston.MinLimit = targetHeight / pistons.Count;//don't have to care about the piston's minlimit/maxlimit going out of bounds because SE handles that
                piston.MaxLimit = targetHeight / pistons.Count;
            }
            
            //pistons can be below, above or at target
            float direction = targetHeight - currentHeight;
            int multiplier = Math.Sign(direction*pistons[0].Velocity);
            if (multiplier == 0){//shouldn't happen unless the player has set the velocity to 0, but juuuust in case
                return;
            }
            foreach (IMyExtendedPistonBase piston in pistons){
                piston.Velocity = piston.Velocity*multiplier;
                if(piston.Velocity > 0){
                    piston.MinLimit = 0;
                }
                else{
                    piston.MaxLimit = piston.HighestPosition;
                }
            }
        }

        private Dictionary<string, float> setupFloors(string customData){
            Dictionary<string, float> newFLoors = new Dictionary<string, float>();
            string[] floorPears = customData.Split(pairDelimiter);
            foreach (string pair in floorPears){
                string[] individualPair = pair.Split(keyDelimiter);
                newFLoors[individualPair[0]] = float.Parse(individualPair[1]);
            }
            return newFLoors;
        }
    }
}