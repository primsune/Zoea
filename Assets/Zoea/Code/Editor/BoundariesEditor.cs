using UnityEditor;
using UnityEngine;
using Zoea.World;

namespace Zoea.EditorTools{
    /// <summary>
    /// Custom Inspector for <see cref="Boundaries"/>. Adds a "Rebuild Walls"
    /// button below the default serialized fields, so Prim can regenerate
    /// the six boundary walls after editing the centre, size or thickness
    /// without leaving the Inspector.
    ///
    /// The namespace is Zoea.EditorTools rather than Zoea.Editor, because
    /// Zoea.Editor would collide with UnityEditor.Editor.
    /// </summary>
    [CustomEditor(typeof(Boundaries))]
    public class BoundariesEditor : Editor{
        /// <summary>
        /// Draws the default Inspector, then a "Rebuild Walls" button that
        /// records an undo step, rebuilds the walls, and marks the scene
        /// dirty so the change is not lost if Prim forgets to save.
        /// </summary>
        public override void OnInspectorGUI(){
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if(GUILayout.Button("Rebuild Walls")){
                Boundaries boundaries = (Boundaries)target;
                Undo.RegisterFullObjectHierarchyUndo(boundaries.gameObject, "Rebuild Boundary Walls");
                boundaries.RebuildWalls();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boundaries.gameObject.scene);
            }
        }
    }
}
