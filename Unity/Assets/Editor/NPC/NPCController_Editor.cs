﻿using UnityEditor;
using UnityEngine;

namespace NPC {

    [CustomEditor(typeof(NPCController))]
    public class NPCController_Editor : Editor {

        NPCController gController;

        #region Constants
        private const string label_ViewAngle = "View Angle";
        private const string label_PerceptionRadius = "Perception Radius";
        private const string label_BodyNavigation = "Navigation";
        private const string label_IKEnabled = "Enable IK";
        private const string label_MainAgent = "Main Agent";
        private const string label_SelectHighlight = "Enable Selection Indicator";
        private const string label_AnimatorEnabled = "Use Animator";
        private const string label_UseAnimCurves = "Use Animation Curves";
        #endregion

        #region Insperctor_GUI
        private bool gGeneralSettings = true;
        private bool gShowPerception = true;
        private bool gShowBody = true;
        #endregion

        public override void OnInspectorGUI() {

            gController = (NPCController) target;

            EditorGUI.BeginChangeCheck();

            gGeneralSettings = EditorGUILayout.Foldout(gGeneralSettings, "General Settings");

            gController.MainAgent = (bool)EditorGUILayout.Toggle(label_MainAgent, (bool)gController.MainAgent);
            gController.DisplaySelectedHighlight = (bool)EditorGUILayout.Toggle(label_SelectHighlight, (bool)gController.DisplaySelectedHighlight);

            /* Perception Sliders */
            gShowPerception = EditorGUILayout.Foldout(gShowPerception, "Perception") && gController.Perception != null;
            if(gShowPerception) {
                gController.Perception.ViewAngle = (float) EditorGUILayout.IntSlider(label_ViewAngle, (int) gController.Perception.ViewAngle, 
                    (int) NPCPerception.MIN_VIEW_ANGLE, 
                    (int) NPCPerception.MAX_VIEW_ANGLE);
                gController.Perception.PerceptionRadius = (float) EditorGUILayout.IntSlider(label_PerceptionRadius, (int) gController.Perception.PerceptionRadius, 
                    (int) NPCPerception.MIN_PERCEPTION_FIELD, 
                    (int) NPCPerception.MAX_PERCEPTION_FIELD);
            }


            gShowBody = EditorGUILayout.Foldout(gShowBody, "Body") && gController.Body != null;
            if(gShowBody) {
                // gController.Body.SteeringNavigation = (bool)EditorGUILayout.Toggle(label_BodyNavigation, (bool)gController.Body.SteeringNavigation);
                // gController.Body.NavMeshNavigation = (bool)EditorGUILayout.Toggle(label_NavMeshNavigation, (bool)gController.Body.NavMeshNavigation);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label_BodyNavigation);
                gController.Body.Navigation = (NAV_STATE)EditorGUILayout.EnumPopup((NAV_STATE)gController.Body.Navigation);
                EditorGUILayout.EndHorizontal();
                gController.Body.IKEnabled = (bool)EditorGUILayout.Toggle(label_IKEnabled, (bool)gController.Body.IKEnabled);
                gController.Body.UseAnimatorController = (bool)EditorGUILayout.Toggle(label_AnimatorEnabled, (bool)gController.Body.UseAnimatorController);
                gController.Body.UseCurves = (bool)EditorGUILayout.Toggle(label_UseAnimCurves, (bool)gController.Body.UseCurves);
            }

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(gController, "Parameter Changed");
                EditorUtility.SetDirty(gController);
            }
        }

        private void OnSceneGUI() {
            if(gController != null) {
                if(gShowPerception) {
                    Transform t = gController.Perception.PerceptionField.transform;
            
                    /* Draw View Angle */
                    float angleSplit = gController.Perception.ViewAngle / 2;
                    Debug.DrawRay(t.position,
                        Quaternion.AngleAxis(angleSplit, Vector3.up) * t.rotation * Vector3.forward * gController.Perception.PerceptionRadius, Color.red);
                    Debug.DrawRay(t.position, 
                        Quaternion.AngleAxis((-1) * angleSplit, Vector3.up) * t.rotation * Vector3.forward * gController.Perception.PerceptionRadius, Color.red);
                }
            }
        }

    }

}
