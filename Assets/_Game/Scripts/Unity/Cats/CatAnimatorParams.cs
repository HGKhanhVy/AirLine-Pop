using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Names shared by the baked cat controller and the code that drives it, hashed once
    /// so no string lookup happens per frame.
    /// </summary>
    public static class CatAnimatorParams
    {
        public const string SpeedName = "Speed";

        /// <summary>Int parameter: the held <see cref="CatPose"/> the animator loops.</summary>
        public const string ActionName = "Action";
        public const string JumpName = "Jump";
        public const string CelebrateName = "Celebrate";
        public const string PetName = "Pet";
        public const string PlayName = "Play";
        public const string RefuseName = "Refuse";
        public const string EatName = "Eat";

        public static readonly int Speed = Animator.StringToHash(SpeedName);
        public static readonly int Action = Animator.StringToHash(ActionName);
        public static readonly int Jump = Animator.StringToHash(JumpName);
        public static readonly int Celebrate = Animator.StringToHash(CelebrateName);
        public static readonly int Pet = Animator.StringToHash(PetName);
        public static readonly int Play = Animator.StringToHash(PlayName);
        public static readonly int Refuse = Animator.StringToHash(RefuseName);
        public static readonly int Eat = Animator.StringToHash(EatName);

        /// <summary>Speed value the blend tree treats as a walk; run sits above it.</summary>
        public const float WalkSpeed = 1f;
        public const float RunSpeed = 2.5f;
    }
}
