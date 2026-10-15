using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameDevHarness.CrucibleReborn.Tests
{
    public sealed class ChasePbrITests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void RetainedShaderActuallyRendersNormalAndMetallicMapVariants(bool normal)
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var enabled = new bool[lights.Length];
            for (int i = 0; i < lights.Length; i++) { enabled[i] = lights[i].enabled; lights[i].enabled = false; }
            var oldAmbient = RenderSettings.ambientMode;
            var oldLight = RenderSettings.ambientLight;
            var oldSky = RenderSettings.ambientSkyColor;
            var oldEquator = RenderSettings.ambientEquatorColor;
            var oldGround = RenderSettings.ambientGroundColor;
            var oldIntensity = RenderSettings.ambientIntensity;
            var oldReflectionMode = RenderSettings.defaultReflectionMode;
            var oldReflection = RenderSettings.customReflectionTexture;
            var oldReflectionIntensity = RenderSettings.reflectionIntensity;
            var oldProbe = RenderSettings.ambientProbe;
            var oldSun = RenderSettings.sun;
            bool oldFog = RenderSettings.fog;
            var root = new GameObject("H actual GPU material probe");
            var mesh = new Mesh();
            var material = new Material(Shader.Find("Standard"));
            var controlMaterial = new Material(material.shader);
            var input = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var environment = new Cubemap(16, TextureFormat.RGBA32, true);
            var target = new RenderTexture(128, 64, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            try
            {
                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.black;
                RenderSettings.ambientSkyColor = Color.black;
                RenderSettings.ambientEquatorColor = Color.black;
                RenderSettings.ambientGroundColor = Color.black;
                RenderSettings.ambientIntensity = 0;
                RenderSettings.ambientProbe = new SphericalHarmonicsL2();
                var pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(.1f, .4f, .7f);
                for (int face = 0; face < 6; face++) environment.SetPixels(pixels, (CubemapFace)face);
                environment.Apply(true, true);
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = environment;
                RenderSettings.reflectionIntensity = 1;

                root.transform.position = new Vector3(1000, 0, 1000);
                var quad = new GameObject("Explicit UV tangent surface");
                quad.transform.SetParent(root.transform, false); quad.layer = 14;
                quad.transform.localPosition = Vector3.left * .6f;
                mesh.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(-.5f, .5f),
                    new Vector3(.5f, .5f), new Vector3(.5f, -.5f) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                var tangent = new Vector4(1, 0, 0, -1);
                mesh.tangents = new[] { tangent, tangent, tangent, tangent };
                mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                mesh.RecalculateBounds();
                quad.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = quad.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                material.color = new Color(.4f, .4f, .4f);
                controlMaterial.color = material.color;
                controlMaterial.SetFloat("_Metallic", 0); controlMaterial.SetFloat("_Glossiness", 0);
                var control = new GameObject("Simultaneous unmapped control surface");
                control.transform.SetParent(root.transform, false); control.layer = 14;
                control.transform.localPosition = Vector3.right * .6f;
                control.AddComponent<MeshFilter>().sharedMesh = mesh;
                var controlRenderer = control.AddComponent<MeshRenderer>();
                controlRenderer.sharedMaterial = controlMaterial;
                controlRenderer.shadowCastingMode = ShadowCastingMode.Off; controlRenderer.receiveShadows = false;
                control.SetActive(false);
                quad.transform.localPosition = Vector3.zero;
                material.SetFloat("_Metallic", 0); material.SetFloat("_Glossiness", 0);
                material.SetFloat("_BumpScale", 1); material.SetFloat("_GlossMapScale", 1);
                material.SetFloat("_SmoothnessTextureChannel", 0);
                Color packed = normal ? new Color(1, .5f, 1, .9f) : new Color(1, 0, 0, 1);
                input.SetPixels(new[] { packed, packed, packed, packed }); input.Apply(false, true);
                string keyword = normal ? "_NORMALMAP" : "_METALLICGLOSSMAP";
                material.SetTexture(normal ? "_BumpMap" : "_MetallicGlossMap", input);
                material.EnableKeyword(keyword);

                var cameraRoot = new GameObject("H GPU probe camera");
                cameraRoot.transform.SetParent(root.transform, false);
                cameraRoot.transform.localPosition = Vector3.back * 2;
                var camera = cameraRoot.AddComponent<Camera>();
                camera.enabled = false; camera.cullingMask = 1 << 14;
                camera.orthographic = true; camera.orthographicSize = .7f; camera.aspect = 2;
                camera.renderingPath = RenderingPath.Forward;
                camera.nearClipPlane = .1f; camera.farClipPlane = 5;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                var lightRoot = new GameObject("H isolated material illumination");
                lightRoot.transform.SetParent(root.transform, false);
                var light = lightRoot.AddComponent<Light>();
                light.type = LightType.Directional; light.cullingMask = 1 << 14;
                light.color = Color.white; light.intensity = .7f; light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.ForcePixel;
                light.transform.rotation = Quaternion.LookRotation(-new Vector3(.866f, 0, -.5f));
                RenderSettings.sun = light;

                renderer.sharedMaterial = controlMaterial;
                light.enabled = false;
                Vector3 dark = CaptureMean(camera, target, readback);
                light.enabled = true;
                Vector3 plain = CaptureMean(camera, target, readback);
                Assert.That(Vector3.Distance(plain, dark), Is.GreaterThan(.06f),
                    "The isolated probe light must drive actual pixels.");
                renderer.sharedMaterial = material;
                Vector3 mapped = CaptureMean(camera, target, readback);
                renderer.sharedMaterial = controlMaterial;
                if (normal)
                {
                    Vector3 tilted = new Vector3(.8f, 0, -.6f);
                    mesh.normals = new[] { tilted, tilted, tilted, tilted };
                }
                else
                { controlMaterial.SetFloat("_Metallic", 1); controlMaterial.SetFloat("_Glossiness", 1); }
                Vector3 expected = CaptureMean(camera, target, readback);
                Debug.Log("CHASE_GPU_MAP normal=" + normal + " path=" + camera.actualRenderingPath +
                    " mapped=" + mapped + " plain=" + plain + " physicalControl=" + expected +
                    " globalLod=" + Shader.globalMaximumLOD + " shaderLod=" + material.shader.maximumLOD);
                Assert.That(Vector3.Distance(expected, plain), Is.GreaterThan(.06f),
                    "Explicit geometry or scalar material control must change pixels.");
                Assert.That(plain.magnitude, Is.GreaterThan(.05f), "Probe surface must be lit and visible.");
                Assert.That(Vector3.Distance(mapped, plain), Is.GreaterThan(.06f),
                    "Map keyword must change actual GPU pixels, not merely material metadata.");
                Assert.That(Vector3.Distance(mapped, expected), Is.LessThan(.06f),
                    "Mapped shader must reproduce its explicit geometry/scalar control.");
            }
            finally
            {
                RenderSettings.fog = oldFog;
                RenderSettings.ambientMode = oldAmbient; RenderSettings.ambientLight = oldLight;
                RenderSettings.ambientSkyColor = oldSky; RenderSettings.ambientEquatorColor = oldEquator;
                RenderSettings.ambientGroundColor = oldGround; RenderSettings.ambientIntensity = oldIntensity;
                RenderSettings.defaultReflectionMode = oldReflectionMode;
                RenderSettings.customReflectionTexture = oldReflection;
                RenderSettings.reflectionIntensity = oldReflectionIntensity;
                RenderSettings.ambientProbe = oldProbe; RenderSettings.sun = oldSun;
                for (int i = 0; i < lights.Length; i++) if (lights[i]) lights[i].enabled = enabled[i];
                target.Release();
                Object.DestroyImmediate(root); Object.DestroyImmediate(mesh); Object.DestroyImmediate(material);
                Object.DestroyImmediate(controlMaterial);
                Object.DestroyImmediate(input); Object.DestroyImmediate(environment);
                Object.DestroyImmediate(target); Object.DestroyImmediate(readback);
            }
        }

        static Vector3 CaptureMean(Camera camera, RenderTexture target, Texture2D readback)
        {
            var previous = RenderTexture.active;
            try
            {
                camera.Render();
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, 128, 64), 0, 0, false);
                Vector3 mean = Vector3.zero;
                foreach (var pixel in readback.GetPixels(56, 24, 16, 16))
                    mean += new Vector3(pixel.r, pixel.g, pixel.b);
                return mean / (16 * 16);
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
