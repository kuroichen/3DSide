using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Rail
{
    public class TitleView : MonoBehaviour
    {
        [SerializeField]
        Button startButton = null;

        public IObservable<Unit> OnClickStartButtonAsObservable() => this.startButton.OnClickAsObservable();
    }
}
