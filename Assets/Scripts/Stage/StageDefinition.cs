using UnityEngine;
using System;
using UniRx;

namespace Rail
{
    /// <summary>
    /// ステージの情報を取りまとめるクラス。
    /// スポーン地点、最初のエリア情報、各種判定エリアを取りまとめて監視などを行う。
    /// </summary>
    public class StageDefinition : MonoBehaviour
    {
        [SerializeField]
        StageArea initialArea = null;

        [SerializeField]
        SplineAnchor startPoint = null;

        [SerializeField]
        AreaTrigger[] areaTriggers = Array.Empty<AreaTrigger>();

        public StageArea InitialArea => this.initialArea;

        public SplineAnchor StartPoint => this.startPoint;

        public IObservable<AreaEventInfo> OnAreaEventObservable() => this.onAreaEventSubject;

        readonly Subject<AreaEventInfo> onAreaEventSubject = new();

        void Awake()
        {
            this.onAreaEventSubject.AddTo(this);

            foreach (var trigger in this.areaTriggers)
            {
                if (trigger == null) continue;

                trigger.OnEnteredObservable()
                    .Subscribe(info => this.onAreaEventSubject.OnNext(info))
                    .AddTo(this);
            }
        }
    }
}