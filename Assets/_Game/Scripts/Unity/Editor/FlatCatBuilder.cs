using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Bakes the passenger cats: one prefab and one animator per breed.
    ///
    /// Each cat is a chibi traveller drawn frame by frame (the Craftpix cute cats, brought
    /// in by Tools/flat_art/import_craftpix_cats.py): one sprite whose drawing the clips
    /// swap. Held poses follow the animator's Action int (<see cref="CatPose"/>); one-off
    /// actions keep the triggers (<see cref="CatAnimatorParams"/>), so the brain, the lounge
    /// and the win card drive the cat as before. Safe to rerun.
    /// </summary>
    public static class FlatCatBuilder
    {
        private const string ArtFolder = "Assets/_Game/Art/FlatCats";
        private const string ClipFolder = ArtFolder + "/Animations";
        private const string BreedFolder = "Assets/_Game/Config/Cats";
        private const string BodyPath = "Visual/Body";

        // Drawn clips, by the names the import gives them.
        private const string IdleClip = "Idle";
        private const string WalkClip = "Walk";
        private const string JumpClip = "Jump";
        private const string RollClip = "Roll";
        private const string DizzyClip = "Dizzy";

        // Run reuses the walk drawings played faster.
        private const float RunSeconds = 0.5f;

        [MenuItem("Tools/AirLine Pop/Build Flat Cats (2D)")]
        public static void BuildFromMenu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            CatMotionMeta motion = CatMotionSheets.LoadMeta();

            if (motion == null)
            {
                return "No cat drawings yet; run Tools/flat_art/import_craftpix_cats.py.";
            }

            AssetWriter.EnsureFolder(ClipFolder);
            var baked = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CatBreedSO), new[] { BreedFolder }))
            {
                var breed = AssetDatabase.LoadAssetAtPath<CatBreedSO>(AssetDatabase.GUIDToAssetPath(guid));
                Dictionary<string, Sprite[]> drawings = ImportDrawings(breed.Id, motion);

                if (drawings == null)
                {
                    Debug.LogWarning("No drawings for breed '" + breed.Id + "'; run import_craftpix_cats.py.");
                    continue;
                }

                AnimatorController controller = BuildController(breed.Id, drawings, motion);
                CatView view = BakeBreed(breed.Id, drawings[IdleClip][0], controller);
                breed.EditorSetPrefab(view);
                EditorUtility.SetDirty(breed);
                baked++;
            }

            AssetDatabase.SaveAssets();
            return "Baked " + baked + " passenger cats into " + ArtFolder + ".";
        }

        /// <summary>
        /// Bakes a visitor who is not one of the regulars, such as the VIP guest, from its
        /// drawings the same way, or returns null when they are missing.
        /// </summary>
        public static CatView BuildGuest(string id)
        {
            CatMotionMeta motion = CatMotionSheets.LoadMeta();
            Dictionary<string, Sprite[]> drawings = motion == null ? null : ImportDrawings(id, motion);

            if (drawings == null)
            {
                return null;
            }

            AssetWriter.EnsureFolder(ClipFolder);
            AnimatorController controller = BuildController(id, drawings, motion);
            return BakeBreed(id, drawings[IdleClip][0], controller);
        }

        private static Dictionary<string, Sprite[]> ImportDrawings(string breedId, CatMotionMeta motion)
        {
            var drawings = new Dictionary<string, Sprite[]>();

            foreach (CatMotionClipMeta clip in motion.clips)
            {
                Sprite[] frames = CatMotionSheets.Import(breedId, clip, motion);

                if (frames == null || frames.Length == 0)
                {
                    return null;
                }

                drawings[clip.name] = frames;
            }

            return drawings.ContainsKey(IdleClip) && drawings.ContainsKey(WalkClip) ? drawings : null;
        }

        // ------------------------------------------------------------------ prefab

        private static CatView BakeBreed(string id, Sprite standing, RuntimeAnimatorController controller)
        {
            var root = new GameObject("CatFlat_" + id);

            try
            {
                var visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
                visual.gameObject.AddComponent<SortingGroup>();

                // The drawings carry their own floor shadow.
                var body = new GameObject("Body");
                body.transform.SetParent(visual, false);
                var renderer = body.AddComponent<SpriteRenderer>();
                renderer.sprite = standing;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                BoxCollider tap = root.AddComponent<BoxCollider>();
                tap.center = new Vector3(0f, 0.32f, 0f);
                tap.size = new Vector3(0.6f, 0.64f, 0.45f);

                CatBillboard billboard = root.AddComponent<CatBillboard>();
                billboard.EditorLink(root.transform, visual);

                CatView view = root.AddComponent<CatView>();
                view.EditorLink(animator, tap, billboard);

                var anchors = AssetDatabase.LoadAssetAtPath<CatHeadAnchorsSO>(CatHeadAnchorBaker.AssetPath);

                if (anchors != null)
                {
                    CatAccessoryInstaller.AddTo(root, renderer, anchors);
                }

                if (id == VipRobeInstaller.GuestId)
                {
                    VipRobeInstaller.Dress(root);
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ArtFolder + "/CatFlat_" + id + ".prefab");
                return saved.GetComponent<CatView>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ animator

        private static AnimatorController BuildController(string breedId, Dictionary<string, Sprite[]> drawings, CatMotionMeta motion)
        {
            string folder = ClipFolder + "/" + breedId;
            AssetWriter.EnsureFolder(folder);
            string path = ArtFolder + "/CatFlat_" + breedId + ".controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(CatAnimatorParams.SpeedName, AnimatorControllerParameterType.Float);
            controller.AddParameter(CatAnimatorParams.ActionName, AnimatorControllerParameterType.Int);

            var clips = new ClipShelf(folder, drawings, motion);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState locomotion = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = CatAnimatorParams.SpeedName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips.Get(IdleClip), 0f);
            tree.AddChild(clips.Get(WalkClip), CatAnimatorParams.WalkSpeed);
            tree.AddChild(clips.Get("Run", WalkClip, RunSeconds, true), CatAnimatorParams.RunSpeed);
            machine.defaultState = locomotion;

            AnimatorState wait = AddPose(machine, locomotion, CatPose.Wait, clips.Get("Wait", IdleClip));
            AddPose(machine, locomotion, CatPose.Play, clips.Get(RollClip));
            AddPose(machine, locomotion, CatPose.Hop, clips.Get("Hop", JumpClip));
            AddPose(machine, locomotion, CatPose.Dizzy, clips.Get(DizzyClip));

            AddAction(controller, machine, locomotion, wait, CatAnimatorParams.JumpName, clips.Get(JumpClip, JumpClip, 0f, false), 1f);
            AddAction(controller, machine, locomotion, wait, CatAnimatorParams.CelebrateName, clips.Get("Celebrate", JumpClip, 0f, false), 2f);
            AddAction(controller, machine, locomotion, wait, CatAnimatorParams.PetName, clips.Get("Pet", JumpClip, 0f, false), 2f);
            AddAction(controller, machine, locomotion, wait, CatAnimatorParams.PlayName, clips.Get("PlayBurst", RollClip, 0f, false), 3f);
            AddAction(controller, machine, locomotion, wait, CatAnimatorParams.RefuseName, clips.Get("Refuse", DizzyClip, 0f, false), 1f);
            AddAction(controller, machine, locomotion, wait, CatAnimatorParams.EatName, clips.Get("Eat", JumpClip, 0f, false), 3f);

            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>
        /// A held pose, entered from locomotion while Action equals it and left as soon as it
        /// does not. Poses hang off locomotion rather than Any State, so a one-off action can
        /// play over a held pose without the pose snatching control back mid-way.
        /// </summary>
        private static AnimatorState AddPose(AnimatorStateMachine machine, AnimatorState locomotion, CatPose pose, AnimationClip clip)
        {
            AnimatorState state = machine.AddState(pose.ToString());
            state.motion = clip;

            AnimatorStateTransition enter = locomotion.AddTransition(state);
            enter.AddCondition(AnimatorConditionMode.Equals, (int)pose, CatAnimatorParams.ActionName);
            enter.hasExitTime = false;
            enter.duration = 0f;

            AnimatorStateTransition leave = state.AddTransition(locomotion);
            leave.AddCondition(AnimatorConditionMode.NotEqual, (int)pose, CatAnimatorParams.ActionName);
            leave.hasExitTime = false;
            leave.duration = 0f;
            return state;
        }

        /// <summary>
        /// A one-off action fired by a trigger from anywhere. When it ends it returns to
        /// waiting if the cat is still being looked at, otherwise to locomotion.
        /// </summary>
        private static void AddAction(AnimatorController controller, AnimatorStateMachine machine, AnimatorState locomotion,
            AnimatorState wait, string trigger, AnimationClip clip, float loops)
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            AnimatorState state = machine.AddState(trigger);
            state.motion = clip;

            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            enter.duration = 0f;

            AnimatorStateTransition toWait = state.AddTransition(wait);
            toWait.AddCondition(AnimatorConditionMode.Equals, (int)CatPose.Wait, CatAnimatorParams.ActionName);
            toWait.hasExitTime = true;
            toWait.exitTime = loops;
            toWait.duration = 0f;

            AnimatorStateTransition toLocomotion = state.AddTransition(locomotion);
            toLocomotion.AddCondition(AnimatorConditionMode.NotEqual, (int)CatPose.Wait, CatAnimatorParams.ActionName);
            toLocomotion.hasExitTime = true;
            toLocomotion.exitTime = loops;
            toLocomotion.duration = 0f;
        }

        /// <summary>
        /// Makes one breed's flip-book clips. Drawings are shown in turn, evenly over the clip;
        /// a sprite cannot blend, so every transition above is a cut.
        /// </summary>
        private sealed class ClipShelf
        {
            private readonly string folder;
            private readonly Dictionary<string, Sprite[]> drawings;
            private readonly Dictionary<string, CatMotionClipMeta> metas = new Dictionary<string, CatMotionClipMeta>();

            public ClipShelf(string folder, Dictionary<string, Sprite[]> drawings, CatMotionMeta motion)
            {
                this.folder = folder;
                this.drawings = drawings;

                foreach (CatMotionClipMeta meta in motion.clips)
                {
                    metas[meta.name] = meta;
                }
            }

            public AnimationClip Get(string drawing)
            {
                return Get(drawing, drawing, 0f, true);
            }

            public AnimationClip Get(string name, string drawing)
            {
                return Get(name, drawing, 0f, true);
            }

            /// <summary>A clip named <paramref name="name"/> from a drawing; 0 seconds keeps the drawing's own pace.</summary>
            public AnimationClip Get(string name, string drawing, float seconds, bool loop)
            {
                // A breed missing an optional drawing stands still for it instead.
                if (!drawings.TryGetValue(drawing, out Sprite[] frames))
                {
                    drawing = IdleClip;
                    frames = drawings[drawing];
                }

                float length = seconds > 0f ? seconds : metas[drawing].seconds;
                var clip = new AnimationClip { name = name, frameRate = 30f };
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                settings.stopTime = length;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                var keys = new ObjectReferenceKeyframe[frames.Length];
                float step = length / frames.Length;

                for (int i = 0; i < frames.Length; i++)
                {
                    keys[i] = new ObjectReferenceKeyframe { time = i * step, value = frames[i] };
                }

                var binding = EditorCurveBinding.PPtrCurve(BodyPath, typeof(SpriteRenderer), "m_Sprite");
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

                string path = folder + "/" + name + ".anim";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(clip, path);
                return clip;
            }
        }
    }
}
