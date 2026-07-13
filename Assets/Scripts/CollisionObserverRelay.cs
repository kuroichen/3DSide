using UnityEngine;

namespace Rail
{
    public class CollisionObserverRelay : MonoBehaviour
    {
        CollisionObserver observer = null;

        public void Initialize(CollisionObserver collisionObserver)
        {
            this.observer = collisionObserver;
        }

        void OnTriggerEnter(Collider collider)
        {
            this.observer?.NotifyTriggerEnter(collider);
        }

        void OnCollisionEnter(Collision collision)
        {
            this.observer?.NotifyCollisionEnter(collision);
        }
    }
}
