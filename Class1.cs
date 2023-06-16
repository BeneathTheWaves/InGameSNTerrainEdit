using BepInEx;
using BepInEx.Logging;
using Nautilus.Handlers;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace ClassLibrary1
{ 
    [BepInPlugin("com.hamlet.Editor", "Editor", "0.0.1")]
    [BepInDependency("Esper89.TerrainPatcher",BepInDependency.DependencyFlags.SoftDependency)]
    public class Class1 : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static GameObject land;
        public static AssetBundle bundle;
        public static AssetBundle shaderbundle;
        void Awake()
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            Logger.LogInfo(names.ToString());
            var img = Assembly.GetExecutingAssembly().GetManifestResourceStream(names[0]);
            var texture = new Texture2D(1,1);
            using(var ms = new MemoryStream())
            {
                img.CopyTo(ms);
                texture.LoadImage(ms.ToArray());
            }
            // You can also register by just passing in Info.
            ModDatabankHandler.RegisterMod(new ModDatabankHandler.ModData()
            {
                guid = Info.Metadata.GUID,
                name = "In-Game World Editor",
                desc = "The world's first in-game world editor for Subnautica, with a full feature set and entity modifying support (coming at a later release)",
                version = "0.0.1",
                image = texture
            });

            bundle = AssetBundle.LoadFromFile(Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "Editor", "editor.assetbundle"));
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
        }
        public static IEnumerator EmptyCoroutine()
        {
            yield return null;
        }
    }

}
