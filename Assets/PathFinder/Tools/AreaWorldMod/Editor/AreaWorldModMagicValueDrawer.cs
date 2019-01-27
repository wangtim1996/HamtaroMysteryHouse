using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace K_PathFinder {
    [CustomPropertyDrawer(typeof(AreaWorldModMagicValue))]
    public class AreaWorldModMagicValueEditor : PropertyDrawer {
        //static string filterString = "filter";

        SerializedProperty nameP;
        static string namePString = "name";
        static GUIContent namePContentSphere = new GUIContent("Sphere Name", "");
        static GUIContent namePContentCapsule = new GUIContent("Capsule Name", "");
        static GUIContent namePContentCuboid = new GUIContent("Cuboid Name", "");

        SerializedProperty elementType;
        static string elementTypeString = "myType";

        SerializedProperty expanded;
        static string expandedString = "expanded";

        SerializedProperty pos;
        static string posString = "position";
        static GUIContent posContent = new GUIContent("Position", "");

        SerializedProperty rotationProperty;
        static string rotationPropertyString = "rotation";
        static GUIContent rotationPropertyContent = new GUIContent("Rotation", "");

        SerializedProperty value1;
        static string value1String = "value1";
        SerializedProperty value2;
        static string value2String = "value2";
        SerializedProperty value3;
        static string value3String = "value3";

        static GUIContent radiusContent = new GUIContent("Radius", "");
        static GUIContent heightContent = new GUIContent("Height", "");
        static GUIContent sizeContent = new GUIContent("Size", "");


    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float _propertyHeight = 0;
            SerializedProperty prop;
            prop = property.FindPropertyRelative(namePString);
            _propertyHeight += EditorGUI.GetPropertyHeight(prop);

            //prop = property.FindPropertyRelative(modeString);
            //_propertyHeight += EditorGUI.GetPropertyHeight(prop);

            prop = property.FindPropertyRelative(expandedString);
            //_propertyHeight += EditorGUI.GetPropertyHeight(prop);

            if (prop.boolValue) {
                prop = property.FindPropertyRelative(posString);
                float V3height = EditorGUI.GetPropertyHeight(prop);
                _propertyHeight += V3height;
                _propertyHeight += V3height;

                prop = property.FindPropertyRelative(elementTypeString);

                float floatHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative(value1String));
                switch (prop.enumValueIndex) {
                    case 0://sphere
                        _propertyHeight += floatHeight;
                        break;
                    case 1://capsule
                        _propertyHeight += floatHeight;
                        _propertyHeight += floatHeight;
                        break;
                    case 2://cuboid
                        _propertyHeight += V3height;
                        break;
                }
            }

            return _propertyHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            float _height = 0;
            float curHeight;
            
            EditorGUI.BeginProperty(position, label, property);


            elementType = property.FindPropertyRelative(elementTypeString);

            nameP = property.FindPropertyRelative(namePString);
            GUIContent nameContent;

            switch (elementType.enumValueIndex) {
                case 0: //sphere
                    nameContent = namePContentSphere;
                    break;
                case 1: //capsule
                    nameContent = namePContentCapsule;
                    break;
                case 2: //cuboid
                    nameContent = namePContentCuboid;
                    break;
                default:
                    nameContent = new GUIContent("no name error this thing is on fire");
                    break;
            }

            curHeight = EditorGUI.GetPropertyHeight(nameP, nameContent);
            EditorGUI.PropertyField(new Rect(position.x, position.y + _height, position.width, curHeight), nameP, nameContent);
            _height += curHeight;

            //mode = property.FindPropertyRelative(modeString);
            //curHeight = EditorGUI.GetPropertyHeight(mode, modeContent);
            //EditorGUI.PropertyField(new Rect(position.x, position.y + _height, position.width, curHeight), mode, modeContent);
            //_height += curHeight;


            var expandedProperty = property.FindPropertyRelative(expandedString);
            EditorGUI.BeginChangeCheck();
            bool expanded = EditorGUI.Foldout(new Rect(position.x, position.y + _height - curHeight, position.width, curHeight), expandedProperty.boolValue, string.Empty);
            if (EditorGUI.EndChangeCheck()) {
                expandedProperty.boolValue = expanded;
            }
            //_height += curHeight;


            if (expandedProperty.boolValue) {
                pos = property.FindPropertyRelative(posString);
                curHeight = EditorGUI.GetPropertyHeight(pos, posContent);
                EditorGUI.PropertyField(new Rect(position.x, position.y + _height, position.width, curHeight), pos, posContent);
                _height += curHeight;

                rotationProperty = property.FindPropertyRelative(rotationPropertyString);
                EditorGUI.BeginChangeCheck();
                Quaternion rotValue = Quaternion.Euler(
                    EditorGUI.Vector3Field(
                        new Rect(position.x, position.y + _height, position.width, curHeight), 
                        rotationPropertyContent, 
                        rotationProperty.quaternionValue.eulerAngles));

                if (EditorGUI.EndChangeCheck()) 
                    rotationProperty.quaternionValue = rotValue;
                
                _height += curHeight;

                value1 = property.FindPropertyRelative(value1String);
                value2 = property.FindPropertyRelative(value2String);
                value3 = property.FindPropertyRelative(value3String);

                switch (elementType.enumValueIndex) {
                    case 0://sphere
                        value1 = property.FindPropertyRelative(value1String);
                        curHeight = EditorGUI.GetPropertyHeight(value1, radiusContent);
                        EditorGUI.PropertyField(new Rect(position.x, position.y + _height, position.width, curHeight), value1, radiusContent);
                        _height += curHeight;
                        break;
                    case 1://capsule
                        value1 = property.FindPropertyRelative(value1String);
                        value2 = property.FindPropertyRelative(value2String);

                        curHeight = EditorGUI.GetPropertyHeight(value1, radiusContent);
                        EditorGUI.PropertyField(new Rect(position.x, position.y + _height, position.width, curHeight), value1, radiusContent);
                        _height += curHeight;

                        curHeight = EditorGUI.GetPropertyHeight(value2, radiusContent);
                        EditorGUI.PropertyField(new Rect(position.x, position.y + _height, position.width, curHeight), value2, heightContent);
                        _height += curHeight;
                        break;
                    case 2://cuboid
                        value1 = property.FindPropertyRelative(value1String);
                        value2 = property.FindPropertyRelative(value2String);
                        value3 = property.FindPropertyRelative(value3String);

                        EditorGUI.BeginChangeCheck();
                        Vector3 sizeValue = EditorGUI.Vector3Field(new Rect(position.x, position.y + _height, position.width, curHeight), sizeContent, new Vector3(value1.floatValue, value2.floatValue, value3.floatValue));
                        if (EditorGUI.EndChangeCheck()) {
                            value1.floatValue = sizeValue.x;
                            value2.floatValue = sizeValue.y;
                            value3.floatValue = sizeValue.z;
                        }
                        break;
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
