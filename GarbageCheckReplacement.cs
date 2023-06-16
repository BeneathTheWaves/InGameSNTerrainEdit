using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace ClassLibrary1
{
    internal class GarbageCheckReplacement : MonoBehaviour
    {
        public static GarbageCheckReplacement main;
        private int lastFrameGCCount;
        private float lastGarbagecollecttime;
        private int lastGarbagecollectframe;
        void Awake()
        {
            main = this;
        }
        void Update()
        {
            if (GC.CollectionCount(0) != this.lastFrameGCCount)
                NotifyGarbageCollected();
            this.lastFrameGCCount = GC.CollectionCount(0);
        }
        void NotifyGarbageCollected()
        {
            this.lastGarbagecollecttime = Time.time;
            this.lastGarbagecollectframe = Time.frameCount;
        }
        public bool GCCheckThisFrame()
        {
            return this.lastGarbagecollectframe == Time.frameCount;
        }
    }
}
