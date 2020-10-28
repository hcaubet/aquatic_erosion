using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (Generateur))]
public class GenerateurEditor : Editor {

    Generateur g;

    public override void OnInspectorGUI () {
        DrawDefaultInspector ();

        if (GUILayout.Button ("Générer le terrain")) {
            g.Generer ();
        }

    }

    void OnEnable () {
        g = (Generateur) target;
    }
}