using UnityEngine;

namespace Rail
{
    public enum GoalType
    {
        Normal,
        Secret,
        Fake,
    }

    /// <summary>
    /// AreaTriggerで発火するイベント用の情報を保持するクラス
    /// ゴール判定に必要な情報を保持する
    /// </summary>
    public class GoalInfo : AreaEventInfo
    {
        [SerializeField]
        GoalType goalType = GoalType.Normal;

        public GoalType GoalType => this.goalType;
    }
}