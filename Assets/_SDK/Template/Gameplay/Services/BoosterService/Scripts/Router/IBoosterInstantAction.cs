using ASTeams.Base.Data;

namespace ASTeams.Base.Gameplay
{
    public interface IBoosterInstantAction
    {
        BoosterType Type { get; }
        /// return true nếu apply thành công (lúc đó service mới consume)
        bool TryApply();
    }
}