using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RPGame.Core.Effects;
using RPGame.Core.Spells;

namespace RPGame.Progression.Editor
{
    [CustomPropertyDrawer(typeof(IRuntimeSpellBehavior), true)]
    public sealed class RuntimeSpellBehaviorDrawer : PropertyDrawer
    {
        private static readonly IReadOnlyList<Type> ConcreteTypes = GetConcreteTypes();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue == null)
            {
                return height;
            }

            foreach (SerializedProperty child in GetDirectChildren(property))
            {
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string[] options = new string[ConcreteTypes.Count + 1];
            options[0] = "None";
            int selectedIndex = 0;
            for (int i = 0; i < ConcreteTypes.Count; i++)
            {
                options[i + 1] = ConcreteTypes[i].Name;
                if (property.managedReferenceValue != null
                    && property.managedReferenceValue.GetType() == ConcreteTypes[i])
                {
                    selectedIndex = i + 1;
                }
            }

            Rect selectorRect = new(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);
            int newIndex = EditorGUI.Popup(selectorRect, label.text, selectedIndex, options);
            if (newIndex != selectedIndex)
            {
                property.managedReferenceValue = newIndex == 0
                    ? null
                    : Activator.CreateInstance(ConcreteTypes[newIndex - 1]);
            }

            if (property.managedReferenceValue != null)
            {
                float y = selectorRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                foreach (SerializedProperty child in GetDirectChildren(property))
                {
                    float childHeight = EditorGUI.GetPropertyHeight(child, true);
                    Rect childRect = new(position.x, y, position.width, childHeight);
                    EditorGUI.PropertyField(childRect, child, true);
                    y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            EditorGUI.EndProperty();
        }

        private static IEnumerable<SerializedProperty> GetDirectChildren(SerializedProperty property)
        {
            SerializedProperty current = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enterChildren = true;
            while (current.NextVisible(enterChildren)
                && !SerializedProperty.EqualContents(current, end))
            {
                if (current.depth == property.depth + 1)
                {
                    yield return current.Copy();
                }

                enterChildren = false;
            }
        }

        private static IReadOnlyList<Type> GetConcreteTypes()
        {
            List<Type> types = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IRuntimeSpellBehavior>())
            {
                if (type.IsSerializable
                    && !type.IsAbstract
                    && !type.IsGenericType
                    && (type.IsPublic || type.IsNestedPublic)
                    && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    types.Add(type);
                }
            }

            types.Sort((first, second) => string.CompareOrdinal(first.Name, second.Name));
            return types;
        }
    }
}
