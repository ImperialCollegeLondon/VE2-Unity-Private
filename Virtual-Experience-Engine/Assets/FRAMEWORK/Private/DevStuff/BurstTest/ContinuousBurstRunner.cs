using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;

public class ContinuousBurstRunner : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private int iterations = 100_000;

    private NativeArray<float> result;
    private NativeArray<Vector3> positionInput;

    void Start()
    {
        result = new NativeArray<float>(1, Allocator.Persistent);
        positionInput = new NativeArray<Vector3>(1, Allocator.Persistent);
    }

    void Update()
    {
        // Update the input for the job
        //positionInput[0] = trackedTransform ? trackedTransform.position : new Vector3(Time.time, Mathf.Sin(Time.time), 0f);
        positionInput[0] = VE2API.Player.PlayerPosition;


        var job = new ContinuousStressJob
        {
            position = positionInput,
            iterations = iterations,
            result = result
        };

        JobHandle handle = job.Schedule();
        handle.Complete();

        resultText.text = $"Burst live result: {result[0]:F5}";
    }

    void OnDestroy()
    {
        if (result.IsCreated) result.Dispose();
        if (positionInput.IsCreated) positionInput.Dispose();
    }

    [BurstCompile(FloatPrecision = FloatPrecision.Standard, FloatMode = FloatMode.Fast)]
    private struct ContinuousStressJob : IJob
    {
        [ReadOnly] public NativeArray<Vector3> position;
        [WriteOnly] public NativeArray<float> result;
        public int iterations;

        public void Execute()
        {
            Vector3 pos = position[0];
            float sum = 0f;

            for (int i = 1; i < iterations; i++)
            {
                float s = math.sin(pos.x * i) * math.cos(pos.y * i);
                sum += s * 0.0001f;
            }

            result[0] = sum;
        }
    }
}
