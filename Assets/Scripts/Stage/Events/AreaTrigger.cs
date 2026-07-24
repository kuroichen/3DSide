using System;
using UnityEngine;
using UniRx;

namespace Rail
{

    public class AreaTrigger : MonoBehaviour
    {
        [SerializeField]
        AreaEventInfo eventInfo = null;

        public IObservable<AreaEventInfo> OnEnteredObservable() => this.onEnteredSubject;

        readonly Subject<AreaEventInfo> onEnteredSubject = new();

        void Awake()
        {
            this.onEnteredSubject.AddTo(this);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (this.eventInfo == null)
            {
                Debug.LogWarning($"[{name}] AreaEventInfo is not assigned.", this);

                return;
            }

            this.onEnteredSubject.OnNext(this.eventInfo);
        }
    }
}
