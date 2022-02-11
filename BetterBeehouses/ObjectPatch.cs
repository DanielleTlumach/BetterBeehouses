﻿using HarmonyLib;
using StardewValley;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BetterBeehouses
{
    [HarmonyPatch(typeof(Object))]
    class ObjectPatch
    {
        private static ILHelper minutesElapsedPatch = new ILHelper("Object: Minutes Elapsed")
            .SkipTo(new CodeInstruction[] { 
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(Object).MethodNamed("get_name")),
                new(OpCodes.Ldstr, "Bee House"),
                new(OpCodes.Callvirt, typeof(string).MethodNamed("Equals", new[]{typeof(string)}))
            })
            .Skip(2)
            .Remove()
            .Add(new CodeInstruction[] { 
                new(OpCodes.Call,typeof(ObjectPatch).MethodNamed("CanProduceHere"))
            })
            .Finish();

        private static ILHelper dayUpdatePatch = new ILHelper("Object: Day Update")
            .SkipTo(new CodeInstruction[] {
                new(OpCodes.Callvirt, typeof(GameLocation).MethodNamed("GetSeasonForLocation")),
                new(OpCodes.Ldstr, "winter"),
                new(OpCodes.Callvirt, typeof(string).MethodNamed("Equals",new System.Type[]{typeof(string)}))
            })
            .Add(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_1),
                new(OpCodes.Call, typeof(ObjectPatch).MethodNamed("CantProduceToday"))
            })
            .SkipTo(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Object).FieldNamed("minutesUntilReady")),
                new(OpCodes.Ldsfld, typeof(Game1).FieldNamed("timeOfDay"))
            })
            .Remove()
            .Add(new CodeInstruction(OpCodes.Call,typeof(ObjectPatch).MethodNamed("GetProduceDays")))
            .Finish();

        private static ILHelper checkForActionPatch = new ILHelper("Object: Check for Action")
            .SkipTo(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(Object).MethodNamed("get_name")),
                new(OpCodes.Ldstr,"Bee House"),
                new(OpCodes.Callvirt,typeof(string).MethodNamed("Equals",new System.Type[]{typeof(string)}))
            })
            .SkipTo(new CodeInstruction[]
            {
                new(OpCodes.Callvirt, typeof(Character).MethodNamed("get_currentLocation")),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Object).FieldNamed("tileLocation"))
            })
            .Skip()
            .Remove()
            .Add(new CodeInstruction(OpCodes.Call,typeof(ObjectPatch).MethodNamed("GetSearchRange")))
            .SkipTo(new CodeInstruction[]
            {
                new(OpCodes.Call,typeof(Game1).MethodNamed("get_currentLocation")),
                new(OpCodes.Call,typeof(Game1).MethodNamed("GetSeasonForLocation")),
                new(OpCodes.Ldstr, "winter"),
                new(OpCodes.Callvirt, typeof(string).MethodNamed("Equals",new System.Type[]{typeof(string)}))
            })
            .Add(new CodeInstruction[]
            {
                new(OpCodes.Call,typeof(Game1).MethodNamed("get_currentLocation")),
                new(OpCodes.Call,typeof(ObjectPatch).MethodNamed("CantProduceToday"))
            })
            .Finish();

        //---------

        [HarmonyPatch("minutesElapsed")]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> minutesElapsed(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in minutesElapsedPatch.Run(instructions))
                yield return code;
        }

        [HarmonyPatch("DayUpdate")]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> DayUpdate(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in dayUpdatePatch.Run(instructions))
                yield return code;
        }

        [HarmonyPatch("checkForAction")]
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> checkForAction(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in checkForActionPatch.Run(instructions))
                yield return code;
        }

        //--------

        public static bool CantProduceToday(bool isWinter, GameLocation loc)
        {
            return isWinter && !Utils.GetProduceHere(loc, ModEntry.config.ProduceInWinter);
        }
        public static int GetSearchRange()
        {
            return ModEntry.config.FlowerRange;
        }
        public static int GetProduceDays()
        {
            return ModEntry.config.DaysToProduce;
        }
        public static bool CanProduceHere(GameLocation loc)
        {
            var where = ModEntry.config.UsableIn;
            return where is Config.UsableOptions.Anywhere || loc.IsOutdoors || loc.isGreenhouse && where is not Config.UsableOptions.Outdoors;
        }
    }
}
