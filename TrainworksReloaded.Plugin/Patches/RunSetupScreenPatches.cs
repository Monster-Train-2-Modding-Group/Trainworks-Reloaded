using DG.Tweening;
using HarmonyLib;
using ShinyShoe;
using SimpleInjector;
using System.Reflection;
using System.Reflection.Emit;
using TrainworksReloaded.Base.Class;
using TrainworksReloaded.Base.Prefab;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(RunSetupScreen), "Initialize")]
    public class RunSetupScreenPatches
    {
        public static Container? container;
        private static FieldInfo fieldClanIndex = AccessTools.Field(typeof(ClassSelectCharacterDisplay), "clanIndex");
        public static int RandomIndex = 10;

        public static void Prefix(GameObject ___characterDisplayRoot)
        {
            if (container == null)
            {
                Plugin.Logger.LogError("Did not find Container instance");
                return;
            }

            var classRegister = container.GetInstance<ClassDataRegister>();
            var gameObjectRegister = container.GetInstance<GameObjectRegister>();
            var characterDisplayDelegator = container.GetInstance<ClassAssetsDelegator>();
            var randomMain = ___characterDisplayRoot.transform.Find("Random main");
            int i = randomMain.GetSiblingIndex();

            foreach (var classData in classRegister)
            {
                var main = new GameObject()
                {
                    name = classData.Value.GetTitle() + " main"
                };
                main.transform.SetParent(___characterDisplayRoot.transform, false);
                main.transform.SetSiblingIndex(i);
                var championGameObjects = characterDisplayDelegator.GetCharacterDisplays(classData.Value.name) ?? [];
                var characterDisplay = main.AddComponent<ClassSelectCharacterDisplay>();
                fieldClanIndex.SetValue(characterDisplay, i);

                List<CharacterState> characters = [];
                foreach (var gameObject in championGameObjects)
                {
                    var clone = GameObject.Instantiate(gameObject);

                    clone.gameObject.SetActive(true);
                    var prefabCharacterUI = gameObject.transform.Find("CharacterScale/CharacterUI");
                    // Record the current position/scale values, because it gets overwritten by CharacterUI.
                    var localPosition = prefabCharacterUI.localPosition;
                    var localScale = prefabCharacterUI.localScale;
                    var characterUITransform = clone.transform.Find("CharacterScale/CharacterUI");
                    var characterUI = characterUITransform.GetComponent<CharacterUI>();

                    // Ensure GameObject is on layer 10.
                    // It could be on layer 20, but no need to have it on that layer as a non animated character
                    // in this scene is drawn by the CharacterUI directly.
                    characterUI.gameObject.layer = LayerMask.NameToLayer("Character_Lights");

                    clone.transform.SetParent(main.transform, false);


                    // Quad Default displays a square here.
                    var quadDefault = clone.transform.Find("CharacterScale/CharacterUI/Quad_Default");
                    var animatedSpriteMesh = quadDefault?.GetComponent<CharacterUIMeshAnimatedSprite>() as CharacterUIMeshBase;
                    var quadDefaultScale = quadDefault?.localScale ?? Vector3.one;
                    if (animatedSpriteMesh == null)
                        quadDefault?.gameObject.SetActive(false);
                    if (quadDefault != null)
                    {
                        quadDefault.localPosition = localPosition;
                        //quadDefault.localScale = localScale;
                    }

                    var spineMeshes = clone.transform.Find("CharacterScale/CharacterUI/SpineMeshes");
                    var characterMeshSpine = spineMeshes?.GetComponent<CharacterUIMeshSpine>();
                    // If using a static image put the scale on the CharacterUI which will display the sprite.
                    // Quad_Default doesn't have the correct scaling but the GameObject placed under it at construction does.
                    if (characterMeshSpine == null && animatedSpriteMesh == null)
                    {
                        var scaleFix = quadDefault?.Find("ScaleFixer")?.localScale ?? Vector3.one;
                        characterUI.transform.localScale = scaleFix;
                    }

                    var characterState = clone.transform.GetComponentInChildren<CharacterState>();
                    var characterMesh = clone.transform.GetComponentInChildren<CharacterUIMesh>(includeInactive: true);
                    // Can't do a child search because Red Crown has a CharacterUIMeshSpine component.
                    // Fix the CharacterMesh and CharacterState fields just in case. Otherwise a NRE happens.
                    AccessTools.Field(typeof(CharacterUI), "_characterMesh").SetValue(characterUI, (CharacterUIMeshBase?)(characterMeshSpine ?? characterMesh ?? animatedSpriteMesh));
                    AccessTools.Field(typeof(CharacterUI), "_characterState").SetValue(characterUI, characterState);

                    characters.Add(characterState);
                }

                i++;
            }

            RandomIndex = i;
            var randomDisplay = randomMain.GetComponent<ClassSelectCharacterDisplay>();
            fieldClanIndex.SetValue(randomDisplay, i);
        }
    }

    [HarmonyPatch(typeof(RunSetupScreen), "InitializeCharacters")]
    public static class Patch_InitializeCharacters
    {
        public static MethodInfo methodGetValue = AccessTools.Method(typeof(Patch_InitializeCharacters), nameof(GetMainClanIndex));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool inNullCheckBlock = false;
            bool changesMade = false;

            for (int i = 0; i < codes.Count - 1; i++)
            {
                // Looking for if (mainChampionData == null)
                if (codes[i].opcode == OpCodes.Ldarg_1 &&
                    codes[i + 1].opcode == OpCodes.Ldnull &&
                    codes[i + 2].opcode == OpCodes.Call &&
                    codes[i + 3].opcode == OpCodes.Brfalse)
                {
                    inNullCheckBlock = true;
                    continue;
                }

                // Find mainClanIndex = 10. Replace with the new RandomIndex
                if (inNullCheckBlock &&
                    codes[i].opcode == OpCodes.Stloc_0 &&
                    codes[i - 1].opcode == OpCodes.Ldc_I4_S)
                {
                    codes[i - 1] = new CodeInstruction(OpCodes.Call, methodGetValue);
                    changesMade = true;
                    break;
                }
            }

            if (!changesMade)
            {
                Plugin.Logger.LogError("Patch failed, did not find a mainClanIndex = 10 in the IL.");
            }

            return codes;
        }

        public static int GetMainClanIndex()
        {
            return RunSetupScreenPatches.RandomIndex;
        }
    }
}