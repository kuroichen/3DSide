using System;
using UnityEngine;
using UniRx;


namespace Rail
{
    public class AnimationEventObserver : MonoBehaviour
    {
        public IObservable<string> OnAnimationEventObservable() => this.onAnimationEventSubject;
        private Subject<string> onAnimationEventSubject = new();

        // AnimationEventから呼び出される関数
        // Animation側でイベント名を指定できるようにstring引数を取る。
        // 全Animationで同じメソッドを使用して、購読側でイベント名を判別する想定。
        void OnAnimationEvent(string eventName)
        {
            this.onAnimationEventSubject.OnNext(eventName);
        }
    }
}
