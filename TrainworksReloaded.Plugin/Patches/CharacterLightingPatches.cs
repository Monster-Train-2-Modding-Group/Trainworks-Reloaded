using HarmonyLib;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    /// <summary>
    /// Class to setup Layer 20 (which is unused) with a Light component with full intensity for Non Animated Characters.
    /// The Shader Shiny Shoe/Character Shader requires there to be a Light in the scene.
    /// Under the default culling layer 10, the light is set to an intensity of 0.45 making the characters
    /// look way darker than the original images.
    /// </summary>
    static class FixLighting
    {
        public static void SetupCullingLayer(GameObject envFx, Camera camera)
        {
            camera.cullingMask |= 1 << 20;

            var brightLights = new GameObject
            {
                name = "Light_Bright"
            };
            var light = brightLights.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shape = LightShape.Cone;
            light.cullingMask = 1 << 20;
            light.spotAngle = 30;
            light.innerSpotAngle = 21.80208f;
            light.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            light.useColorTemperature = false;
            light.intensity = 1;
            light.bounceIntensity = 1;
            light.useViewFrustumForShadowCasterCull = true;
            light.shadowCustomResolution = -1;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.shadowNearPlane = 0.2f;
            light.useShadowMatrixOverride = false;
            light.range = 10;

            brightLights.transform.parent = envFx.transform;
        }
    }

    [HarmonyPatch(typeof(ChampionUpgradeScreen), "Initialize")]
    class ChampionUpgradeScreenLightingPatch
    {
        public static void Postfix(GameObject ___sceneRoot)
        {
            var env_lights = ___sceneRoot.transform.Find("EnvFX_Lights").gameObject;
            var camera = ___sceneRoot.transform.Find("GameCamera").GetComponent<Camera>();
            FixLighting.SetupCullingLayer(env_lights, camera);
        }
    }

    [HarmonyPatch(typeof(GameScreen), "Initialize")]
    class GameScreenLightingPatch
    {
        public static void Postfix()
        {
            var env_lights = GameObject.Find("EnvFX_Lights");
            var camera = GameObject.Find("MainCamera/Shake1Layer/ShakeLayer2/GameCamera").GetComponent<Camera>();
            FixLighting.SetupCullingLayer(env_lights, camera);
        }
    }
}
