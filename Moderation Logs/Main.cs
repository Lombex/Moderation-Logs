using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using System.IO;
using Newtonsoft.Json;
using Utils;

namespace MainSpace
{
    public class Main : MelonMod
    {
        #pragma warning disable CS0108
        public static readonly HarmonyLib.Harmony Harmony = new HarmonyLib.Harmony("Moderation_Logs");
        public override void OnApplicationStart()
        {
            Patches.OnEventPatching(Harmony);
            MelonLogger.Msg("Moderation Logs Has been activated successfully!");
        }
    }
}
