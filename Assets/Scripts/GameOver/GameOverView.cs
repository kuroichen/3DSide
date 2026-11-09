using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Rail
{
    public class GameOverView : MonoBehaviour
    {
        [SerializeField]
        Button continueButton = null;

        [SerializeField]
        Button returnTitleButton = null;

        public IObservable<Unit> OnClickContinueObservable() => this.continueButton.OnClickAsObservable();
        public IObservable<Unit> OnClickReturnTitleObservable() => this.returnTitleButton.OnClickAsObservable();
    }
}
