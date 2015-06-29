using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;

namespace uTasks.Dispatchers
{
    public class UnityThreadDispatcher : MonoBehaviour, IThreadDispatcher
    {
        private static UnityThreadDispatcher _instance;
        private readonly Queue<Action> _actions = new Queue<Action>();
        private readonly object _actionsLock = new object();
        private readonly object _threadLock = new object();
        private Thread _unityThread;

        public static UnityThreadDispatcher Instance
        {
            get { return _instance ?? (_instance = FindObjectsOfType<UnityThreadDispatcher>().Single()); }
        }

        public void BeginInvoke(Action action)
        {
            bool isOnUnityThread;

            lock (_threadLock)
            {
                if (_unityThread == null)
                    throw new NullReferenceException("Unity thread is null");

                isOnUnityThread = Thread.CurrentThread == _unityThread;
            }

            if (isOnUnityThread)
            {
                action();
            }
            else
            {
                lock (_actionsLock)
                {
                    _actions.Enqueue(action);
                }
            }
        }

        #region Unity Messages

        private void Awake()
        {
            lock (_threadLock)
            {
                _unityThread = Thread.CurrentThread;
            }
        }

        #endregion

        [UsedImplicitly]
        private void Update()
        {
            lock (_actionsLock)
            {
                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    action();
                }
            }
        }
    }
}