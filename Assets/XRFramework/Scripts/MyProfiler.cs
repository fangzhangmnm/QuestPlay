using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;


namespace fzmnm
{
    public class MyProfiler : MonoBehaviour
    {
        ProfilerRecorder systemMemoryRecorder;
        ProfilerRecorder gcMemoryRecorder;
        ProfilerRecorder drawCallsCountRecorder;
        ProfilerRecorder mainThreadTimeRecorder;
        ProfilerRecorder renderThreadTimeRecorder;
        ProfilerRecorder physicsFixedUpdateTimeRecorder;
        ProfilerRecorder scriptFixedUpdateTimeRecorder;

        public Text text;

        void OnEnable()
        {
            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            drawCallsCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread",15);
            renderThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 15);
            physicsFixedUpdateTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "FixedUpdate.PhysicsFixedUpdate", 15);
            scriptFixedUpdateTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "FixedUpdate.ScriptRunBehaviourFixedUpdate", 15);
            
            StartCoroutine(MainLoop());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            systemMemoryRecorder.Dispose();
            gcMemoryRecorder.Dispose();
            drawCallsCountRecorder.Dispose();
            mainThreadTimeRecorder.Dispose();
            renderThreadTimeRecorder.Dispose();
            physicsFixedUpdateTimeRecorder.Dispose();
            scriptFixedUpdateTimeRecorder.Dispose();
        }
        IEnumerator MainLoop()
        {
            while (true)
            {
                UpdateProfiler(.5f);
                yield return new WaitForSeconds(.5f);
            }
        }
        void UpdateProfiler(float dt)
        {
            var sb = new StringBuilder(500);
            sb.AppendLine($"FPS: {1f / Time.smoothDeltaTime:F1}    {Time.smoothDeltaTime * 1000:F1} ms");
            sb.AppendLine($"Main Thread: {GetRecorderFrameAverage(mainThreadTimeRecorder) * (1e-6f):F2} ms");
            sb.AppendLine($"Render Thread: {GetRecorderFrameAverage(renderThreadTimeRecorder) * (1e-6f):F2} ms");
            sb.AppendLine($"Physics: {GetRecorderFrameAverage(physicsFixedUpdateTimeRecorder) * (1e-6f):F2} ms");
            sb.AppendLine($"Scripts FixedUpdate: {GetRecorderFrameAverage(scriptFixedUpdateTimeRecorder) * (1e-6f):F3} ms");
            sb.AppendLine($"GC Memory: {gcMemoryRecorder.LastValue / 1048576} MB");
            sb.AppendLine($"System Memory: {systemMemoryRecorder.LastValue / 1048576} MB");
            sb.AppendLine($"Draw Calls: {drawCallsCountRecorder.LastValue}");
            text.text = sb.ToString();
        }
        static double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samples.Count; ++i)
                if(samples[i].Value>0)
                    r += ((double)samples[i].Value)/samplesCount;
            return r;
        }
    }

}