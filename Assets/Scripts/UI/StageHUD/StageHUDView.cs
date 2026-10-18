using UnityEngine;

namespace Rail
{
    public sealed class StageHUDView : MonoBehaviour
    {
        [SerializeField]
        LifeView lifeView = null;

        [SerializeField]
        LiveView liveView = null;

        [SerializeField]
        GameObject goalView = null;

        [SerializeField]
        GameObject gameOverView = null;

        public void SetHitPoint(int hp) => this.lifeView.SetPoint(hp);
        public void SetLives(int lives) => this.liveView.SetLives(lives);
        public void ShowGoal() => this.goalView.SetActive(true);
        public void ShowGameOver() => this.gameOverView.SetActive(true);
    }
}
