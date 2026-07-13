using UniRx;
using UnityEngine;

namespace Rail
{
    public class PlayerStatusUI : MonoBehaviour
    {
        [SerializeField]
        GameObject[] lifeImages = null;

        [SerializeField]
        TMPro.TextMeshProUGUI liveText = null;
        
        CompositeDisposable playerLifeDisposables = new();

        void Start()
        {
            var playerManager = PlayerManager.Instance;
            if (playerManager == null)
            {
                Debug.LogWarning("[PlayerStatusUI]PlayerManager not found.");
                return;
            }

            playerManager.OnPlayerSpawnedObservable()
                .StartWith(playerManager.CurrentPlayer)
                .Where(player => player != null)
                .Subscribe(BindPlayer)
                .AddTo(this);

            playerManager.OnLivesChangedObservable()
                .StartWith(playerManager.CurrentLives)
                .Subscribe(SetCurrentLives)
                .AddTo(this);
        }

        void OnDestroy()
        {
            this.playerLifeDisposables.Dispose();
        }

        void BindPlayer(PlayerController player)
        {
            this.playerLifeDisposables.Clear();

            var life = player.LifeProperty;
            life.OnLifeChangedObservable()
                .StartWith(life.CurrentHp)
                .Subscribe(SetCurrentLife)
                .AddTo(this.playerLifeDisposables);
        }

        void SetCurrentLife(int currentLife)
        {
            for (var i = 0; i < lifeImages.Length; i++)
            {
                lifeImages[i].SetActive(i < currentLife);
            }
        }

        void SetCurrentLives(int currentLives)
        {
            if (this.liveText == null) return;

            this.liveText.text = $"x{currentLives}";
        }
    }
}
