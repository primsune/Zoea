using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Zoea.EditorTools{
    /// <summary>
    /// Editor-only tool that dumps the active scene's hierarchy — transforms,
    /// render settings and component data — to a plain text file at the
    /// project root. Read-only: never touches the scene, never marks it
    /// dirty, never records undo.
    ///
    /// Sibling GameObjects that share a base name (e.g. "Pellet (1)",
    /// "Pellet (2)", ...) are collapsed to a single-line summary after the
    /// first if the group has more than three members, so large runs of
    /// spawned objects (pellets, etc.) don't dominate the output.
    /// </summary>
    public static class HierarchyDump{
        private static readonly Regex SiblingSuffixPattern = new Regex(@"^(.*) \(\d+\)$", RegexOptions.Compiled);

        /// <summary>Dumps the active scene's hierarchy to hierarchy.txt in the project root.</summary>
        [MenuItem("Zoea/Dump Scene Hierarchy")]
        public static void Dump(){
            StringBuilder sb = new StringBuilder();
            Scene scene = SceneManager.GetActiveScene();

            sb.AppendLine("SCENE: " + scene.name);
            sb.AppendLine("UNITY: " + Application.unityVersion);
            sb.AppendLine("GENERATED: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            sb.AppendLine();
            AppendRenderSettings(sb);
            sb.AppendLine();

            DumpSiblingGroup(scene.GetRootGameObjects(), 0, sb);

            string path = Path.Combine(Application.dataPath, "..", "hierarchy.txt");
            string fullPath = Path.GetFullPath(path);
            File.WriteAllText(fullPath, sb.ToString());
            Debug.Log("Hierarchy dumped to " + fullPath);
        }

        private static void AppendRenderSettings(StringBuilder sb){
            sb.AppendLine("RENDER SETTINGS");
            sb.AppendLine("  Fog: " + RenderSettings.fog + ", " + RenderSettings.fogMode
                + ", density " + FormatFloat(RenderSettings.fogDensity)
                + ", colour " + ColorToHex(RenderSettings.fogColor));
            sb.AppendLine("  Ambient: " + RenderSettings.ambientMode + ", " + ColorToHex(RenderSettings.ambientLight));
            sb.AppendLine("  Skybox: " + (RenderSettings.skybox != null ? RenderSettings.skybox.name : "None"));
        }

        private static void DumpSiblingGroup(GameObject[] siblings, int depth, StringBuilder sb){
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (GameObject sibling in siblings){
                string baseName = StripSiblingSuffix(sibling.name);
                counts[baseName] = counts.TryGetValue(baseName, out int existing) ? existing + 1 : 1;
            }

            HashSet<string> seenFull = new HashSet<string>();
            string indent = new string(' ', depth * 2);

            foreach (GameObject sibling in siblings){
                string baseName = StripSiblingSuffix(sibling.name);
                if (counts[baseName] > 3){
                    if (seenFull.Add(baseName)){
                        DumpGameObject(sibling, depth, sb);
                    }else{
                        Vector3 pos = sibling.transform.localPosition;
                        sb.AppendLine(indent + sibling.name + "  pos" + FormatVector3(pos));
                    }
                }else{
                    DumpGameObject(sibling, depth, sb);
                }
            }
        }

        private static void DumpGameObject(GameObject go, int depth, StringBuilder sb){
            string indent = new string(' ', depth * 2);

            string headerLine = indent + go.name;
            if (!go.activeSelf){
                headerLine += "  INACTIVE";
            }
            if (go.tag != "Untagged"){
                headerLine += "  tag:" + go.tag;
            }
            sb.AppendLine(headerLine);

            Transform transform = go.transform;
            sb.AppendLine(indent + "  Transform  pos" + FormatVector3(transform.localPosition)
                + "  rot" + FormatVector3(transform.localEulerAngles)
                + "  scale" + FormatVector3(transform.localScale));

            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components){
                if (component is Transform){
                    continue;
                }

                if (component == null){
                    sb.AppendLine(indent + "  *** MISSING SCRIPT ***");
                    continue;
                }

                DumpComponent(component, indent, sb);
            }

            int childCount = transform.childCount;
            if (childCount > 0){
                GameObject[] children = new GameObject[childCount];
                for (int i = 0; i < childCount; i++){
                    children[i] = transform.GetChild(i).gameObject;
                }
                DumpSiblingGroup(children, depth + 1, sb);
            }
        }

        private static void DumpComponent(Component component, string indent, StringBuilder sb){
            Type type = component.GetType();

            if (type.Namespace != null && type.Namespace.StartsWith("Zoea", StringComparison.Ordinal)){
                sb.AppendLine(indent + "  " + type.Name);
                DumpZoeaFields(component, indent, sb);
                return;
            }

            if (component is Rigidbody rigidbody){
                sb.AppendLine(indent + "  Rigidbody");
                sb.AppendLine(indent + "    mass: " + FormatFloat(rigidbody.mass)
                    + ", linearDamping: " + FormatFloat(rigidbody.linearDamping)
                    + ", angularDamping: " + FormatFloat(rigidbody.angularDamping)
                    + ", useGravity: " + rigidbody.useGravity
                    + ", isKinematic: " + rigidbody.isKinematic
                    + ", interpolation: " + rigidbody.interpolation
                    + ", constraints: " + rigidbody.constraints);
                return;
            }

            if (component is Light light){
                sb.AppendLine(indent + "  Light");
                sb.AppendLine(indent + "    type: " + light.type
                    + ", color: " + ColorToHex(light.color)
                    + ", intensity: " + FormatFloat(light.intensity)
                    + ", range: " + FormatFloat(light.range));
                return;
            }

            if (component is Camera camera){
                sb.AppendLine(indent + "  Camera");
                sb.AppendLine(indent + "    clearFlags: " + camera.clearFlags
                    + ", backgroundColor: " + ColorToHex(camera.backgroundColor)
                    + ", fieldOfView: " + FormatFloat(camera.fieldOfView)
                    + ", nearClipPlane: " + FormatFloat(camera.nearClipPlane)
                    + ", farClipPlane: " + FormatFloat(camera.farClipPlane));
                return;
            }

            if (component is AudioSource audioSource){
                sb.AppendLine(indent + "  AudioSource");
                sb.AppendLine(indent + "    clip: " + (audioSource.clip != null ? audioSource.clip.name : "None")
                    + ", volume: " + FormatFloat(audioSource.volume)
                    + ", pitch: " + FormatFloat(audioSource.pitch)
                    + ", loop: " + audioSource.loop
                    + ", playOnAwake: " + audioSource.playOnAwake
                    + ", spatialBlend: " + FormatFloat(audioSource.spatialBlend));
                return;
            }

            if (component is Collider collider){
                sb.AppendLine(indent + "  " + type.Name);
                sb.AppendLine(indent + "    isTrigger: " + collider.isTrigger);
                return;
            }

            if (component is Renderer renderer){
                sb.AppendLine(indent + "  " + type.Name);
                Material[] materials = renderer.sharedMaterials;
                string materialNames = materials.Length > 0
                    ? string.Join(", ", materials.Select(m => m != null ? m.name : "None"))
                    : "None";
                sb.AppendLine(indent + "    materials: " + materialNames);
                return;
            }

            sb.AppendLine(indent + "  " + type.Name);
        }

        private static void DumpZoeaFields(Component component, string indent, StringBuilder sb){
            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren)){
                enterChildren = false;
                if (prop.name == "m_Script"){
                    continue;
                }
                sb.AppendLine(indent + "    " + prop.displayName + ": " + FormatProperty(prop));
            }
        }

        private static string FormatProperty(SerializedProperty prop){
            switch (prop.propertyType){
                case SerializedPropertyType.Float:
                    return FormatFloat(prop.floatValue);
                case SerializedPropertyType.Integer:
                    return prop.intValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return prop.boolValue.ToString();
                case SerializedPropertyType.String:
                    return prop.stringValue;
                case SerializedPropertyType.Enum:
                    int index = prop.enumValueIndex;
                    if (index >= 0 && prop.enumDisplayNames != null && index < prop.enumDisplayNames.Length){
                        return prop.enumDisplayNames[index];
                    }
                    return index.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector3:
                    return FormatVector3(prop.vector3Value);
                case SerializedPropertyType.Color:
                    return ColorToHex(prop.colorValue);
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "None";
                default:
                    return prop.propertyType.ToString();
            }
        }

        private static string FormatFloat(float value){
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static string FormatVector3(Vector3 v){
            return "(" + FormatFloat(v.x) + ", " + FormatFloat(v.y) + ", " + FormatFloat(v.z) + ")";
        }

        private static string ColorToHex(Color color){
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        private static string StripSiblingSuffix(string name){
            Match match = SiblingSuffixPattern.Match(name);
            return match.Success ? match.Groups[1].Value : name;
        }
    }
}
