using UnityEngine;

namespace Zoea.World{
    /// <summary>
    /// A single consumable pellet worth a fixed amount of EP.
    /// </summary>
    public class Pellet : MonoBehaviour{
        [SerializeField] private int _value = 1;

        private bool _consumed = false;

        /// <summary>
        /// Consumes the pellet, destroying it, and returns the EP it was
        /// worth. Returns 0 and does nothing if it was already consumed.
        ///
        /// The _consumed guard is load-bearing: Destroy() does not remove
        /// the GameObject until the end of the frame, so its collider stays
        /// live and can register further trigger events in the interim.
        /// Without the guard a pellet consumed once could pay out again.
        /// </summary>
        public int TryConsume(){
            if(_consumed){
                return 0;
            }
            _consumed = true;
            Destroy(gameObject);
            return _value;
        }
    }
}
