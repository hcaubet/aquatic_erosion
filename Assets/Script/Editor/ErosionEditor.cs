using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (Erosion))]
public class ErosionEditor : Editor {

    Erosion e;

    public override void OnInspectorGUI () {
        DrawDefaultInspector ();

        if (GUILayout.Button ("Eroder la scene")) {
            e.Eroder ();
        }
    }

    void OnEnable () {
        e = (Erosion) target;
    }
}