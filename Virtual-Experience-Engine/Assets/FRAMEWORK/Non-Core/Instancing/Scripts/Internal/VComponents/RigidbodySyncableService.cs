using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using static VE2.Common.Shared.CommonSerializables;
using Time = UnityEngine.Time;

namespace VE2.NonCore.Instancing.Internal
{
    internal class RigidbodySyncableService
    {
        #region Interfaces
        public IRigidbodySyncableStateModule StateModule => _stateModule;
        private IGrabbableRigidbody _grabbableRigidbody;
        private IRigidbodyWrapper _rigidbody;
        private IInstanceServiceInternal _instanceService;
        private bool _isHost => _instanceService.IsHost;
        #endregion

        #region Modules
        private readonly RigidbodySyncableStateModule _stateModule;
        private readonly RigidbodySyncableStateConfig _config;
        #endregion

        private bool _isKinematicOnStart;
        private List<RigidbodySyncableState> _receivedRigidbodyStates;

        // Baseline difference for interpolation purposes
        private float _timeDifferenceFromHost;

        // Time delay for interpolation, so non host always behind host 
        private readonly float _timeBehind = 0.04f;

        #region Grab-related variables

        // Lag compensation values to make non-host dropping smooth
        private readonly int LAG_COMP_SMOOTHING_FRAMES = 10;
        private readonly float LAG_COMP_EXTRA_TIME = 0.4f;
        private int _hostSmoothingFramesLeft;
        private List<RigidbodySyncableState> _storedHostLagCompensationStates = new();

        // Flags to track state & transition between grabbing / syncing
        private bool _hostNotSendingStates = false;
        private bool _nonHostSimulating = false;

        // Smoothing values for non-host non-dropper when non-host drops
        private readonly float TOTAL_SMOOTHING_TIME_S = 0.4f;
        private float _nonHostSmoothingTimeLeft = 0;

        // Track who is grabbing - currently only using for whether being grabbed or not
        private ushort? _currentGrabberID;

        // Track how many times rbs is grabbed to prevent accidentally using data from before last grab
        private uint _grabCounter = 0;
        #endregion

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, IWorldStateSyncableContainer worldStateSyncableContainer, 
            IInstanceServiceInternal instanceService, IRigidbodyWrapper rigidbodyWrapper, IGrabbableRigidbody grabbableRigidbody)
        {
            _config = config;
            _stateModule = new(state, config, id, worldStateSyncableContainer);
            _instanceService = instanceService;
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;

            if (grabbableRigidbody != null)
            {
                _grabbableRigidbody = grabbableRigidbody;
                _grabbableRigidbody.InternalOnGrab += HandleOnGrab;
                _grabbableRigidbody.InternalOnDrop += HandleOnDrop;
                _grabbableRigidbody.FreeGrabbableHandlesKinematics = false;
            }

            _receivedRigidbodyStates = new();

            _stateModule.OnReceiveState?.AddListener(HandleReceiveRigidbodyState);
            _instanceService.OnBecomeHostInternal += HandleBecomeHost;
            _instanceService.OnBecomeNonHostInternal += HandleBecomeNonHost;
        }

        private void HandleOnGrab(ushort grabberClientID)
        {
            _grabCounter++;
            _currentGrabberID = grabberClientID;

            if (_isHost)
            {
                // Host stops sending states, store kinematic state
                _hostNotSendingStates = true;
                _hostSmoothingFramesLeft = 0;
                _isKinematicOnStart = _rigidbody.isKinematic;
            }
            else
            {
                // Non host starts simulating for themselves, removes old states
                _nonHostSimulating = true;
                _receivedRigidbodyStates.Clear();
            }

            // On grab, rb is not kinematic and FreeGrabbableService handles behaviour
            _rigidbody.isKinematic = false;
        }

