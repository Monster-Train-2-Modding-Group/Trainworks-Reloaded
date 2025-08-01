using HarmonyLib;
using UnityEngine;
using SimpleInjector;
using TrainworksReloaded.Base.Class;
using System.Reflection;
using TrainworksReloaded.Base.Prefab;
using System.Reflection.Emit;
using ShinyShoe;
using static RotaryHeart.Lib.DataBaseExample;
using UnityEngine.TextCore.Text;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(RunSetupScreen), "Initialize")]
    public class RunSetupPatches
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
            var characterDisplayDelegator = container.GetInstance<ClassSelectCharacterDisplayDelegator>();
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
                /*foreach (var championCard in classData.Value.GetAllChampionCards())
                {
                    var character = championCard.GetSpawnCharacterData();
                    if (character == null)
                        continue;
                    var gameObject = character.GetCharacterPrefabVariant();
                    var clone = GameObject.Instantiate(gameObject);
                    clone.transform.SetScale(2.5f, 2.5f, 1f);
                    clone.transform.SetParent(main.transform, false);
                }*/
                List<CharacterState> characters = [];
                foreach (var gameObject in championGameObjects)
                {
                    var clone = GameObject.Instantiate(gameObject);
                    clone.transform.SetParent(main.transform, false);

                    // Quad Default displays a square here.
                    var quadDefault = clone.transform.Find("CharacterScale/CharacterUI/Quad_Default");
                    quadDefault?.gameObject.SetActive(false);

                    var characterUI = clone.transform.GetComponentInChildren<CharacterUI>();
                    var characterState = clone.transform.GetComponentInChildren<CharacterState>();

                    
                    var characterMesh = clone.transform.GetComponentInChildren<CharacterUIMesh>(includeInactive: true);
                    // Can't do a child search because Red Crown has a CharacterUIMeshSpine component.
                    var characterMeshSpine = clone.transform.Find("CharacterScale/CharacterUI/SpineMeshes")?.GetComponent<CharacterUIMeshSpine>();
                    AccessTools.Field(typeof(CharacterUI), "_characterMesh").SetValue(characterUI, (CharacterUIMeshBase?) characterMeshSpine ?? characterMesh);
                    AccessTools.Field(typeof(CharacterUI), "_characterState").SetValue(characterUI, characterState);
                                        
                    characters.Add(characterState);
                }
                AccessTools.Field(typeof(ClassSelectCharacterDisplay), "characters").SetValue(characterDisplay, characters);

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
            return RunSetupPatches.RandomIndex;
        }
    }
}
