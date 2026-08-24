#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GreatswordDodgeSetup
{
    private const string ControllerPath = "Assets/PlayerAnimations/PlayerAnimator.controller";
    private const string DodgeClipKeyword = "DiveRoll-Forward1";

    [MenuItem("Tools/Player/Greatsword Dodge/1. Apply Dodge Motion")]
    public static void ApplyDodgeMotion()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "PlayerAnimator 없음",
                ControllerPath + " 를 찾지 못했습니다.",
                "확인"
            );
            return;
        }

        if (controller.layers == null || controller.layers.Length == 0)
        {
            EditorUtility.DisplayDialog("Animator 오류", "Base Layer를 찾지 못했습니다.", "확인");
            return;
        }

        AnimationClip dodgeClip = FindAnimationClip(DodgeClipKeyword);
        if (dodgeClip == null)
        {
            EditorUtility.DisplayDialog(
                "회피 모션을 찾지 못했습니다",
                "프로젝트에서 'RPG-Character@2Hand-Sword-DiveRoll-Forward1.max.FBX'를 찾지 못했습니다.\n\n" +
                "RPG Character Mecanim Animation Pack FREE의 2Hand-Sword 폴더가 프로젝트 안에 있는지 확인하세요.",
                "확인"
            );
            return;
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState dodge = FindState(sm, "Dodge");
        AnimatorState combatLocomotion = FindState(sm, "CombatLocomotion");
        AnimatorState locomotion = FindState(sm, "Locomotion");

        Undo.RegisterCompleteObjectUndo(controller, "Apply Greatsword Dodge");
        Undo.RegisterCompleteObjectUndo(sm, "Apply Greatsword Dodge");

        EnsureParameter(controller, "doDodge", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "isCombat", AnimatorControllerParameterType.Bool);

        if (dodge == null)
        {
            dodge = sm.AddState("Dodge", new Vector3(720f, 330f, 0f));
        }

        dodge.motion = dodgeClip;
        dodge.speed = 1.35f;

        // Any State -> Dodge를 하나만 남긴다.
        foreach (AnimatorStateTransition t in sm.anyStateTransitions.ToArray())
        {
            if (t.destinationState == dodge)
                sm.RemoveAnyStateTransition(t);
        }

        AnimatorStateTransition anyToDodge = sm.AddAnyStateTransition(dodge);
        anyToDodge.hasExitTime = false;
        anyToDodge.hasFixedDuration = true;
        anyToDodge.duration = 0.02f;
        anyToDodge.offset = 0f;
        anyToDodge.canTransitionToSelf = false;
        anyToDodge.interruptionSource = TransitionInterruptionSource.None;
        anyToDodge.AddCondition(AnimatorConditionMode.If, 0f, "doDodge");

        // Dodge에서 나가는 기존 전환을 정리한다.
        foreach (AnimatorStateTransition t in dodge.transitions.ToArray())
            dodge.RemoveTransition(t);

        // 전투 중이면 CombatLocomotion, 아니면 Locomotion으로 복귀.
        if (combatLocomotion != null && locomotion != null)
        {
            AnimatorStateTransition toCombat = dodge.AddTransition(combatLocomotion);
            ConfigureExitTransition(toCombat);
            toCombat.AddCondition(AnimatorConditionMode.If, 0f, "isCombat");

            AnimatorStateTransition toLocomotion = dodge.AddTransition(locomotion);
            ConfigureExitTransition(toLocomotion);
            toLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0f, "isCombat");
        }
        else
        {
            AnimatorState fallback = combatLocomotion != null ? combatLocomotion : locomotion;
            if (fallback != null)
            {
                AnimatorStateTransition back = dodge.AddTransition(fallback);
                ConfigureExitTransition(back);
            }
        }

        EditorUtility.SetDirty(dodge);
        EditorUtility.SetDirty(sm);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controller;

        EditorUtility.DisplayDialog(
            "회피 모션 적용 완료",
            "Dodge State에 아래 모션을 연결했습니다.\n\n" +
            dodgeClip.name + "\n\n" +
            "Left Ctrl → 즉시 전방 구르기\n" +
            "Animator Dodge Speed = 1.35\n" +
            "Transition Duration = 0.02\n\n" +
            "기존 PlayerMovement.cs의 회피 이동 로직은 그대로 사용합니다.",
            "확인"
        );
    }

    private static void ConfigureExitTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = true;
        transition.exitTime = 0.90f;
        transition.hasFixedDuration = true;
        transition.duration = 0.04f;
        transition.offset = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string stateName)
    {
        foreach (ChildAnimatorState child in sm.states)
        {
            if (child.state != null && child.state.name == stateName)
                return child.state;
        }
        return null;
    }

    private static void EnsureParameter(
        AnimatorController controller,
        string name,
        AnimatorControllerParameterType type)
    {
        AnimatorControllerParameter existing = controller.parameters.FirstOrDefault(p => p.name == name);
        if (existing == null)
        {
            controller.AddParameter(name, type);
            return;
        }

        // 이미 같은 이름이 있다면 기존 타입을 존중한다.
        // 현재 프로젝트에서는 doDodge=Trigger, isCombat=Bool로 사용 중이다.
    }

    private static AnimationClip FindAnimationClip(string keyword)
    {
        string[] guids = AssetDatabase.FindAssets(keyword);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c =>
                    !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    c.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

            if (clip != null)
                return clip;
        }

        // 검색 인덱스가 파일명만 잡지 못하는 경우를 대비해 FBX 전체를 한 번 더 확인.
        string[] modelGuids = AssetDatabase.FindAssets("t:Model");
        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));

            if (clip != null)
                return clip;
        }

        return null;
    }
}
#endif
