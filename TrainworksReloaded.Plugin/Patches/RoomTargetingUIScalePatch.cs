using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using ShinyShoe;
using Spine.Unity;
using TrainworksReloaded.Base.Prefab;
using UnityEngine;

namespace TrainworksReloaded.Plugin.Patches
{
    [HarmonyPatch(typeof(RoomTargetingUI), nameof(RoomTargetingUI.CharacterPlacementPreview), [typeof(SpawnPoint), typeof(CharacterData), typeof(MonsterManager)])]
    public static class RoomTargetingUIScalePatch
    {
        private static readonly MethodInfo TransformPositionSetter =
                AccessTools.PropertySetter(typeof(Transform), nameof(Transform.position));

        private static readonly MethodInfo ComponentTransformGetter =
            AccessTools.PropertyGetter(typeof(Component), nameof(Component.transform));

        private static readonly FieldInfo CharacterPreviewField =
            AccessTools.Field(typeof(RoomTargetingUI), "characterPreview");

        private static readonly MethodInfo SetTransformScaleFunction = 
            AccessTools.Method(typeof(RoomTargetingUIScalePatch), nameof(RoomTargetingUIScalePatch.SetTransformScale));

        // Matcher finding GetComponentInChildren<CharacterUI>() to reliably get componentInChildren's local index
        private static readonly MethodInfo GetComponentInChildrenMethod =
            AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponentInChildren), generics: new[] { typeof(CharacterUI) });

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var matcher = new CodeMatcher(instructions, generator);

            // 1. Locate where componentInChildren is stored to capture its LocalBuilder/variable
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, GetComponentInChildrenMethod),
                new CodeMatch(instr => instr.IsStloc())
            );

            if (matcher.IsInvalid)
            {
                Plugin.Logger.LogError("[Transpiler] Failed to match GetComponentInChildren call.");
                return matcher.InstructionEnumeration();
            }

            // Advance to the store instruction and extract the local
            matcher.Advance(1);
            object localComponentInChildren = StLocIndex(matcher.Instruction);

            // 2. Find characterPreview.transform.position = position
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Callvirt, TransformPositionSetter)
            );

            if (matcher.IsInvalid)
            {
                Plugin.Logger.LogError("[Transpiler] Failed to match Transform.position setter.");
                return matcher.InstructionEnumeration();
            }

            // Advance past set_position
            matcher.Advance(1);

            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, CharacterPreviewField),       // SpriteRenderer characterPreview
                new CodeInstruction(OpCodes.Ldloc_S, localComponentInChildren),  // CharacterUI componentInChildren
                new CodeInstruction(OpCodes.Callvirt, SetTransformScaleFunction) // SetTransformScale(characterPreview, componentInChildren)
            );

            return matcher.InstructionEnumeration();
        }

        public static void SetTransformScale(SpriteRenderer characterPreview, CharacterUI characterUI)
        {
            var spineMeshes = characterUI.transform.Find("SpineMeshes");
            var quadDefault = characterUI.transform.Find("Quad_Default");
            var hasCharacterUIMesh = quadDefault.GetComponent<CharacterUIMesh>() != null;
            Vector3 scale;
            int spineChildren = spineMeshes?.childCount ?? 0;
            if (spineChildren > 0)
                scale = spineMeshes!.localScale;
            else
                scale = quadDefault.localScale;
            // For a Static sprite used the saved ScaleFixer because the QuadDefault scale is wrong.
            if (hasCharacterUIMesh && spineChildren == 0)
            {
                scale = quadDefault.Find("ScaleFixer")?.localScale ?? Vector3.one;
            }
            characterPreview.transform.localScale = scale;

            if (scale.y > 1 && spineChildren > 0)
            {
                var pos = characterPreview.transform.localPosition;
                var skeletonAnimation = spineMeshes!.GetChild(0).GetComponent<SkeletonAnimation>();
                var lowestY = GameObjectCharacterArtFinalizer.CalcSkeletonLowestY(skeletonAnimation);
                CalculateSpineAlignment(characterPreview.sprite.bounds.extents.y, lowestY, scale.y, out float uiLocalY);
                characterPreview.transform.localPosition = new Vector3(pos.x, pos.y - characterUI.transform.localPosition.y + uiLocalY, pos.z);
            }
        }

        private static void CalculateSpineAlignment(float extentsY, float lowestY, float scaleY, out float uiLocalY)
        {
            var baseUIY = Mathf.Max(0.05f, (0.84f * extentsY) - 0.37f);

            const float floorPlaneY = -0.45f;
            float baseDrop = baseUIY - floorPlaneY; // Equivalent to: uiLocalY + 0.45f

            float spineLocalY;
            if (lowestY < -0.40f && lowestY < 1000f)
            {
                float bakedExcess = lowestY - (-0.25f);
                spineLocalY = -baseDrop - bakedExcess;
            }
            else
            {
                spineLocalY = -baseDrop;
            }

            uiLocalY = baseUIY + (scaleY - 1f) * Mathf.Abs(spineLocalY);
        }

        public static int StLocIndex(CodeInstruction ci)
        {
            if (ci == null)
                return -1;

            var op = ci.opcode;

            // operand could be a byte, int, or LocalBuilder
            if (op == OpCodes.Stloc_S || op == OpCodes.Stloc)
            {
                if (ci.operand is byte b) return b;
                if (ci.operand is int i) return i;
                if (ci.operand is LocalBuilder lb) return lb.LocalIndex;
            }

            // Also check for dedicated short forms (ldloc_0 .. ldloc_3)
            if (op == OpCodes.Stloc_0) return 0;
            if (op == OpCodes.Stloc_1) return 1;
            if (op == OpCodes.Stloc_2) return 2;
            if (op == OpCodes.Stloc_3) return 3;

            return -1;
        }
    }
}
