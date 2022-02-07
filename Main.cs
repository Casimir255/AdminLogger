using AdminLogger.AdminLogging;
using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using static Sandbox.Game.Entities.MyCubeGrid;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using System.Reflection;
using System.Linq;
using System;

namespace AdminLogger
{
    public class Main : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetLogger("AdminLogger");

        private static Harmony Patcher = new Harmony("AdminLogger");

        private static MethodInfo DenyPlayersProfiling;



        public bool ServerRunning { get; private set; }

        public override void Init(ITorchBase torch)
        {
            SetLoggingRules();


            Log.Warn("Lauching Big Brother Protocal");

            ApplyPatch();
        }


        private void SetLoggingRules()
        {
            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--)
            {
                

                if (rules[i].LoggerNamePattern != "AdminLogger") 
                    continue;

                rules.RemoveAt(i);
            }


            var logTarget = new FileTarget
            {
                FileName = "Logs/AdminLog-${shortdate}.log",
                Layout = "${var:logStamp} ${logger}: ${var:logContent}",
                
            };

           // Target Rule = LogManager.Configuration.AllTargets.FirstOrDefault(x => x.Name == "console");
           // if (Rule != null)
           // {
               // var ConsoleRule = new LoggingRule("console", LogLevel.Debug, logTarget) { Final = true };
               // LogManager.Configuration.LoggingRules.Insert(1, ConsoleRule);
           // }



            var fullRule = new LoggingRule("AdminLogger", LogLevel.Debug, logTarget) { Final = true  };



            LogManager.Configuration.LoggingRules.Insert(0, fullRule);
            LogManager.Configuration.Reload();
        }

        private static void ApplyPatch()
        {
            try
            {
                Patcher.PatchAll();
                AdminLoggerClass.ApplyPatch(Patcher);

            }catch(Exception ex)
            {
                Log.Fatal(ex);
            }
        }



    }

}