        private void HandleOnDrop(ushort grabberClientID)
        {
            _currentGrabberID = null;

            if (_isHost && _instanceService.LocalClientID == grabberClientID)
            {
                // Host who dropped immediately starts sending messages again
                HandleHostSideLagCompensation(_timeBehind/1000f);
                //_hostNotSendingStates = false;
            }
            else if (_instanceService.LocalClientID == grabberClientID)
            {
                // Non host dropper sends state to host
                _stateModule.SetStateFromNonHost(Time.fixedTime, _rigidbody.position, _rigidbody.rotation, _grabCounter, _instanceService.Ping, _rigidbody.linearVelocity, _rigidbody.angularVelocity);
            }
            else if (!_isHost && _instanceService.HostID != grabberClientID)
            {
                // When a non-host drops, all other non-hosts need to do extra smoothing
                _nonHostSmoothingTimeLeft = TOTAL_SMOOTHING_TIME_S;
            }
        }

        private void HandleHostSideLagCompensation(float lagCompensationTime)
        {

            // Make all rigidbodys in the scene, apart from this one, kinematic
            Dictionary<IRigidbodyWrapper, bool> kinematicStates = new();

            foreach (Rigidbody rigidbodyInScene in GameObject.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (_rigidbody.Equals(rigidbodyInScene))
                {
                    continue;
                }

                IRigidbodyWrapper rigidbodyInSceneWrapper = new RigidbodyWrapper(rigidbodyInScene);
                kinematicStates.Add(rigidbodyInSceneWrapper, rigidbodyInSceneWrapper.isKinematic);
                rigidbodyInSceneWrapper.isKinematic = true;
            }

            // Simulate a "lag compensation time" into the future so that the non-host starts receiving states
            // for a smooth drop on their side
            // float lagCompensationTime = ;
            int cyclesToSkipForward = Mathf.CeilToInt(lagCompensationTime / Time.fixedDeltaTime) + 1;

            // Simulate physics into the future in steps
            Physics.simulationMode = SimulationMode.Script;

            for (int i = 0; i < cyclesToSkipForward + LAG_COMP_SMOOTHING_FRAMES; i++)
            {
                Physics.Simulate(UnityEngine.Time.fixedDeltaTime);
                // Keep a record of simulated steps for host smoothing purposes
                _storedHostLagCompensationStates.Add(new(Time.fixedTime, _rigidbody.position, _rigidbody.rotation, _grabCounter, 0, _rigidbody.linearVelocity, _rigidbody.angularVelocity));
            }

            // Return physics and the locked rigidbodies back to normal
            Physics.simulationMode = SimulationMode.FixedUpdate;

            foreach (KeyValuePair<IRigidbodyWrapper, bool> kinematicState in kinematicStates)
                kinematicState.Key.isKinematic = kinematicState.Value;

            // Set a > 0 value for host smoothing frames, which are then handled in FixedUpdate
            _hostSmoothingFramesLeft = LAG_COMP_SMOOTHING_FRAMES;

            // Rigidbody set to kinematic to prep for fixed upate smoothing
            _rigidbody.isKinematic = true;
        }


