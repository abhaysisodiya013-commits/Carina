# Chaser Goat Enemy AI Plan (Corgi Engine)

Based on the `Chaser_Goat` sprite sheet, the enemy AI will use a complex `AIBrain` from the Corgi Engine. The animations indicate a multi-phase movement and attack system.

## 1. AI Brain States

The AIBrain will transition between the following key states, matching the provided animation names:

### State: Idle
* **Animations**: `Idle`
* **Actions**: `AIActionDoNothing` or `AIActionPatrol` (with a slow wait).
* **Transitions**:
  - If Target Spotted -> Transition to **RunStart** or **RunLoop**.

### State: Move (Chase)
* **Animations**: `Run_Start`, `Run_Loop`, `Run_End`, `Run_End_to_Idle`
* **Actions**: `AIActionMoveTowardsTarget2D` (for chasing the player).
* **Transitions**:
  - If Target in Melee Range -> Transition to **BasicAttackStart**.
  - If Target in Mid/Jump Range -> Transition to **JumpAttack**.
  - If Target lost -> Transition to **RunEnd** -> **Idle**.

### State: Basic Attack
* **Animations**: `Basic_Attack_Start`, `Basic_Attack_Loop`, `Basic_Attack_Finisher`, `Basic_Attack_Loop_to_Idle`
* **Actions**: `AIActionMeleeAttack` (with combo timings synced to the animation loop and finisher).
* **Transitions**:
  - After Finisher completes -> Transition to **Idle**.
  - If Player dodges/attacks during loop -> Transition to **DodgeAttack**.

### State: Jump Attack
* **Animations**: `Run_to_Jump_Attack`
* **Actions**: `AIActionJump` & `AIActionMeleeAttack` or a custom `AIActionDashAttack`.
* **Transitions**:
  - Upon Landing/Attack finish -> Transition to **Idle** or **RunLoop**.

### State: Dodge & Counter
* **Animations**: `Dodge_Attack`
* **Actions**: Custom `AIActionDodge` (moving backward quickly with invincibility frames), followed immediately by a quick attack hitbox.
* **Transitions**:
  - After dodge -> Transition to **Idle** or **Move**.

### State: Hit & Death
* **Animations**: `Hit`, `Death`
* **Actions**: `CharacterDamage` handles `Hit`, triggering the hit animation. `Health` handles `Death`, transitioning brain to a dead state.

## 2. Recommended Corgi Engine Components

To bring this plan to life, you will need to set up the following on the `Chaser_Goat` prefab:

1. **AIBrain**: The core state machine.
2. **AIDecisionTargetIsAlive / DetectTargetRadius2D**: To spot the player and trigger chases.
3. **AIDecisionDistanceToTarget**: To choose between `Basic_Attack` (close range) and `Run_to_Jump_Attack` (mid/long range).
4. **CharacterRun / CharacterMovement**: To handle the running loop and acceleration.
5. **CharacterJump / CharacterDash**: For the `Run_to_Jump_Attack` and `Dodge_Attack` maneuvers.
6. **DamageOnTouch / MeleeWeapon**: Attached to hitboxes activated via animation events during `Basic_Attack_Loop`, `Basic_Attack_Finisher`, and `Run_to_Jump_Attack`.

## 3. Animator Setup (Transitions)
You will need an Animator Controller with blend trees or trigger-based transitions for the complex animation chains:
- `Run_Start` -> `Run_Loop` -> `Run_End` -> `Run_End_to_Idle`
- `Basic_Attack_Start` -> `Basic_Attack_Loop` -> `Basic_Attack_Finisher` -> `Basic_Attack_Loop_to_Idle`