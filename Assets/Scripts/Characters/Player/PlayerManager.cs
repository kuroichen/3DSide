using System;
using UniRx;
using UnityEngine;

namespace Rail
{
    public class PlayerManager : SingletonMonoBehaviour<PlayerManager>
    {
        [SerializeField]
        PlayerController playerPrefab = null;

        [SerializeField]
        SplineAnchor spawnPoint = null;

        [SerializeField]
        SplineFollowCamera followCamera = null;

        [SerializeField]
        int initialLives = 2;



        public IObservable<Unit> OnPlayerDeadObservable() => this.onPlayerDeadSubject;
        public IObservable<Unit> OnPlayerDiedObservable() => this.onPlayerDiedSubject;
        public IObservable<PlayerController> OnPlayerSpawnedObservable() => this.onPlayerSpawnedSubject;
        public IObservable<int> OnLivesChangedObservable() => this.onLivesChangedSubject;
        public IObservable<Unit> OnGameOverObservable() => this.onGameOverSubject;
        public PlayerController CurrentPlayer => this.playerController;
        public int CurrentLives => this.currentLives;

        PlayerController playerController = null;
        int currentLives = 0;

        CompositeDisposable disposables = new();

        Subject<Unit> onPlayerDeadSubject = new();
        Subject<Unit> onPlayerDiedSubject = new();
        Subject<PlayerController> onPlayerSpawnedSubject = new();
        Subject<int> onLivesChangedSubject = new();
        Subject<Unit> onGameOverSubject = new();

        protected override void Awake()
        {
            base.Awake();
            
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
            if (point == null || this.playerController == null)
            {
                return;
            }

            var characterController = this.playerController.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            this.playerController.transform.SetPositionAndRotation(
                point.transform.position,
                point.transform.rotation);

            this.playerController.SetSpline(
                point.SplineContainer,
                point.T);

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        void Respawn()
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

            this.playerController.LifeProperty.OnDeadObservable()
                .Subscribe(_ => this.onPlayerDeadSubject.OnNext(Unit.Default))
                .AddTo(this.disposables);

            this.playerController.OnDiedObservable()
                .Subscribe(_ => this.onPlayerDiedSubject.OnNext(Unit.Default))
                .AddTo(this.disposables);
            
            this.playerController.SetSpline(this.spawnPoint.SplineContainer, this.spawnPoint.T);
            this.followCamera.SetTarget(this.playerController);
            this.onPlayerSpawnedSubject.OnNext(this.playerController);
        }

        void Delete()
        {
            this.disposables.Clear();
            Destroy(this.playerController.gameObject);
        }
    }
}
