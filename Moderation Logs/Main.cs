using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using System.IO;
using Newtonsoft.Json;

namespace MainSpace
{
    public class Main : MelonMod
    {
        #pragma warning disable CS0108
        public static readonly HarmonyLib.Harmony Harmony = new HarmonyLib.Harmony("Moderation Logs");

        public override void OnApplicationStart()
        {
            Directory.CreateDirectory("./Mods/ModerationLogs");
            Patches.OnEventPatching(Harmony);
        }

        public override void OnApplicationQuit()
        {
            string BlockedUsers = JsonConvert.SerializeObject(Patches.BlockList, Formatting.Indented);
            File.WriteAllText("./Mods/ModerationLogs/BlockedUsers.json", BlockedUsers);

            string MutedUsers = JsonConvert.SerializeObject(Patches.MuteList, Formatting.Indented);
            File.WriteAllText("./Mods/ModerationLogs/MutedUsers.json", MutedUsers);
        }
    }
}
