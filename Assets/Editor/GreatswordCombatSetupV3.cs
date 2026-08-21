#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GreatswordCombatSetupV3
{
    private const string ControllerPath = "Assets/PlayerAnimations/PlayerAnimator.controller";

    [MenuItem("Tools/Player/Greatsword Combat V3/1. Setup All Combat Animator")]
    public static void SetupAnimator()
    {
        SetLoop("2Hand-Sword-Idle-Static", true);
        SetLoop("2Hand-Sword-Walk-Slow", true);
        SetLoop("2Hand-Sword-Run-Forward", true);
        SetLoop("2Hand-Sword-Run-Forward-Attack1", false);
        SetLoop("2Hand-Sword-Attack1", false);
        SetLoop("2Hand-Sword-Attack2", false);
        SetLoop("2Hand-Sword-Attack3", false);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("PlayerAnimator 없음", ControllerPath + " 를 찾지 못했습니다.", "확인");
            return;
        }

        AnimationClip combatIdle = FindExactClip("2Hand-Sword-Idle-Static") ?? FindExactClip("2Hand-Sword-Idle");
        AnimationClip combatWalk = FindExactClip("2Hand-Sword-Walk-Slow") ?? FindExactClip("2Hand-Sword-Walk");
        AnimationClip combatRun = FindExactClip("2Hand-Sword-Run-Forward");
        AnimationClip attack1 = FindExactClip("2Hand-Sword-Attack1");
        AnimationClip attack2 = FindExactClip("2Hand-Sword-Attack2");
        AnimationClip attack3 = FindExactClip("2Hand-Sword-Attack3");
        AnimationClip dashAttack = FindExactClip("2Hand-Sword-Run-Forward-Attack1");
        AnimationClip jumpAttack = FindFirstExact(
            "2Hand-Sword-Jump-Attack",
            "2Hand-Sword-JumpAttack",
            "2Hand-Sword-Slam",
            "2Hand-Sword-Overhead-Attack",
            "2Hand-Sword-Attack8",
            "2Hand-Sword-Attack4"
        );

        List<string> missing = new List<string>();
        if (combatIdle == null) missing.Add("2Hand-Sword Idle");
        if (combatWalk == null) missing.Add("2Hand-Sword Walk");
        if (combatRun == null) missing.Add("2Hand-Sword-Run-Forward");
        if (attack1 == null) missing.Add("2Hand-Sword-Attack1");
        if (attack2 == null) missing.Add("2Hand-Sword-Attack2");
        if (attack3 == null) missing.Add("2Hand-Sword-Attack3");
        if (dashAttack == null) missing.Add("2Hand-Sword-Run-Forward-Attack1");
        if (jumpAttack == null) missing.Add("Jump Attack 후보");

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog("필요한 모션이 부족합니다", string.Join("\n", missing), "확인");
            return;
        }

        EnsureParameter(controller, "isCombat", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "doAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "attackIndex", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "doDashAttack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "doJumpAttack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState locomotion = FindState(sm, "Locomotion");
        if (locomotion == null)
        {
            EditorUtility.DisplayDialog("Locomotion 없음", "기존 PlayerAnimator의 Locomotion State를 찾지 못했습니다.", "확인");
            return;
        }

        RemoveOldCombatStates(sm);

        AnimatorState combat = sm.AddState("CombatLocomotion", new Vector3(610, 100, 0));
        BlendTree combatTree = new BlendTree
        {
            name = "GreatswordCombatBlendTree_V3",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(combatTree, controller);
        combatTree.AddChild(combatIdle, 0f);
        combatTree.AddChild(combatWalk, 0.57f);
        combatTree.AddChild(combatRun, 1f);
        combat.motion = combatTree;

        AddBoolTransition(locomotion, combat, "isCombat", true, 0.08f);
        AddBoolTransition(combat, locomotion, "isCombat", false, 0.12f);

        AnimatorState a1 = CreateAttackState(sm, "Attack1", attack1, new Vector3(880, 0, 0));
        AnimatorState a2 = CreateAttackState(sm, "Attack2", attack2, new Vector3(880, 90, 0));
        AnimatorState a3 = CreateAttackState(sm, "Attack3", attack3, new Vector3(880, 180, 0));
        AnimatorState dash = CreateAttackState(sm, "DashAttack", dashAttack, new Vector3(880, 290, 0));
        AnimatorState jump = CreateAttackState(sm, "JumpAttack", jumpAttack, new Vector3(880, 400, 0));

        a1.speed = 1.10f;
        a2.speed = 1.10f;
        a3.speed = 1.08f;
        dash.speed = 1.08f;
        jump.speed = 1.10f;

        AddAnyComboTransition(sm, a1, 1);
        AddAnyComboTransition(sm, a2, 2);
        AddAnyComboTransition(sm, a3, 3);
        AddAnyTriggerTransition(sm, dash, "doDashAttack");
        AddAnyTriggerTransition(sm, jump, "doJumpAttack");

        AddExitTransition(a1, combat, 0.92f);
        AddExitTransition(a2, combat, 0.92f);
        AddExitTransition(a3, combat, 0.94f);
        AddExitTransition(dash, combat, 0.92f);
        AddExitTransition(jump, combat, 0.92f);

        SetupPlayerReferencesInternal();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "대검 전투 V3 설정 완료",
            "달리기: " + combatRun.name + "\n" +
            "대시 공격: " + dashAttack.name + "\n" +
            "점프 내려찍기: " + jumpAttack.name + "\n\n" +
            "좌클릭 = 3타 콤보\n" +
            "Shift + 이동 + 좌클릭 = 대시 공격\n" +
            "공중에서 좌클릭 = 내려찍기",
            "확인"
        );

        Selection.activeObject = controller;
    }

    [MenuItem("Tools/Player/Greatsword Combat V3/2. Setup Player References")]
    public static void SetupPlayerReferences()
    {
        if (SetupPlayerReferencesInternal())
            EditorUtility.DisplayDialog("참조 연결 완료", "PlayerAttack / PlayerMovement / Animator / AnimationEventRelay / Greatsword 참조를 확인했습니다.", "확인");
    }

    [MenuItem("Tools/Player/Greatsword Combat V3/3. Use Selected Clip as Jump Slam")]
    public static void UseSelectedClipAsJumpAttack()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Animation Clip 선택 필요", "Project 창에서 FBX를 펼친 뒤 초록색 삼각형 Animation Clip을 선택하고 다시 실행해 주세요.", "확인");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) return;

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState jump = FindState(sm, "JumpAttack");
        if (jump == null)
        {
            EditorUtility.DisplayDialog("JumpAttack 없음", "먼저 V3의 1. Setup All Combat Animator를 실행해 주세요.", "확인");
            return;
        }

        SetLoopByClip(clip, false);
        jump.motion = clip;
        EditorUtility.SetDirty(jump);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("점프 내려찍기 모션 교체 완료", "JumpAttack = " + clip.name, "확인");
        Selection.activeObject = controller;
    }

    private static bool SetupPlayerReferencesInternal()
    {
        GameObject player = FindPlayer();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Player 없음", "Hierarchy에서 Player를 선택해 주세요.", "확인");
            return false;
        }

        PlayerAttack attack = player.GetComponent<PlayerAttack>();
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (attack == null || movement == null || animator == null)
        {
            EditorUtility.DisplayDialog("컴포넌트 부족", "PlayerAttack / PlayerMovement / 자식 Animator가 모두 필요합니다.", "확인");
            return false;
        }

        attack.animator = animator;
        movement.animator = animator;
        movement.visualRoot = animator.transform;

        if (animator.GetComponent<AnimationEventRelay>() == null)
            Undo.AddComponent<AnimationEventRelay>(animator.gameObject);

        if (attack.weaponObject == null)
        {
            Transform hand = FindRightHand(animator);
            if (hand != null)
            {
                Transform socket = hand.Find("GreatswordSocket");
                if (socket != null && socket.childCount > 0)
                    attack.weaponObject = socket.GetChild(0).gameObject;
            }
        }

        EditorUtility.SetDirty(attack);
        EditorUtility.SetDirty(movement);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return true;
    }

    private static GameObject FindPlayer()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null && (selected.name == "Player" || selected.GetComponent<PlayerMovement>() != null))
            return selected;
        return GameObject.Find("Player");
    }

    private static Transform FindRightHand(Animator animator)
    {
        if (animator.isHuman)
        {
            Transform bone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (bone != null) return bone;
        }

        foreach (Transform t in animator.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("righthand") || n.Contains("right_hand") || n.Contains("hand_r")) return t;
        }
        return null;
    }

    private static AnimationClip FindFirstExact(params string[] names)
    {
        foreach (string name in names)
        {
            AnimationClip clip = FindExactClip(name);
            if (clip != null) return clip;
        }
        return null;
    }

    private static AnimationClip FindExactClip(string exactName)
    {
        string wanted = Normalize(exactName);
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(obj is AnimationClip clip)) continue;
                if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) continue;
                if (Normalize(clip.name) == wanted) return clip;
            }
        }
        return null;
    }

    private static string Normalize(string value)
    {
        return value.Replace("-", "").Replace("_", "").Replace(" ", "").ToLowerInvariant();
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string stateName)
    {
        foreach (ChildAnimatorState child in sm.states)
            if (child.state != null && child.state.name == stateName) return child.state;
        return null;
    }

    private static void RemoveOldCombatStates(AnimatorStateMachine sm)
    {
        string[] names = { "Attack", "Attack1", "Attack2", "Attack3", "DashAttack", "RunAttack", "JumpAttack", "CombatLocomotion" };

        foreach (AnimatorStateTransition transition in sm.anyStateTransitions.ToArray())
        {
            if (transition.destinationState != null && names.Contains(transition.destinationState.name))
                sm.RemoveAnyStateTransition(transition);
        }

        foreach (ChildAnimatorState child in sm.states.ToArray())
        {
            if (child.state != null && names.Contains(child.state.name)) sm.RemoveState(child.state);
        }
    }

    private static AnimatorState CreateAttackState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 position)
    {
        AnimatorState state = sm.AddState(name, position);
        state.motion = clip;
        return state;
    }

    private static void AddAnyComboTransition(AnimatorStateMachine sm, AnimatorState state, int index)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.hasExitTime = false;
        t.duration = 0.03f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, "doAttack");
        t.AddCondition(AnimatorConditionMode.Equals, index, "attackIndex");
    }

    private static void AddAnyTriggerTransition(AnimatorStateMachine sm, AnimatorState state, string trigger)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.hasExitTime = false;
        t.duration = 0.03f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.duration = 0.04f;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value, float duration)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = duration;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (!controller.parameters.Any(p => p.name == name)) controller.AddParameter(name, type);
    }

    private static void SetLoop(string clipName, bool loop)
    {
        AnimationClip clip = FindExactClip(clipName);
        if (clip != null) SetLoopByClip(clip, loop);
    }

    private static void SetLoopByClip(AnimationClip clip, bool loop)
    {
        string path = AssetDatabase.GetAssetPath(clip);
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;

        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            if (Normalize(clips[i].name) != Normalize(clip.name)) continue;
            if (clips[i].loopTime != loop || clips[i].loopPose != loop)
            {
                clips[i].loopTime = loop;
                clips[i].loopPose = loop;
                changed = true;
            }
        }

        if (!changed) return;
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }
}
#endif
