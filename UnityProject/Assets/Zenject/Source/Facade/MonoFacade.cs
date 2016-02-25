#if !ZEN_NOT_UNITY3D

using System;
using System.Collections.Generic;
using ModestTree;
using ModestTree.Util;
using UnityEngine;

// Ignore the fact that _initialObjects is not used
#pragma warning disable 414

namespace Zenject
{
    // Derive from this class to create your own MonoBehaviour based facades
    // Then attach that and a FacadeCompositionRoot component to the same
    // game object
    public abstract class MonoFacade : MonoBehaviour, IDependencyRoot
    {
        [Inject(InjectSources.Local)]
        TickableManager _tickableManager = null;

        [Inject(InjectSources.Local)]
        InitializableManager _initializableManager = null;

        [Inject(InjectSources.Local)]
        DisposableManager _disposablesManager = null;

        // For cases where you have objects that aren't referenced anywhere but still want them to be
        // created on startup
        [InjectOptional(InjectSources.Local)]
        List<object> _initialObjects = null;

        bool _isDisposed;

        public virtual void Start()
        {
            Log.Debug("MonoFacade ({0}): Start called, Initializing IInitializable's", this.GetType().Name());
            _initializableManager.Initialize();
        }

        public virtual void Update()
        {
            // Don't spam the log every frame if initialization fails and leaves it as null
            if (_tickableManager != null)
            {
                _tickableManager.Update();
            }
        }

        public virtual void FixedUpdate()
        {
            // Don't spam the log every frame if initialization fails and leaves it as null
            if (_tickableManager != null)
            {
                _tickableManager.FixedUpdate();
            }
        }

        public virtual void LateUpdate()
        {
            // Don't spam the log every frame if initialization fails and leaves it as null
            if (_tickableManager != null)
            {
                _tickableManager.LateUpdate();
            }
        }

        public virtual void OnApplicationQuit()
        {
            // _disposablesManager can be null if we get destroyed before the Start event
            if (_disposablesManager != null)
            {
                Log.Debug("MonoFacade ({0}): OnApplicationQuit called, disposing IDisposable's", this.GetType().Name());

                // In some cases we have monobehaviour's that are bound to IDisposable, and who have
                // also been set with Application.DontDestroyOnLoad so that the Dispose() is always
                // called instead of OnDestroy.  This is nice because we can actually reliably predict the
                // order Dispose() is called in which is not the case for OnDestroy.
                // However, when the user quits the app, OnDestroy is called even for objects that
                // have been marked with Application.DontDestroyOnLoad, and so the destruction order
                // changes.  So to address this case, dispose before the OnDestroy event below (OnApplicationQuit
                // is always called before OnDestroy) and then don't call dispose in OnDestroy
                Assert.That(!_isDisposed);
                _disposablesManager.Dispose();
                _isDisposed = true;
            }
        }

        public virtual void OnDestroy()
        {
            // _disposablesManager can be null if we get destroyed before the Start event
            if (_disposablesManager != null)
            {
                // See comment in OnApplicationQuit
                if (!_isDisposed)
                {
                    Log.Debug("MonoFacade ({0}): OnDestroy called, disposing IDisposable's", this.GetType().Name());

                    _isDisposed = true;
                    _disposablesManager.Dispose();
                }
            }
        }
    }
}

#endif
