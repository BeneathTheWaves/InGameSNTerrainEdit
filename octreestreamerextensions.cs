using ClassLibrary1.WorldStreaming;
using System;
using System.Collections.Generic;
using System.Text;
using WorldStreaming;

namespace ClassLibrary1
{
    // Again, thanks to Repkins for this class
    internal static class BatchOctreesStreamerExtension
    {
        public static void SetBatchOctree(this BatchOctreesStreamer batchOctreesStreamer, Int3 absoluteOctreeId, VoxelandData.OctNode root)
        {
            var numOctreesPerBatch = batchOctreesStreamer.numOctreesPerBatch;

            var batchId = Int3.FloorDiv(absoluteOctreeId, numOctreesPerBatch);
            var batch = batchOctreesStreamer.GetBatch(batchId);
            if (batch is null)
                return;
            var octreeId = absoluteOctreeId - (batchId * numOctreesPerBatch);

            var allocator = batch.allocator;
            var octree = batch.GetOctree(octreeId);
            if (octree is null) return;
            octree.Set(root, allocator);
        }
    }
}
