using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Rail
{
    public class WorldView : MonoBehaviour
    {
        [SerializeField]
        Button[] stageButtons = null;

        /// <summary>
        /// ボタン配列の順序に対応するステージIDを通知する。
        /// </summary>
        public IObservable<StageId> OnClickAsObservable() => this.onClickSubject;

        Subject<StageId> onClickSubject = new();

        private void Awake()
        {
            for (var i = 0; i < this.stageButtons.Length; i++)
            {
                // コールバックではループ変数を参照せず、各ボタンのIDを保持する。
                var stageId = (StageId)i;
                this.stageButtons[i].OnClickAsObservable()
                    .Subscribe(_ => this.onClickSubject.OnNext(stageId))
                    .AddTo(this);
            }
        }
    }
}
