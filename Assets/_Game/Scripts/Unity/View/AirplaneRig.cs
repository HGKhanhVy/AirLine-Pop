using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What draws the airplane. <see cref="AirplaneView"/> decides where it flies; a rig only
    /// turns that pose into renderers, so a flat sprite plane and a modelled one share the
    /// same flight.
    /// </summary>
    public abstract class AirplaneRig : MonoBehaviour
    {
        /// <param name="position">Point on the board plane under the airplane.</param>
        /// <param name="heading">Degrees about Z; 0 points up the screen.</param>
        /// <param name="bank">Roll in degrees; positive lowers the left wing.</param>
        /// <param name="scale">Size relative to parked.</param>
        /// <param name="height">Height above the board in world units.</param>
        public abstract void Pose(Vector3 position, float heading, float bank, float scale, float height);

        public abstract void SetVisible(bool isVisible);
    }
}
