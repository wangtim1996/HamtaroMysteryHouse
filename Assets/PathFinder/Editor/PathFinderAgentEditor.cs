using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace K_PathFinder {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PathFinderAgent))]
    public class PathFinderAgentEditor : Editor {
        GUILayoutOption[] guiLayoutForNiceLine = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) };

        Rect lastPropertiesRect;
        static Color niceGreen = new Color(0f, 1f, 0f, 0.3f);
        static Color niceYellow = new Color(1f, 1f, 0f, 0.7f);
        static Color niceRed = Color.red;

        //agent properties
        static string propertiesString = "properties";
        SerializedProperty properties;
        static GUIContent agentPropertiesWarningRed = new GUIContent("Agent Properties", "Add agent properties to this agent in order it to works. You can create properties in Window/K-PathFinder/Create Agent Properties");
        static GUIContent agentPropertiesWarningYellow = new GUIContent("Agent Properties", "Some of values are null and some are not. Dont forget to add properties to agents who dont have it");
        static GUIContent agentPropertiesDescription = new GUIContent("Agent Properties", "Properties file that describe your agent. Serve as dictionary key to return navmes or as instruction to create new one");

        //updateNavmeshPosition
        SerializedProperty updateNavmeshPosition;
        static string updateNavmeshPositionString = "updateNavmeshPosition";
        static GUIContent updateNavmeshPositionContent = new GUIContent("Update Navmesh Position", "If true then agent will recieve nearest point on navmesh. Automaticaly true if velocity obstacle enabled");

        //updateNeighbourAgents
        SerializedProperty updateNeighbourAgents;
        static string updateNeighbourAgentsString = "updateNeighbourAgents";  
        static GUIContent updateNeighbourAgentsContent = new GUIContent("Update Neighbour Agents", "If true then agent will update list of nearby agents. If false it will only stop search for itself. But will NOT be excluded from search for other agents. Automaticaly true if velocity obstacle enabled");

        //maxNeighbors
        SerializedProperty maxNeighbors;
        static string maxNeighborsString = "maxNeighbors";
        static GUIContent maxNeighboursContent = new GUIContent("Max Neighbours", "Maximum neighbours in neighbour list. This value are used to tell which agent should be evaded if veocity obstacle is enabled");

        //maxNeighbourDistance
        SerializedProperty maxNeighbourDistance;
        static string maxNeighbourDistanceString = "maxNeighbourDistance";  
        static GUIContent maxNeighbourDistanceContent = new GUIContent("Max Neighbour Distance", "Maximum distance to agent to consider it as neighbour");

        //velocityObstacle
        SerializedProperty velocityObstacle;
        static string velocityObstacleString = "velocityObstacle";
        static GUIContent velocityObstacleContent = new GUIContent("Dynamic Obstacle Agent", "If true then this object marked as siutable for dynamic avoidance");

        //maxAgentVelocity
        SerializedProperty maxAgentVelocity;
        static string maxAgentVelocityString = "maxAgentVelocity";
        static GUIContent maxAgentVelocityContent = new GUIContent("Max Velocity", "Agent maximum velocity in units per second. This will be considered as limitation to change velocity");

        //avoidanceResponsibility
        SerializedProperty avoidanceResponsibility;
        static string avoidanceResponsibilityString = "avoidanceResponsibility";
        static GUIContent avoidanceResponsibilityContent = new GUIContent("Avoidance Responsibility", "How likely this agent will avoid other agents.\nFormula is: this value / (this value + other agent value)");

        //careDistance
        SerializedProperty careDistance;
        static string careDistanceString = "careDistance";
        static GUIContent careDistanceContent = new GUIContent("Care Distance", "In range 0.01f-0.99f. How far other agents will react to this one in order to evade it. Lower - sooner");

        //useDeadLockFailsafe
        SerializedProperty useDeadLockFailsafe;
        static string useDeadLockFailsafeString = "useDeadLockFailsafe";
        static GUIContent useDeadLockFailsafeContent = new GUIContent("Deadlock Fail-Safe", "Agents actualy can lock each others. If this option enabled they will try to move in oposite direction for some time if they stuck");

        //deadLockVelocityThreshold
        SerializedProperty deadLockVelocityThreshold;
        static string deadLockVelocityThresholdString = "deadLockVelocityThreshold";
        static GUIContent deadLockVelocityThresholdContent = new GUIContent("DL Velocity Threshold", "Agent Velocity considered as being stuck");

        //deadLockFailsafeVelocity
        SerializedProperty deadLockFailsafeVelocity;
        static string deadLockFailsafeVelocityString = "deadLockFailsafeVelocity";
        static GUIContent deadLockFailsafeVelocityContent = new GUIContent("DL Failsafe Velocity", "If failsafe triggered this will be target velocity to move out from other obstacles");

        //deadLockFailsafeTime
        SerializedProperty deadLockFailsafeTime;
        static string deadLockFailsafeTimeString = "deadLockFailsafeTime";
        static GUIContent deadLockFailsafeTimeContent = new GUIContent("DL Failsafe Time", "If Failsafe Triggered this will be time how long failsafe remain active");

        //preferOneSideEvasion
        SerializedProperty preferOneSideEvasion;
        static string preferOneSideEvasionString = "preferOneSideEvasion";
        static GUIContent preferOneSideEvasionContent = new GUIContent("Prefer One Side", "If True then agent will prefer right side to evade. Evaded agent will be slightly bigger and moved to the left. Also in case if agent still deside to go left it's responcibility will be higher");

        //preferOneSideEvasionOffset
        SerializedProperty preferOneSideEvasionOffset;
        static string preferOneSideEvasionOffsetString = "preferOneSideEvasionOffset";
        static GUIContent preferOneSideEvasionOffsetContent = new GUIContent("Side Offset", "To this value radius of ther agent will be multiplied");



        //SerializedProperty preferOneSideEvasionOffset;
        //static string preferOneSideEvasionOffsetString = "preferOneSideEvasionOffset";
        //static GUIContent preferOneSideEvasionOffsetContent = new GUIContent("Side Offset", "To this value radius of ther agent will be multiplied");





        [SerializeField] bool debugPath;


        void OnEnable() {
            properties = serializedObject.FindProperty(propertiesString);

            updateNavmeshPosition = serializedObject.FindProperty(updateNavmeshPositionString);

            updateNeighbourAgents = serializedObject.FindProperty(updateNeighbourAgentsString);
            maxNeighbors = serializedObject.FindProperty(maxNeighborsString);
            maxNeighbourDistance = serializedObject.FindProperty(maxNeighbourDistanceString);            

            velocityObstacle = serializedObject.FindProperty(velocityObstacleString);
            maxAgentVelocity = serializedObject.FindProperty(maxAgentVelocityString);
            avoidanceResponsibility = serializedObject.FindProperty(avoidanceResponsibilityString);
            careDistance = serializedObject.FindProperty(careDistanceString);

            useDeadLockFailsafe = serializedObject.FindProperty(useDeadLockFailsafeString);
            deadLockVelocityThreshold = serializedObject.FindProperty(deadLockVelocityThresholdString);
            deadLockFailsafeVelocity = serializedObject.FindProperty(deadLockFailsafeVelocityString);
            deadLockFailsafeTime = serializedObject.FindProperty(deadLockFailsafeTimeString);

            preferOneSideEvasion = serializedObject.FindProperty(preferOneSideEvasionString);
            preferOneSideEvasionOffset = serializedObject.FindProperty(preferOneSideEvasionOffsetString);
        }


        public override void OnInspectorGUI() {
            Event e = Event.current;
            serializedObject.Update();
            EditorGUI.showMixedValue = true;

            Color guiColor = GUI.color;
            if (properties.hasMultipleDifferentValues)
                GUI.color = niceYellow;
            else if (properties.objectReferenceValue != null)
                GUI.color = niceGreen;
            else
                GUI.color = niceRed;

            GUI.Box(lastPropertiesRect, string.Empty);
            GUI.color = guiColor;

            if (properties.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(properties, agentPropertiesWarningYellow);
            else if (properties.objectReferenceValue != null)
                EditorGUILayout.PropertyField(properties, agentPropertiesDescription);
            else
                EditorGUILayout.PropertyField(properties, agentPropertiesWarningRed);

            if (e.type == EventType.Repaint) {
                lastPropertiesRect = GUILayoutUtility.GetLastRect();
            }  

            if (properties.objectReferenceValue != null) {    
                EditorGUILayout.PropertyField(updateNavmeshPosition, updateNavmeshPositionContent);

                //updateNeighbourAgents
                if (updateNeighbourAgents.boolValue)
                    GUILayout.Box(string.Empty, guiLayoutForNiceLine);
                EditorGUILayout.PropertyField(updateNeighbourAgents, updateNeighbourAgentsContent);
                if (updateNeighbourAgents.boolValue) {
                    EditorGUILayout.PropertyField(maxNeighbors, maxNeighboursContent);
                    EditorGUILayout.PropertyField(maxNeighbourDistance, maxNeighbourDistanceContent);
                }
                if (updateNeighbourAgents.boolValue)
                    GUILayout.Box(string.Empty, guiLayoutForNiceLine);

                EditorGUILayout.PropertyField(velocityObstacle, velocityObstacleContent);
                if (velocityObstacle.boolValue) {
                    updateNavmeshPosition.boolValue = true;
                    updateNeighbourAgents.boolValue = true;

                    EditorGUILayout.PropertyField(maxAgentVelocity, maxAgentVelocityContent);
                    EditorGUILayout.PropertyField(avoidanceResponsibility, avoidanceResponsibilityContent);
                    EditorGUILayout.PropertyField(careDistance, careDistanceContent);

                    EditorGUILayout.PropertyField(useDeadLockFailsafe, useDeadLockFailsafeContent);
                    if (useDeadLockFailsafe.boolValue) {
                        EditorGUILayout.PropertyField(deadLockVelocityThreshold, deadLockVelocityThresholdContent);
                        EditorGUILayout.PropertyField(deadLockFailsafeVelocity, deadLockFailsafeVelocityContent);
                        EditorGUILayout.PropertyField(deadLockFailsafeTime, deadLockFailsafeTimeContent);
                    }

                    EditorGUILayout.PropertyField(preferOneSideEvasion, preferOneSideEvasionContent);
                    if (preferOneSideEvasion.boolValue) {
                        EditorGUILayout.PropertyField(preferOneSideEvasionOffset, preferOneSideEvasionOffsetContent);
                        if (preferOneSideEvasionOffset.floatValue < 0)
                            preferOneSideEvasionOffset.floatValue = 0f;
                    }
                }
            }

        
            UITools.Line();

            debugPath = EditorGUILayout.Toggle("Debug Path", debugPath);
            if (debugPath) {
                PathFinderAgent agent = (PathFinderAgent)target;
                if(agent != null && agent.path != null) {
                    lock (agent.path) {
                        for (int i = 0; i < agent.path.pathNodes.Count - 1; i++) {
                            Debug.DrawLine(agent.path.pathNodes[i], agent.path.pathNodes[i + 1], Color.red);
                        }
                    }
                }
            }


            EditorGUI.showMixedValue = false;
            serializedObject.ApplyModifiedProperties();

        }
    }
}