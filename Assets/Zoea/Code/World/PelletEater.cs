using UnityEngine;
using Zoea.Economy;

namespace Zoea.World{
    /// <summary>
    /// Sits on the player. Detects pellets via a trigger collider, consumes
    /// them, and credits their value to the player's carried EP.
    /// </summary>
    public class PelletEater : MonoBehaviour{
        [SerializeField] private SelfEP _ep = null;
        [SerializeField] private AudioSource _eatAudio = null;
        [SerializeField] private AudioClip _eatClip = null;
        [SerializeField] private float _pitchVariation = 0.1f;

        private void Start(){
            if(_ep == null){
                Debug.LogError($"{nameof(PelletEater)} on {name} is missing its {nameof(_ep)} reference.");
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other){
            Pellet pellet = other.GetComponent<Pellet>();
            if(pellet == null){
                return;
            }
            int gained = pellet.TryConsume();
            if(gained <= 0){
                // Already eaten this frame by the player's other collider.
                return;
            }
            _ep.Add(gained);
            // Audio is optional: the eater must work silently without it.
            if(_eatAudio != null && _eatClip != null){
                // PlayOneShot rather than Play so overlapping eats layer
                // instead of cutting each other off.
                _eatAudio.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
                _eatAudio.PlayOneShot(_eatClip);
            }
        }
    }
}
