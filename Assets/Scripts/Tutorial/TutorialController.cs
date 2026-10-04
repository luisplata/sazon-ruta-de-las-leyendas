using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scripted guided tutorial (Option A). Runs a 7-step ladder of
/// [text, objective, zone pulse, minHold, condition] records polled in Update:
/// each step blocks until its gameplay condition holds (plus a minimum hold),
/// then advances to the next narration. Built in Start — AFTER every Awake —
/// so HandController.CurrentLevel is already the tutorial's La Llorona level
/// (GameSession.tutorialLevel, design D1). The objective panel is built from
/// the level's star recipe. NO completion hook: the star-only recipe ends the
/// round through the stock win flow (ServeFood → WinRound → fade → next scene).
/// </summary>
public class TutorialController : MonoBehaviour
{
    /// <summary>Which zone to pulse when a step advances (visual hint).</summary>
    enum ZoneTarget { None, Center, Corner }

    /// <summary>Which gameplay condition gates a step (maps a StepConfig to a code check).</summary>
    enum StepType { Welcome, RoleColors, CookZone, TrashRefill, LogicalDish, GoalPark, Serve }

    /// <summary>One ladder rung: narration + objective + pulse + hold + gameplay condition.</summary>
    [Serializable]
    class Step
    {
        public string text;
        public string objective;
        public ZoneTarget pulse;
        public float minHold;
        public Func<bool> done;
        public StepType type;

        /// <summary>Show the no-refills hint text while this step is active (trash step).</summary>
        public bool noRefillsHint;
    }

    /// <summary>
    /// One Inspector-editable ladder rung. <see cref="objective"/> empty means
    /// "use the goal line" (_goalLine) — the default for the welcome and goal
    /// steps. <see cref="type"/> selects the gameplay condition in BuildSteps().
    /// </summary>
    [Serializable]
    class StepConfig
    {
        public string text;
        [Tooltip("Tiempo mínimo en pantalla (segundos) antes de poder avanzar")]
        public float minHold;
        [Tooltip("Dejar vacío usa el objetivo del plato (Objetivo: ...)")]
        public string objective;
        public ZoneTarget pulse;
        public bool noRefillsHint;
        public StepType type;
    }

    [SerializeField] HandController handController;
    [SerializeField] EnemyView enemyView;
    [SerializeField] DropZone centerZone;
    [SerializeField] DropZone cornerZone;
    [SerializeField] Image centerImage;
    [SerializeField] Image cornerImage;
    [SerializeField] TMP_Text objectiveLabel;
    [SerializeField] RecipeData goalRecipe;

    /// <summary>Editable step ladder (Inspector). Defaults to the 7 authored steps.</summary>
    [SerializeField] StepConfig[] steps = DefaultStepConfigs();

    readonly List<Step> _steps = new();
    string _goalLine;
    int _index;
    float _stepTime;
    bool _finished;

    // Fix WARNING 2 (trash-step softlock): trash park + REFILL dump are observed
    // at ANY step, not frame-locked to step 4. The corner drain edge (>=1 -> 0)
    // only ever happens via Refill() consuming the trash zone, so it is a
    // reliable "a refill was used" signal. maxRefillsPerGame is 2 in the shared
    // CardVisualConfig (tutorial scene uses it), mirrored here for the hint.
    const int MaxTutorialRefills = 2;
    const string NoRefillsHint = "Sin refills, usa la cocina";
    bool _sawTrash;
    bool _sawRefillDump;
    int _prevCornerCount;
    int _refillDumps;

    // Fix WARNING 3 (step-6 auto-advance): remember the dish instance present
    // when the goal set parked; step 6 only completes on a NEW serve after that.
    bool _goalParked;
    DishView _dishWhenGoalParked;

    // Patience beat (exploration plan 9): a Presented (blue) resolution observed
    // at ANY step latches (same global-observer pattern as ObserveZones) so the
    // LogicalDish step can never softlock — a blue cook before the step still
    // counts once the ladder reaches it.
    bool _sawPresented;

