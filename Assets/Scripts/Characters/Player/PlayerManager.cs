using System;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class PlayerManager : SingletonMonoBehaviour<PlayerManager>
    {
        [SerializeField]
        PlayerView playerPrefab = null;

        [SerializeField]
        SplineAnchor spawnPoint = null;

        [SerializeField]
        SplineFollowCamera followCamera = null;

        [SerializeField]
        int initialLives = 2;



        public IObservable<Unit> OnPlayerDeadObservable() => this.onPlayerDeadSubject;
        public IObservable<Unit> OnPlayerDiedObservable() => this.onPlayerDiedSubject;
        public IObservable<PlayerView> OnPlayerSpawnedObservable() => this.onPlayerSpawnedSubject;
        public IObservable<int> OnLivesChangedObservable() => this.onLivesChangedSubject;
        public IObservable<Unit> OnGameOverObservable() => this.onGameOverSubject;
        public PlayerView CurrentPlayer => this.playerSpawner?.CurrentView;
        public int CurrentLives => this.currentLives;

        PlayerSpawner playerSpawner = null;
        int currentLives = 0;

        CompositeDisposable disposables = new();

        Subject<Unit> onPlayerDeadSubject = new();
        Subject<Unit> onPlayerDiedSubject = new();
        Subject<PlayerView> onPlayerSpawnedSubject = new();
        Subject<int> onLivesChangedSubject = new();
        Subject<Unit> onGameOverSubject = new();

        protected override void Awake()
        {
            base.Awake();

            this.playerSpawner = new PlayerSpawner(this.playerPrefab);
            
            this.disposables.AddTo(this);
        }

        void Start()
        {
            this.currentLives = this.initialLives;
            this.onLivesChangedSubject.OnNext(this.currentLives);
            Spawn();
        }

        public bool TryRespawn()
        {
            if (this.currentLives <= 0)
            {
                this.onGameOverSubject.OnNext(Unit.Default);
                return false;
            }

            this.currentLives--;
            this.onLivesChangedSubject.OnNext(this.currentLives);
            Respawn();
            return true;
        }

        public void SetRespawnPoint(SplineAnchor point)
        {
            if (point == null) return;

            this.spawnPoint = point;
        }

        public void MoveTo(SplineAnchor point)
        {
            this.playerSpawner.CurrentView?.PlaceAt(point);
        }

        void Respawn()
        {
            Delete();
            Spawn();
        }

        void Spawn()
        {
            var playerView = this.playerSpawner.Spawn(this.spawnPoint);
            if (playerView == null) return;

            playerView.OnDeadObservable()
                .Subscribe(_ => this.onPlayerDeadSubject.OnNext(Unit.Default))
                .AddTo(this.disposables);

            playerView.OnDeathAnimationFinishedObservable()
                .Subscribe(_ => this.onPlayerDiedSubject.OnNext(Unit.Default))
                .AddTo(this.disposables);
            
            this.followCamera.SetTarget(playerView.Mover);
            this.onPlayerSpawnedSubject.OnNext(playerView);
        }

        void Delete()
        {
            this.disposables.Clear();
            this.playerSpawner.Despawn();
        }
    }
}
