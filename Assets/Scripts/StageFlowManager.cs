using UniRx;
using UnityEngine;

namespace Rail
{
    public class StageFlowManager : MonoBehaviour
    {
        [SerializeField]
        PlayerManager playerManager = null;

        void Start()
        {
            this.playerManager.OnPlayerDiedObservable()
                .Subscribe(_ => OnPlayerDied())
                .AddTo(this);
        }

        void OnPlayerDied()
        {
            this.playerManager.Respawn();
        }
    }
}
