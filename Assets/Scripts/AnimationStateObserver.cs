using System;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class AnimationStateObserver : MonoBehaviour
    {
        [SerializeField]
        Animator animator = null;

        Subject<int> onStateEnterSubject = new();
        Subject<int> onStateExitSubject = new();

        public IObservable<int> OnStateEnterObservable() => this.onStateEnterSubject;
        public IObservable<int> OnStateExitObservable() => this.onStateExitSubject;

        int previousStateHash = 0;

        void Awake()
        {
            this.onStateEnterSubject.AddTo(this);
            this.onStateExitSubject.AddTo(this);
        }

        void Update()
        {
            if (this.animator.IsInTransition(0)) return;

            var info = this.animator.GetCurrentAnimatorStateInfo(0);

            if (this.previousStateHash != info.shortNameHash)
            {
                this.onStateExitSubject.OnNext(this.previousStateHash);
                this.onStateEnterSubject.OnNext(info.shortNameHash);

                this.previousStateHash = info.shortNameHash;
            }
        }
    }
}
