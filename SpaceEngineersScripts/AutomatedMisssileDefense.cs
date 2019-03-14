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

    //MUCH less fancy than it sounds, this script manages the targeting data for specified turrets and triggers the launch code for LIDAR missiles. It does not do any of the missile guidance.
    partial class Program : MyGridProgram
    {

        string designatorNameTag = "Designator";
        List<IMyLargeTurretBase> designator;

        string launchControlNameTag = "LCS";
        List<IMyProgrammableBlock>;
        public Program()
        {

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //does turret have a target?
            //if no, return
            //if target find 
        }
    }
}