using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Exposes every take of the CubeAnimals animation file as a named clip and wires them
    /// into one controller shared by all cats: a speed blend tree for idle, walk and run,
    /// and one-shot actions fired by trigger that return to locomotion on their own.
    /// </summary>
    public sealed class CatAnimatorBuilder
    {
        private readonly string animationPath;
        private readonly string controllerPath;

        public CatAnimatorBuilder(string packFolder, string outputFolder)
        {
            animationPath = packFolder + "/Animations/Master_Animation.fbx";
            controllerPath = outputFolder + "/Cat.controller";
        }

        public RuntimeAnimatorController Build()
        {
            ImportClips();

            AssetDatabase.DeleteAsset(controllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter(CatAnimatorParams.SpeedName, AnimatorControllerParameterType.Float);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState locomotion = AddLocomotion(controller, machine);
            machine.defaultState = locomotion;

            AddAction(controller, machine, locomotion, CatAnimatorParams.JumpName, "Jump", 1f);
            AddAction(controller, machine, locomotion, CatAnimatorParams.CelebrateName, "Celebrate", 1f);
            AddAction(controller, machine, locomotion, CatAnimatorParams.PetName, "NodSoft", 2f);
            AddAction(controller, machine, locomotion, CatAnimatorParams.PlayName, "Pounce", 1f);
            AddAction(controller, machine, locomotion, CatAnimatorParams.RefuseName, "Shake", 1f);

            // No eating take ships with the pack; a firm nod repeated reads as head-down bites.
            AddAction(controller, machine, locomotion, CatAnimatorParams.EatName, "Nod", 3f);

            AssetDatabase.SaveAssets();
            return controller;
        }

        private void ImportClips()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(animationPath);
            ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
            var clips = new ModelImporterClipAnimation[takes.Length];

            for (int i = 0; i < takes.Length; i++)
            {
                clips[i] = takes[i];
                clips[i].name = ClipName(takes[i].takeName);
                clips[i].loopTime = IsLooping(clips[i].name);
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static string ClipName(string take)
        {
            switch (take)
            {
                case "root|IDLE_Anim": return "IdleLong";
                case "root|Jump_1": return "Jump";
                case "root|Jump_2": return "Celebrate";
                case "root|Nod Firmly": return "Nod";
                case "root|Nod Slightly": return "NodSoft";
                case "root|Shake Head": return "Shake";
                case "root|Shake Head_2": return "ShakeAlt";
                case "root|Attack": return "Pounce";
                default: return take.Replace("root|", string.Empty);
            }
        }

        private static bool IsLooping(string clip)
        {
            return clip == "Idle" || clip == "IdleLong" || clip == "Walk" || clip == "Run" || clip == "Nod";
        }

        private AnimationClip LoadClip(string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(animationPath))
            {
                if (asset is AnimationClip clip && clip.name == name)
                {
                    return clip;
                }
            }

            throw new System.InvalidOperationException("Clip '" + name + "' not found in " + animationPath);
        }

        private AnimatorState AddLocomotion(AnimatorController controller, AnimatorStateMachine machine)
        {
            AnimatorState state = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = CatAnimatorParams.SpeedName;
            tree.useAutomaticThresholds = false;
            tree.AddChild(LoadClip("Idle"), 0f);
            tree.AddChild(LoadClip("Walk"), CatAnimatorParams.WalkSpeed);
            tree.AddChild(LoadClip("Run"), CatAnimatorParams.RunSpeed);
            return state;
        }

        private void AddAction(AnimatorController controller, AnimatorStateMachine machine, AnimatorState locomotion, string trigger, string clip, float loops)
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

            AnimatorState state = machine.AddState(trigger);
            state.motion = LoadClip(clip);

            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            enter.duration = 0.1f;

            AnimatorStateTransition exit = state.AddTransition(locomotion);
            exit.hasExitTime = true;
            exit.exitTime = loops - 0.1f;
            exit.duration = 0.15f;
        }
    }
}
