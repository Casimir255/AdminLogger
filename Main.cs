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
using AdminLogger.Utils;
using Torch.Managers.PatchManager;
using AdminLogger.AntiCheat;
using AdminLogger.Configs;
using System.IO;

namespace AdminLogger
{
    public class Main : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetLogger("AdminLogger");

        private static MethodInfo DenyPlayersProfiling;
        private static PatchManager _pm;
        private static PatchContext _context { get; set; }


        public static Settings Config => _config?.Data;
        public static Persistent<Settings> _config;

        public bool ServerRunning { get; private set; }


        public override void Init(ITorchBase torch)
        {
            try
            {
                string path = Path.Combine(StoragePath, "AdminLogger.cfg");
                _config = Persistent<Settings>.Load(path);
                SetLoggingRules();


                _pm = torch.Managers.GetManager<PatchManager>();
                _context = _pm.AcquireContext();


                Log.Warn("Lauching Big Brother Protocal");
             
                ApplyPatch();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }


        private void SetLoggingRules()
        {
            if (!Config.AdminLoggerOwnLog)
                return;

            var rules = LogManager.Configuration.LoggingRules;

            for (int i = rules.Count - 1; i >= 0; i--)
            {
                //Log.Warn(rules[i].LoggerNamePattern);

                if (rules[i].LoggerNamePattern != "AdminLogger")
                    continue;

                rules.RemoveAt(i);
            }


            var logTarget = new FileTarget
            {
                FileName = "Logs/AdminLog-${shortdate}.log",
                Layout = "${var:logStamp} ${logger}: ${var:logContent}",

            };


 



            var fullRule = new LoggingRule("AdminLogger", LogLevel.Debug, logTarget) { Final = true };



            LogManager.Configuration.LoggingRules.Insert(0, fullRule);
            LogManager.Configuration.Reload();
        }


        private static void ApplyPatch()
        {
            try
            {

                Patcher.InitilizePatcherContext(_context);

                //AntiCheatClass.ApplyPatching();
                Harmony s = new Harmony("AdminLogger");
                s.PatchAll();

                AdminLoggerClass.ApplyPatch(s);
                JoinValidation m = new JoinValidation();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
            }
        }



    }

}
