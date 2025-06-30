using System;
using UnityEngine;

namespace VE2.Common.Shared
{
    /// <summary>
    /// Attribute to specify the order of properties in the inspector.
    /// </summary>
    /// <remarks>
    /// This attribute is used to control the order in which properties are displayed in the Unity Inspector.
    /// The lower the number, the higher the priority.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    internal class PropertyOrderAttribute : ToolboxAttribute
    {
        public int Order { get; }

        public PropertyOrderAttribute(int order)
        {
            Order = order;
        }
    }
}
