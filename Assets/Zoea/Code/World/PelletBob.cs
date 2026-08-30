using UnityEngine;

namespace Zoea.World{
    /// <summary>
    /// Makes a pellet drift slowly in place so it reads as suspended in
    /// water rather than frozen.
    /// </summary>
    public class PelletBob : MonoBehaviour{
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobSpeed = 2.0f;
        [SerializeField] private float _swaySpeed = 0.8f;
        //[SerializeField] private float _spinSpeed = 12f;
        [SerializeField] private bool _randomisePhase = true;

        private Vector3 _origin;
        private float _phase;

        private void Awake(){
            _origin = transform.position;
            // Small offsets for the pellets to prevent them from bobbing in unison
            _phase = _randomisePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
        }

        private void Update(){
            float t = Time.time;
            float y = Mathf.Sin(t * _bobSpeed + _phase) * _bobAmplitude;
            float x = Mathf.Sin(t * _swaySpeed + _phase * 1.3f) * _bobAmplitude * 0.5f;
            transform.position = _origin + new Vector3(x, y, 0f);
            // uncomment once textures
            // transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
