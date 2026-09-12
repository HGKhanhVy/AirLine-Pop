using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A ring of instances of one particle prefab.
    ///
    /// Nothing is created or destroyed while the board is being played: every instance is
    /// made once, and a burst that is asked for while all of them are busy reuses the
    /// oldest. Overrunning is normal at speed and must cost nothing, so the oldest burst
    /// is restarted rather than a new object made.
    /// </summary>
    public sealed class EffectPool
    {
        private readonly List<ParticleSystem> instances;
        private readonly bool isTinted;
        private int next;

        public EffectPool(ParticleSystem prefab, Transform parent, int capacity, bool isTinted)
        {
            this.isTinted = isTinted;
            instances = new List<ParticleSystem>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                ParticleSystem instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(true);
                instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                instances.Add(instance);
            }
        }

        public void Play(Vector3 worldPosition, Color tint)
        {
            ParticleSystem instance = instances[next];
            next = next + 1 >= instances.Count ? 0 : next + 1;

            instance.transform.position = worldPosition;

            if (isTinted)
            {
                ParticleSystem.MainModule main = instance.main;
                main.startColor = tint;
            }

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.Play(true);
        }

        public void StopAll()
        {
            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
