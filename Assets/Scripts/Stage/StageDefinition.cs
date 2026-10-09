using UnityEngine;

namespace Rail
{
    /// <summary>
    /// ステージのメタ情報
    /// </summary>
    public class StageDefinition : MonoBehaviour
    {
        [SerializeField]
        StageArea initialArea = null;

        [SerializeField]
        StageView view = null;

        // Stage開始時のエリア
        public StageArea InitialArea => this.initialArea;

        // 個別ステージSceneに置いてあるView
        public StageView View => this.view;
    }
}