        public void HandleFixedUpdate()
        {
            _stateModule.HandleFixedUpdate();

            // Hosts send states on FixedUpdate when hostNotSendingStates flag is false
            if (_instanceService.IsConnectedToServer && _isHost && !_hostNotSendingStates)
            {
                if (_config.LogSendReceiveDebugMessages)
                { 
                    Debug.Log($"Setting state fixed time {Time.fixedTime}, position {_rigidbody.position}, rotation {_rigidbody.rotation}, grabID {_grabCounter}");
                }
                _stateModule.SetStateFromHost(Time.fixedTime, _rigidbody.position, _rigidbody.rotation, _grabCounter);
            }

            // If _hostSmoothingFramesLeft > 0, extra processing has to be done for host-side
            if (_instanceService.IsConnectedToServer && _isHost && _hostNotSendingStates && _hostSmoothingFramesLeft > 0)
            {
                // Send state from list instead of current _rigidbody state
                RigidbodySyncableState syncState = _storedHostLagCompensationStates[^_hostSmoothingFramesLeft];
                _stateModule.SetStateFromHost(syncState.FixedTime, syncState.Position, syncState.Rotation, syncState.GrabCounter);

                // Then do host smoothing
                // Figure out which store states to interpolate between, and where in between those states
                float interpolationValueAlongStoredStates = _storedHostLagCompensationStates.Count * (1 - (float)_hostSmoothingFramesLeft / LAG_COMP_SMOOTHING_FRAMES);
                int indexOfStateToInterpolateFrom = (int)(interpolationValueAlongStoredStates);

                float interpValueBetweenStates = interpolationValueAlongStoredStates - (float)indexOfStateToInterpolateFrom;

                (RigidbodySyncableState previousState, RigidbodySyncableState nextState) = GetRelevantRigidbodyStates(_storedHostLagCompensationStates, indexOfStateToInterpolateFrom + 1);

                SetRigidbodyValues(Vector3.Lerp(previousState.Position, nextState.Position, interpValueBetweenStates), Quaternion.Slerp(previousState.Rotation, nextState.Rotation, interpValueBetweenStates));

                if (_config.LogInterpolationDebug)
                    Debug.Log($"Smoothing on host over {_storedHostLagCompensationStates.Count} frames, with {_hostSmoothingFramesLeft} frames left. TotalVal = {interpolationValueAlongStoredStates}, indexFrom = {indexOfStateToInterpolateFrom}, interpVal = {interpValueBetweenStates}");

                _hostSmoothingFramesLeft--;

                if (_hostSmoothingFramesLeft == 0)
                {
                    _hostNotSendingStates = false;
                    _rigidbody.isKinematic = _isKinematicOnStart;

                    RigidbodySyncableState mostRecentState = _storedHostLagCompensationStates[_storedHostLagCompensationStates.Count - 1];
                    SetRigidbodyValuesWithVelocity(mostRecentState);

                    _storedHostLagCompensationStates.Clear();
                }
            }
        }

        public void HandleUpdate()
        {
            // Non host interpolates on Update when not simulating for themselves
            if (_instanceService.IsConnectedToServer && !_isHost && !_nonHostSimulating)
            {
                InterpolateRigidbody();
            }

        }

        public void TearDown()
        {
            _stateModule.OnReceiveState?.RemoveListener(HandleReceiveRigidbodyState);
            _instanceService.OnBecomeHostInternal -= HandleBecomeHost;
            _instanceService.OnBecomeNonHostInternal -= HandleBecomeNonHost;
            _stateModule.TearDown();
        }

        private void HandleBecomeHost()
        {
            _rigidbody.isKinematic = _isKinematicOnStart;

            // No longer non-host, return to default
            _nonHostSimulating = false;
            // If someone is grabbing, don't send states. Otherwise, start sending states
            _hostNotSendingStates = (_currentGrabberID != null);

            // If the change happened while non-dropper non-host smoothing, make sure to reset this value
            _nonHostSmoothingTimeLeft = 0;

            if (_receivedRigidbodyStates.Count >= 2)
            {
                // Calculate RB velocities, which we take to be the most recent we received for now
                (RigidbodySyncableState previous, RigidbodySyncableState next) latestStates = GetRelevantRigidbodyStates(_receivedRigidbodyStates, _receivedRigidbodyStates.Count - 1);
                Vector3 linearVelocity = GetVelocityFromStates(latestStates);
                Vector3 angularVelocity = GetAngularVelocityFromStates(latestStates);

                SetRigidbodyValues(latestStates.next.Position, latestStates.next.Rotation, linearVelocity, angularVelocity);
            }
            else if (_receivedRigidbodyStates.Count == 1)
            {
                // If we have fewer states, we have yet to receive many states and can't assume a velocity
                SetRigidbodyValues(_receivedRigidbodyStates[0].Position, _receivedRigidbodyStates[0].Rotation);
            }

            _receivedRigidbodyStates.Clear();
        }

        private void HandleBecomeNonHost()
        {
            _receivedRigidbodyStates.Clear();
        }

