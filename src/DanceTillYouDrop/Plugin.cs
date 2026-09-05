using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon.StructWrapping;
using HarmonyLib;
using Sirenix.Utilities;
using UnityEngine;

namespace DanceTillYouDrop;

// Here are some basic resources on code style and naming conventions to help
// you in your first CSharp plugin!
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
// https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces

[BepInPlugin("com.github.Elteeb.DanceTillYouDrop", "DanceTillYouDrop", "0.2.0")]
public partial class Plugin : BaseUnityPlugin
{
    public const string Id = "com.github.Elteeb.DanceTillYouDrop";
    public static string Name => "DanceTillYouDrop";
    public static string Version => "0.2.0";
    internal static ManualLogSource Log { get; private set; } = null!;
    private static string[] emotesList = new[]
    {
        "A_Scout_Emote_Dance1",
        "A_Scout_Emote_Dance2",
        "A_Scout_Emote_Dance2",
        "A_Scout_Emote_Nono",
        "A_Scout_Emote_Panic",
        "A_Scout_Emote_Salute",
        "A_Scout_Emote_Shrug",
        "A_Scout_Emote_Sit",
        "A_Scout_Emote_Think",
        "A_Scout_Emote_ThumbsUp",
        
    };
    private class Patcher
    {
        [HarmonyPatch(typeof(Character), "CreateHelperObjects")]
        [HarmonyPostfix]
        public static void CharacterUpdateAnims(Character __instance)
        {

            List<KeyValuePair<AnimationClip, AnimationClip>> clips = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            AnimatorOverrideController overrideController = new AnimatorOverrideController();
            overrideController.runtimeAnimatorController = __instance.refs.animator.runtimeAnimatorController;
            
            //Modify clips to be set to looping and build new override controller https://discussions.unity.com/t/animation-clip-looping-issue/257886/2
            foreach (AnimationClip originalClip in overrideController.animationClips.Where(o => emotesList.Contains(o.name)))
            { 
                AnimationClip newClip = Object.Instantiate(originalClip);
                newClip.wrapMode = WrapMode.Loop;
                clips.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip));
            }
            overrideController.ApplyOverrides(clips);
            __instance.refs.animator.runtimeAnimatorController = overrideController;
        }
        
        
        [HarmonyPatch(typeof(CharacterAnimations), "Update")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CharacterAnimationsUpdateLogic(IEnumerable<CodeInstruction>  instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            int targetIndex1 = 0;
            for (int i = 1; i < instructionList.Count; i++)
            {
                if (instructionList[i].operand == null)
                    continue;

                if (instructionList[i].opcode == OpCodes.Ldc_R4 && (float)instructionList[i].operand == 2f)
                {
                    targetIndex1 = i;
                    break;
                }
            }
            Log.LogInfo($"DanceTillYouDrop CharacterUpdateLogic found! {instructionList.Count}");
            instructionList[targetIndex1] = new  CodeInstruction(OpCodes.Ldc_R4, float.PositiveInfinity); //Replace 2f with Infinity so it cannot be canceled over time
            
            return instructionList;

        }
        
    }

    private void Awake()
    {
        // BepInEx gives us a logger which we can use to log information.
        // See https://lethal.wiki/dev/fundamentals/logging
        Log = Logger;

        // BepInEx also gives us a config file for easy configuration.
        // See https://lethal.wiki/dev/intermediate/custom-configs

        // We can apply our hooks here.
        // See https://lethal.wiki/dev/fundamentals/patching-code

        // Log our awake here so we can see it in LogOutput.log file
        Log.LogInfo($"Plugin {Name} is loaded!");
        Harmony.CreateAndPatchAll(typeof(Patcher));
    }
}
