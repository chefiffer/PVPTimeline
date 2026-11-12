namespace PvpSkillLab;

/// <summary>纯状态策略：同地图手动停止/故障/结算后不立即重启，换地图后重新允许自动采集。</summary>
internal sealed class RecordingPolicy
{
    public uint Territory { get; private set; } = uint.MaxValue;
    public bool Suppressed { get; private set; }
    public bool Enter(uint territory)
    {
        if (territory == Territory) return false;
        Territory = territory; Suppressed = false; return true;
    }
    public void Pause() => Suppressed = true;
    public void Resume() => Suppressed = false;
    public bool ShouldStart(bool enabled, bool loggedIn, uint duty) => enabled && loggedIn && !Suppressed && IsEligible(Territory, duty);
    // 与主采集器 GameLayout 的 ContentFinderCondition 白名单一致，不引用主插件 DLL。
    // TerritoryType 250 = Wolves' Den Pier，已核对 XIVAPI；未知新地图不猜测启用。
    public static bool IsEligible(uint territory, uint duty) => territory == 250 || duty is
        127 or 130 or 180 or 701 or 1080 or
        835 or 836 or 837 or 912 or 967 or 1046 or 1102 or
        838 or 841 or 847 or 850 or 853 or 856 or 859 or
        839 or 842 or 848 or 851 or 854 or 857 or 860 or
        840 or 843 or 849 or 852 or 855 or 858 or 861 or
        913 or 914 or 915 or 916 or 917 or 918 or 919 or 920 or 921 or 922 or
        968 or 969 or 970 or 971 or 972 or 973 or 974 or 975 or 976 or 977 or
        1047 or 1048 or 1049 or 1050 or 1051 or 1052 or 1053 or 1054 or 1055 or 1056 or
        1103 or 1104 or 1105 or 1106 or 1107 or 1108 or 1109 or 1110 or 1111 or 1112;
}
