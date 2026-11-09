using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using VContainer;

namespace Rail
{    
    public sealed class InputBlocker : IDisposable
    {
        public IReadOnlyList<string> ActiveBlocks => this.activeBlocks;
        public bool IsBlocked => this.activeBlocks.Count > 0;

        readonly EventSystem eventSystem = null;
        readonly List<string> activeBlocks = new();


        [Inject]
        public InputBlocker(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;    
        }

        void IDisposable.Dispose()
        {
            ClearBlocks();
        }

        /// <summary>
        /// UI入力のブロックを開始する。
        /// </summary>
        public void Block(string reason)
        {
            this.activeBlocks.Add(reason);
            this.eventSystem.enabled = false;
        }

        /// <summary>
        /// UI入力のブロックを取り除く。
        /// </summary>
        public void UnBlock(string reason)
        {
            if (!this.activeBlocks.Remove(reason))
            {
                Debug.LogWarning($"[InputBlocker] '{reason}' is not blocked.");
                return;
            }

            if (this.activeBlocks.Count == 0)
            {
                this.eventSystem.enabled = true;
            }
        }

        /// <summary>
        /// すべての入力ブロックを解除する。
        /// </summary>
        public void ClearBlocks()
        {
            this.activeBlocks.Clear();

            if (this.eventSystem != null)
            {
                this.eventSystem.enabled = true;
            }
        }
    }
}
