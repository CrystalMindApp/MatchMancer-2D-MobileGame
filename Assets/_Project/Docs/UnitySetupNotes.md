# MatchMancer Unity Setup Notes

## Core Scene Object Setup

- `StageSession` is a static runtime class. Do not attach it to a GameObject.
- `StageProgression` is a static runtime class. Do not attach it to a GameObject.
- Runtime progression is currently memory-only. Stopping Play Mode resets progression.
- Home/Stage Select requires one active `StageSelectPanel` in the scene.
- MainGame requires `GameManager`, `BoardManager`, `GameHUD`, player/enemy actor objects, and scene audio setup.

## Stage Select Setup

### StageSelectPanel

- Type: `MonoBehaviour`.
- Attach to: the Home scene Stage Select container, usually the parent of all stage buttons.
- Required Inspector refs:
  - `stageButtons`, unless every `StageSelectButton` is a child of this panel.
- Optional Inspector refs:
  - None.
- Important timing fields:
  - `transitionStartDelay`
  - `initialUnlockStartDelay`
  - `starRevealDelay`
  - `starPopScale`
  - `starPopDuration`
  - `postStarRevealDelay`
  - `lockShakeDuration`
  - `lockShakeStrength`
  - `postLockShakeDelay`
  - `postUnlockSpriteDelay`
  - `lockFadeDuration`
  - `postUnlockDelay`
- If missing:
  - Stage buttons can still load stages, but first unlock, star reveal, and next-stage unlock animations will not run.
- Test:
  - Fresh Play should show Stage 1 lock, shake, unlock sprite, fade, then enable Stage 1.
  - Returning after a win should reveal stars, pause, then unlock the next stage.

### StageSelectButton

- Type: `MonoBehaviour`.
- Attach to: each stage button object.
- Required Inspector refs:
  - `stageIndex`
  - `stageDefinition`
  - `button`
  - `starObjects`
  - `lockImage`
  - `lockCanvasGroup`
  - `lockedSprite`
  - `unlockedSprite`
- Optional Inspector refs:
  - `sceneAudioLibrary`; auto-finds `SceneAudioLibrary.Current` if missing.
- Removed old fallback fields:
  - `lockedVisual`
  - `lockedVisualCanvasGroup`
  - `lockedVisualGraphic`
  - `unlockVisual`
- Supported lock setup:
  - Use one `lockImage`.
  - Swap between `lockedSprite` and `unlockedSprite`.
  - Fade through `lockCanvasGroup`.
- Indexing:
  - Player-facing Stage 1 uses backend `stageIndex = 0`.
  - Player-facing Stage 2 uses backend `stageIndex = 1`.
- If missing:
  - Missing `button`: interactability cannot be controlled.
  - Missing `stageDefinition`: click logs a warning and does not load.
  - Missing `starObjects`: saved stars and star reveal are invisible.
  - Missing `lockImage`: unlock shake/sprite/fade cannot play.
  - Missing `lockCanvasGroup`: fade cannot control lock alpha.
  - Missing sprites: lock may show stale or unchanged art.
- Test:
  - Locked stages should not be clickable.
  - Unlocked stages should only become clickable after unlock visual completes.
  - Old completed stages should show saved stars immediately.

## Gameplay Scene Setup

### BoardManager

- Type: `MonoBehaviour`.
- Attach to: board root/controller object in MainGame.
- Required Inspector refs:
  - `tilePrefab`
  - `boardRoot`
  - `inputCamera`
  - `gameManager`
- Optional Inspector refs:
  - `comboTextController`
  - `sceneAudioLibrary`
- If missing:
  - Missing tile prefab/root breaks board generation.
  - Missing combo/audio refs disables only related feedback.
- Test:
  - Initial board has no matches and at least one valid move.
  - Swap, invalid swap-back, clear, gravity, refill, cascade, and special tile creation remain synced.

### GameManager

- Type: `MonoBehaviour`.
- Attach to: main gameplay controller object in MainGame.
- Required Inspector refs:
  - `playerActor`
  - `enemyActor`
  - `boardManager`
  - `gameHUD`
- Optional Inspector refs:
  - `damagePopupController`
  - `tinyImpulse`
  - debug/direct-play `enemyRoundSequence`
- Important settings:
  - `enemySkillCooldownTurns` controls enemy readiness pacing.
  - `enemyRoundSequence` is only a direct-play fallback when no `StageDefinition` is selected.
- If missing:
  - Missing actors/board/HUD prevents normal gameplay setup.
  - Missing optional polish refs disables only that polish.
- Test:
  - Win/loss result appears.
  - Retry reloads current stage.
  - Back Home clears selected stage.
  - Enemy readiness does not trigger special on the same turn it fills.

### GameHUD

- Type: `MonoBehaviour`.
- Attach to: gameplay HUD Canvas object.
- Required Inspector refs:
  - `gameManager`
  - HP texts or fill images as desired
  - result panel/title/description/buttons for result flow
  - active skill blocker/button if active skill UI is used
- Optional Inspector refs:
  - `enemyIntentUIController`
  - `sceneAudioLibrary`
  - `resultPanelTransition`
  - restart button; global settings panel can own restart instead.
- If missing:
  - Missing optional text/images hides that UI only.
  - Missing result refs prevents visible result panel, but game state still changes.
