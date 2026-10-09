using UnityEngine;

namespace Rail
{
    public sealed class PlayerSpawner
    {
        readonly PlayerView playerPrefab = null;

        PlayerView currentView = null;
        public PlayerView CurrentView => this.currentView;

        public PlayerSpawner(
            PlayerView playerPrefab)
        {
            this.playerPrefab = playerPrefab;
        }

        public PlayerView Spawn(SplineAnchor spawnPoint)
        {
            if (this.playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] Player prefab is null.");
                return null;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("[PlayerSpawner] Spawn point is null.");
                return null;
            }

            this.Despawn();

            this.currentView = Object.Instantiate(
                this.playerPrefab,
                spawnPoint.transform.position,
                spawnPoint.GetRotation());

            this.currentView.PlaceAt(spawnPoint);

            return this.currentView;
        }

        public void Despawn()
        {
            if (this.currentView == null) return;

            Object.Destroy(this.currentView.gameObject);

            this.currentView = null;
        }
    }
}
