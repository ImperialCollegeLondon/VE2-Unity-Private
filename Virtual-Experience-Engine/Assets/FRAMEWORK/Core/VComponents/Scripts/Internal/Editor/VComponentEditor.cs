// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using Toolbox.Editor;
// using UnityEditor;

// namespace VE2.Core.VComponents.Internal
// {
//     [CustomEditor(typeof(BaseVComponent), true)]
//     [CanEditMultipleObjects]
//     public class myBaseClassEditor : ToolboxEditor
//     {
//         /// <summary>
//         /// This is a HashSet is just used to get a unique list of SerializedProperties.
//         /// </summary>
//         private readonly HashSet<SerializedProperty> _allPropsHashSet = new();
//         /// <summary>
//         /// List is used in lieu of HashSet to preserve order.
//         /// </summary>
//         private readonly List<SerializedProperty> _allPropsList = new();
//         private BaseVComponent baseClassTarget;
//         private SerializedObject baseClassSO;
//         private readonly Type baseClassType = typeof(BaseVComponent);

//         protected void OnEnable()
//         {
//             baseClassTarget = (BaseVComponent)target;
//             baseClassSO = new SerializedObject(baseClassTarget);
//             //Note: This will iterate through ALL props. Baseclass and your child subclass.
//             // Don't ask me why, ask Unity.
//             // todo: be wary of unity updates that may break this
//             SerializedProperty prop = baseClassSO.GetIterator();
//             if (prop.NextVisible(true))
//             {
//                 do
//                 {
//                     var propCopy = prop.Copy();
//                     if (_allPropsHashSet.Add(propCopy))
//                         _allPropsList.Add(propCopy);
//                 }
//                 while (prop.NextVisible(false));
//             }
//         }

//         public override void DrawCustomInspector()
//         {
//             base.DrawCustomInspector();

//             List<string> baseClassPropNames = new();
//             List<string> subClassPropNames = new();

//             //Reflection magic. Will also retrieve ALL INHERITED members.
//             MemberInfo[] allMembers = baseClassTarget.GetType().GetMembers();

//             foreach (MemberInfo memberInfo in allMembers)
//             {
//                 if (IsPubliclyGettableHelper(memberInfo) && memberInfo.MemberType == MemberTypes.Field)
//                 {
//                     if (memberInfo.DeclaringType == baseClassType)
//                         baseClassPropNames.Add(memberInfo.Name);
//                     else
//                         subClassPropNames.Add(memberInfo.Name);
//                 }
//             }

//             baseClassPropNames.Add("m_Script");
//             subClassPropNames.Add("m_Script");
//             var excludeBaseProps = baseClassPropNames.ToArray();
//             var excludeSubClassProps = subClassPropNames.ToArray();

//             // Draw remaining properties
//             // Draw subclassmembers _allPropsHashSet
//             DrawPropertiesExcluding(baseClassSO, excludeBaseProps);
//             DrawPropertiesExcluding(baseClassSO, excludeSubClassProps);

//             serializedObject.ApplyModifiedProperties();
//         }

//         public bool IsPubliclyGettableHelper(MemberInfo memberInfo)
//         {
//             if (memberInfo is FieldInfo fieldInfo)
//             {
//                 return fieldInfo.IsPublic;
//             }
//             else if (memberInfo is PropertyInfo propertyInfo)
//             {
//                 return propertyInfo.CanRead && propertyInfo.GetGetMethod(false) != null;
//             }
//             else if (memberInfo is MethodInfo methodInfo)
//             {
//                 return methodInfo.IsPublic;
//             }
//             else if (memberInfo is ConstructorInfo constructorInfo)
//             {
//                 return constructorInfo.IsPublic;
//             }
//             else
//             {
//                 // Unknown member type
//                 return false;
//             }
//         }
//     }
// }