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

        [SerializeField]
        Collider hitCollider = null;

        void Awake()
        {
            if (this.hitCollider == null)
            {
                foreach (var col in GetComponents<Collider>())
                {
                    if (col is CharacterController) continue;

                    this.hitCollider = col;
                    break;
                }
            }

            if (this.hitCollider == null)
            {
                Debug.LogWarning($"[CollisionObserver][{this.gameObject.name}]collision not found.");
                return;
            }

            if (this.hitCollider.gameObject != this.gameObject)
            {
                Debug.LogWarning(
                    $"[CollisionObserver][{this.gameObject.name}]" +
                    "hitCollider must be attached to the same GameObject.",
                    this);
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

        internal void NotifyTriggerEnter(Collider collider)
        {
            this.onTriggerEnterSubject.OnNext(collider);
        }

        internal void NotifyCollisionEnter(Collision collision)
        {
            this.onCollisionEnterSubject.OnNext(collision);
        }

        protected void OnTriggerEnter(Collider collider)
        {
            NotifyTriggerEnter(collider);
        }

        protected void OnCollisionEnter(Collision collision)
        {
            NotifyCollisionEnter(collision);
        }
    }    
}
