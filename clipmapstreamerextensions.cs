using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WorldStreaming;

namespace ClassLibrary1
{
    // Again, thanks to Repkins for this class
    internal static class ClipmapStreamerExtensions
    {
        private static List<Int3.Bounds> rangesEdited = new List<Int3.Bounds>();

        public static void AddToRangesEdited(this ClipmapStreamer clipmapStreamer, Int3.Bounds blockRange)
        {
            rangesEdited.Add(blockRange);
        }

        public static void FlushRangesEdited(this ClipmapStreamer clipmapStreamer, int minLod, int maxLod)
        {
            var rangesCount = rangesEdited.Count;

            clipmapStreamer.OnRangesEdited(rangesEdited, minLod, maxLod);
            rangesEdited.Clear();
        }

        private static void OnRangesEdited(this ClipmapStreamer clipmapStreamer, List<Int3.Bounds> blockRanges, int minLod, int maxLod)
        {
            var clipmapStreamerLevels = clipmapStreamer.levels;

            minLod = Mathf.Clamp(minLod, 0, clipmapStreamerLevels.Length - 1);
            maxLod = Mathf.Clamp(maxLod, 0, clipmapStreamerLevels.Length - 1);
            for (int i = minLod; i <= maxLod; i++)
            {
                clipmapStreamerLevels[i].OnBatchOctreesEdited(blockRanges);
            }
        }

    }
}
