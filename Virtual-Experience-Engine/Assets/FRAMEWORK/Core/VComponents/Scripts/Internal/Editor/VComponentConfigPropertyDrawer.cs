using UnityEditor;
using UnityEngine;
using Toolbox.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Toolbox.Editor.Drawers;

namespace VE2.Core.VComponents.Internal
{
    public abstract class OrderedTargetTypeDrawer : ToolboxTargetTypeDrawer
    {
        public OrderedTargetTypeDrawer() { }

        public override void OnGui(SerializedProperty property, GUIContent label)
        {
            //Debug.Log($"Hello???");

            if (!property.isExpanded)
            {
                EditorGUILayout.PropertyField(property);
                //Debug.Log($"Property not expanded");
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                var child = property.Copy();
                var depth = property.depth;

                if (!child.NextVisible(true))
                {
                    //Debug.Log($"No child properties found at depth {depth + 1} for property {property.propertyPath}");
                    return;
                }

                var orderedProperties = new List<OrderedProperty>();

                do
                {
                    if (child.depth != depth + 1)
                        break;

                    int order = GetPropertyOrder(child, property.serializedObject.targetObject);
                    //Debug.Log($"Property: {child.name}, Order: {order}");
                    orderedProperties.Add(new OrderedProperty(child.Copy(), order));
                }
                while (child.NextVisible(false));

                foreach (var ordered in orderedProperties.OrderBy(p => p.Order))
                {
                    ToolboxEditorGui.DrawToolboxProperty(ordered.Property);
                }
            }
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

        //These are Toolbox's way of implementing [CustomPropertyDrawer(typeof(object), true)]
        // public override System.Type GetTargetType() => throw new System.NotImplementedException();
        public override System.Type GetTargetType() => TargetType;
        public override bool UseForChildren() => true;

        protected abstract System.Type TargetType { get; }

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

    #region Property Drawer Implementations
    //NOTE, each of these drawers must be added to the ToolboxEditorSettings' TargetTypeDrawers list

    public class RangedAdjustableInteractionConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(RangedAdjustableInteractionConfig);
    }

    public class RangedFreeGrabInteractionConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(RangedFreeGrabInteractionConfig);
    }

    public class RangedClickInteractionConfigDrawer : OrderedTargetTypeDrawer
    {
        protected override System.Type TargetType => typeof(RangedClickInteractionConfig);
    }
    #endregion
}
