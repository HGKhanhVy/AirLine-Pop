using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Plays the board's particles from pools built once at load.
    ///
    /// Every effect the board can show is listed in the Inspector, so which of the
    /// reference game's particles belongs to which moment is a data decision a designer
    /// can see and change. A moment with no prefab assigned simply plays nothing, which
    /// is what lets the mapping be filled in one effect at a time.
    /// </summary>
    public sealed class PooledEffectPlayer : MonoBehaviour, IEffectPlayer
    {
        [Tooltip("Parent for the pooled instances. Leave empty to use this object.")]
        [SerializeField] private Transform effectRoot;

        [SerializeField] private EffectBinding[] bindings;

        private readonly Dictionary<GameplayEffect, EffectPool> pools =
            new Dictionary<GameplayEffect, EffectPool>();

        private void Awake()
        {
            if (effectRoot == null)
            {
                effectRoot = transform;
            }

            if (bindings == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Length; i++)
            {
                EffectBinding binding = bindings[i];

                if (binding == null || binding.Prefab == null || pools.ContainsKey(binding.Effect))
                {
                    continue;
                }

                pools.Add(binding.Effect, new EffectPool(
                    binding.Prefab, effectRoot, binding.Capacity, binding.IsTinted, binding.Scale));
            }
        }

        public void Play(GameplayEffect effect, Vector3 worldPosition, Color tint)
        {
            if (pools.TryGetValue(effect, out EffectPool pool))
            {
                pool.Play(worldPosition, tint);
            }
        }

        public void StopAll()
        {
            foreach (KeyValuePair<GameplayEffect, EffectPool> entry in pools)
            {
                entry.Value.StopAll();
            }
        }
    }
}
