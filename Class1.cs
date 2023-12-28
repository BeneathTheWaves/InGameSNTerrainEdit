using BepInEx;
using BepInEx.Logging;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace ClassLibrary1
{
    [BepInPlugin("com.Sora.OhThePlacesYoullGO", "Oh The Places You'll Go", "0.0.1")]
    [BepInDependency("Esper89.TerrainPatcher", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency((DearImGuiInjection.Metadata.GUID))]
    public class Class1 : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static GameObject land;
        public static AssetBundle bundle;
        public static AssetBundle shaderbundle;
        void Awake() {
        logger = Logger;
        SetUpPatches();
        }
        public static void SetUpPatches()
        {
            On.uGUI_MainMenu.Awake += (orig, self) =>
            {
                orig(self);
                Patches.AwakePatchuGUI(self);
            };
            On.PAXTerrainController.LoadAsync += (orig, self) =>
            {
                if (Editor.isInEditor)
                {
                    land = self.gameObject;
                    Editor.voxeland = self.gameObject;
                    return Editor.Load();
                }
                return orig(self);
            };
            On.SaveLoadManager.GetTemporarySavePath += orig =>
            {
                if (!Editor.isInEditor)
                {
                    return orig();
                }
                return Path.Combine(Directory.GetCurrentDirectory(), "tempsavepathfix");
            };
            On.PDASounds.LateUpdate += (orig, self) =>
            {
                if (!Editor.isInEditor)
                    orig(self);
            };
            On.LargeWorldStreamer.Start += (orig, self) =>
            {
                if (Editor.isInEditor)
                {
                    return Patches.PatchLWSStart(self);
                }
                return orig(self);
            };
            On.BehaviourLOD.UpdateCachedData += orig =>
            {
                if (!Editor.isInEditor)
                    orig();
            };
            On.Scareable.ScheduledUpdate += (orig, self) =>
            {
                if (!Editor.isInEditor)
                    orig(self);
            };
            On.AggressiveWhenSeePlayer.GetAggressionTarget += (orig, self) =>
            {
                if (!Editor.isInEditor)
                    return orig(self);
                return null;
            };
            On.AggressiveWhenSeeTarget.IsTargetValid_GameObject += (orig, self, go) =>
            {
                if (!Editor.isInEditor)
                    return orig(self, go);
                return false;
            };
            On.SwimBehaviour.SwimToInternal += (orig, self, targetPosition, targetDirection, velocity, overshoot, ignoreTargetOverride) =>
            {
                if(Editor.isInEditor)
                {
                    return;
                } else
                {
                    orig(self, targetPosition, targetDirection, velocity, overshoot, ignoreTargetOverride);
                }
            };
            On.DayNightCycle.Update += (orig, self) =>
            {
                if (!Editor.isInEditor)
                    orig(self);
            };
            On.WorldStreaming.WorldStreamer.GetOctreesStreamer += (orig, self, lod) =>
            {
                if (Editor.isInEditor)
                    return self.octreesStreamer;
                return orig(self, lod);
            };
            On.LargeWorldStreamer.UnloadBatch += (orig, self, index) =>
            {
                if (Editor.isInEditor)
                    Editor.SaveForTempBatch(index);
                orig(self, index);
            };
            On.LargeWorldStreamer.FinalizeLoadBatch += (orig, self, index) =>
            {
                if (Editor.isInEditor)
                    Editor.LoadForTempBatch(index);
                orig(self, index);
            };
        }
        public static IEnumerator EmptyCoroutine()
        {
            yield return null;
        }
    }

}
