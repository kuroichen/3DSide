using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Rail
{
    public sealed class EnemyFactory : IDisposable
    {
        // 地面に少し浮かせて生成する（CharacterControllerが接地させる）
        static readonly Vector3 SpawnOffset = Vector3.up * 0.5f;

        readonly IObjectResolver objectResolver = null;


        [Inject]
        public EnemyFactory(IObjectResolver objectResolver)
        {
            this.objectResolver = objectResolver;
        }

        void IDisposable.Dispose()
        {
            
        }

        /// <summary>
        /// 出現地点に敵を生成してレールに乗せる。
        /// </summary>
        public EnemyCharacter Create(EnemySpawnPoint spawnPoint, Transform parent)
        {
            if (spawnPoint == null || spawnPoint.Prefab == null)
            {
                Debug.LogError("[EnemyFactory] Spawn point or prefab is null.");
                return null;
            }

            var anchor = spawnPoint.Anchor;

            var enemy = this.objectResolver.Instantiate(
                spawnPoint.Prefab,
                anchor.transform.position + SpawnOffset,
                anchor.GetRotation(),
                parent);

            enemy.SetSpline(anchor.SplineContainer, anchor.T);

            return enemy;
        }
    }
}