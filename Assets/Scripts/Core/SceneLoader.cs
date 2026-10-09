using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace Rail
{
    public sealed class SceneLoader
    {
        /// <summary>
        /// Sceneロード
        /// </summary>
        /// <param name="sceneName">開くScene名</param>
        /// <param name="loadSceneMode">ロードモード(Single, Additive)</param>
        /// <returns></returns>
        public async UniTask<Scene> LoadAsync(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            await SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
            return SceneManager.GetSceneByName(sceneName);
        }

        /// <summary>
        /// Sceneアンロード
        /// </summary>
        /// <param name="scene">アンロードするScene</param>
        public async UniTask UnLoadAsync(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            } 

            await SceneManager.UnloadSceneAsync(scene);
        }
    }
}
