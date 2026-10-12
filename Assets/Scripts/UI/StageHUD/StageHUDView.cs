using UnityEngine;

namespace Rail
{
    public sealed class StageHUDView : MonoBehaviour
    {
        [SerializeField]
        LifeView lifeView = null;

        [SerializeField]
        LiveView liveView = null;

        public void SetHitPoint(int hp) => this.lifeView.SetPoint(hp);
        public void SetLives(int lives) => this.liveView.SetLives(lives);
    }
}
