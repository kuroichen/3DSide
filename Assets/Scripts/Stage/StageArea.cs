using System.Collections.Generic;
using UnityEngine;

namespace Rail
{
    /// <summary>
    /// ステージ内の各エリアを管理するクラス。
    /// </summary>
    public class StageArea : MonoBehaviour
    {
        [SerializeField]
        string areaId = "";

        [SerializeField]
        GameObject contentsRoot = null;

        [SerializeField]
        SplineAnchor startPoint = null;

        [SerializeField]
        EnemySpawnPoint[] enemySpawnPoints = null;

        // このエリアに割り振られたID
        public string AreaId => this.areaId;

        // このエリアのスタート地点
        public SplineAnchor StartPoint => this.startPoint;

        readonly List<EnemyCharacter> spawnedEnemies = new();
    
        /// <summary>
        /// エリアの有効無効の切り替え
        /// </summary>
        public void SetAreaActive(bool active)
        {
            if (this.contentsRoot == null)
            {
                Debug.LogWarning($"[{name}] ContentsRoot is not assigned.", this);
                return;
            }

            this.contentsRoot.SetActive(active);
        }

        /// <summary>
        /// 出現地点から敵を生成する。エリアの表示に追従するようcontentsRoot配下に置く。
        /// </summary>
        /// <param name="factory"></param>
        public void SpawnEnemies(EnemyFactory factory)
        {
            foreach (var spawnPoint in this.enemySpawnPoints)
            {
                var enemy = factory.Create(spawnPoint, this.contentsRoot.transform);
                
                if (enemy != null)
                {
                    this.spawnedEnemies.Add(enemy);
                }
            }
        }

        /// <summary>
        /// 生成した敵を全て削除する。
        /// </summary>
        public void ClearEnemies()
        {
            foreach (var enemy in this.spawnedEnemies)
            {
                // 倒されて破棄済みの場合もある
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }    
            }

            this.spawnedEnemies.Clear();
        }
    }
}