        #region Receive States Logic
        public void HandleReceiveRigidbodyState(RigidbodySyncableState receivedState)
        {
            if (_config.LogSendReceiveDebugMessages)
            {
                Debug.Log($"Received state from host {receivedState.FromHost}, fixed time {receivedState.FixedTime}, position {receivedState.Position}, rotation {receivedState.Rotation}, grabID {_grabCounter}");
            }

            if (_isHost && !receivedState.FromHost)
            {
                // If a host receives a state from a non-host, we'll assume it's a drop state
                _rigidbody.isKinematic = _isKinematicOnStart;
                SetRigidbodyValuesWithVelocity(receivedState);

                // Do the special lag compensation required by the host when the non-host drops, and set kinematic
                HandleHostSideLagCompensation(_timeBehind + (receivedState.LatestRoundTripTime + LAG_COMP_EXTRA_TIME) / 1000f);
                
            } 
            else if (!_isHost && receivedState.FromHost)
            {

                if (receivedState.GrabCounter > _grabCounter)
                {
                    // We are behind grab IDs - likely because we joined the scene after grabs happened - better catch up
                    _grabCounter = receivedState.GrabCounter;
                }

                if (receivedState.GrabCounter != _grabCounter)
                {
                    // Ignore all states from before the latest grab, or from another non host
                    return;
                }

                if (_receivedRigidbodyStates.Count == 1)
                {
                    // Let's try only syncing once we have at least 2 states to interp between
                    _timeDifferenceFromHost = receivedState.FixedTime - Time.fixedTime;
                    _nonHostSimulating = false;
                    _isKinematicOnStart = _rigidbody.isKinematic;
                    _rigidbody.isKinematic = true;

                    if (_config.LogSendReceiveDebugMessages)
                        Debug.Log($"Received first pair of states at time {Time.fixedTime}");
                }

                AddReceivedStateToHistory(new(receivedState.FixedTime, receivedState.Position, receivedState.Rotation, receivedState.GrabCounter));
            }
        }

        private void AddReceivedStateToHistory(RigidbodySyncableState newState)
        {
            if (_config.DrawInterpolationLines)
            {
                // Draw lines for received states to compare to interpolation
                Debug.DrawLine(_rigidbody.position, _rigidbody.position + Vector3.Cross(_rigidbody.linearVelocity, Vector3.up).normalized / 5, Color.green, 20f);
            }

            int index = _receivedRigidbodyStates.FindIndex(rbState => rbState.FixedTime > newState.FixedTime);

            if (index == -1)
            {
                // If no state has a FixedTime > NewFixedTime, add to the end
                _receivedRigidbodyStates.Add(newState);
            }
            else
            {
                // Insert before the first state with FixedTime > NewFixedTime
                _receivedRigidbodyStates.Insert(index, newState);
            }
        }
        #endregion


        #region Interpolation Logic

        private void InterpolateRigidbody()
        {
            int numStates = _receivedRigidbodyStates.Count;

            if (numStates == 1)
            {
                // SetRigidbodyValues(_receivedRigidbodyStates[0]);
            }
            else if (numStates >= 2)
            { 
                // Calculate the time the rigidbody should be displayed at
                float delayedLocalTime = Time.time + _timeDifferenceFromHost - _timeBehind;
                
                // Calculate index of next state
                int index = _receivedRigidbodyStates.FindIndex(rbState => rbState.FixedTime > delayedLocalTime);

                // Get the previous and next states to interpolate betwen
                (RigidbodySyncableState previousState, RigidbodySyncableState nextState) = GetRelevantRigidbodyStates(_receivedRigidbodyStates, index);

                ClearHistoricalStates(index);

                // Calculate interpolation parameter
                float lerpParameter = InverseLerpUnclamped(previousState.FixedTime, nextState.FixedTime, delayedLocalTime);

                // Find new position and rotation for object based on interpolation
                Vector3 newPosition = Vector3.Lerp(previousState.Position, nextState.Position, lerpParameter);
                Quaternion newRotation = Quaternion.Slerp(previousState.Rotation, nextState.Rotation, lerpParameter);

                // If non host is smoothing, we have another interpolation step to perform
                if (_nonHostSmoothingTimeLeft > 0)
                {
                    float smoothingParameter = 1 - _nonHostSmoothingTimeLeft / TOTAL_SMOOTHING_TIME_S;

                    newPosition = Vector3.Lerp(_rigidbody.position, newPosition, smoothingParameter);
                    newRotation = Quaternion.Slerp(_rigidbody.rotation, newRotation, smoothingParameter);

                    _nonHostSmoothingTimeLeft -= Time.deltaTime;
                }

                // Set values based on interp positions
                SetRigidbodyValues(newPosition, newRotation);

                if (_config.DrawInterpolationLines)
                {
                    Color lineColour = lerpParameter >= 0 ? Color.white : Color.red;
                    Debug.DrawLine(_rigidbody.position, _rigidbody.position + Vector3.Cross(_rigidbody.linearVelocity, Vector3.up).normalized / 5, Color.white, 20f);
                }
                if (_config.LogInterpolationDebug) 
                { 
                    Debug.Log($"LocalTime = {delayedLocalTime}, StateFixedTimes = {previousState.FixedTime} & {nextState.FixedTime}, lerpParam = {lerpParameter}");
                }
            }
        }

