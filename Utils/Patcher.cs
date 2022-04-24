using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;

namespace AdminLogger.Utils
{
    public static class Patcher
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static PatchContext ctx;

        public static void InitilizePatcherContext(PatchContext Context)
        {
            ctx = Context;
        }

        public static MethodInfo GetMethod<T>(string TargetMethodName, BindingFlags Flags)
        {
            return MethodGet(typeof(T), TargetMethodName, Flags, null);
        }

        public static MethodInfo GetMethod<T>(string TargetMethodName, BindingFlags Flags, Type[] Types)
        {
            return MethodGet(typeof(T), TargetMethodName, Flags, Types);
        }

        public static MethodInfo SuffixPatch<T>(string TargetMethodName, BindingFlags Flags, string ReplaceMentMethodName)
        {
            return Patch(false, typeof(T), TargetMethodName, Flags, ReplaceMentMethodName, null);
        }

        public static MethodInfo SuffixPatch<T>(string TargetMethodName, BindingFlags Flags, Type[] Types, string ReplaceMentMethodName)
        {
            return Patch(false, typeof(T), TargetMethodName, Flags, ReplaceMentMethodName, Types);
        }

        public static MethodInfo PrePatch<T>(string TargetMethodName, BindingFlags Flags, string ReplaceMentMethodName)
        {
            return Patch(true, typeof(T), TargetMethodName, Flags, ReplaceMentMethodName, null);
        }

        public static MethodInfo PrePatch<T>(string TargetMethodName, BindingFlags Flags, Type[] Types, string ReplaceMentMethodName)
        {
            return Patch(true, typeof(T), TargetMethodName, Flags, ReplaceMentMethodName, Types);
        }

        private static MethodInfo Patch(bool PreFix, Type TargetClass, string TargetMethodName, BindingFlags Flags, string ReplaceMentMethodName, Type[] Types)
        {
            try
            {
                MethodInfo FoundTargetMethod;
                if (Types == null)
                {
                    FoundTargetMethod = TargetClass.GetMethod(TargetMethodName, Flags);
                }
                else
                {
                    FoundTargetMethod = TargetClass.GetMethod(TargetMethodName, Flags, null, Types, null);
                }





                if (FoundTargetMethod == null)
                {
                    Log.Error("Unable to find patch method " + TargetClass.Name + "." + TargetMethodName);
                    return null;
                }



                Type CallingClassType = new StackFrame(2, false).GetMethod().DeclaringType;

                //Log.Warn("Debug CallingClass Type: " + CallingClassType.Name+"   Elapsed: "+ Watch.ElapsedMilliseconds);

                if (CallingClassType == null)
                {
                    Log.Error("Unable to find calling class!");
                }

                MethodInfo PatchedMethod = CallingClassType.GetMethod(ReplaceMentMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (PreFix)
                {
                    //Runs when we want to add a prefix;
                    ctx.GetPattern(FoundTargetMethod).Prefixes.Add(PatchedMethod);
                }
                else
                {
                    //Runs when we want to add a suffix;
                    ctx.GetPattern(FoundTargetMethod).Suffixes.Add(PatchedMethod);


                }


                return FoundTargetMethod;

            }
            catch (AmbiguousMatchException ex)
            {
                Log.Error(ex, "You need to specify the method types! More than one method named: " + TargetClass.Name + "." + TargetMethodName);

            }
            catch (ArgumentException ex)
            {
                Log.Error(ex, "Invalid Arguments for " + TargetMethodName + " exsisting in: " + TargetClass.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unkown Patch Error!");
            }

            return null;


        }

        private static MethodInfo MethodGet(Type TargetClass, string TargetMethodName, BindingFlags Flags, Type[] Types)
        {
            MethodInfo FoundTargetMethod;
            if (Types == null)
            {
                FoundTargetMethod = TargetClass.GetMethod(TargetMethodName, Flags);
            }
            else
            {
                FoundTargetMethod = TargetClass.GetMethod(TargetMethodName, Flags, null, Types, null);
            }

            if (FoundTargetMethod == null)
            {
                Log.Error("Unable to find patch method " + TargetClass.Name + "." + TargetMethodName);
                return null;
            }

            return FoundTargetMethod;

        }


    }
}
