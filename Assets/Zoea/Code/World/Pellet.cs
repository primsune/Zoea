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
        /// The _consumed guard is load-bearing: the player has two
        /// colliders, so OnTriggerEnter can fire twice for the same pellet
        /// in one physics step, and Destroy() does not remove the object
        /// until the end of the frame. Without the guard the same pellet
        /// would be counted twice.
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
