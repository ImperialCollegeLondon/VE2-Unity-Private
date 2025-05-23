// using UnityEditor;
// using UnityEngine;
// using Toolbox.Editor;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using Toolbox.Editor.Drawers;

// namespace VE2.Core.VComponents.Internal
// {
//     public class OrderedTargetTypeDrawer : ToolboxTargetTypeDrawer
//     {
//         public OrderedTargetTypeDrawer()
//         {
//             Debug.Log($"OrderedTargetTypeDrawer constructor called");
//         }

//         public override void OnGui(SerializedProperty property, GUIContent label)
//         {
//             Debug.Log($"Hello???");

//             if (!property.isExpanded)
//             {
//                 EditorGUILayout.PropertyField(property);
//                 Debug.Log($"Returning");
//                 return;
//             }

//             using (new EditorGUI.IndentLevelScope())
//             {
//                 var child = property.Copy();
//                 var depth = property.depth;

//                 if (!child.NextVisible(true))
//                     return;

//                 var orderedProperties = new List<OrderedProperty>();

//                 do
//                 {
//                     if (child.depth != depth + 1)
//                         break;

//                     int order = GetPropertyOrder(child, property.serializedObject.targetObject);
//                     Debug.Log($"Property: {child.name}, Order: {order}");
//                     orderedProperties.Add(new OrderedProperty(child.Copy(), order));
//                 }
//                 while (child.NextVisible(false));

//                 foreach (var ordered in orderedProperties.OrderBy(p => p.Order))
//                 {
//                     ToolboxEditorGui.DrawToolboxProperty(ordered.Property);
//                 }
//             }
//         }

//         private int GetPropertyOrder(SerializedProperty property, Object rootObject)
//         {
//             var fieldInfo = GetFieldInfoFromPropertyPath(rootObject, property.propertyPath);
//             return fieldInfo?.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0;
//         }

//         private FieldInfo GetFieldInfoFromPropertyPath(object root, string propertyPath)
//         {
//             var type = root.GetType();
//             object currentObject = root;

//             var elements = propertyPath.Split('.');
//             FieldInfo field = null;

//             foreach (var element in elements)
//             {
//                 if (element == "Array")
//                     continue;

//                 field = type.GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
//                 if (field == null)
//                     return null;

//                 currentObject = field.GetValue(currentObject);
//                 if (currentObject == null)
//                     return field;

//                 type = currentObject.GetType();
//             }

//             return field;
//         }

//         public override System.Type GetTargetType()
//         {
//             return typeof(object);
//         }

//         public override bool UseForChildren()
//         {
//             return true;
//         }

//         private struct OrderedProperty
//         {
//             public SerializedProperty Property;
//             public int Order;

//             public OrderedProperty(SerializedProperty property, int order)
//             {
//                 Property = property;
//                 Order = order;
//             }
//         }
//     }
// }

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VE2.Core.VComponents.Internal
{
    [CustomPropertyDrawer(typeof(BaseConfig), true)] // replace with actual serialized class type
    public class OrderedPropertyDrawer : PropertyDrawer
    {
        // Cache for reordered properties to avoid rebuilding every frame if performance is an issue
        private List<OrderedProperty> _orderedProperties;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.indentLevel++;
            
            var propCopy = property.Copy();
            int depth = property.depth;

            if (!propCopy.NextVisible(true))
            {
                EditorGUI.indentLevel--;
                return;
            }

            _orderedProperties = new List<OrderedProperty>();

            do
            {
                if (propCopy.depth != depth + 1)
                    break;

                int order = GetPropertyOrder(propCopy, property.serializedObject.targetObject);
                _orderedProperties.Add(new OrderedProperty(propCopy.Copy(), order));
            }
            while (propCopy.NextVisible(false));

            float y = position.y;

            foreach (var ordered in _orderedProperties.OrderBy(p => p.Order))
            {
                var propHeight = EditorGUI.GetPropertyHeight(ordered.Property, true);
                var rect = new Rect(position.x, y, position.width, propHeight);

                EditorGUI.PropertyField(rect, ordered.Property, true);
                y += propHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float totalHeight = 0f;

            var propCopy = property.Copy();
            int depth = property.depth;

            if (!propCopy.NextVisible(true))
                return EditorGUIUtility.singleLineHeight;

            var orderedProperties = new List<OrderedProperty>();

            do
            {
                if (propCopy.depth != depth + 1)
                    break;

                int order = GetPropertyOrder(propCopy, property.serializedObject.targetObject);
                orderedProperties.Add(new OrderedProperty(propCopy.Copy(), order));
            }
            while (propCopy.NextVisible(false));

            foreach (var ordered in orderedProperties.OrderBy(p => p.Order))
            {
                totalHeight += EditorGUI.GetPropertyHeight(ordered.Property, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return totalHeight;
        }

        private int GetPropertyOrder(SerializedProperty property, Object rootObject)
        {
            var fieldInfo = GetFieldInfoFromPropertyPath(rootObject, property.propertyPath);
            return fieldInfo?.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0;
        }

        private FieldInfo GetFieldInfoFromPropertyPath(object root, string propertyPath)
        {
            var type = root.GetType();
            object currentObject = root;

            var elements = propertyPath.Split('.');
            FieldInfo field = null;

            foreach (var element in elements)
            {
                if (element == "Array")
                    continue;

                field = type.GetField(element, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                    return null;

                currentObject = field.GetValue(currentObject);
                if (currentObject == null)
                    return field;

                type = currentObject.GetType();
            }

            return field;
        }

        private struct OrderedProperty
        {
            public SerializedProperty Property;
            public int Order;

            public OrderedProperty(SerializedProperty property, int order)
            {
                Property = property;
                Order = order;
            }
        }
    }
}