    // Zone gating (exploration plan 1): the cook (center) zone stays LOCKED
    // through steps 1-2 and unlocks when the CookZone step teaches it. Visual
    // hint while locked = dim the center zone image.
    float _centerBaseAlpha = 0.25f;
    bool _lockVisualApplied;

    /// <summary>Dimmed center-image alpha while the cook zone is locked (hint).</summary>
    const float LockedCenterAlpha = 0.12f;

    void Start()
    {
        if (handController == null || enemyView == null || centerZone == null || cornerZone == null) return;
        var level = handController.CurrentLevel;
        if (level == null || level.recipes == null || level.recipes.Count == 0) return;

        // Objective panel: the star goal, driven entirely by data (design D3).
        if (goalRecipe == null) goalRecipe = level.recipes[0];
        _goalLine = $"Objetivo: {goalRecipe.icon} {goalRecipe.dishName} = {IngredientNames()}";
        if (objectiveLabel != null) objectiveLabel.text = _goalLine;

        // Zone gating: the cook zone stays locked through steps 1-2 (only the
        // corner accepts); the CookZone step unlocks it. The dim is applied on
        // the first Update so DropZone.Start (which sets the zone color) has
        // already run.
        if (centerZone != null) centerZone.Locked = true;

        BuildSteps();
        enemyView.Say(_steps[_index].text);
    }

    void Update()
    {
        if (_finished || _steps.Count == 0) return;
        ObserveZones();
        ObservePresented();
        UpdateLockVisual();
        UpdateNoRefillsHint();
        _stepTime += Time.deltaTime;
        if (_steps[_index].done() && _stepTime >= _steps[_index].minHold)
        {
            if (_index == _steps.Count - 1) _finished = true; // final rung: panel stays
            else Advance();
        }
    }

    void Advance()
    {
        _stepTime = 0f;
        _index++;
        var s = _steps[_index];
        if (s.type == StepType.CookZone) UnlockCenterZone();
        if (s.type == StepType.LogicalDish) handController?.ClearCookQueue(); // clean-room: step-3 prop card must not pollute the blue match
        enemyView.Say(s.text);
        if (objectiveLabel != null) objectiveLabel.text = s.objective;
        StartCoroutine(PulseZone(s.pulse));
    }

    void BuildSteps()
    {
        // Defensive: if the Inspector ladder is missing/empty, fall back to the
        // authored defaults so the tutorial never breaks on missing data.
        var configs = steps != null && steps.Length > 0 ? steps : DefaultStepConfigs();
        _steps.Clear();
        foreach (var cfg in configs)
            if (cfg != null) _steps.Add(MakeStep(cfg));
    }

    /// <summary>
    /// Maps one Inspector StepConfig onto the internal step record, resolving
    /// the empty-objective goal-line fallback and the per-type gameplay check.
    /// </summary>
    Step MakeStep(StepConfig cfg)
    {
        Func<bool> done = cfg.type switch
        {
            // Step 2 is corner-only: the cook zone is LOCKED until the CookZone
            // step, so the condition reads the corner park only.
            StepType.RoleColors => () => cornerZone.Count >= 1,
            StepType.CookZone => () => centerZone.Count >= 1,
            StepType.TrashRefill => TrashThenRefill,
            StepType.LogicalDish => () => _sawPresented,
            StepType.GoalPark => GoalParked,
            StepType.Serve => ServedAfterGoalParked,
            _ => () => true // Welcome
        };
        return new Step
        {
            text = cfg.text,
            objective = string.IsNullOrEmpty(cfg.objective) ? _goalLine : cfg.objective,
            pulse = cfg.pulse,
            minHold = cfg.minHold,
            noRefillsHint = cfg.noRefillsHint,
            type = cfg.type,
            done = done
        };
    }

