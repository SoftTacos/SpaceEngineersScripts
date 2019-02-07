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
    partial class HelloWorld : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.

        public void Program()
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
            //gonna doodle around and figure out how to use classes in C#, and interact with objectins in SE(Space Engineers)
            List<IMyLandingGear> gearsOfLanding = new List<IMyLandingGear>();
            GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(gearsOfLanding);
            foreach (var block in gearsOfLanding)
            {
                block.ToggleLock();
                LandingGearMode asdf = block.LockMode;
                bool billy = block.AutoLock;
                Echo($"ASDF'{billy}'!");
                if (billy)
                    block.AutoLock = !billy;
                //ITerminalProperty asdf = block.GetProperty("LockMode");
            }

            //messing around with text panels to actuall /helloworld/
            //put "OwO" tag on a LCD panel
            List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            //IMyTextPanel helloTextPanel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD Panel OwO");//getblockwithname doesn't match substrings
            //instead of getblockwithname, getblocksoftype, then find blocks of name?
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(textPanels);
            IMyTextPanel helloTextPanel;// = new IMyTextPanel();
            if (textPanels.Count > 0)
            {
                helloTextPanel = textPanels[0];
                helloTextPanel.ShowPublicTextOnScreen();
                Echo($"{helloTextPanel.ShowOnScreen}");
                helloTextPanel.WritePublicText("hello world\nthat was a lot of stuff for some text" ,false);

            }
            else
            {
                Echo("Text panel block not found");
            }
            //IMyTerminalBlock NANI = GridTerminalSystem.GetBlockWithName("OwO");
            //helloTextPanel = (IMyTextPanel)NANI;


        }

        public bool checkTextPanels(List<IMyTerminalBlock> panels)
        {
            return true;
        }
    }
}
