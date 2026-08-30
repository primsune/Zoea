using UnityEngine;

namespace Zoea.World{
    /// <summary>
    /// Defines a rectangular playable volume in world space and builds six
    /// box-collider walls around it so the player and AI creatures cannot
    /// swim out of the level.
    /// </summary>
    public class Boundaries : MonoBehaviour{
        [SerializeField] private Vector3 _center = Vector3.zero;
        [SerializeField] private Vector3 _size = new Vector3(120f, 120f, 120f);
        [SerializeField] private float _wallThickness = 1f;

        /// <summary>The centre of the playable volume, in world space.</summary>
        public Vector3 Center => _center;

        /// <summary>The full width, height and depth of the playable volume.</summary>
        public Vector3 Size => _size;

        /// <summary>
        /// Builds or repositions the six boundary walls (Wall_North,
        /// Wall_South, Wall_East, Wall_West, Wall_Floor, Wall_Ceiling) as
        /// direct children of this GameObject.
        ///
        /// Each wall is a cube whose centre sits on the boundary plane, so
        /// half its thickness sits inside the volume and half outside. An
        /// existing child of the right name is repositioned in place rather
        /// than destroyed and recreated, so any material or other component
        /// a user has added to it survives a rebuild.
        ///
        /// _center and _size are world-space values independent of this
        /// transform's own position, so wall positions are assigned via
        /// transform.position rather than localPosition. Wall scales are
        /// assigned via localScale directly from _size and _wallThickness,
        /// which is only correct if this transform's lossy scale is
        /// (1,1,1); a non-identity scale is warned about but not corrected.
        /// </summary>
        public void RebuildWalls(){
            if(!IsApproximatelyOne(transform.lossyScale)){
                Debug.LogWarning($"{nameof(Boundaries)} on '{name}' has a non-identity transform scale; wall scales will be incorrect.", this);
            }
            Vector3 c = _center;
            Vector3 s = _size;
            float t = _wallThickness;
            PlaceWall("Wall_North", new Vector3(c.x, c.y, c.z + s.z / 2f), new Vector3(s.x, s.y, t));
            PlaceWall("Wall_South", new Vector3(c.x, c.y, c.z - s.z / 2f), new Vector3(s.x, s.y, t));
            PlaceWall("Wall_East", new Vector3(c.x + s.x / 2f, c.y, c.z), new Vector3(t, s.y, s.z));
            PlaceWall("Wall_West", new Vector3(c.x - s.x / 2f, c.y, c.z), new Vector3(t, s.y, s.z));
            PlaceWall("Wall_Ceiling", new Vector3(c.x, c.y + s.y / 2f, c.z), new Vector3(s.x, t, s.z));
            PlaceWall("Wall_Floor", new Vector3(c.x, c.y - s.y / 2f, c.z), new Vector3(s.x, t, s.z));
        }

        // Finds a direct child by name and repositions it, or creates a new
        // cube primitive if none exists yet. Reused for all six walls.
        private void PlaceWall(string wallName, Vector3 position, Vector3 scale){
            Transform existing = transform.Find(wallName);
            GameObject wall;
            if(existing != null){
                wall = existing.gameObject;
            }else{
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = wallName;
                wall.transform.SetParent(transform);
            }
            wall.transform.position = position;
            wall.transform.rotation = Quaternion.identity;
            wall.transform.localScale = scale;
        }

        private static bool IsApproximatelyOne(Vector3 v){
            return Mathf.Approximately(v.x, 1f) && Mathf.Approximately(v.y, 1f) && Mathf.Approximately(v.z, 1f);
        }

        private void OnDrawGizmosSelected(){
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_center, _size);
        }
    }
}
