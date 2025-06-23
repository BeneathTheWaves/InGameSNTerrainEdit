extern alias monocecil;
using BepInEx;
using BepInEx.Logging;
using monocecil::Mono.Cecil;
using monocecil::Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Buffers.Text;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using WorldStreaming;
using static MiniWorld;

namespace ClassLibrary1
{
    [BepInPlugin("com.Aqua.OhThePlacesYoullGO", "Oh The Places You'll Go", "0.0.1")]
    [BepInDependency("Esper89.TerrainPatcher")]
    public class Class1 : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public static GameObject land;
        public static AssetBundle bundle;
        public static AssetBundle shaderbundle;
        public static Material testMat;
        public static Material opaqueTestMat;
        void Awake() {
            var shader = Shader.Find("UWE/Terrain/Triplanar");
            if (shader == null)
            {
                logger.LogInfo("Null!");
            }
            testMat = new Material(Shader.Find("UWE/Terrain/Triplanar"));
            Texture2D tex = new Texture2D(1024, 1024);
            Span<byte> imgBytes = Encoding.UTF8.GetBytes(image64.str).AsSpan();
            int writtenBytes = 0;
            Base64.DecodeFromUtf8InPlace(imgBytes, out writtenBytes);
            tex.LoadImage(imgBytes.ToArray());
            var newTex = new Texture2D(1024, 1024, TextureFormat.ARGB32,false);
            newTex.SetPixels(tex.GetPixels());
            newTex.Apply();
            for(var x = 0; x < 1024; x++)
            {
                for(var y = 0; y < 1024; y++)
                {
                    var pix = newTex.GetPixel(x, y);
                    var newPix = new Color(pix.r, pix.g, pix.b, pix.grayscale);
                    newTex.SetPixel(x, y, newPix);
                }
            }
            newTex.Apply();
            testMat.mainTexture = newTex;
            opaqueTestMat = new Material(testMat)
            {
                renderQueue = 1000
            };
            opaqueTestMat.SetInt(ShaderPropertyID._ZWrite, 1);
            opaqueTestMat.SetInt(ShaderPropertyID._ColorMask, (int)byte.MaxValue);
            opaqueTestMat.SetInt(ShaderPropertyID._BlendSrcFactor, 1);
            opaqueTestMat.SetInt(ShaderPropertyID._BlendDstFactor, 0);
            opaqueTestMat.SetInt(ShaderPropertyID._IsOpaque, 1);
            opaqueTestMat.SetFloat(ShaderPropertyID._AlphaTestValue, 0.0f);
            logger = Logger;
        SetUpPatches();
        }
        public static void SetUpPatches()
        {
            var assemblydef = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
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
                if (Editor.isInEditor)
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
            On.WorldStreaming.BatchOctreesStreamer.EnqueueForUnloading += (orig, self, batch) =>
            {
                if (Editor.isInEditor)
                    Editor.SaveForTempBatch(batch.id);
                orig(self, batch);
            };
            On.WorldStreaming.BatchOctreesStreamer.OnBatchLoaded += (orig, self, index) =>
            {
                if (Editor.isInEditor)
                    Editor.LoadForTempBatch(index);
                orig(self, index);
            };
            IL.VoxelandVisualMeshSimplifier.BuildLayerObjects_IVoxelandChunk2_IVoxelandChunkInfo_bool_int_TerrainPoolManager += (il) =>
            {
                var c = new ILCursor(il);
                var blocktype = typeof(VoxelandBlockType);
     
                if (c.TryGotoNext(
                            i => i.MatchLdfld<VoxelandBlockType>("opaqueMaterial")
                       ))
                {
                    c.Remove();
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate((VoxelandBlockType typ,ClipmapChunk chunk) =>
                    {
                        logger.LogInfo("opaque!");
                        logger.LogInfo($"Original type: {typ.name}");
                        if (chunk is null || chunk.transform is null)
                        {
                            return typ.opaqueMaterial;
                        }
                        var pos = chunk.transform.position;
                        var batch = LargeWorldStreamer.main.GetContainingBatch(pos);
                        if(batch != new Int3(12,18,12))
                        {
                            return typ.opaqueMaterial;
                        }
                        var idx = 20;
                        var (x, y, z) = (idx / (5 * 5), idx % (5 * 5) / 5, idx % 5);
                        var chunkblock = LargeWorldStreamer.main.GetBlock(pos);
                        var nodeBlock = new Int3(x, y, z);
                        var transformed = LargeWorldStreamer.main.land.transform.InverseTransformPoint(nodeBlock.ToVector3());
                        nodeBlock = new Int3((int)transformed.x,(int)transformed.y,(int)transformed.z);
                        if (chunkblock != nodeBlock)
                        {
                            logger.LogInfo($"Not Equal! Chunk: {chunkblock} Node: {nodeBlock}");
                            return typ.opaqueMaterial;
                        }
                        logger.LogInfo("Match!");
                        return opaqueTestMat;
                    });
                }
                if (c.TryGotoNext(
                            i => i.MatchLdfld<VoxelandBlockType>("material")
                       ))
                {
                    c.Remove();
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate((VoxelandBlockType typ, ClipmapChunk chunk) =>
                    {
                        logger.LogInfo("not opaque!");
                        logger.LogInfo($"Original type: {typ.name}");
                        if (chunk is null || chunk.transform is null)
                        {
                            return typ.material;
                        }
                        var pos = chunk.transform.position;
                        var batch = LargeWorldStreamer.main.GetContainingBatch(pos);
                        if (batch != new Int3(12, 18, 12))
                        {
                            return typ.material;
                        } 
                        var idx = 20;
                        var (x, y, z) = (idx / (5 * 5), idx % (5 * 5) / 5, idx % 5);
                        var chunkblock = LargeWorldStreamer.main.GetBlock(pos);
                        var nodeBlock = new Int3(x, y, z);
                        var transformed = LargeWorldStreamer.main.land.transform.InverseTransformPoint(nodeBlock.ToVector3());
                        nodeBlock = new Int3((int)transformed.x, (int)transformed.y, (int)transformed.z);
                        if (chunkblock != nodeBlock)
                        {
                            logger.LogInfo($"Not Equal! Chunk: {chunkblock} Node: {nodeBlock}");
                            return typ.material;
                        }
                        logger.LogInfo("Match!");
                        return testMat;
                    });
                    //c.Emit(OpCodes.Ldfld, typeof(Class1).GetField(nameof(testMat)));
                }
            };
            }
        public static IEnumerator EmptyCoroutine()
        {
            yield return null;
        }
    }

}
