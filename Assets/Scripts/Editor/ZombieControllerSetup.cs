using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class ZombieControllerSetup
{
    const string ControllerPath = "Assets/Animations/Zombie/Zombie.controller";

    const string FbxIdle   = "Assets/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx";
    const string FbxWalk   = "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Walk_N.anim.fbx";
    const string FbxRun    = "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_N.anim.fbx";
    const string FbxDeath  = "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx";

    [MenuItem("Tools/Zombie/Setup Animator Controller")]
    public static void Setup()
    {
        AnimationClip idle  = LoadClip(FbxIdle,  "Idle");
        AnimationClip walk  = LoadClip(FbxWalk,  "Walk_N");
        AnimationClip run   = LoadClip(FbxRun,   "Run_N");
        AnimationClip death = LoadClip(FbxDeath, "JumpLand");

        if (idle == null || walk == null || run == null)
        {
            Debug.LogError("[ZombieSetup] One or more required clips not found — aborting.");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[ZombieSetup] Controller not found at {ControllerPath}");
            return;
        }

        // Ensure at least one layer exists
        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        // Add parameters (safe to call even if already present)
        EnsureParameter(controller, "Speed",       AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsDead",      AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Remove pre-existing states so the setup is idempotent
        foreach (var s in sm.states)
            sm.RemoveState(s.state);

        // Create states
        AnimatorState stIdle   = sm.AddState("Idle",    new Vector3(250,   0, 0));
        AnimatorState stWalk   = sm.AddState("Walk",    new Vector3(250,  80, 0));
        AnimatorState stRun    = sm.AddState("Run",     new Vector3(250, 160, 0));
        AnimatorState stAttack = sm.AddState("Attack",  new Vector3(500,  80, 0));
        AnimatorState stDead   = sm.AddState("Dead",    new Vector3(500,   0, 0));

        stIdle.motion   = idle;
        stWalk.motion   = walk;
        stRun.motion    = run;
        stAttack.motion = walk;                    // placeholder — swap for a real attack clip later
        stDead.motion   = death ?? (Motion)idle;   // placeholder until a proper death clip exists

        sm.defaultState = stIdle;

        // ── Locomotion transitions ───────────────────────────────────────────
        MakeTransition(stIdle, stWalk, 0.1f)
            .AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        MakeTransition(stWalk, stIdle, 0.1f)
            .AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        MakeTransition(stWalk, stRun, 0.1f)
            .AddCondition(AnimatorConditionMode.Greater, 2.5f, "Speed");

        MakeTransition(stRun, stWalk, 0.1f)
            .AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");

        // ── Attack (any → Attack, Attack → Idle on exit) ────────────────────
        var toAttack = sm.AddAnyStateTransition(stAttack);
        toAttack.hasExitTime        = false;
        toAttack.duration           = 0.1f;
        toAttack.canTransitionToSelf = false;
        toAttack.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");

        var attackToIdle = MakeTransition(stAttack, stIdle, 0.1f);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime    = 1f;
        attackToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");

        // ── Death (any → Dead, no return) ───────────────────────────────────
        var toDead = sm.AddAnyStateTransition(stDead);
        toDead.hasExitTime        = false;
        toDead.duration           = 0.25f;
        toDead.canTransitionToSelf = false;
        toDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

        // Write back the modified layer
        var layers = controller.layers;
        controller.layers = layers;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ZombieSetup] Zombie.controller configured successfully. " +
                  "Replace placeholder clips for Attack and Dead states when you have real animations.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static AnimatorStateTransition MakeTransition(AnimatorState from, AnimatorState to, float duration)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration    = duration;
        return t;
    }

    static void EnsureParameter(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, type);
    }

    static AnimationClip LoadClip(string fbxPath, string clipName)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && clip.name == clipName)
                return clip;
        }
        Debug.LogWarning($"[ZombieSetup] Clip '{clipName}' not found in {fbxPath}");
        return null;
    }
}
