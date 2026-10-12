using UniRx;
using UnityEngine;

namespace Rail
{
    public abstract class ItemBase : MonoBehaviour
    {
        [SerializeField]
        CollisionObserver collisionObserver = null;

        protected abstract ItemEffect Effect { get; }

        void Awake()
        {
            this.collisionObserver.OnTriggerEnterObservable()
                .Where(col => col.gameObject.layer == LayerMask.NameToLayer("Player"))
                .Select(col => col.GetComponentInParent<IItemReceiver>())
                .Where(receiver => receiver != null)
                .First() // 同じフレームに複数の当たり判定が来ても、1回だけ渡す。
                .Subscribe(receiver =>
                {
                    receiver.ReceiveItem(this.Effect);
                    Destroy(this.gameObject);
                })
                .AddTo(this);
        }
    }
}
