using UnityEngine;

namespace Zoea.Economy{
    /// <summary>
    /// Holds the evolution points the player is currently carrying.
    /// Carried EP is at risk: it will be lost on death in a later milestone.
    /// This is NOT the bank — a separate BankEP class will hold safely
    /// banked EP once that milestone exists.
    /// </summary>
    public class SelfEP : MonoBehaviour{
        private int _current = 0;

        /// <summary>The EP currently carried and at risk of loss.</summary>
        public int Current{ get{ return _current; } }

        /// <summary>Raised with the new total whenever Current changes. "Subscription list" in effect.</summary>
        public event System.Action<int> Changed;

        private void Start(){
            // Raised in Start rather than Awake so listeners have had their
            // own Awake() run and subscribed already — Awake runs on every
            // object before Start runs on any object.
            Changed?.Invoke(_current);
        }

        /// <summary>
        /// Adds to the carried total and raises Changed. Amounts that are
        /// negative or zero are ignored.
        /// </summary>
        public void Add(int amount){
            if(amount <= 0){
                return;
            }
            _current += amount;
            Changed?.Invoke(_current);
        }
    }
}
