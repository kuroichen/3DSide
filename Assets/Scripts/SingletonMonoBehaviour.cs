using UnityEngine;

namespace Rail
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        static T instance = null;
        public static T Instance => instance;

        protected virtual void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;
        }

        public bool HasInstance => instance != null;
    }    
}