        private (RigidbodySyncableState previous, RigidbodySyncableState next) GetRelevantRigidbodyStates(List<RigidbodySyncableState> statesList, int indexOfNextState)
        {
            RigidbodySyncableState previousState;
            RigidbodySyncableState nextState;

            switch (indexOfNextState)
            {
                case -1:
                    // In this case we're ahead and need to extrapolate from the last two
                    previousState = statesList[^2];
                    nextState = statesList[^1];
                    break;
                case 0:
                    // In this case we're behind all states and need to extrapolate behind the first two states
                    previousState = statesList[0];
                    nextState = statesList[1];
                    break;
                default:
                    // In the typical case we have two states that we can interpolate between
                    previousState = statesList[indexOfNextState - 1];
                    nextState = statesList[indexOfNextState];
                    break;
            }
            return (previousState, nextState);
        }

        private float InverseLerpUnclamped (float a, float b, float value)
        {
            if (a != b)
            {
                return (value - a) / (b - a);
            }

            return 0f;
        }

        private void ClearHistoricalStates(int indexOfNextState)
        {
            if (indexOfNextState == -1)
                _receivedRigidbodyStates.RemoveRange(0, _receivedRigidbodyStates.Count - 2);
            else if (indexOfNextState >= 1)
                _receivedRigidbodyStates.RemoveRange(0, indexOfNextState - 1);
            // If index == 0 there are no historical states, so we don't clear them
        }

        private Vector3 GetVelocityFromStates((RigidbodySyncableState previous, RigidbodySyncableState next) statesTuple)
        {
            // Calculate the time difference
            float deltaTime = statesTuple.next.FixedTime - statesTuple.previous.FixedTime;

            if (Mathf.Approximately(deltaTime, 0f))
                return Vector3.zero;

            return (statesTuple.next.Position - statesTuple.previous.Position) / deltaTime;
        }

        private Vector3 GetAngularVelocityFromStates((RigidbodySyncableState previous, RigidbodySyncableState next) statesTuple)
        {
            // Calculate the time difference
            float deltaTime = statesTuple.next.FixedTime - statesTuple.previous.FixedTime;

            if (Mathf.Approximately(deltaTime, 0f))
                return Vector3.zero;

            // Calculate the relative rotation
            Quaternion deltaRotation = statesTuple.next.Rotation * Quaternion.Inverse(statesTuple.previous.Rotation);

            // Extract the angle and axis from the delta rotation
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            // Ensure the angle is in radians for velocity calculation
            angle *= Mathf.Deg2Rad;

            // Calculate the angular velocity vector
            Vector3 angularVelocity = (axis * angle) / deltaTime;

            return angularVelocity;
        }

        private void SetRigidbodyValues(RigidbodySyncableState newState)
        {
            _rigidbody.position = newState.Position;
            _rigidbody.rotation = newState.Rotation;
        }

        private void SetRigidbodyValuesWithVelocity(RigidbodySyncableState newState)
        {
            _rigidbody.position = newState.Position;
            _rigidbody.rotation = newState.Rotation;
            _rigidbody.linearVelocity = newState.Velocity;
            _rigidbody.angularVelocity = newState.AngularVelocity;
        }

        private void SetRigidbodyValues(Vector3 position, Quaternion rotation)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        private void SetRigidbodyValues(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = angularVelocity;
        }


        #endregion

        private void LogStates()
        {
            string stateString = "rbState times = [";
            foreach (RigidbodySyncableState rbState in _receivedRigidbodyStates)
            {
                stateString += rbState.FixedTime.ToString() + " ";
            }

            stateString += "]";
            Debug.Log(stateString);
        }

    }
}
