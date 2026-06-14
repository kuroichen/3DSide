using System;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class CollisionObserver : MonoBehaviour
    {
        public IObservable<Collider> OnTriggerEnterObservable() => this.onTriggerEnterSubject;
        public IObservable<Collision> OnCollisionEnterObservable() => this.onCollisionEnterSubject;

        private Subject<Collider> onTriggerEnterSubject = new();
        private Subject<Collision> onCollisionEnterSubject = new();

        Collider hitCollider = null;

        void Awake()
        {
            if (!TryGetComponent<Collider>(out this.hitCollider))
            {
                Debug.LogWarning($"[CollisionObserver][{this.gameObject.name}]collision not found.");
            }
        }

        void OnDestroy()
        {
            this.onTriggerEnterSubject.Dispose();
            this.onCollisionEnterSubject.Dispose();
        }

        public void SetCollisionEnabled(bool enable)
        {
            if (this.hitCollider != null)
            {
                this.hitCollider.enabled = enable;
            }
        }

        protected void OnTriggerEnter(Collider collider)
        {
            this.onTriggerEnterSubject.OnNext(collider);
        }

        protected void OnCollisionEnter(Collision collision)
        {
            this.onCollisionEnterSubject.OnNext(collision);
        }
    }    
}

