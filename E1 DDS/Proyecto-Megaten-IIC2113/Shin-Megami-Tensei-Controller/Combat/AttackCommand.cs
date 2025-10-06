using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei.Combat;

public sealed record AttackCommand(Unit Attacker, Unit Target, AttackKind Kind);