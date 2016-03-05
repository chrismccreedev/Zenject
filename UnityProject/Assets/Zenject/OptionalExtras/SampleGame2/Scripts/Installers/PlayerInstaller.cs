﻿using System;
using UnityEngine;
using Zenject;
using Zenject.Commands;

namespace ModestTree
{
    public class PlayerInstaller : MonoInstaller
    {
        [SerializeField]
        Settings _settings = null;

        public override void InstallBindings()
        {
            Container.BindInstance(_settings.Rigidbody).WhenInjectedInto<PlayerModel>();
            Container.BindInstance(_settings.MeshRenderer).WhenInjectedInto<PlayerModel>();
            Container.Bind<PlayerModel>().ToSingle();

            Container.BindAllInterfaces<PlayerInputHandler>().ToSingle<PlayerInputHandler>();
            Container.BindAllInterfaces<PlayerMoveHandler>().ToSingle<PlayerMoveHandler>();
            Container.BindAllInterfaces<PlayerBulletHitHandler>().ToSingle<PlayerBulletHitHandler>();
            Container.BindAllInterfaces<PlayerDirectionHandler>().ToSingle<PlayerDirectionHandler>();
            Container.BindAllInterfaces<PlayerShootHandler>().ToSingle<PlayerShootHandler>();

            Container.Bind<PlayerInputState>().ToSingle();

            Container.BindAllInterfaces<PlayerHealthWatcher>().ToSingle<PlayerHealthWatcher>();

            Container.BindSignal<PlayerSignals.Hit>();
            Container.BindTrigger<PlayerSignals.Hit.Trigger>();

            InstallSettings();
        }

        void InstallSettings()
        {
            Container.BindInstance(_settings.PlayerMoveHandler);
            Container.BindInstance(_settings.PlayerShootHandler);
            Container.BindInstance(_settings.PlayerCollisionHandler);
        }

        [Serializable]
        public class Settings
        {
            public Rigidbody Rigidbody;
            public MeshRenderer MeshRenderer;

            public PlayerMoveHandler.Settings PlayerMoveHandler;
            public PlayerShootHandler.Settings PlayerShootHandler;
            public PlayerBulletHitHandler.Settings PlayerCollisionHandler;
        }
    }
}
