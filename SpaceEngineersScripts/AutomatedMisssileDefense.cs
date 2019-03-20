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
        //keep this the same as your lidar designator tag for easy use
        string designatorNameTag = "R_DESIGNATOR";
        //what tag to look for on the programmable block that controls the missile.
        string missileComputerTag = "TGASM";//name tag on current test missile script "Turret Guided Anti Ship Missile"

        List<IMyProgrammableBlock> missileComputers;
        List<IMyLargeTurretBase> designators;
        List<MyDetectedEntityInfo> targets;

        //nice little visualizer to tell me the script is just idle
        List<char> runningStrings = new List<char>() { '|', '/', '-', '\\' };
        int runningIndex = 0;
        public Program()
        {
            targets = new List<MyDetectedEntityInfo>();
            designators = new List<IMyLargeTurretBase>();
            missileComputers = new List<IMyProgrammableBlock>();

            List<IMyLargeTurretBase> possibleDesignators = new List<IMyLargeTurretBase>();
            GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(possibleDesignators);
            foreach(IMyLargeTurretBase turret in possibleDesignators) {
                if (turret.CustomName.Contains(designatorNameTag))
                    designators.Add(turret);
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            runningIndex++;
            if (runningIndex >= runningStrings.Count)
                runningIndex = 0;
            Echo($"[{runningStrings[runningIndex]}]");

            //does turret have a target?
            foreach (IMyLargeTurretBase designator in designators) {
                if (designator.HasTarget) {
                    targets.Add(designator.GetTargetedEntity());
                }
            }
            //if no, return
            if (targets.Count == 0)//might need to add logic for determining if we want to shoot at those targets here
                return;

            //look for all programmable blocks of name NAME TAG and launch 1(no logic for now, just want POC)
            List<IMyProgrammableBlock> possibleMissileComputers = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(possibleMissileComputers);
            foreach (IMyProgrammableBlock computer in possibleMissileComputers){
                if (computer.CustomName.Contains(missileComputerTag))
                    missileComputers.Add(computer);
            }
            if (missileComputers.Count > 0) {
                missileComputers[0].TryRun("");
                Echo($"LAUNCHING: ");
                Runtime.UpdateFrequency = UpdateFrequency.None;//turn it off for now for debugging
            }//wipe all targets for now
            targets.Clear();
            missileComputers.Clear();
        }
    }
}