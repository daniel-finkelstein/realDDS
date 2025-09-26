using Shin_Megami_Tensei_Models;
using Shin_Megami_Tensei_View;

namespace Shin_Megami_Tensei;

public static partial class CombatLogic
{
    private readonly record struct ActionContext(
        View View,
        Unit Actor,
        string AttackingSamuraiName,
        string AttackerTag,
        Team AttackingTeam,
        Team DefendingTeam);

    // Firma definitiva: (Full perdido, Blink ganado, Blink perdido extra)
    private enum ActionKind
    {
        Pass,
        Attack,
        Skill,
        Summon,
        Surrender
    };
    private readonly record struct ActionEffect(int FullTurnsLost, int BlinkTurnsGained, int BlinkTurnsLost, ActionKind Kind);

    
    private static ActionEffect ActionLoop(in ActionContext actionContext)
    {
        for (int i = 0; ; i++)
        {
            if (i > 0) WriteSeparator(actionContext.View);

            ShowActionMenu(actionContext.View, actionContext.Actor);

            int selectedAction = ReadMenuInput(actionContext.View);
            AfterSelectionSeparator(actionContext.View);

            if (TryHandleActionSelection(in actionContext, selectedAction, out var effectOnTurns))
                return effectOnTurns; // ← devolver el efecto (no solo break)
        }
    }

    private static bool TryHandleActionSelection(in ActionContext actionContext, int actionSelection, out ActionEffect effectOnTurns)
    {
        return IsSamurai(actionContext.Actor)
            ? HandleSamuraiSelection(in actionContext, actionSelection, out effectOnTurns)
            : HandleNonSamuraiSelection(in actionContext, actionSelection, out effectOnTurns);
    }

    private static bool ActionIsSurrender(int selectedAction) => selectedAction == 6;
    private static bool ActionIsPassSamurai(int selectedAction) => selectedAction == 5;
    private static bool ActionIsUseSkillSamurai(int selectedAction) => selectedAction == 3;
    private static bool ActionIsAttackOrShootSamurai(int selectedAction) => selectedAction == 1 || selectedAction == 2;
    private static bool ActionIsShoot(int selectedAction) => selectedAction == 2;

    private static bool ActionIsPassNonSamurai(int selectedAction) => selectedAction == 4;
    private static bool IsUseSkillNonSamurai(int selectedAction) => selectedAction == 2;
    private static bool IsAttackOnly(int selectedAction) => selectedAction == 1;

    private static bool HandleSamuraiSelection(in ActionContext actionContext, int selectedAction, out ActionEffect effectOnTurns)
    {
        if (ActionIsSurrender(selectedAction))       return HandleSurrender(actionContext, out effectOnTurns);
        if (ActionIsPassSamurai(selectedAction))     return HandlePassTurn(actionContext.View, out effectOnTurns);
        if (ActionIsUseSkillSamurai(selectedAction)) return HandleUseSkill(actionContext.View, actionContext.Actor, out effectOnTurns);
        if (ActionIsAttackOrShootSamurai(selectedAction)) return HandleAttackOrShoot(actionContext, ActionIsShoot(selectedAction), out effectOnTurns);
        effectOnTurns = default; return true;
    }

    private static bool HandleNonSamuraiSelection(in ActionContext actionContext, int selectedAction, out ActionEffect effectOnTurns)
    {
        if (ActionIsPassNonSamurai(selectedAction)) return HandlePassTurn(actionContext.View, out effectOnTurns);
        if (IsUseSkillNonSamurai(selectedAction))   return HandleUseSkill(actionContext.View, actionContext.Actor, out effectOnTurns);
        if (IsAttackOnly(selectedAction))           return HandleAttackOrShoot(actionContext, false, out effectOnTurns);
        effectOnTurns = default; return true;
    }

    // Usar habilidad: imprime solo “usa …”; en E1 costo base = 1 Full, sin blink extra
    private static bool HandleUseSkill(View view, Unit attackingUnit, out ActionEffect effect)
    {
        var usable = GetUsableSkills(attackingUnit);
        var chosen = SelectSkill(view, attackingUnit, usable);
        if (SkillUseIsCancelled(chosen)) { effect = default; return false; }

        PerformSkillUse(view, attackingUnit, chosen!); // NO imprime consumo aquí
        effect = new ActionEffect(
            FullTurnsLost: 1,
            BlinkTurnsGained: 0,
            BlinkTurnsLost: 0,
            Kind: ActionKind.Skill
        );
        return true;
    }

    private static List<Skill> GetUsableSkills(Unit attackingUnit)
    {
        var attackingUnitSkills = attackingUnit.Skills ?? new List<Skill>();
        var usableSkillsForAttack = new List<Skill>(attackingUnitSkills.Count);
        for (int i = 0; i < attackingUnitSkills.Count; i++)
            if (CanAffordSkill(attackingUnit, attackingUnitSkills[i])) usableSkillsForAttack.Add(attackingUnitSkills[i]);
        return usableSkillsForAttack;
    }

    private static bool CanAffordSkill(Unit attackingUnit, Skill skill) =>
        skill.Cost <= attackingUnit.Stats.ManaPoints;

    private static bool SkillUseIsCancelled(Skill? chosenSkill) => chosenSkill is null;

    // ❌ Quita el ReportTurnConsumption de aquí
    private static void PerformSkillUse(View view, Unit attackingUnit, Skill chosenSkill)
    {
        ShowUseSkillMessage(view, attackingUnit, chosenSkill);
    }

