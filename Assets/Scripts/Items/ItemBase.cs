using UniRx;
using UnityEngine;

namespace Rail
{
    public abstract class ItemBase : MonoBehaviour
    {
        [SerializeField]
        CollisionObserver collisionObserver = null;

        void Awake()
        {
            this.collisionObserver.OnTriggerEnterObservable()
                .Where(col => col.gameObject.layer == LayerMask.NameToLayer("Player"))
                .Select(col => col.GetComponentInParent<ICharacterProperties>())
                .Where(properties => properties != null)
                .Subscribe(properties => OnCollected(properties))
                .AddTo(this);
        }

        protected abstract void OnCollected(ICharacterProperties characterProperties);
    }
}
