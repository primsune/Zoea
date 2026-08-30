using TMPro;
using UnityEngine;
using Zoea.Economy;

namespace Zoea.UI{
    /// <summary>
    /// Displays carried EP as text, kept in sync via SelfEP.Changed.
    /// </summary>
    public class CounterEP : MonoBehaviour{
        [SerializeField] private SelfEP _ep = null;
        [SerializeField] private TMP_Text _label = null;
        [SerializeField] private string _format = "EP: {0}";

        private bool _subscribed = false;

        private void Awake(){
            if(_ep == null || _label == null){
                Debug.LogError($"{nameof(CounterEP)} on {name} is missing its {nameof(_ep)} or {nameof(_label)} reference.");
                enabled = false;
                return;
            }
            // Subscribed in Awake, not Start, because SelfEP raises its
            // initial Changed event in Start. Awake runs on every object
            // before Start runs on any object, so this ordering guarantees
            // the counter catches that first event.
            _ep.Changed += OnEPChanged;
            _subscribed = true;
        }

        private void OnDestroy(){
            if(_subscribed){
                _ep.Changed -= OnEPChanged;
                _subscribed = false;
            }
        }

        private void OnEPChanged(int newTotal){
            _label.text = string.Format(_format, newTotal);
        }
    }
}
