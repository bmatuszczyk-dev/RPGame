using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RPGame.Core.Spells
{
    public static class RuntimeSpellBehaviorCloner
    {
        public static IRuntimeSpellBehavior Clone(IRuntimeSpellBehavior template)
        {
            if (template == null)
            {
                return null;
            }

            Dictionary<object, object> clones = new(new ReferenceEqualityComparer());
            return (IRuntimeSpellBehavior)CloneValue(template, clones);
        }

        private static object CloneValue(
            object value,
            Dictionary<object, object> clones)
        {
            if (value == null)
            {
                return null;
            }

            Type valueType = value.GetType();
            if (IsSimpleValue(valueType)
                || typeof(UnityEngine.Object).IsAssignableFrom(valueType))
            {
                return value;
            }

            if (clones.TryGetValue(value, out object existingClone))
            {
                return existingClone;
            }

            if (valueType.IsArray)
            {
                return CloneArray((Array)value, clones);
            }

            if (IsList(valueType))
            {
                return CloneList((IList)value, valueType, clones);
            }

            if (valueType.IsValueType)
            {
                object valueClone = value;
                CopySerializedFields(value, valueClone, valueType, clones);
                return valueClone;
            }

            object managedClone = CreateManagedInstance(valueType);
            clones.Add(value, managedClone);
            CopySerializedFields(value, managedClone, valueType, clones);
            return managedClone;
        }

        private static void CopySerializedFields(
            object source,
            object target,
            Type type,
            Dictionary<object, object> clones)
        {
            if (type.BaseType != null)
            {
                CopySerializedFields(source, target, type.BaseType, clones);
            }

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly);

            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                FieldInfo field = fields[fieldIndex];
                if (!IsSerializedField(field))
                {
                    continue;
                }

                object fieldValue = field.GetValue(source);
                object clonedValue = CloneValue(fieldValue, clones);
                field.SetValue(target, clonedValue);
            }
        }

        private static Array CloneArray(
            Array sourceArray,
            Dictionary<object, object> clones)
        {
            if (sourceArray.Rank != 1)
            {
                throw new InvalidOperationException(
                    $"Cannot clone multidimensional array type '{sourceArray.GetType().FullName}'.");
            }

            Type arrayType = sourceArray.GetType();
            Type elementType = arrayType.GetElementType();
            Array clonedArray = Array.CreateInstance(elementType, sourceArray.Length);
            clones.Add(sourceArray, clonedArray);

            for (int i = 0; i < sourceArray.Length; i++)
            {
                object element = sourceArray.GetValue(i);
                clonedArray.SetValue(CloneValue(element, clones), i);
            }

            return clonedArray;
        }

        private static object CloneList(
            IList sourceList,
            Type listType,
            Dictionary<object, object> clones)
        {
            IList clonedList = (IList)CreateManagedInstance(listType);
            clones.Add(sourceList, clonedList);

            for (int i = 0; i < sourceList.Count; i++)
            {
                clonedList.Add(CloneValue(sourceList[i], clones));
            }

            return clonedList;
        }

        private static object CreateManagedInstance(Type type)
        {
            if (!HasDefaultConstructor(type))
            {
                throw new InvalidOperationException(
                    $"Cannot clone managed reference type '{type.FullName}' because it has no default constructor.");
            }

            return Activator.CreateInstance(type, true);
        }

        private static bool IsSimpleValue(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal);
        }

        private static bool IsList(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructor(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null) != null;
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic
                || field.IsInitOnly
                || Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
            {
                return false;
            }

            return field.IsPublic
                || Attribute.IsDefined(field, typeof(SerializeField))
                || Attribute.IsDefined(field, typeof(SerializeReference));
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