    /// <summary>
    /// The 7-step tutorial ladder authored here as editable defaults: same texts
    /// and timings as before (2.5s welcome, 0.4s the rest; steps 1 &amp; 6 use the
    /// goal-line fallback via empty objective). The Inspector overrides these.
    /// </summary>
    static StepConfig[] DefaultStepConfigs()
    {
        return new[]
        {
            new StepConfig
            {
                type = StepType.Welcome,
                text = "Bienvenido, cocinero. Soy la Llorona y tengo antojo de TACOS DE CHAPULINES. Te enseño a prepararlos.",
                minHold = 2.5f
            },
            new StepConfig
            {
                type = StepType.RoleColors,
                text = "Mira el color de cada carta: ROJO es base, VERDE es complemento y AMARILLO es sazón. Lleva cualquier carta a la ZONA ROJA (basura).",
                objective = "Arrastra una carta a la ZONA ROJA (basura)",
                minHold = 0.4f
            },
            new StepConfig
            {
                type = StepType.CookZone,
                text = "La ZONA AZUL es la COCINA: ahí se arma el plato con hasta 3 cartas.",
                objective = "Coloca una carta en la ZONA AZUL (cocina)",
                pulse = ZoneTarget.Center,
                minHold = 0.4f
            },
            new StepConfig
            {
                type = StepType.TrashRefill,
                text = "La ZONA ROJA es la BASURA. Tira una carta que no uses y pulsa REFILL para descartarla y llenar tu mano. ¡Los refills son limitados!",
                objective = "Tira una carta a la ZONA ROJA (basura) y pulsa REFILL",
                pulse = ZoneTarget.Corner,
                minHold = 0.4f,
                noRefillsHint = true
            },
            new StepConfig
            {
                type = StepType.LogicalDish,
                text = "¡NO cocines los tacos todavía! Cocina un plato AZUL (una receta de otra región) y verás cómo baja mi paciencia. Fácil: ELOTE ASADO = 1 sola carta de MAÍZ, o ESQUITES = Maíz + Cebolla. Evita la comida cruda.",
                objective = "Cocina un plato AZUL (no el objetivo) en la cocina",
                pulse = ZoneTarget.Center,
                minHold = 0.4f
            },
            new StepConfig
            {
                type = StepType.GoalPark,
                text = "Ahora arma el plato que te pedí. Solo se cocinan 3 cartas exactas.",
                minHold = 0.4f
            },
            new StepConfig
            {
                type = StepType.Serve,
                text = "¡Perfecto! Cuando el plato esté listo, pulsa PREPARAR para cocinarlo.",
                objective = "Pulsa PREPARAR",
                minHold = 0.4f
            }
        };
    }

    /// <summary>
    /// Every-frame zone observer (fix WARNING 2): tracks trash parks and REFILL
    /// dumps globally so step 4 is not frame-locked to a park landing exactly
    /// while it is active. A corner drain (count >= 1 -> 0) only ever happens
    /// via Refill() consuming the trash zone (PrepareFood ignores the corner,
    /// StartDeal only clears at game start), so it is a reliable "refill used"
    /// signal.
    /// </summary>
    void ObserveZones()
    {
        if (cornerZone == null) return;
        if (cornerZone.Count >= 1) _sawTrash = true;
        if (_prevCornerCount >= 1 && cornerZone.Count == 0)
        {
            _refillDumps++;
            _sawRefillDump = true;
        }
        _prevCornerCount = cornerZone.Count;
    }

    /// <summary>
    /// Every-frame Presented (blue) observer (patience step): latches when the
    /// last cooked dish resolved to a logical dish from another region. The
    /// same global-observer pattern as <see cref="ObserveZones"/> — a blue cook
    /// at ANY step counts, so the LogicalDish step can never softlock.
    /// </summary>
    void ObservePresented()
    {
        if (handController != null && handController.LastResolutionKind == RecipeKind.Presented)
            _sawPresented = true;
    }

    /// <summary>
    /// One-shot lock visual (applied on the first Update so DropZone.Start,
    /// which sets the zone image color from the config, has already run): while
    /// the cook zone is locked the center image is dimmed as a visual hint.
    /// </summary>
    void UpdateLockVisual()
    {
        if (_lockVisualApplied || centerImage == null) return;
        _lockVisualApplied = true;
        _centerBaseAlpha = centerImage.color.a;
        if (centerZone != null && centerZone.Locked) SetCenterAlpha(LockedCenterAlpha);
    }

