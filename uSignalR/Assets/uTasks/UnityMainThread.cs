using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace uTasks
{
    internal class UnityMainThread : MainThread
    {
        private readonly Executor _executor;

        /// <summary>
        ///     Constructor should be called in main thread.
        /// </summary>
        public UnityMainThread()
        {
            var behaviour = Object.FindObjectsOfType<Executor>()
                .SingleOrDefault();

            if (behaviour != null)
            {
                _executor = behaviour;
                return;
            }

            var gameObject = new GameObject(typeof (UnityMainThread).Name)
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
            };

            Object.DontDestroyOnLoad(gameObject);
            _executor = gameObject.AddComponent<Executor>();
        }

        public override void Start(IEnumerator enumerator)
        {
            _executor.StartCoroutine(enumerator);
        }

        protected override void Schedule(Action action)
        {
            _executor.BeginInvoke(action);
        }

        public override void BeginStart(IEnumerator enumerator)
        {
            _executor.BeginStart(enumerator);
        }

        public override void BeginStop(IEnumerator enumerator)
        {
            _executor.BeginStop(enumerator);
        }
    }
}