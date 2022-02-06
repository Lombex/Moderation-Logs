using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace MainSpace
{
    public static class Patches
    {
        public static HarmonyMethod GetPatch(this Type Type, string Name, bool PublicMethod = false)
        {
            return new HarmonyMethod(Type.GetMethod(Name, (PublicMethod ? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Static));
        }
        public static void OnEventPatching(HarmonyLib.Harmony Instance)
        {
            try
            {
                Instance.Patch(typeof(Photon.Realtime.LoadBalancingClient).GetMethod(nameof(Photon.Realtime.LoadBalancingClient.OnEvent)), typeof(Patches).GetPatch(nameof(OnModerationEvent)));
            } catch (Exception e)
            {
                MelonLogger.Msg("OnEvent patch failed:" + e.Message);
            }
        }

        private static Dictionary<int, bool> ModerationBlockState = new Dictionary<int, bool>();
        private static Dictionary<int, bool> ModerationMuteState = new Dictionary<int, bool>();
        public static Dictionary<string, string> BlockList = new Dictionary<string, string>();
        public static Dictionary<string, string> MuteList = new Dictionary<string, string>();
        private static IEnumerator ModerationStates(int ActorID, bool BlockState, bool MuteState)
        {
            while (true)
            {
                Photon.Realtime.Player _GetPlayer = PhotonExtensions.LoadBalancingClient.GetPlayerByID(ActorID);
                if (_GetPlayer.field_Public_Player_0 != null && _GetPlayer.field_Public_Player_0.field_Private_APIUser_0 != null)
                {
                    if (BlockState && !ModerationBlockState[ActorID])
                    {
                        Console.WriteLine($"{_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName} has blocked u");
                        if (!BlockList.ContainsKey(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id)) 
                            BlockList.Add(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName, _GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id);
                    }
                    if (!BlockState && ModerationBlockState[ActorID])
                    {
                        Console.WriteLine($"{_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName} has unblocked u");
                        if (BlockList.ContainsKey(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id)) BlockList.Remove(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id);
                    }
                    if (MuteState && !ModerationMuteState[ActorID])
                    {
                        Console.WriteLine($"{_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName} has muted u");
                        if (!MuteList.ContainsKey(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id))
                            MuteList.Add(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName, _GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id);
                    }
                    if (!MuteState && ModerationMuteState[ActorID])
                    {
                        Console.WriteLine($"{_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName} has unmuted u");
                        if (MuteList.ContainsKey(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id)) MuteList.Remove(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id);
                    }
                    ModerationBlockState[ActorID] = BlockState;
                    ModerationMuteState[ActorID] = MuteState;
                    yield break;
                }
                yield return new UnityEngine.WaitForSeconds(0.1f);
            }
        }
        public enum GetModerationState
        {
            Block,
            Mute
        }
        private static IEnumerator NewModerationState(GetModerationState Type, int ActorID)
        {
            while (true)
            {
                Photon.Realtime.Player _GetPlayer = PhotonExtensions.LoadBalancingClient.GetPlayerByID(ActorID);
                if (_GetPlayer.field_Public_Player_0 != null && _GetPlayer.field_Public_Player_0.field_Private_APIUser_0 != null)
                {
                    switch (Type)
                    {
                        case GetModerationState.Block:
                            Console.WriteLine($"{_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName} Blocked Before!");
                            if (!BlockList.ContainsKey(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id))
                                BlockList.Add(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName, _GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id);
                            yield break;
                        case GetModerationState.Mute:
                            Console.WriteLine($"{_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName} Muted Before!");
                            if (!MuteList.ContainsKey(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id))
                                MuteList.Add(_GetPlayer.field_Public_Player_0.field_Private_APIUser_0.displayName, _GetPlayer.field_Public_Player_0.field_Private_APIUser_0.id);
                            yield break;
                    }
                }
                yield return new UnityEngine.WaitForSeconds(0.1f);
            }
        }
        private static bool OnModerationEvent(EventData __0)
        {
            if (__0.Code == 33)
            {
                object DataRPC = Utils.Serialization.FromIL2CPPToManaged<object>(__0.Parameters);
                string ModerationData = JsonConvert.SerializeObject(DataRPC, Formatting.Indented);
                JObject ParsedData = JObject.Parse(ModerationData);
                if (ParsedData["245"].SelectToken("1") != null && ParsedData["245"].SelectToken("10") != null && ParsedData["245"].SelectToken("11") != null)
                {
                    int ActorID = ParsedData["245"].SelectToken("1").ToObject<int>();
                    bool BlockState = ParsedData["245"].SelectToken("10").ToObject<bool>();
                    bool MuteState = ParsedData["245"].SelectToken("11").ToObject<bool>();
                    if (!ModerationBlockState.ContainsKey(ActorID))
                    {
                        ModerationBlockState.Add(ActorID, BlockState);
                        if (BlockState) MelonCoroutines.Start(NewModerationState(GetModerationState.Block, ActorID));
                    }
                    if (!ModerationMuteState.ContainsKey(ActorID))
                    {
                        ModerationMuteState.Add(ActorID, MuteState);
                        if (MuteState) MelonCoroutines.Start(NewModerationState(GetModerationState.Mute, ActorID));
                    }
                    MelonCoroutines.Start(ModerationStates(ActorID, BlockState, MuteState));
                    return true;
                } else return true;
            } else return true;
        }
    }
}
