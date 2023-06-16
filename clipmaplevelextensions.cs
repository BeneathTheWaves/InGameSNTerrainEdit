using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using WorldStreaming;
using static OVRHaptics;
namespace ClassLibrary1
{
    internal static class ClipmapLevelExtensions
    {
        private const string rebuildingTerrainMsg = "Rebuilding terrain area...";

        private static Dictionary<int, int> remainingCellCount = new Dictionary<int, int>();
        private static Dictionary<int, Dictionary<Int3, ClipmapChunk>> nullableClipmapChunks = new Dictionary<int, Dictionary<Int3, ClipmapChunk>>();

        private static ErrorMessage._Message rebuildingMessage = null;
        public static bool isMeshesRebuilding = false;

        public static void OnBatchOctreesEdited(this ClipmapLevel clipmapLevel, List<Int3.Bounds> blockRanges)
        {
            Class1.logger.LogInfo("OnBatchOctreesEdited run!");
            var processingCells = clipmapLevel.GetProcessingCells(blockRanges);

            if (!remainingCellCount.ContainsKey(clipmapLevel.id))
            {
                remainingCellCount[clipmapLevel.id] = 0;
            }

            if (remainingCellCount.All((levelCountPair) => levelCountPair.Value <= 0))
            {
                isMeshesRebuilding = true;
                rebuildingMessage = ErrorMessageExtensions.AddReturnMessage(rebuildingTerrainMsg);
            }

            remainingCellCount[clipmapLevel.id] += processingCells.Count;
            if (!nullableClipmapChunks.ContainsKey(clipmapLevel.id))
            {
                nullableClipmapChunks[clipmapLevel.id] = new Dictionary<Int3, ClipmapChunk>();
            }

            foreach (var cell in processingCells)
            {
                cell.OnBatchOctreesEdited();
            }
        }

        public static void OnEndBuildLayers(this ClipmapLevel clipmapLevel, ClipmapCell clipmapCell, ClipmapChunk nullableClipmapChunk)
        {
            nullableClipmapChunks[clipmapLevel.id][clipmapCell.id] = nullableClipmapChunk;

            remainingCellCount[clipmapLevel.id]--;

            if (remainingCellCount[clipmapLevel.id] <= 0)
            {
                clipmapLevel.SwapChunks(nullableClipmapChunks[clipmapLevel.id]);
                nullableClipmapChunks[clipmapLevel.id].Clear();

                if (remainingCellCount.All((levelCountPair) => levelCountPair.Value <= 0))
                {
                    isMeshesRebuilding = false;


                    if (rebuildingMessage != null)
                    {
                        ErrorMessageExtensions.SetMessageTimeEnd(rebuildingMessage, PDA.time);
                        ErrorMessageExtensions.pendingMessageToRemove = rebuildingMessage;
                        rebuildingMessage = null;
                    }
                }
            }
            else if (rebuildingMessage != null)
            {
                var oldTimeEnd = ErrorMessageExtensions.GetMessageTimeEnd(rebuildingMessage);
                var timeFadeOut = ErrorMessageExtensions.GetTimeFadeOut();
                if (oldTimeEnd - timeFadeOut < PDA.time)
                {
                    rebuildingMessage = ErrorMessageExtensions.AddReturnMessage(rebuildingTerrainMsg);
                }
            }
        }

        private static void SwapChunks(this ClipmapLevel clipmapLevel, Dictionary<Int3, ClipmapChunk> clipmapChunks)
        { 

            foreach (var cellChunkPair in clipmapChunks)
            {
                var cell = clipmapLevel.GetCell(cellChunkPair.Key);
                var clipmapChunk = cellChunkPair.Value;

                if (cell != null && cell.IsLoaded())
                {
                    cell.SwapChunk(clipmapChunk);
                }
                else
                {
                    UnityEngine.Object.Destroy(clipmapChunk);
                }
            }
        }

        private static List<ClipmapCell> GetProcessingCells(this ClipmapLevel clipmapLevel, List<Int3.Bounds> blockRanges)
        {
            var processingCells = new List<ClipmapCell>();

            foreach (var blockRange in blockRanges)
            {
                var cellBounds = clipmapLevel.GetCellRange(blockRange);

                foreach (Int3 cellId in cellBounds)
                {
                    ClipmapCell cell = clipmapLevel.GetCell(cellId);
                    if (cell != null)
                    {
                        if (!processingCells.Contains(cell))
                        {
                            processingCells.Add(cell);
                        }
                    }
                }
            }

            return processingCells;
        }
    }
    static class ErrorMessageExtensions
    {
        public static ErrorMessage._Message pendingMessageToRemove;

        public static ErrorMessage._Message AddReturnMessage(string messageString)
        {
            ErrorMessage.AddMessage(messageString);

            var main = ErrorMessage.main;

            return main.GetExistingMessage(messageString);
        }

        public static ErrorMessage._Message GetExistingMessage(this ErrorMessage errorMessage, string messageString)
        {
            return errorMessage.GetExistingMessage(messageString);
        }

        public static void SetMessageTimeEnd(ErrorMessage._Message message, float timeEnd)
        {
            message.timeEnd = timeEnd;
        }

        public static void AddMessageTimeEnd(ErrorMessage._Message message, float delayTime)
        {
            var messageTimeEnd = message.timeEnd;

            message.timeEnd = messageTimeEnd + delayTime;
        }

        public static float GetMessageTimeEnd(ErrorMessage._Message message)
        {
            return message.timeEnd;
        }

        public static float GetTimeFadeOut()
        {
            var main = ErrorMessage.main;

            return main.timeFadeOut;
        }

        public static float GetTimeInvisible()
        {
            var main = ErrorMessage.main;

            return main.timeInvisible;
        }

        public static void RemoveOffsetY(float offsetToRemove)
        {
            var main = ErrorMessage.main;
            var offsetY = main.offsetY;

            main.offsetY = offsetY - offsetToRemove;
        }

        public static TextMeshProUGUI GetMessageEntry(ErrorMessage._Message message)
        {
            return message.entry;
        }
    }
}