    /// <summary>Unlocks the cook zone and restores the center image alpha (CookZone step).</summary>
    void UnlockCenterZone()
    {
        if (centerZone != null) centerZone.Locked = false;
        if (centerImage != null) SetCenterAlpha(_centerBaseAlpha);
    }

    void SetCenterAlpha(float a)
    {
        var c = centerImage.color;
        c.a = a;
        centerImage.color = c;
    }

    /// <summary>
    /// Step 4: a trash park was seen AND a REFILL dump was seen, at ANY step.
    /// Burning refills before step 4 still means the dumps happened, so the step
    /// can never softlock — every spent refill requires a trash park + dump.
    /// </summary>
    bool TrashThenRefill() => _sawTrash && _sawRefillDump;

    /// <summary>
    /// Fix WARNING 2 hint: while the trash step is active and both refills are
    /// spent, swap the objective for a hint that the corner is closed. Defensive
    /// fallback — with the global park/dump tracker the step normally completes
    /// on the last dump before this can display.
    /// </summary>
    void UpdateNoRefillsHint()
    {
        if (objectiveLabel == null || _index >= _steps.Count) return;
        if (!_steps[_index].noRefillsHint) return;
        if (_refillDumps >= MaxTutorialRefills)
            objectiveLabel.text = NoRefillsHint;
    }

    /// <summary>
    /// Step 6: the center cook queue is exactly the goal recipe's ingredient set.
    /// On the rising edge it records the dish present at parking time so step 7
    /// can require a serve that happens AFTER the goal was parked.
    /// </summary>
    bool GoalParked()
    {
        if (goalRecipe == null || goalRecipe.ingredients == null || goalRecipe.ingredients.Count == 0) return false;
        if (centerZone.Count != goalRecipe.ingredients.Count) return false;
        var set = new HashSet<CardData>();
        foreach (var c in centerZone.Held)
            if (c != null && c.Data != null) set.Add(c.Data);
        bool parked = set.SetEquals(goalRecipe.ingredients);
        if (parked && !_goalParked)
        {
            _goalParked = true;
            _dishWhenGoalParked = handController != null ? handController.LiveDish : null;
        }
        return parked;
    }

    /// <summary>
    /// Step 7 (fix WARNING 3): a NEW serve happened after the goal set parked.
    /// Every PREPARAR spawns a fresh DishView instance, so instance inequality
    /// vs the dish present at parking time proves the player served AFTER
    /// parking. A wrong dish cooked BEFORE parking stays the same instance and
    /// does NOT auto-complete the step.
    /// </summary>
    bool ServedAfterGoalParked()
    {
        if (!_goalParked) return false;
        var dish = handController != null ? handController.LiveDish : null;
        return dish != null && !ReferenceEquals(dish, _dishWhenGoalParked);
    }

    string IngredientNames()
    {
        var names = new List<string>();
        if (goalRecipe != null && goalRecipe.ingredients != null)
            foreach (var ing in goalRecipe.ingredients)
                if (ing != null) names.Add(ing.displayName);
        return string.Join(" + ", names);
    }

    /// <summary>Flashes the zone image alpha so the player's eye finds the target zone.</summary>
    IEnumerator PulseZone(ZoneTarget target)
    {
        Image img = target == ZoneTarget.Center ? centerImage
                 : target == ZoneTarget.Corner ? cornerImage : null;
        if (img == null) yield break;
        float baseA = img.color.a;
        for (int i = 0; i < 3; i++)
        {
            yield return LerpAlpha(img, baseA, 0.9f, 0.22f);
            yield return LerpAlpha(img, 0.9f, baseA, 0.22f);
        }
    }

    IEnumerator LerpAlpha(Image img, float from, float to, float duration)
    {
        Color c = img.color;
        for (float p = 0f; p < 1f; p += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(p));
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}