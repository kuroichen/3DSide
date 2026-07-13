using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Rail.EditorTools
{
    /// <summary>
    /// PlayerAnimationControllerを物理状態に追従する構成へ組み替えるツール。
    /// - JumpBegin(上昇) → Fall(下降ループ) → Land の空中ステートを再構成
    /// - 歩き落ち(IsGrounded=false)でもFallへ入る(空中歩き対策)
    /// - AnyState→Dead(空中・攻撃中でも死亡アニメが再生される)
    /// - JumpStateMachine出口がAttackに誤配線されていた問題を撤去
    /// Library/fix_player_animator が存在する場合はコンパイル直後に一度だけ自動実行する。
    /// </summary>
    public static class PlayerAnimatorFixer
    {
        const string RequestMarkerPath = "Library/fix_player_animator";
        const string ControllerPath = "Assets/Design/Animations/Controllers/PlayerAnimationController.controller";
        const string ClipFolder = "Assets/Design/Animations/Clips/Character";

        [MenuItem("Tools/Rail/Fix Player Animator")]
        public static void Fix()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"[PlayerAnimatorFixer] {ControllerPath} が見つかりません。");
                return;
            }

            var rootSm = controller.layers[0].stateMachine;

            // --- パラメータ追加(上昇/下降の判定用・ハード着地判定用) ---
            if (controller.parameters.All(p => p.name != "VerticalVelocity"))
            {
                controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            }
            if (controller.parameters.All(p => p.name != "HardLand"))
            {
                controller.AddParameter("HardLand", AnimatorControllerParameterType.Bool);
            }

            // --- 旧JumpStateMachineを撤去(出口→Attackの誤配線ごと消える) ---
            foreach (var child in rootSm.stateMachines.ToArray())
            {
                if (child.stateMachine.name == "JumpStateMachine")
                {
                    rootSm.RemoveStateMachine(child.stateMachine);
                }
            }

            // --- 再実行に備えて同名ステートを撤去 ---
            RemoveState(rootSm, "JumpBegin");
            RemoveState(rootSm, "Fall");
            RemoveState(rootSm, "Land");

            var move = FindState(rootSm, "Move Blend Tree");
            var dead = FindState(rootSm, "Dead");
            if (move == null || dead == null)
            {
                Debug.LogWarning("[PlayerAnimatorFixer] Move Blend Tree / Dead ステートが見つかりません。");
                return;
            }

            // --- 空中ステートを構築 ---
            var jumpBegin = rootSm.AddState("JumpBegin", new Vector3(300.0f, 350.0f, 0.0f));
            jumpBegin.motion = LoadClip("HumanM@Jump01 - Begin");

            var fall = rootSm.AddState("Fall", new Vector3(500.0f, 350.0f, 0.0f));
            fall.motion = LoadClip("HumanM@Fall01");

            var land = rootSm.AddState("Land", new Vector3(700.0f, 350.0f, 0.0f));
            land.motion = LoadClip("HumanM@Jump01 - Land");

            // Fall01はループ再生にする
            if (fall.motion is AnimationClip fallClip)
            {
                var settings = AnimationUtility.GetAnimationClipSettings(fallClip);
                if (!settings.loopTime)
                {
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(fallClip, settings);
                    EditorUtility.SetDirty(fallClip);
                }
            }

            // --- Moveの遷移を整理(壊れた旧Jump行き・Dead行きを除去) ---
            foreach (var transition in move.transitions.ToArray())
            {
                var isStale = transition.destinationState == null && transition.destinationStateMachine == null;
                var isDead = transition.destinationState == dead;
                if (isStale || isDead)
                {
                    move.RemoveTransition(transition);
                }
            }

            // Move → JumpBegin (踏切。ブレンドは短く=キレ優先)
            var toJump = move.AddTransition(jumpBegin);
            toJump.hasExitTime = false;
            toJump.duration = 0.05f;
            toJump.AddCondition(AnimatorConditionMode.If, 0.0f, "JumpTrigger");

            // Move → Fall (歩き落ち。空中歩きの解消)
            var toFall = move.AddTransition(fall);
            toFall.hasExitTime = false;
            toFall.duration = 0.15f;
            toFall.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "IsGrounded");

            // JumpBegin → Fall (頂点を過ぎたら)
            var beginToFall = jumpBegin.AddTransition(fall);
            beginToFall.hasExitTime = false;
            beginToFall.duration = 0.15f;
            beginToFall.AddCondition(AnimatorConditionMode.Less, 0.0f, "VerticalVelocity");

            // Fall → Land (高所からの落下=ハード着地。硬直つき)
            var fallToLandHard = fall.AddTransition(land);
            fallToLandHard.hasExitTime = false;
            fallToLandHard.duration = 0.1f;
            fallToLandHard.AddCondition(AnimatorConditionMode.If, 0.0f, "IsGrounded");
            fallToLandHard.AddCondition(AnimatorConditionMode.If, 0.0f, "HardLand");

            // Fall → Move (移動しながらの通常着地はLandを跳ばして直接走りへ=着地滑り対策)
            var fallToMove = fall.AddTransition(move);
            fallToMove.hasExitTime = false;
            fallToMove.duration = 0.12f;
            fallToMove.AddCondition(AnimatorConditionMode.If, 0.0f, "IsGrounded");
            fallToMove.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "HardLand");
            fallToMove.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            // Fall → Land (静止状態での通常着地も着地モーションを再生。
            // こちらは硬直なしで、入力が来ればLand側の早期キャンセル遷移で即Moveへ)
            var fallToLandSoft = fall.AddTransition(land);
            fallToLandSoft.hasExitTime = false;
            fallToLandSoft.duration = 0.1f;
            fallToLandSoft.AddCondition(AnimatorConditionMode.If, 0.0f, "IsGrounded");
            fallToLandSoft.AddCondition(AnimatorConditionMode.IfNot, 0.0f, "HardLand");
            fallToLandSoft.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // Land → JumpBegin (バッファジャンプで即再ジャンプ)
            var landToJump = land.AddTransition(jumpBegin);
            landToJump.hasExitTime = false;
            landToJump.duration = 0.05f;
            landToJump.AddCondition(AnimatorConditionMode.If, 0.0f, "JumpTrigger");

            // Land → Move (着地中に動き出したら即切り上げる=着地滑り対策)
            var landToMoveBySpeed = land.AddTransition(move);
            landToMoveBySpeed.hasExitTime = false;
            landToMoveBySpeed.duration = 0.1f;
            landToMoveBySpeed.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            // Land → Move (静止したままなら着地モーションを短めに再生して戻る)
            var landToMove = land.AddTransition(move);
            landToMove.hasExitTime = true;
            landToMove.exitTime = 0.35f;
            landToMove.duration = 0.15f;

            // --- AnyState → Dead (空中・攻撃中でも死亡アニメを再生) ---
            foreach (var transition in rootSm.anyStateTransitions.ToArray())
            {
                if (transition.destinationState == dead)
                {
                    rootSm.RemoveAnyStateTransition(transition);
                }
            }

            var anyToDead = rootSm.AddAnyStateTransition(dead);
            anyToDead.hasExitTime = false;
            anyToDead.duration = 0.1f;
            anyToDead.canTransitionToSelf = false;
            anyToDead.AddCondition(AnimatorConditionMode.If, 0.0f, "DeadTrigger");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlayerAnimatorFixer] コントローラを再構成しました: " +
                "JumpBegin/Fall/Land追加、歩き落ち対応、AnyState→Dead、誤配線撤去");
        }

        static AnimatorState FindState(AnimatorStateMachine sm, string name)
        {
            foreach (var child in sm.states)
            {
                if (child.state.name == name) return child.state;
            }
            return null;
        }

        static void RemoveState(AnimatorStateMachine sm, string name)
        {
            var state = FindState(sm, name);
            if (state != null) sm.RemoveState(state);
        }

        static AnimationClip LoadClip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipFolder}/{name}.anim");
            if (clip == null)
            {
                Debug.LogWarning($"[PlayerAnimatorFixer] クリップが見つかりません: {name}");
            }
            return clip;
        }

        [InitializeOnLoadMethod]
        static void RunIfRequested()
        {
            if (!File.Exists(RequestMarkerPath)) return;

            EditorApplication.update += RunOnce;
        }

        static void RunOnce()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(RequestMarkerPath))
            {
                EditorApplication.update -= RunOnce;
                return;
            }

            EditorApplication.update -= RunOnce;
            File.Delete(RequestMarkerPath);

            try
            {
                Fix();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
