using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public static class ChaseGraphics
    {
        public static int CurrentPreset { get; private set; } = 1;
        public static string Description(int preset)
        {
            return preset <= 0 ? "Low: no shadows, no MSAA, half-size textures, fewer animated rigs and sparse decoration." :
                preset == 1 ? "Medium: hard shadows to 32m, 2x MSAA, full-size textures, balanced rigs and decoration." :
                "High: very-high-resolution soft shadows to 90m, four cascades, 4x MSAA, 16x anisotropic PBR maps, maximum rigs and decoration.";
        }

        public static void Apply(int preset, Camera camera, ChaseSwarm swarm, ChaseArena arena)
        {
            preset = Mathf.Clamp(preset, 0, 2);
            CurrentPreset = preset;
            QualitySettings.shadows = preset == 0 ? ShadowQuality.Disable :
                preset == 1 ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = preset == 2 ? ShadowResolution.VeryHigh :
                preset == 1 ? ShadowResolution.Medium : ShadowResolution.Low;
            QualitySettings.shadowDistance = preset == 0 ? 0 : preset == 1 ? 32 : 90;
            QualitySettings.shadowCascades = preset == 2 ? 4 : 0;
            QualitySettings.shadowCascade4Split = new Vector3(.07f, .22f, .5f);
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowNearPlaneOffset = .5f;
            QualitySettings.antiAliasing = preset == 0 ? 0 : preset == 1 ? 2 : 4;
            QualitySettings.globalTextureMipmapLimit = preset == 0 ? 1 : 0;
            QualitySettings.anisotropicFiltering = preset == 2 ? AnisotropicFiltering.ForceEnable :
                preset == 1 ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable;
            if (camera != null) camera.allowMSAA = preset > 0;
            swarm?.SetQuality(preset);
            arena?.SetQuality(preset);
            if (arena != null)
            {
                if (arena.Sun != null)
                {
                    arena.Sun.shadows = preset == 0 ? LightShadows.None :
                        preset == 1 ? LightShadows.Hard : LightShadows.Soft;
                    arena.Sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.FromQualitySettings;
                    arena.Sun.shadowBias = preset == 2 ? .025f : .05f;
                    arena.Sun.shadowNormalBias = preset == 2 ? .3f : .4f;
                    arena.Sun.shadowNearPlane = .1f;
                }
                int cosmetic = 0;
                foreach (var part in arena.Root.GetComponentsInChildren<Transform>(true))
                    if (part.name == "Boundary crown" || part.name == "Outer circuit inlay")
                        part.gameObject.SetActive(preset == 2 || (preset == 1 && cosmetic++ % 2 == 0));
            }
            // Built-in forward rendering has no generic render-scale property.
            // Do not advertise resolution scaling without a supported pipeline.
            // Soft filtering is the retained built-in shadow filter, not PCSS,
            // screen-space contact shadows, SSAO or an uninstalled SRP stack.
        }
    }
}
