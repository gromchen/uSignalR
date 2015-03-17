using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace uTasks
{
    // todo: use hash sets
    public class Executor : MonoBehaviour
    {
        private readonly Queue<Action> _actions = new Queue<Action>();
        private readonly object _actionsLock = new object();
        private readonly Queue<IEnumerator> _startEnumerators = new Queue<IEnumerator>();
        private readonly object _startEnumeratorsLock = new object();
        private readonly Queue<IEnumerator> _stopEnumerators = new Queue<IEnumerator>();
        private readonly object _stopEnumeratorsLock = new object();

        public void BeginInvoke(Action action)
        {
            lock (_actionsLock)
            {
                _actions.Enqueue(action);
            }
        }

        public void BeginStart(IEnumerator enumerator)
        {
            lock (_startEnumeratorsLock)
            {
                _startEnumerators.Enqueue(enumerator);
            }
        }

        public void BeginStop(IEnumerator enumerator)
        {
            lock (_stopEnumeratorsLock)
            {
                _stopEnumerators.Enqueue(enumerator);
            }
        }

        [UsedImplicitly]
        private void Update()
        {
            lock (_stopEnumeratorsLock)
            {
                while (_stopEnumerators.Count > 0)
                {
                    StopCoroutine(_stopEnumerators.Dequeue());
                }
            }

            lock (_startEnumeratorsLock)
            {
                while (_startEnumerators.Count > 0)
                {
                    StartCoroutine(_startEnumerators.Dequeue());
                }
            }

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