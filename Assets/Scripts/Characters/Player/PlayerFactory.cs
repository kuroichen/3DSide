using System;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Rail
{
    public sealed class PlayerFactory : IDisposable
    {
        readonly PlayerView playerPrefab = null;
        readonly IObjectResolver objectResolver = null;

        [Inject]
        public PlayerFactory(
            PlayerView playerPrefab,
            IObjectResolver objectResolver)    
        {
            this.playerPrefab = playerPrefab;
            this.objectResolver = objectResolver;
        }

        void IDisposable.Dispose()
        {
        }

        /// <summary>
        /// 指定した地点にPlayerViewを生成する。
        /// </summary>
        /// <param name="spawnPoint"></param>
        /// <returns></returns>
        public PlayerView Create(SplineAnchor spawnPoint)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerFactory] Player prefab is null.");
                return null;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("[PlayerFactory] Spawn point is null.");
                return null;
            }

            var playerView = this.objectResolver.Instantiate(
                this.playerPrefab,
                spawnPoint.transform.position,
                spawnPoint.GetRotation());

            playerView.PlaceAt(spawnPoint);

            return playerView;
        }
    }
}