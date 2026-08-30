using UnityEditor;
using UnityEngine;
using Zoea.World;
using System.Collections.Generic;

namespace Zoea.EditorTools{
    /// <summary>
    /// Editor window that scatters pellet prefab instances at random
    /// positions inside the volume defined by the scene's Boundaries
    /// component, keeping a minimum spacing between them.
    /// </summary>
    public class PelletScatterWindow : EditorWindow{
        private GameObject _prefab = null;
        private Transform _parent = null;
        private int _count = 60;
        private float _minSpacing = 4f;
        private float _margin = 5f;
        private bool _clearExisting = true;

        /// <summary>Opens the Scatter Pellets window from the Zoea menu.</summary>
        [MenuItem("Zoea/Scatter Pellets")]
        public static void ShowWindow(){
            GetWindow<PelletScatterWindow>("Scatter Pellets");
        }

        private void OnGUI(){
            _prefab = (GameObject)EditorGUILayout.ObjectField("Pellet Prefab", _prefab, typeof(GameObject), false);
            _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);
            _count = EditorGUILayout.IntField("Count", _count);
            _minSpacing = EditorGUILayout.FloatField("Min Spacing", _minSpacing);
            _margin = EditorGUILayout.FloatField("Margin", _margin);
            _clearExisting = EditorGUILayout.Toggle("Clear Existing", _clearExisting);

            EditorGUILayout.Space();

            if(GUILayout.Button("Scatter")){
                Scatter();
            }
        }

        private void Scatter(){
            if(_prefab == null){
                EditorUtility.DisplayDialog("Scatter Pellets", "Assign a pellet prefab first.", "OK");
                return;
            }
            if(_parent == null){
                EditorUtility.DisplayDialog("Scatter Pellets", "Assign a parent transform first.", "OK");
                return;
            }
            if(_count < 1){
                EditorUtility.DisplayDialog("Scatter Pellets", "Count must be at least 1.", "OK");
                return;
            }

            Boundaries[] found = Object.FindObjectsByType<Boundaries>();
            if(found.Length == 0){
                EditorUtility.DisplayDialog("Scatter Pellets", "No Boundaries component found in the open scene.", "OK");
                return;
            }
            if(found.Length > 1){
                EditorUtility.DisplayDialog("Scatter Pellets", "Multiple Boundaries components found. There must be exactly one.", "OK");
                return;
            }
            Boundaries boundaries = found[0];

            Vector3 c = boundaries.Center;
            Vector3 half = boundaries.Size * 0.5f;
            Vector3 min = new Vector3(c.x - half.x + _margin, c.y - half.y + _margin, c.z - half.z + _margin);
            Vector3 max = new Vector3(c.x + half.x - _margin, c.y + half.y - _margin, c.z + half.z - _margin);
            if(min.x > max.x || min.y > max.y || min.z > max.z){
                EditorUtility.DisplayDialog("Scatter Pellets", "Margin is too large for the volume.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Scatter Pellets");
            int undoGroup = Undo.GetCurrentGroup();

            if(_clearExisting){
                for(int i = _parent.childCount - 1; i >= 0; i--){
                    Undo.DestroyObjectImmediate(_parent.GetChild(i).gameObject);
                }
            }

            // Rejection sampling: for each pellet, try random candidate
            // positions within the inset volume and accept the first one
            // that is at least _minSpacing away from every position already
            // accepted. Squared distances are compared against
            // _minSpacing squared so no square root is needed per check.
            // Giving up after 30 attempts keeps a bad configuration (too
            // many pellets for the volume, spacing too large) from hanging
            // the editor instead of looping forever.
            List<Vector3> accepted = new List<Vector3>();
            float minSpacingSqr = _minSpacing * _minSpacing;
            for(int i = 0; i < _count; i++){
                bool placed = false;
                for(int attempt = 0; attempt < 30; attempt++){
                    Vector3 candidate = new Vector3(
                        Random.Range(min.x, max.x),
                        Random.Range(min.y, max.y),
                        Random.Range(min.z, max.z));
                    bool farEnough = true;
                    for(int j = 0; j < accepted.Count; j++){
                        if((candidate - accepted[j]).sqrMagnitude < minSpacingSqr){
                            farEnough = false;
                            break;
                        }
                    }
                    if(farEnough){
                        accepted.Add(candidate);
                        placed = true;
                        break;
                    }
                }
                if(!placed){
                    continue;
                }
            }

            for(int i = 0; i < accepted.Count; i++){
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
                instance.transform.position = accepted[i];
                instance.transform.rotation = Quaternion.identity;
                instance.transform.SetParent(_parent);
                Undo.RegisterCreatedObjectUndo(instance, "Scatter Pellets");
            }

            Undo.CollapseUndoOperations(undoGroup);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_parent.gameObject.scene);

            if(accepted.Count < _count){
                Debug.Log($"Scatter Pellets: requested {_count}, placed {accepted.Count}. Fewer pellets were placed than requested; try lowering the minimum spacing or the count.");
            }else{
                Debug.Log($"Scatter Pellets: requested {_count}, placed {accepted.Count}.");
            }
        }
    }
}
