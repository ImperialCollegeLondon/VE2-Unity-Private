using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal interface IWorldStateSyncableContainer
    {
        Dictionary<string, IWorldStateModule> WorldStateSyncables { get; }
        event Action<IWorldStateModule> OnWorldStateSyncableRegistered;
        event Action<IWorldStateModule> OnWorldStateSyncableDeregistered;
        void RegisterWorldStateSyncable(IWorldStateModule worldStateSyncable);
        void DeregisterWorldStateSyncable(IWorldStateModule worldStateSyncable);
    }

    internal class WorldStateSyncableContainer : IWorldStateSyncableContainer
    {
        public Dictionary<string, IWorldStateModule> WorldStateSyncables { get; } = new();
        public event Action<IWorldStateModule> OnWorldStateSyncableRegistered;
        public event Action<IWorldStateModule> OnWorldStateSyncableDeregistered;

        private bool _dontShowRegErrors = false;
        private List<string> _clashedIDs = new();
        private bool _waitingToShowErrors = false;

        public void RegisterWorldStateSyncable(IWorldStateModule worldStateSyncable)
        {
            if (WorldStateSyncables.ContainsKey(worldStateSyncable.ID))
            {
                Debug.LogError($"WorldStateSyncable with ID {worldStateSyncable.ID} is already registered.");
                _clashedIDs.Add(worldStateSyncable.ID);

#if UNITY_EDITOR
                if (!_dontShowRegErrors && !_waitingToShowErrors)
                {
                    _waitingToShowErrors = true;

                    //Delay so we have time to catch all syncables trying to register
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        _waitingToShowErrors = false;

                        // Display the dialog and capture the button pressed
                        _dontShowRegErrors = UnityEditor.EditorUtility.DisplayDialog(
                            "VE2 Syncable Registration Error",
                            $"Multiple networked VE2 components have the same gameobject name, this is not allowed\n\n" +
                            $"Clashing IDs: \n -{string.Join("\n -", _clashedIDs)}\n\n" +
                            "Please ensure that all WorldStateSyncables have unique IDs.",
                            "Don't show again",
                            "Keep reminding me");

                        _clashedIDs.Clear();
                    });
                }
#endif

                return;
            }

            WorldStateSyncables.Add(worldStateSyncable.ID, worldStateSyncable);
            OnWorldStateSyncableRegistered?.Invoke(worldStateSyncable);
        }

        public void DeregisterWorldStateSyncable(IWorldStateModule worldStateSyncable)
        {
            WorldStateSyncables.Remove(worldStateSyncable.ID);
            OnWorldStateSyncableDeregistered?.Invoke(null);
        }
    }
}
