using System;
using System.Collections.Generic;

namespace Shin_Megami_Tensei_Models;

public sealed class AffinityProfile
{
    private readonly Dictionary<Element, Affinity> _map;

    public AffinityProfile(Dictionary<Element, Affinity>? map = null)
        => _map = map ?? new Dictionary<Element, Affinity>();

    public Affinity Get(Element element)
        => _map.TryGetValue(element, out var a) ? a : Affinity.Neutral;

    public static AffinityProfile NeutralAll => new();

    public static Affinity FromCode(string code) => code switch
    {
        "Wk" => Affinity.Weak,
        "Rs" => Affinity.Resist,
        "Nu" => Affinity.Null,
        "Rp" => Affinity.Repel,
        "Dr" => Affinity.Drain,
        "-"  => Affinity.Neutral,
        _    => Affinity.Neutral
    };
}