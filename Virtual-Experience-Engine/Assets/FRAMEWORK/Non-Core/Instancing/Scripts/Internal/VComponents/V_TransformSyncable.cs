#if UNITY_EDITOR
using UnityEditor;
using Toolbox.Editor;
#endif

using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal partial class V_TransformSyncable : IV_transformSyncable
    {
        //No plugin-facing interfaces here (yet)
    }

    internal partial class V_TransformSyncable : MonoBehaviour
    {
        [SerializeField, HideLabel, IgnoreParent] private TransformSyncableStateConfig _config = new();

        #region Plugin Interfaces
        ITransformSyncableStateModule _StateModule => _Service.StateModule;
        #endregion

        private TransformSyncableService _service = null;
        private TransformSyncableService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "TS-" + gameObject.name;
            ITransformWrapper transformWrapper = new TransformWrapper(transform);

            if (VE2API.InstanceService == null)
            {
                Debug.LogError("Instance service is null, cannot initialise TransformSyncable, please add a V_InstanceIntegration component to the scene.");
                return;
            }

            _service = new TransformSyncableService(_config, id, VE2API.WorldStateSyncableContainer, VE2API.InstanceService as IInstanceServiceInternal, transformWrapper);
        }

        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate();
        }

        private void Onisable()
        {
            _service?.TearDown();
            _service = null;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(V_TransformSyncable), true)]
    internal class V_TransformSyncableEditor : ToolboxEditor
    {
        public override void DrawCustomInspector()
        {
            V_TransformSyncable script = (V_TransformSyncable)target;

            if (script.GetComponent<Rigidbody>() != null)
            {
                // Only show the help box if there is a Rigidbody component
                EditorGUILayout.HelpBox("This GameObject has a Rigidbody component. Consider using V_RigidbodySyncable instead for better performance and functionality.", MessageType.Warning);
            }

            base.DrawCustomInspector();
        }
    }
#endif
}
