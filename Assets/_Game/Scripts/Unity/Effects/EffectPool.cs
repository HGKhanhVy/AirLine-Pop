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

        // Per instance, every particle system in it and the alpha each was authored with.
        // The reference bursts are layered: FX_glow's root is a soft glow, and the sparks
        // that fly out are a child system with a fixed blue of their own. Tinting only the
        // root left those sparks blue on every level, whatever colour the square was.
        private readonly List<ParticleSystem[]> tintedSystems;
        private readonly List<float[]> authoredAlphas;

        private int next;

        public EffectPool(ParticleSystem prefab, Transform parent, int capacity, bool isTinted, float scale)
        {
            this.isTinted = isTinted;
            instances = new List<ParticleSystem>(capacity);
            tintedSystems = new List<ParticleSystem[]>(capacity);
            authoredAlphas = new List<float[]>(capacity);

            for (int i = 0; i < capacity; i++)
            {
                ParticleSystem instance = Object.Instantiate(prefab, parent);
                instance.transform.localScale *= scale;
                instance.gameObject.SetActive(true);
                instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                instances.Add(instance);

                if (isTinted)
                {
                    CacheTintTargets(instance);
                }
            }
        }

        public void Play(Vector3 worldPosition, Color tint)
        {
            int index = next;
            ParticleSystem instance = instances[index];
            next = next + 1 >= instances.Count ? 0 : next + 1;

            instance.transform.position = worldPosition;

            if (isTinted)
            {
                ApplyTint(index, tint);
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

        /// <summary>
        /// Looked up once, when the pool is built, so a burst during a drag touches no
        /// hierarchy at all.
        /// </summary>
        private void CacheTintTargets(ParticleSystem instance)
        {
            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            float[] alphas = new float[systems.Length];

            for (int i = 0; i < systems.Length; i++)
            {
                alphas[i] = systems[i].main.startColor.color.a;
            }

            tintedSystems.Add(systems);
            authoredAlphas.Add(alphas);
        }

        /// <summary>
        /// Takes the hue from the tint and the transparency from the art: a layer authored
        /// half transparent stays half transparent, it just changes colour.
        /// </summary>
        private void ApplyTint(int index, Color tint)
        {
            ParticleSystem[] systems = tintedSystems[index];
            float[] alphas = authoredAlphas[index];

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem.MainModule main = systems[i].main;
                main.startColor = new Color(tint.r, tint.g, tint.b, tint.a * alphas[i]);
            }
        }
    }
}
