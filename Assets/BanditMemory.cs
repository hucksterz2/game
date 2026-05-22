using System.Collections.Generic;
using UnityEngine;

public enum PlayerAttackType : byte
{
    Combo1 = 0,
    Combo2 = 1,
    Combo3 = 2,
    Dash = 3,
    Plunge = 4
}

public struct ActionSnapshot
{
    public sbyte moveDir;
    public bool inAir;
    public bool recentDash;
    public bool recentJump;
    public bool wasBlocking;

    public byte ToKey()
    {
        byte b = 0;

        if (moveDir < 0) b |= 0b00 << 6;
        else if (moveDir == 0) b |= 0b01 << 6;
        else b |= 0b10 << 6;

        if (inAir) b |= 1 << 0;
        if (recentDash) b |= 1 << 1;
        if (recentJump) b |= 1 << 2;
        if (wasBlocking) b |= 1 << 3;
        return b;

    }
}

public static class BanditMemory
{
    private static Dictionary<PlayerAttackType, Dictionary<byte, float>> patternCounts 
        = new Dictionary<PlayerAttackType, Dictionary<byte, float>>();

    private static Dictionary<PlayerAttackType, float> typeTotals 
        = new Dictionary<PlayerAttackType, float>();

    private static List<(PlayerAttackType type, byte patternKey)> currentFightLog
        = new List<(PlayerAttackType, byte)>();

    public const int MinSamplesForLearning = 3;

    public static bool HasKnowledge { get; private set; }
    public static PlayerAttackType MostCommonAttack { get; private set; }
    public static byte MostCommonPatternKey { get; private set; }
    private static int totalFights = 0;

    public static void RecordPlayerAttack(PlayerAttackType type, ActionSnapshot snap)
    {
        currentFightLog.Add((type, snap.ToKey()));
    }

    public static void OnBanditDefeat()
    {
        ApplyDecay();
        foreach (var (type, key) in currentFightLog)
        {
            if (!patternCounts.ContainsKey(type))
                patternCounts[type] = new Dictionary<byte, float>();
            if (!patternCounts[type].ContainsKey(key))
                patternCounts[type][key] = 0;
            patternCounts[type][key]++;

            if (!typeTotals.ContainsKey(type)) typeTotals[type] = 0;
            typeTotals[type]++;
        }
        currentFightLog.Clear();
        totalFights++;

        float bestTypeCount = 0;
        PlayerAttackType bestType = PlayerAttackType.Combo1;
        foreach (var kv in typeTotals)
        {
            if (kv.Value > bestTypeCount)
            {
                bestTypeCount = kv.Value;
                bestType = kv.Key;
            }
        }

        if (bestTypeCount < MinSamplesForLearning) { HasKnowledge = false; return; }

        float bestPatternCount = 0;
        byte bestKey = 0;
        foreach (var kv in patternCounts[bestType])
        {
            if (kv.Value > bestPatternCount)
            {
                bestPatternCount = kv.Value;
                bestKey = kv.Key;
            }
        }

        MostCommonAttack = bestType;
        MostCommonPatternKey = bestKey;
        HasKnowledge = true;
    }

    public static bool ShouldParry(PlayerAttackType incomingType, ActionSnapshot snap)
    {
        if (!HasKnowledge) return false;
        if (incomingType != MostCommonAttack) return false;
        return snap.ToKey() == MostCommonPatternKey;
    }

    public static float GetParryConfidence(PlayerAttackType type, ActionSnapshot snap)
    {
        if (!HasKnowledge) return 0f;
        if (type != MostCommonAttack) return 0f;
        if (!patternCounts.ContainsKey(type)) return 0f;

        byte key = snap.ToKey();
        if (!patternCounts[type].ContainsKey(key)) return 0f;

        float hits = patternCounts[type][key];
        float total = typeTotals[type];

        float rawFreq = hits / total;
        float fightsFactor = Mathf.Min(1f, totalFights / 3f);

        return rawFreq * fightsFactor;
    }

    public const float DecayFactor = 0.7f;

    private static void ApplyDecay()
    {
        var typeKeys = new List<PlayerAttackType>(typeTotals.Keys);
        foreach (var type in typeKeys)
        {
            typeTotals[type] *= DecayFactor;

            var patternKeys = new List<byte>(patternCounts[type].Keys);
            foreach (var pKey in patternKeys)
            {
                patternCounts[type][pKey] *= DecayFactor;
                if (patternCounts[type][pKey] < 0.1f)
                    patternCounts[type].Remove(pKey);
            }
        }
    }

    public static void Reset()
    {
        patternCounts.Clear();
        typeTotals.Clear();
        currentFightLog.Clear();
        HasKnowledge = false;
        totalFights = 0;
    }
}
