using System;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace Journey.Editor
{
    public static class ProfileExport
    {
        [Serializable] sealed class Marker
        {
            public string thread, name;
            public int calls;
            public double totalInclusiveMs, maxCallMs;
        }
        [Serializable] sealed class Summary
        {
            public string note = "Development profiler diagnostics for the loaded frame window; Unity history limits may truncate long recordings. Inclusive samples overlap; worker sums are not wall-clock latency. No Deep Profiling.";
            public int frames, firstFrameIndex, lastFrameIndex;
            public List<Marker> markers = new List<Marker>();
        }
        public static void Export()
        {
            string[] args = Environment.GetCommandLineArgs();
            int argument = Array.IndexOf(args, "-profileInput");
            if (argument < 0 || argument + 1 >= args.Length) throw new ArgumentException("Supply -profileInput path.raw");
            string path = Path.GetFullPath(args[argument + 1]);
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            ProfilerDriver.LoadProfile(path, false);
            var summary = new Summary { firstFrameIndex = ProfilerDriver.firstFrameIndex, lastFrameIndex = ProfilerDriver.lastFrameIndex };
            var markers = new Dictionary<string, Marker>();
            for (int frame = ProfilerDriver.firstFrameIndex; frame <= ProfilerDriver.lastFrameIndex; frame++)
            {
                if (frame < 0) continue;
                summary.frames++;
                for (int thread = 0; ; thread++)
                using (var data = ProfilerDriver.GetRawFrameDataView(frame, thread))
                {
                    if (!data.valid) break;
                    for (int i = 0; i < data.sampleCount; i++)
                    {
                        string name = data.GetSampleName(i);
                        if (string.IsNullOrEmpty(name)) continue;
                        if (!name.Contains("Journey") && !name.Contains("Job") && !name.Contains("Gfx") && !name.Contains("Render") && !name.Contains("Camera") && !name.Contains("PlayerLoop") && !data.threadName.Contains("Render") && !data.threadName.Contains("GPU")) continue;
                        string key = data.threadName + "/" + name;
                        if (!markers.TryGetValue(key, out var marker))
                        { marker = new Marker { thread = data.threadName, name = name }; markers.Add(key, marker); }
                        double ms = data.GetSampleTimeMs(i);
                        marker.calls++; marker.totalInclusiveMs += ms; marker.maxCallMs = Math.Max(marker.maxCallMs, ms);
                    }
                }
            }
            if (summary.frames == 0 || markers.Count == 0) throw new InvalidOperationException("Profile contains no readable Journey/job markers.");
            summary.markers.AddRange(markers.Values);
            summary.markers.Sort((a, b) => b.totalInclusiveMs.CompareTo(a.totalInclusiveMs));
            string output = Path.ChangeExtension(path, ".summary.json");
            File.WriteAllText(output, JsonUtility.ToJson(summary, true));
            Debug.Log("PROFILE_SUMMARY " + output + " frames=" + summary.frames);
        }
    }
}
