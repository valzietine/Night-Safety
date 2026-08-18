using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace NightSafety.Compatibility
{
    [StaticConstructorOnStartup]
    public static class RimWorldTogetherIntegration
    {
        static RimWorldTogetherIntegration()
        {
            Assembly? client = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "RTClient");
            Type? loader = client?.GetType("GameClient.Misc.MapSaveLoader");
            MethodInfo? stringToMap = loader?.GetMethod("StringToMap", BindingFlags.Public | BindingFlags.Static);
            Assembly? harmonyAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "0Harmony");
            Type? harmonyType = harmonyAssembly?.GetType("HarmonyLib.Harmony");
            Type? harmonyMethodType = harmonyAssembly?.GetType("HarmonyLib.HarmonyMethod");
            if (stringToMap == null || harmonyType == null || harmonyMethodType == null) return;

            try
            {
                object harmony = Activator.CreateInstance(harmonyType, "valzietine.nightsafety.rimworldtogether")!;
                MethodInfo postfix = typeof(RimWorldTogetherIntegration).GetMethod(nameof(AfterStringToMap),
                    BindingFlags.NonPublic | BindingFlags.Static)!;
                object harmonyPostfix = Activator.CreateInstance(harmonyMethodType, postfix)!;
                MethodInfo patch = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name == "Patch")
                    .First(method => method.GetParameters().Length >= 3
                        && typeof(MethodBase).IsAssignableFrom(method.GetParameters()[0].ParameterType));
                object?[] arguments = new object?[patch.GetParameters().Length];
                arguments[0] = stringToMap;
                arguments[2] = harmonyPostfix;
                patch.Invoke(harmony, arguments);
                Log.Message("[Night Safety] RimWorld Together detected, map-transfer ownership repair enabled.");
            }
            catch (Exception exception)
            {
                Log.Error($"[Night Safety] RimWorld Together ownership repair could not be installed: {exception}");
            }
        }

        private static void AfterStringToMap(Map __result)
        {
            __result?.GetComponent<NightSafetyMapComponent>()?.RepairTransferredHarasserOwnership();
        }
    }
}
