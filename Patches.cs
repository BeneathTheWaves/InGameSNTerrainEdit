using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UWE;

namespace ClassLibrary1
{
    internal class Patches
    {
        public static void AwakePatchuGUI(uGUI_MainMenu mainmenu)
        {
            var buttonofplaying = mainmenu.gameObject.GetComponentInChildren<MainMenuPrimaryOptionsMenu>().transform
               .Find("PrimaryOptions/MenuButtons/ButtonPlay").gameObject;
            var mybutton = GameObject.Instantiate(buttonofplaying);
            mybutton.GetComponent<RectTransform>().SetParent(buttonofplaying.transform.parent, false);
            mybutton.name = "ButtonWorldEditor";
            var text = mybutton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = "World Editor";
            GameObject.DestroyImmediate(text.gameObject.GetComponent<TranslationLiveUpdate>());
            mybutton.transform.SetSiblingIndex(1);
            var mybuttonsbutton = mybutton.GetComponent<Button>();
            mybuttonsbutton.onClick = new Button.ButtonClickedEvent();
            mybuttonsbutton.onClick.AddListener((() => { Editor.StartLoad();}));
        }
        // Copy-pasted from decompiled game code, MonoMod can't patch MoveNext
        public static IEnumerator PatchLWSStart(LargeWorldStreamer self)
        {
            while (!self.inited)
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            while (!self.land)
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine = CoroutineUtils.PumpCoroutine(self.LoopUpdateBatchStreamingAsync(), "UpdateBatchStreamingFSM", 1f);
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine2 = CoroutineUtils.PumpCoroutine(self.LoopUpdateCellStreamingAsync(), "UpdateCellStreamingFSM", 1f);
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> updateCellManagementPriorities = CoroutineUtils.PumpCoroutine(self.LoopUpdateCellManagementPrioritiesAsync(), "UpdateCellManagementQueue", 1f);
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> update = pooledStateMachine;
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> update2 = pooledStateMachine2;
            updateCellManagementPriorities.stateMachine.SetMaxFrameMs(0.5f);
            Stopwatch watch = new Stopwatch();
            for (; ; )
            {
                Transform transform = MainCamera.camera.transform;
                self.cachedCameraPosition = transform.position;
                self.cachedCameraForward = transform.forward;
                self.cachedTime = Time.realtimeSinceStartup;
                self.streamerV2.UpdateStreamingCenter(self.cachedCameraPosition);
                while (GarbageCheckReplacement.main.GCCheckThisFrame())
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
                try
                {
                    updateCellManagementPriorities.MoveNext();
                    float num = (float)self.settings.GetMaxFrameMs(Player.main);
                    PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine3 = update2;
                    PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine4 = update;
                    update = pooledStateMachine3;
                    update2 = pooledStateMachine4;
                    float maxFrameMs = Mathf.Max(1f, num * 0.7f);
                    update.stateMachine.SetMaxFrameMs(maxFrameMs);
                    watch.Restart();
                    update.MoveNext();
                    watch.Stop();
                    float timeElapsedMS = UWE.Utils.GetTimeElapsedMS(watch);
                    float num2 = num - timeElapsedMS;
                    float maxFrameMs2 = Mathf.Max(1f, num2 * 0.5f);
                    update2.stateMachine.SetMaxFrameMs(maxFrameMs2);
                    watch.Restart();
                    update2.MoveNext();
                    watch.Stop();
                }
                finally
                {
                }
                bool wait = true;
                if (update.Current is YieldInstruction)
                {
                    wait = false;
                    yield return update.Current;
                }
                if (update2.Current is YieldInstruction)
                {
                    wait = false;
                    yield return update2.Current;
                }
                if (wait)
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
            }
        }
        }
    }
