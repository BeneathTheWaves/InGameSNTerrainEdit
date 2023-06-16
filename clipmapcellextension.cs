using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UWE;
using WorldStreaming;

namespace ClassLibrary1
{
    internal static class ClipmapCellExtensions
    {
        public static void OnBatchOctreesEdited(this ClipmapCell clipmapCell)
        { 
            clipmapCell.level.streamer.meshingThreads.Enqueue(new Task.Function(RebuildMeshTask), clipmapCell, null);
        }

        private static void RebuildMeshTask(object owner, object state)
        {
            var clipmapCell = (ClipmapCell)owner;
            clipmapCell.RebuildMesh(out var meshBuilder);

            clipmapCell.level.streamer.buildLayersThread.Enqueue(new Task.Function(RebuildLayersTask), clipmapCell, meshBuilder);
        }

        public static void RebuildMesh(this ClipmapCell clipmapCell, out MeshBuilder meshBuilder)
        {
            var clipmapStreamer = clipmapCell.streamer;
            var octreesStreamer = clipmapStreamer.host.GetOctreesStreamer(clipmapCell.level.id);

            meshBuilder = clipmapStreamer.meshBuilderPool.Get();
            meshBuilder.Reset(clipmapCell.level.id, clipmapCell.id, clipmapCell.level.cellSize, clipmapCell.level.settings, clipmapStreamer.host.blockTypes);
            meshBuilder.DoThreadablePart(octreesStreamer, clipmapStreamer.settings.collision);
        }

        private static void RebuildLayersTask(object owner, object state)
        {
            var clipmapCell = (ClipmapCell)owner;
            var meshBuilder = (MeshBuilder)state;

            CoroutineHost.StartCoroutine(clipmapCell.RebuildLayersAsync(meshBuilder));
        }

        public static IEnumerator RebuildLayersAsync(this ClipmapCell clipmapCell, MeshBuilder meshBuilder)
        {

            ClipmapChunk nullableClipmapChunk = null;
            if (clipmapCell.streamer != null && clipmapCell.streamer.host != null)
            {
                var host = clipmapCell.level.streamer.host;
                nullableClipmapChunk = meshBuilder.DoFinalizePart(host.chunkRoot, host.terrainPoolManager);
                clipmapCell.streamer.meshBuilderPool.Return(meshBuilder);
                yield return clipmapCell.ActivateChunkAndCollider(nullableClipmapChunk);
            }
            clipmapCell.level.OnEndBuildLayers(clipmapCell, nullableClipmapChunk);

            yield break;
        }

        public static void SwapChunk(this ClipmapCell clipmapCell, ClipmapChunk nullableClipmapChunk)
        {
            if (clipmapCell.IsVisible())
            {
                if (nullableClipmapChunk)
                {
                    nullableClipmapChunk.Show();
                }
            }

            var oldClipmapChunk = clipmapCell.chunk;
            if (oldClipmapChunk)
            {
                if (!clipmapCell.streamer.host.terrainPoolManager.meshPoolingEnabled)
                {
                    MeshBuilder.DestroyMeshes(oldClipmapChunk);
                }
                clipmapCell.ReturnChunkToPool(oldClipmapChunk);
            }

            clipmapCell.chunk = nullableClipmapChunk;
        }

        public static bool IsVisible(this ClipmapCell clipmapCell)
        {
            var clipmapCellState = clipmapCell.state;
            var visibleState = ClipmapCell.State.Visible;
            return clipmapCellState.Equals(visibleState);
        }
    }
}
