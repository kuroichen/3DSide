using System;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class PlayerManager : MonoBehaviour
    {
        [SerializeField]
        PlayerController playerPrefab = null;

        [SerializeField]
        SplineAnchor spawnPoint = null;

        [SerializeField]
        SplineFollowCamera followCamera = null;

        public IObservable<Unit> OnPlayerDeadObservable() => this.onPlayerDeadSubject;
        public IObservable<Unit> OnPlayerDiedObservable() => this.onPlayerDiedSubject;

        PlayerController playerController = null;

        CompositeDisposable disposables = new();

        Subject<Unit> onPlayerDeadSubject = new();
        Subject<Unit> onPlayerDiedSubject = new();

        void Awake()
        {
            this.disposables.AddTo(this);
        }

        void Start()
        {
            Spawn();
        }

        public void Respawn()
        {
            Delete();
            Spawn();
        }

        void Spawn()
        {
            this.playerController = Instantiate(
                this.playerPrefab,
                this.spawnPoint.transform.position,
                this.spawnPoint.transform.rotation);

            this.playerController.Life.OnDeadObservable()
                .Subscribe(_ => this.onPlayerDeadSubject.OnNext(Unit.Default))
                .AddTo(this.disposables);

            this.playerController.OnDiedObservable()
                .Subscribe(_ => this.onPlayerDiedSubject.OnNext(Unit.Default))
                .AddTo(this.disposables);
            
            this.playerController.SetSpline(this.spawnPoint.SplineContainer);
            this.followCamera.SetTarget(this.playerController);
        }

        void Delete()
        {
            this.disposables.Clear();
            Destroy(this.playerController.gameObject);
        }
    }
}