- Test:
  - HP text/fills update.
  - Passive bar updates.
  - Active skill blocker fill updates.
  - Result stars show on win and hide on loss.

## Audio Setup

### AudioManager

- Type: `MonoBehaviour`.
- Attach to: one persistent audio object.
- Required refs:
  - BGM/SFX audio sources if not auto-created by current prefab setup.
- Test:
  - BGM crossfades between scenes.
  - Sliders and mute work through global settings.

### SceneAudioLibrary

- Type: `MonoBehaviour`.
- Attach to: one scene-owned object in each scene, often `SceneAudioLibrary`.
- Required refs:
  - Assign scene BGM if the scene should request BGM.
  - Assign button/panel SFX if UI SFX are desired.
  - Assign combo, failed swap, special spawn, win, and lose SFX for gameplay scenes.
- Current note:
  - This class currently holds both scene/UI audio and gameplay SFX. A future `GameplayAudioLibrary` split would reduce overlap, but no split has been implemented yet.
- If missing:
  - Gameplay continues, but scene BGM and local SFX calls are silent.
- Test:
  - Stage select click SFX plays.
  - Combo SFX escalates by cascade step.
  - Failed swap SFX plays on invalid swap.
  - Special spawn SFX plays for match-created specials and passive-created Bombs.

## Actor Setup

### PlayerActor

- Type: `MonoBehaviour`.
- Attach to: player actor object.
- Required refs:
  - `characterData`
  - `combatProfile`
- Optional refs:
  - `visualController`
  - `turnScaleHighlighter`
  - `combatMotionController`
  - `damagePopupAnchor`
- If missing:
  - Missing data refs breaks combat setup.
  - Missing visual refs disables only visual polish.
- Test:
  - HP initializes.
  - active skill gauge and passive stack reset on restart.
  - attack/get-hit/skill visuals play if assigned.

### EnemyActor

- Type: `MonoBehaviour`.
- Attach to: enemy actor object.
- Required refs:
  - fallback `characterData`
  - fallback `combatProfile`
- Optional refs:
  - `visualController`
  - `turnScaleHighlighter`
  - `combatMotionController`
  - `damagePopupAnchor`
- If using enemy round sequence:
  - `EnemyDefinition` applies runtime character/combat/visual data to the same scene `EnemyActor`.
- Test:
  - Enemy swaps definitions across rounds.
  - Enemy HP resets per round.
  - Player HP/board/skill gauge persist across enemy rounds.

## ScriptableObject Setup

### CharacterData

- Stores shared actor stats:
  - name
  - max HP
  - base speed
  - attack multiplier
  - base attack damage
  - character type

### PlayerCombatProfile

- Stores player-side tile effect and skill settings:
  - base damage per tile
  - red crit
  - green heal
  - yellow speed
  - blue gauge
  - active skill
  - passive skill

### EnemyCombatProfile

- Stores enemy-side behavior chances:
  - disrupt chance
  - curse chance
  - crit chance/multiplier
  - self-heal chance/amount
  - skill announcement text
- Cleanup note:
  - `enemyActionDelay` / `EnemyActionDelay` currently appears unused by runtime turn flow. Do not remove until enemy profile assets are reviewed.

### EnemyDefinition

- Links:
  - `CharacterData`
  - `EnemyCombatProfile`
  - optional idle/attack/skill/get-hit sprites

### EnemyDatabase

- Data container for enemy definitions.
- Current main round flow uses `StageDefinition.enemyRoundSequence`; database is available for future selection tools.

### StageDefinition

- Stores:
  - stage id/name/description
  - enemy round sequence
  - optional background/foreground sprites
  - optional stage BGM

## Deprecated Fields / Safe Cleanup Candidates

- `StageSelectButton.unlockVisual`
  - Removed.
- `StageSelectButton.lockedVisual`
- `StageSelectButton.lockedVisualCanvasGroup`
- `StageSelectButton.lockedVisualGraphic`
  - Removed.
  - Stage Select now has one supported lock visual path only: `lockImage`, `lockCanvasGroup`, `lockedSprite`, `unlockedSprite`.
- `GameHUD.restartButton`
  - Likely optional now that `GlobalSettingsPanel` owns restart. Keep until MainGame HUD prefab is confirmed migrated.
- `EnemyCombatProfile.enemyActionDelay`
  - Appears unused by runtime flow. Keep until enemy data assets and intended behavior are reviewed.
- `SceneAudioLibrary.PlayMatchClear`
  - Currently retained as fallback/legacy API while combo SFX migration is still recent.

## Playtest Checklist After Setup Changes

- Fresh Home Play:
  - Stage 1 lock is visible.
  - Stage 1 unlock waits, shakes, swaps sprite, fades, then becomes clickable.
- Win Stage 1:
  - Stage 1 stars reveal one by one.
  - Stage 2 unlock waits, shakes, swaps sprite, fades, then becomes clickable.
- Lose Stage 1:
  - No new stars reveal.
  - No new stage unlocks.
- Gameplay:
  - board input is blocked during swaps/resolves/enemy actions.
  - combo SFX escalates by cascade step.
  - failed swap SFX plays.
  - special spawn SFX plays for match-created specials and passive Bombs.
  - result panel retry/back home works.
- Actors:
  - damage popups appear if configured.
  - tiny camera impulse triggers only if configured.
  - combat motion does not conflict with turn scale highlight.
