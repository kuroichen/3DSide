using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Rail
{
    public sealed class StageLoader : IDisposable
    {
        readonly SceneLoader sceneLoader = null;
        readonly IObjectResolver objectResolver = null;

        [Inject]
        public StageLoader(
            SceneLoader sceneLoader,
            IObjectResolver objectResolver)
        {
            this.sceneLoader = sceneLoader;
            this.objectResolver = objectResolver;
        }

        void IDisposable.Dispose()
        {
        }

        /// <summary>
        /// 個別ステージSceneをロードして構成情報を返す。
        /// </summary>
        public async UniTask<StageDefinition> LoadStageAsync(
            WorldId worldId,
            StageId stageId)
        {
            var sceneName = $"{worldId}_{stageId}";
            var scene = await sceneLoader.LoadAsync(
                sceneName,
                LoadSceneMode.Additive);


            // definitionやViewを探して注入する。
            foreach (var root in scene.GetRootGameObjects())
            {
                // 確認用に置いてあるカメラを無効化する。実行時のカメラはStageScene側の1台だけ
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    camera.gameObject.SetActive(false);
                }

                this.objectResolver.InjectGameObject(root);
            }

            var definition = scene
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageDefinition>(true))
                .FirstOrDefault();

            return definition;
        }

        /// <summary>
        /// 個別ステージSceneをアンロードする
        /// </summary>
        public async UniTask UnLoadStageAsync(StageDefinition definition)
        {
            var scene = definition.gameObject.scene;
            await sceneLoader.UnLoadAsync(scene);
        }
    }
}
