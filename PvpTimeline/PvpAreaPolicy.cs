namespace PvpTimeline;

internal static class PvpAreaPolicy
{
    // Use the current instance's duty ID. TerritoryType's default duty is zero for
    // several CC maps and cannot distinguish ranked, casual and custom matches.
    // Also require a known territory so a stale runtime duty cannot enable PvE.
    public static bool IsEligible(uint territory, uint currentDuty) => territory switch
    {
        250 => true,
        376 or 1273 => currentDuty==127,
        431 => currentDuty==130,
        554 => currentDuty==180,
        888 => currentDuty==701,
        1313 => currentDuty==1080,
        729 => currentDuty==277,
        791 => currentDuty==599,
        1032 => currentDuty is 835 or 838 or 841 or 847 or 850 or 853 or 856 or 859,
        1033 => currentDuty is 836 or 839 or 842 or 848 or 851 or 854 or 857 or 860,
        1034 => currentDuty is 837 or 840 or 843 or 849 or 852 or 855 or 858 or 861,
        1116 => currentDuty is >=912 and <=922,
        1138 => currentDuty is >=967 and <=977,
        1293 => currentDuty is >=1046 and <=1056,
        1357 => currentDuty is >=1102 and <=1112,
        1058 => currentDuty==862,
        1059 => currentDuty==863,
        1060 => currentDuty==864,
        1117 => currentDuty==923,
        1139 => currentDuty==978,
        1294 => currentDuty==1057,
        1358 => currentDuty==1113,
        _ => false
    };

    // ContentFinderCondition/TerritoryType rows checked against xivapi's English
    // datamining sheets, 2026-11-12. Keep the old Secure territory for older clients.
    // This display-only catalog does not expand the separate recorder's scope.
    internal static bool IsMatchTerritory(uint territory) => territory is
        376 or 1273 or 431 or 554 or 888 or 1313 or // Frontline
        729 or 791 or // Astragalos / Hidden Gorge
        1032 or 1033 or 1034 or 1116 or 1138 or 1293 or 1357 or // CC
        1058 or 1059 or 1060 or 1117 or 1139 or 1294 or 1358; // CC custom
}
