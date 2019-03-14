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
        //EXAMPLE SETUP: 
        string PISTON_GROUP = "ElevatorPistons";
        List<IMyExtendedPistonBase> pistons;
        Dictionary<string, float> floors;//floor height will be indexed off of the pistons being at min length
        float maxheight;
        //specify direction? 
        char keyDelimiter = ':';
        char pairDelimiter = ';';
        //handy ugly mapping of size of a single block to grid size since keen doesn't provide that
        Dictionary<VRage.Game.MyCubeSize, float> GridSizeMapping = new Dictionary<MyCubeSize, float>() { { VRage.Game.MyCubeSize.Large, 2.5f }, { VRage.Game.MyCubeSize.Small, 0.5f } };

        public Program()
        {
            //constructor
            floors = new Dictionary<string, float>();
            floors = setupFloors(Me.CustomData);
            foreach (string floor in floors.Keys){
                Echo($"{floor}->{floors[floor]}");
            }
            pistons = new List<IMyExtendedPistonBase>();
            //GridTerminalSystem.GetBlocksOfType<IMyExtendedPistonBase>(pistons);
            GridTerminalSystem.GetBlockGroupWithName(PISTON_GROUP).GetBlocksOfType<IMyExtendedPistonBase>(pistons);
            //maxheight = pistons.Count* GridSizeMapping[pistons[0].CubeGrid.GridSizeEnum]*2;
            maxheight = 0.0f;
            foreach(IMyExtendedPistonBase piston in pistons) {
                maxheight += piston.HighestPosition;
            }
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (!floors.ContainsKey(argument))
            {
                Echo($"Invalid Argument! Recompile if you have changed the CustomData");
                return;
            }
            float targetHeight = floors[argument];
            //TODO: sanity check the target floor heights
            //Echo($"{argument}:{Me.CustomData.Split(",")}");
            //get current height
            float currentHeight = 0.0f;
            foreach (IMyExtendedPistonBase piston in pistons){
                currentHeight += piston.CurrentPosition;
                Echo($"{piston.Status}");
                piston.MinLimit = targetHeight / pistons.Count;//doing it the quick n dirty way this time
                piston.MaxLimit = targetHeight / pistons.Count;

            }
            Echo($"CURRENT:{currentHeight}");
            Echo($"TARGET:{targetHeight}");

            //pistons can be below, above or at target
            float direction = targetHeight - currentHeight;
            Echo($"DIR:{direction}");
            int multiplier = Math.Sign(direction*pistons[0].Velocity);
            if (multiplier == 0)
            {
                Echo($"MULT=0 EXITING");
                return;
            }
            Echo($"{multiplier}");
            foreach (IMyExtendedPistonBase piston in pistons){
                Echo($"{piston.Velocity}");
                piston.Velocity = piston.Velocity*multiplier;
                Echo($"{piston.Velocity}");

            }

            //TODO: If it's going up, set min to 0, if it's going down, set max to max value. This makes the script unintrusive to players who might not want to use it
        }

        private Dictionary<string, float> setupFloors(string customData)
        {
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