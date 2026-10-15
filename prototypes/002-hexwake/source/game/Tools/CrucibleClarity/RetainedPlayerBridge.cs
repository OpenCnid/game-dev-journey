using System;
using System.Runtime.InteropServices;
using UnityEngine;
using GameDevHarness.CrucibleReborn;

namespace GameDevHarness.SwordTrial
{
    // Only this adapter identity is baked into the preserved native template.
    public sealed class SwordTrialController : MonoBehaviour
    {
        public Shader worldShader, effectShader;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool SetWindowText(IntPtr window, string text);
        void Awake()
        {
            gameObject.name = "Retained full-shader scene adapter - Crucible Clarity J";
            HorrorChaseController.CreateWorld(worldShader, effectShader);
            Debug.Log("REBORN_PACKAGE_ROUTE retained-full-shader-Mono-template J-presentation-overrides freshUnityExport=false");
        }
        void Start()
        {
            var window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (window != IntPtr.Zero) SetWindowText(window, "Hexwake");
            ChaseSmoke.Install();
            ChaseNativeTelemetry.Install();
        }
    }
}
