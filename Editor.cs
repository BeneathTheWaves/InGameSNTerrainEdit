using ClassLibrary1.WorldStreaming;
using IL.UnityEngine.PostProcessing;
using mset;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using uSky;
using UWE;
using WorldStreaming;
using DearImguiSharp;
using DearImGuiInjection.BepInEx;
using static ClassLibrary1.Editor;
using System.Net;
using System.Linq.Expressions;
using Unity.Collections;
namespace ClassLibrary1
{
    internal class Editor
    {
        public static bool isInEditor = false;
        public static GameObject cam;
        public static GameObject voxeland;
        public static GameObject editor;
        public static bool isbrushing = false;
        private bool renderbrushmenu = false;
        public static Int3 octreemouse;
        public static bool iswindowopen = true;
        private string rad = "2.5";
        public static Int3 mousebatch;
        public static List<Int3> modifiedbatches = new();
        public static Dictionary<Int3, List<Octree>> modfiedoctrees = new();
        public static Dictionary<Int3, List<Int3>> modfiedblocks = new();
        public static List<Int3> unloadedbatches = new();
        public static Dictionary<Int3, Dictionary<Int3, Int3>> modifiedindexes = new();
        public static bool showingAurora = false;
        public static int curBrushType = 1;
        public static bool showTypeWindow = false;
        public static bool isInDropDown = false;
        public static List<string> loadedpatchfilenames = new();
        public static void SaveForTempBatch(Int3 batchid)
        {
            unloadedbatches.Add(batchid);
        }
        public static void SaveForBatch(Int3 batchid)
        {
            if (!modifiedbatches.Contains(batchid))
                return;

            using (var fs = File.Open(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "tempsavepathfix"), $"TempOctree_{batchid.x}-{batchid.y}-{batchid.z}.optoctreepatch"), FileMode.Create))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.WriteUInt32(0);
                    Class1.logger.LogInfo(batchid.ToString());
                    var modifiedtrees = modfiedoctrees[batchid];
                    bw.Write((short)batchid.x);
                    bw.Write((short)batchid.y);
                    bw.Write((short)batchid.z);
                    bw.Write((byte)modifiedtrees.Count);
                    var octreestreamer = LargeWorldStreamer.main.streamerV2.octreesStreamer;
                    modifiedtrees.OrderBy(tree => GetIndex(batchid, tree));
                    foreach (var octree in modifiedtrees)
                    {
                        byte index = GetIndex(batchid, octree);
                        Class1.logger.LogInfo($"{batchid} + {index}");
                        bw.Write(index);
                        bw.Write((ushort)(octree.data.Length / 4));
                        bw.WriteBytes(octree.data.ToArray());
                    }
                }
            }
            using (var fs = File.OpenRead(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "tempsavepathfix"), $"TempOctree_{batchid.x}-{batchid.y}-{batchid.z}.optoctreepatch")))
            {
                using (var br = new BinaryReader(fs)) {
                    TerrainPatcher.TerrainRegistry.PatchTerrain($"Temp Patch for {batchid.ToString()}",br.BaseStream);
                }
            }
        }
        public static void LoadForTempBatch(Int3 batchid)
        {
            if (!modifiedbatches.Contains(batchid))
                return;
            if (unloadedbatches.Contains(batchid))
                unloadedbatches.Remove(batchid);
            Class1.logger.LogInfo($"Loading in temp batch ({batchid.x},{batchid.y},{batchid.z})");
            /*
            using (var fs = File.OpenRead(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "tempsavepathfix"), $"TempOctree_{batchid.x}-{batchid.y}-{batchid.z}.optoctreepatch")))
            {
                using (var br = new BinaryReader(fs))
                {
                    if(br.ReadUInt32() != 0)
                    {
                        throw new InvalidDataException("Invalid temp patch?!");
                    }
                        try
                        {
                            ReadBatchId(br);
                            Int3 batchId = batchid;
                            Int3 id = batchId;
                            ApplyBatchPatch(br, id);
                            Class1.logger.LogInfo("Loading in modified octrees!");
                            LargeWorldStreamer.main.streamerV2.clipmapStreamer.AddToRangesEdited(LargeWorldStreamer.main.GetBatchBlockBounds(id));
                        }
                        catch (EndOfStreamException)
                        {
                            throw new InvalidDataException("Invalid Patch!");
                        }
                    }

                Class1.logger.LogInfo("Flushing Ranges Edited!");
                LargeWorldStreamer.main.streamerV2.clipmapStreamer.FlushRangesEdited(PAXTerrainController.main.streamerV2.octreesStreamer.minLod, PAXTerrainController.main.streamerV2.octreesStreamer.maxLod);
            */
        }
        public static void StartLoad()
        {
            isInEditor = true;
            CoroutineHost.StartCoroutine(MainSceneLoading.Launch());
        }
        public static IEnumerator Load()
        {
            var logger = Class1.logger;
            var cam1 = new GameObject("FreeCam");
            cam1.EnsureComponent<Camera>();
            cam1.EnsureComponent<MainCamera>();
            cam1.EnsureComponent<FreeCam>();
            cam1.EnsureComponent<WaterSurfaceOnCamera>();
            GameObject.Destroy(MainCameraV2.main.gameObject);
            MainCamera._camera = cam1.GetComponent<Camera>();
            cam = cam1;
            var garbagecheckgo = new GameObject("GCCheck");
            garbagecheckgo.EnsureComponent<GarbageCheckReplacement>();
            PAXTerrainController.main.streamer.streamerV2 = PAXTerrainController.main.streamerV2;
            TaskResult<Result> mountresult = new();
            yield return PAXTerrainController.main.MountWorld(mountresult);
            if (!mountresult.Get().success)
                yield break;
            yield return PAXTerrainController.main.LoadWorldTiles();
            yield return PAXTerrainController.Await("Octrees", new Func<bool>(PAXTerrainController.main.streamerV2.lowDetailOctreesStreamer.IsIdle), new Func<int>(PAXTerrainController.main.streamerV2.lowDetailOctreesStreamer.GetQueueLength), 1000);
            yield return PAXTerrainController.Await("Terrain", new Func<bool>(PAXTerrainController.main.streamerV2.octreesStreamer.IsIdle), new Func<int>(PAXTerrainController.main.streamerV2.octreesStreamer.GetQueueLength), 100);
            yield return PAXTerrainController.Await("Clipmap", new Func<bool>(PAXTerrainController.main.streamerV2.clipmapStreamer.IsIdle), new Func<int>(PAXTerrainController.main.streamerV2.clipmapStreamer.GetQueueLength), 1000);
            yield return PAXTerrainController.Await("UpdatingVisibility", new Func<bool>(PAXTerrainController.main.streamerV2.visibilityUpdater.IsIdle), new Func<int>(PAXTerrainController.main.streamerV2.visibilityUpdater.GetQueueLength), 1000);
            PAXTerrainController.main.streamer.frozen = false;
            yield return PAXTerrainController.Await("EntityCells", new Func<bool>(PAXTerrainController.main.streamer.IsWorldSettled), new Func<int>(PAXTerrainController.main.streamer.cellManager.GetQueueLength), 1000);
            logger.LogInfo("Creating FreeCam!");
            if (!PlatformUtils.main.IsUserLoggedIn())
                SaveLoadManager.main.Deinitialize();
            logger.LogInfo("Starting to destroy GOs!");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name.ToLower().Contains("escape")) continue;
                foreach (var go in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    if (go == cam || go == voxeland || go.name.ToLower().Contains("land") || go == garbagecheckgo || go.TryGetComponent<TerrainChunkPieceCollider>(out _) || go.TryGetComponent<TerrainChunkPiece>(out _) || go.TryGetComponent<TerrainChunkPieceGrass>(out _) || go.TryGetComponent<TerrainChunkPieceLayer>(out _) || go.TryGetComponent<uSkyLight>(out _) || go.TryGetComponent<Light>(out _) || go.name.ToLower().Contains("sun") || go.name.ToLower().Contains("sky") || go.name == "Waterscape" || go.TryGetComponent<Sky>(out _) || go.TryGetComponent<AtmosphereDirector>(out _) || go.TryGetComponent<WaterSurface>(out _) || go.name == "Clip Camera") continue;
                    GameObject.Destroy(go);
                }
            }
            logger.LogInfo(LargeWorldStreamer.main.blocksPerTree);
            logger.LogInfo("Destroyed GOs!");
            Base.Deinitialize();
            StreamTiming.Deinitialize();
            EcoRegionManager.Deinitialize();
            PDA.Deinitialize();
            FreezeTime.Deinitialize();
            uGUI.Deinitialize();
            PDAData.Deinitialize();
            TimeCapsuleContentProvider.Deinitialize();
            PDASounds.Deinitialize();
            AssetBundleManager.Deinitialize();
            PingManager.Deinitialize();
            ItemDragManager.Deinitialize();
            GameInfoIcon.Deinitialize();
            Language.Deinitialize();
            LanguageSDF.ClearDynamicFontAssets();
            SDFCutout.Deinitialize();
            MainMenuMusic.Stop();
            if (DeferredSpawner.instance != null)
                DeferredSpawner.instance.Reset();
            GameObjectPool.ClearPools();
            AddressablesUtility.Reset();
            Resources.UnloadUnusedAssets();
            logger.LogInfo("Everything Deinit!");
            cam1.GetComponent<WaterSurfaceOnCamera>().waterSurface = GameObject.Find("Waterscape").GetComponent<WaterSurface>();
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name.ToLower().Contains("safe") && go.TryGetComponent<Sky>(out _))
                    SkyManager._Instance.GlobalSky = go.GetComponent<Sky>();
            }
            Time.timeScale = 1f;
            DearImguiSharp.ImGui.GetIO().SetPlatformImeDataFn = null;
            DearImGuiInjection.DearImGuiInjection.Render += GUI;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSaveFileName(ref OpenFileName lpofn);
        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetOpenFileName(ref OpenFileName ofn);
        static void GUI()
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                var ray = cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Vector3 hitpos = default;
                if (Physics.Raycast(ray, out hit, float.PositiveInfinity))
                {
                    hitpos = hit.point;
                }
                mousebatch = LargeWorldStreamer.main.GetContainingBatch(hitpos);
                var block = LargeWorldStreamer.main.GetBlock(hitpos);
                var mouseoctreeindex = block / LargeWorldStreamer.main.blocksPerTree;
                octreemouse = mouseoctreeindex;
            });
            if (ImGui.Begin("World Editor Main Window", ref iswindowopen, (int)ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("File", true))
                    {
                        if (ImGui.MenuItemBool("Save", "Ctrl+S", false, true))
                        {
                            var openfilename = new OpenFileName();
                            openfilename.lStructSize = Marshal.SizeOf(openfilename);
                            openfilename.lpstrFilter = "Octree patch(*.optoctreepatch)\0\0";
                            openfilename.lpstrFile = "test.optoctreepatch";
                            openfilename.nMaxFile = 256;
                            openfilename.lpstrFileTitle = new string(new char[64]);
                            openfilename.nMaxFileTitle = openfilename.lpstrFileTitle.Length;
                            openfilename.lpstrTitle = "Save to...";

                            if (GetSaveFileName(ref openfilename))
                            {
                                using (var fs = File.Open(openfilename.lpstrFile, FileMode.Create))
                                {
                                    using (var bw = new BinaryWriter(fs))
                                    {
                                        bw.WriteUInt32(0);
                                        foreach (var name in loadedpatchfilenames)
                                        {
                                            using (var fs_ = File.OpenRead(name))
                                            {
                                                using (var br = new BinaryReader(fs_))
                                                {
                                                    if (br.ReadUInt32() != 0)
                                                    {
                                                        throw new InvalidDataException("Invalid temp patch?!");
                                                    }
                                                    bw.Write(br.ReadBytes((int)br.BaseStream.Length - (int)br.BaseStream.Position));
                                                }
                                            }
                                        }
                                        foreach (var batchid in modifiedbatches)
                                        {
                                            if (unloadedbatches.Contains(batchid))
                                            {
                                                using (var fs_ = File.OpenRead(Path.Combine(Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName).FullName).FullName).FullName, "tempsavepathfix"), $"TempOctree_{batchid.x}-{batchid.y}-{batchid.z}.optoctreepatch")))
                                                {
                                                    using (var br = new BinaryReader(fs_))
                                                    {
                                                        if (br.ReadUInt32() != 0)
                                                        {
                                                            throw new InvalidDataException("Invalid temp patch?!");
                                                        }
                                                        bw.Write(br.ReadBytes((int)br.BaseStream.Length - (int)br.BaseStream.Position));
                                                    }
                                                }
                                                continue;
                                            }
                                            Class1.logger.LogInfo(batchid.ToString());
                                            var modifiedtrees = modfiedoctrees[batchid];
                                            bw.Write((short)batchid.x);
                                            bw.Write((short)batchid.y);
                                            bw.Write((short)batchid.z);
                                            bw.Write((byte)modifiedtrees.Count);
                                            var octreestreamer = LargeWorldStreamer.main.streamerV2.octreesStreamer;
                                            modifiedtrees.OrderBy(tree => GetIndex(batchid, tree));
                                            foreach (var octree in modifiedtrees)
                                            {
                                                byte index = GetIndex(batchid, octree);
                                                Class1.logger.LogInfo($"{batchid} + {index}");
                                                bw.Write(index);
                                                bw.Write((ushort)(octree.data.Length / 4));
                                                bw.WriteBytes(octree.data.ToArray());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (ImGui.MenuItemBool("Load", "Ctrl+S", false, true))
                        {
                            var openfilename = new OpenFileName();
                            openfilename.lStructSize = Marshal.SizeOf(openfilename);
                            openfilename.lpstrFilter = "Octree patch(*.optoctreepatch)\0\0";
                            openfilename.lpstrFile = new string(new char[256]);
                            openfilename.nMaxFile = 256;
                            openfilename.lpstrFileTitle = new string(new char[64]);
                            openfilename.nMaxFileTitle = openfilename.lpstrFileTitle.Length;
                            openfilename.lpstrTitle = "Open Patch";
                            if (GetOpenFileName(ref openfilename))
                            {
                                using (var fs = File.OpenRead(openfilename.lpstrFile))
                                {
                                    using (var br = new BinaryReader(fs))
                                    {
                                        TerrainPatcher.TerrainRegistry.PatchTerrain(openfilename.lpstrFile, br.BaseStream);
                                        UnityMainThreadDispatcher.Enqueue(() =>
                                        {
                                            LargeWorldStreamer.main.ReloadSettings();
                                        });
                                        loadedpatchfilenames.Add(openfilename.lpstrFile);
                                    }
                                    using (var br = new BinaryReader(fs))
                                    {
                                        if (br.ReadUInt32() != 0)
                                        {
                                            throw new InvalidDataException("Invalid temp patch?!");
                                        }
                                        while (true)
                                        {
                                            Int3? batchid = ReadBatchId(br);
                                            if (batchid is null) break;
                                            modifiedbatches.Add(batchid.Value);
                                        }
                                    }
                                }
                            }
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenuBar();
                }
                if (ImGui.Button("Exit Editor", DearImGuiInjection.Constants.DefaultVector2))
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "tempsavepathfix"), true);
                        SceneCleaner.Open();
                    });
                }
                if (ImGui.Button("Remover Brush", DearImGuiInjection.Constants.DefaultVector2))
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        StartBrush(BrushModes.Remove);
                    });
                }
                if (ImGui.Button("Addition Brush", DearImGuiInjection.Constants.DefaultVector2))
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        StartBrush(BrushModes.Add);
                    });
                }
                if (ImGui.Button("Smoothing Brush", DearImGuiInjection.Constants.DefaultVector2))
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        StartBrush(BrushModes.Smooth);
                    });
                }
                if (ImGui.Button("Flattening Brush", DearImGuiInjection.Constants.DefaultVector2))
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        StartBrush(BrushModes.Flat);
                    });
                }
                if (ImGui.Button("Type Brush", DearImGuiInjection.Constants.DefaultVector2))
                {
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        StartBrush(BrushModes.Change);
                    });
                }
                var shouldsettotrue = false;
                if(ImGui.BeginCombo("Block Types", BlockTypes.blocktypes[curBrushType-1],(int)ImGuiComboFlags.None))
                {
                    shouldsettotrue = true;
                    foreach(var blocktype in BlockTypes.blocktypes)
                    {
                        bool is_selected = false;
                        if (ImGui.SelectableBoolPtr(blocktype, ref is_selected,(int)ImGuiSelectableFlags.None,DearImGuiInjection.Constants.DefaultVector2)) 
                        {
                            curBrushType = Array.IndexOf(BlockTypes.blocktypes,blocktype) + 1;   
                        }
                        if(is_selected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
                isInDropDown = shouldsettotrue;
                ImGui.SliderFloat("Brush radius", ref Brush.radius, 0.1f, 700f,null,(int)ImGuiSliderFlags.AlwaysClamp);
                if (ImGui.Checkbox("Aurora", ref showingAurora))
                {
                    if (showingAurora)
                    {
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
                            AddressablesUtility.LoadScene("Aurora", LoadSceneMode.Additive);
                        });
                    }else
                    {
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
                            GameObject.Destroy(GameObject.Find("Aurora"));
                        });
                    }
                }
                ImGui.SliderFloat("Camera Speed", ref cam.GetComponent<FreeCam>().movementSpeed, 0.1f, 135f, null, (int)ImGuiSliderFlags.AlwaysClamp);
                ImGui.Checkbox("Water", ref cam.GetComponent<WaterSurfaceOnCamera>().visible);
                ImGui.Text($"Current mouse batch: {mousebatch}");
                ImGui.Text($"Current mouse octree: {octreemouse}");
                ImGui.End();
            }
        }
            static Int3? ReadBatchId(BinaryReader patch)
            {
                byte first;
                try { first = patch.ReadByte(); } catch (EndOfStreamException) { return null; }

                return new Int3(
                    first | (patch.ReadSByte() << 8),
                    patch.ReadInt16(),
                    patch.ReadInt16()
                );
            }
        static void ApplyBatchPatch(BinaryReader patch, Int3 batchId)
        {
            int patchtreecount = patch.ReadByte();
            var trees = PAXTerrainController.main.streamerV2.octreesStreamer.GetBatch(batchId).octrees.data;
            for (int i = 0; i < patchtreecount; i++)
            {
                trees[patch.ReadByte()].data.CopyFrom(patch.ReadBytes(patch.ReadUInt16() * 4));
            }
        }
            public static byte GetIndex(Int3 bid,Octree tree)
        {
            var trees = PAXTerrainController.main.streamerV2.octreesStreamer.GetBatch(bid).octrees;
            Int3 index = default;
            for(var x = 0;x < trees.sizeX;x++)
                for(var y = 0; y < trees.sizeY;y++)
                    for(var z = 0; z < trees.sizeZ;z++)
                    {
                        if (trees[x,y,z] == tree)
                        {
                            index = new Int3(x, y, z);
                            break;
                        }
                    }
            var index_x = UWE.Math.PositiveModulo(index.x, 5);
            var index_y = UWE.Math.PositiveModulo(index.y, 5);
            var index_z = UWE.Math.PositiveModulo(index.z, 5);
            return (byte)(index_z + index_y * 5 + index_x * 25);
        }
        public static Int3 GetGlobalIndex(Int3 batch_id, int local_index)
        {
            var index_x = local_index / 25;
            var index_y = local_index % 25 / 5;
            var index_z = local_index % 5;
            return new Int3(
                batch_id.x * 5 + index_x,
                batch_id.y * 5 + index_y,
                batch_id.z * 5 + index_z
            );
        }
        static void StartBrush(BrushModes mode)
        {
            if(isbrushing)
            {
                GameObject.Destroy(Brush.go);
                isbrushing = false;
                return;
            }
            isbrushing = true;
            var brush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject.Destroy(brush.GetComponent<Collider>());
            brush.GetComponent<MeshRenderer>().material.color = Color.red;
            brush.EnsureComponent<Brush>().mode = mode;
        }
        public static void StopBrush()
        {
            isbrushing = false;
        }
    }
    public enum BrushModes
    {
        Remove,
        Add,
        Smooth,
        Flat,
        Change
    }
    public class Brush : MonoBehaviour
    {
        public BrushModes mode;
        public static bool isenabled;
        public static GameObject go;
        public static float radius = 2.5f;
        public static Vector3 mousenormal;
        void Awake()
        {
            if(isenabled)
            {
                isenabled = false;
                GameObject.Destroy(go);
                GameObject.Destroy(gameObject);
                return;
            }
            if(mode == BrushModes.Add)
            {
                var mat = GetComponent<Material>();
                mat.color = Color.green;
            }
            go = gameObject;
        }
        void Update() 
        {
            List<Int3> newmodbatches = new();
            foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.TryGetComponent<TerrainChunkPieceCollider>(out _))
                    go.SetActive(true);
            }
            transform.localScale = new Vector3(radius, radius, radius);
            if (DearImGuiInjection.DearImGuiInjection.IsCursorVisible)
                return;
            var ray = Editor.cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray,out hit,Mathf.Infinity))
            {
                transform.position = hit.point;
                mousenormal = hit.normal;
            }
            if (Input.GetMouseButtonUp(0))
            {
                var octreestreamer = LargeWorldStreamer.main.streamerV2.octreesStreamer;
                VoxelandData.OctNode.BlendArgs args = default;
                if (mode == BrushModes.Remove)
                    args = new VoxelandData.OctNode.BlendArgs(VoxelandData.OctNode.BlendOp.Subtraction, false, 0);
                else if(mode == BrushModes.Add)
                    args = new VoxelandData.OctNode.BlendArgs(VoxelandData.OctNode.BlendOp.Union, false, 0);
                else if(mode == BrushModes.Change)
                    args = new VoxelandData.OctNode.BlendArgs(VoxelandData.OctNode.BlendOp.Overwrite, false, 0);
                var size = new Vector3(radius, radius, radius);
                var bounds = new Bounds(GetComponent<Renderer>().bounds.center, size);
                var mins = Int3.Floor(LargeWorldStreamer.main.land.transform.InverseTransformPoint(bounds.min));
                var maxs = Int3.Floor(LargeWorldStreamer.main.land.transform.InverseTransformPoint(bounds.max));
                var int3bounds = new Int3.Bounds(mins, maxs);
                var blockBounds = int3bounds.Expanded(1);
                var thingnotsurewhatdoes = (Vector3 wsPos) => radius - Vector3.Distance(wsPos, GetComponent<Renderer>().bounds.center);
                foreach (var @int in blockBounds / LargeWorldStreamer.main.blocksPerTree)
                {
                    if (LargeWorldStreamer.main.CheckRoot(@int))
                    {
                        Class1.logger.LogInfo("Root Checked!");
                        var octree = octreestreamer.GetOctree(@int);
                        var wasnull = false;
                        if (octree == null)
                        {
                            octree = new Octree(@int);
                            wasnull = true;
                        }
                        Class1.logger.LogInfo("Octree not null!");
                            var morebounds = @int.Refined(LargeWorldStreamer.main.blocksPerTree);
                        VoxelandData.OctNode root = default;
                        if (!wasnull)
                            root = octree.ToVLOctree();
                        else
                            root = new VoxelandData.OctNode(1, 0);
                            foreach (var int2 in morebounds.Intersect(blockBounds))
                            {
                                var wsPos = LargeWorldStreamer.main.land.transform.TransformPoint(int2 + UWE.Utils.half3);
                                var num = thingnotsurewhatdoes(wsPos);
                                var n = new VoxelandData.OctNode((byte)Editor.curBrushType, VoxelandData.OctNode.EncodeDensity(num));
                                var blocksPerTree = LargeWorldStreamer.main.blocksPerTree;
                                
                                var x = int2.x % blocksPerTree;
                                var y = int2.y % blocksPerTree;
                                var z = int2.z % blocksPerTree;
                                var numOctreesPerBatch_ = octreestreamer.numOctreesPerBatch;
                                var batchid_ = Int3.FloorDiv(@int, numOctreesPerBatch_);
                                if (mode == BrushModes.Remove || mode == BrushModes.Add)
                                {
                                    var octNode = VoxelandData.OctNode.Blend(root.GetNode(x, y, z, blocksPerTree / 2), n, args);

                                    root.SetNode(x, y, z, blocksPerTree / 2, octNode.type, octNode.density);
                                }
                                else if (mode == BrushModes.Smooth)
                                {
                                    bool solidBefore = n.density > 0;
                                    var pos = new Int3(x, y, z);
                                    var blurRadius = 2;
                                    float sum = 0;
                                    int count = 0;
                                    for (int k = pos.z - blurRadius; k <= pos.z + blurRadius; k++)
                                    {
                                        for (int j = pos.y - blurRadius; j <= pos.y + blurRadius; j++)
                                        {
                                            for (int i = pos.x - blurRadius; i <= pos.x + blurRadius; i++)
                                            {
                                                var data = root.GetNode(i, j, k, blocksPerTree / 2);

                                                sum += data.density;
                                                count++;
                                            }
                                        }
                                    }
                                    var average = sum / count;
                                    Class1.logger.LogInfo(average);
                                byte t = default;
                                if (average > 0 && !solidBefore) t = (byte)Editor.curBrushType;
                                else if (average < 0 && solidBefore) t = 0;
                                root.SetNode(x, y, z, blocksPerTree / 2, t, (byte)average);
                                }
                                else if (mode == BrushModes.Flat)
                                {
                                    var planedist = -PlaneSDF(new Vector3(x, y, z), transform.position, mousenormal);
                                    int t = planedist > 0 ? Editor.curBrushType : 0;
                                    root.SetNode(x, y, z, blocksPerTree / 2, (byte)t, (byte)planedist);
                                } 
                            else if(mode == BrushModes.Change)
                            {
                                var newnode = root.GetNode(x, y, z, blocksPerTree / 2);
                                newnode.type = (byte)Editor.curBrushType;
                                root.SetNode(x, y, z, blocksPerTree / 2, newnode.type, newnode.density);
                            }
                            }
                            root.Collapse();
                            octreestreamer.SetBatchOctree(@int, root);
                            var numOctreesPerBatch = octreestreamer.numOctreesPerBatch;
                        var batchid = octreestreamer.batches.First(batch => batch.octrees.Contains(octree)).id;
                        newmodbatches.Add(batchid);
                            if (!Editor.modifiedbatches.Contains(batchid))
                                Editor.modifiedbatches.Add(batchid);
                            if (!Editor.modfiedoctrees.Keys.Contains(batchid))
                            {
                                var list = new List<Octree>();
                                list.Add(octree);
                                Editor.modfiedoctrees.Add(batchid, list);
                            }
                            else
                                Editor.modfiedoctrees[batchid].UniqueAddSlow(octree);
                        if (!Editor.modifiedindexes.ContainsKey(batchid))
                        {
                            var dict = new Dictionary<Int3, Int3>();
                            dict.Add(octree.id, @int);
                            Editor.modifiedindexes.Add(batchid, dict);
                        }
                        else if (!Editor.modifiedindexes[batchid].ContainsKey(octree.id))
                            Editor.modifiedindexes[batchid].Add(octree.id,@int);
                            root.Clear();
                        }
                }
                Class1.logger.LogInfo("Loading in modified octrees!");
                LargeWorldStreamer.main.streamerV2.clipmapStreamer.AddToRangesEdited(blockBounds);
                LargeWorldStreamer.main.streamerV2.clipmapStreamer.FlushRangesEdited(octreestreamer.minLod, octreestreamer.maxLod);
                foreach(var batchid in newmodbatches)
                {
                    SaveForBatch(batchid);
                }
            }
        }
        // from reef editor
        private static float PlaneSDF(Vector3 sample, Vector3 origin, Vector3 normal)
        {
            float d = -(origin.x * normal.x + origin.y * normal.y + origin.z * normal.z);
            return -(sample.x * normal.x + sample.y * normal.y + sample.z * normal.z + d);
        }
    }
}