    private static Skill? SelectSkill(View view, Unit attackingUnit, List<Skill> usableSkillsForAttack)
    {
        ShowSkillPromptHeader(view, attackingUnit);
        if (NoUsableSkills(usableSkillsForAttack)) return ShowAndCancel(view);
        ShowSkillOptions(view, usableSkillsForAttack);
        return ReadAndMapSkillChoice(view, usableSkillsForAttack);
    }

    private static bool NoUsableSkills(List<Skill> usableSkillsForAttack) => usableSkillsForAttack.Count == 0;

    private static Skill? ShowAndCancel(View view)
    {
        view.WriteLine("1-Cancelar");
        ReadIndexAllowCancel(view, 1);
        return null;
    }

    private static Skill? ReadAndMapSkillChoice(View view, List<Skill> usableSkillsForAttack)
    {
        int cancelIndex = usableSkillsForAttack.Count + 1;
        int choice = ReadIndexAllowCancel(view, cancelIndex);
        if (choice == cancelIndex) return null;

        int index = Math.Clamp(choice - 1, 0, usableSkillsForAttack.Count - 1);
        return usableSkillsForAttack[index];
    }

    // ⛳ Surrender: termina batalla; puedes no tocar contadores, pero por consistencia consumimos 1 turno base
    private static bool HandleSurrender(ActionContext actionContext, out ActionEffect effect)
    {
        actionContext.View.WriteLine($"{actionContext.AttackingSamuraiName} ({actionContext.AttackerTag}) se rinde");
        DefeatTeam(actionContext.AttackingTeam);

        // Antes lo dejaste con FullTurnsLost: 1 → eso imprime consumo
        effect = new ActionEffect(
            FullTurnsLost: 0,
            BlinkTurnsGained: 0,
            BlinkTurnsLost: 0,
            Kind: ActionKind.Surrender
        );
        return true;
    }

    
    private static bool HandlePassTurn(View view, out ActionEffect effectOnTurns)
    {
        effectOnTurns = new ActionEffect(
            FullTurnsLost: 1,
            BlinkTurnsGained: 1,
            BlinkTurnsLost: 0,
            Kind:  ActionKind.Pass
        );
        return true;
    }

    // Atacar / Disparar: base 1; sin blink extra (E1)
    private static bool HandleAttackOrShoot(ActionContext actionContext, bool actionIsShoot, out ActionEffect effectOnTurns)
    {
        Unit targetUnit = SelectTarget(actionContext.View, actionContext.Actor.Name, actionContext.DefendingTeam);
        if (targetUnit is null) { effectOnTurns = default; return false; }

        ResolveHit(actionContext, targetUnit, actionIsShoot); // NO imprime consumo aquí
        effectOnTurns = new ActionEffect(
            FullTurnsLost: 1,
            BlinkTurnsGained: 0,
            BlinkTurnsLost: 0,
            Kind: ActionKind.Attack
        );
        return true;
    }

    private static Unit? SelectTarget(View view, string attackingUnitName, Team defendingTeam)
    {
        var opposingTeamUnits = BuildTargetOptions(defendingTeam);
        if (CheckNoTargets(opposingTeamUnits)) return null;
        ShowTargetPrompt(view, attackingUnitName, opposingTeamUnits);
        return MapChoiceToTarget(view, opposingTeamUnits);
    }

    private static List<Unit> BuildTargetOptions(Team team)
    {
        var opponentUnits = new List<Unit>(4);
        var slots = GetTeamSlots(team);
        for (int i = 0; i < 4; i++)
            if (slots[i]?.Stats.HealthPoints > 0) opponentUnits.Add(slots[i]!);
        return opponentUnits;
    }

    private static bool CheckNoTargets(List<Unit> oposingTeamUnits) => oposingTeamUnits.Count == 0;

    private static void ShowTargetPrompt(View view, string attackingUnitName, List<Unit> opposingTeamUnits)
    {
        TargetHeader(view, attackingUnitName);
        PrintTargetOptions(view, opposingTeamUnits);
        PrintCancel(view, opposingTeamUnits.Count + 1);
    }

    private static void PrintTargetOptions(View view, List<Unit> opposingTeamUnits)
    {
        for (int i = 0; i < opposingTeamUnits.Count; i++) TargetLine(view, i, opposingTeamUnits[i]);
    }

    private static Unit? MapChoiceToTarget(View view, List<Unit> opponentUnits)
    {
        int cancel = opponentUnits.Count + 1;
        int choice = ReadIndexAllowCancel(view, cancel);
        return IsTargetCancel(choice, cancel) ? null : PickTarget(opponentUnits, choice);
    }

    private static bool IsTargetCancel(int choice, int cancel) => choice == cancel;

    private static Unit PickTarget(List<Unit> opts, int choice) => opts[choice - 1];

    // ❌ Quita el ReportTurnConsumption de aquí
    private static void ResolveHit(ActionContext actionContext, Unit target, bool actionIsShot)
    {
        int damage = ComputeBasicDamage(actionContext.Actor, actionIsShot);
        ApplyDamage(actionContext.DefendingTeam, target, damage);
        ReportAttack(actionContext, target, damage, actionIsShot);
    }

    private static int ReadMenuInput(View view) =>
        ReadBoundedOrDefault(view, 1, 6, 1);

    private static int ReadIndexAllowCancel(View view, int maxInclusive) =>
        ReadBoundedOrDefault(view, 1, maxInclusive, 1);

    private static int ReadBoundedOrDefault(View view, int min, int max, int @default) =>
        ParseBoundedOrDefault(view.ReadLine(), min, max, @default);

    private static int ParseBoundedOrDefault(string? selectionInput, int min, int max, int @default)
    {
        if (!int.TryParse(selectionInput, out var selectionInputInt)) return @default;
        return (selectionInputInt < min || selectionInputInt > max) ? @default : selectionInputInt;
    }
}
