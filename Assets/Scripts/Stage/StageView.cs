using System.Linq;
using UniRx;
using UnityEngine;
using VContainer;

namespace Rail
{
    /// <summary>
    /// StageのView。表示制御用
    /// </summary>
    public sealed class StageView : MonoBehaviour
    {
        [SerializeField]
        StageArea[] areas = null;

        [SerializeField]
        AreaTrigger[] areaTriggers = null;

        EnemyFactory enemyFactory = null;

        [Inject]
        public void Construct(
            StageEventBus stageEventBus,
            EnemyFactory enemyFactory)
        {
            this.enemyFactory = enemyFactory;

            this.areaTriggers
                .Select(trigger => trigger.OnEnteredObservable())
                .Merge()
                .Subscribe(stageEventBus.PublishAreaTrigger)
                .AddTo(this);
        }

        /// <summary>
        /// 指定したエリアの表示状態を変更する
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="active"></param>
        public void SetAreaActive(string areaId, bool active)
        {
            var area = this.areas.FirstOrDefault(area => area.AreaId == areaId);

            if (area == null)
            {
                Debug.LogError($"[StageView] Area {areaId} is not found.");
                return;
            }

            area.SetAreaActive(active);
        }

        public void ResetEnemies()
        {
            foreach (var area in this.areas)
            {
                if (area == null) continue;

                area.ClearEnemies();
                area.SpawnEnemies(this.enemyFactory);
            }
        }
    }
}