using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;

namespace PvpTimeline;

/// <summary>附在时间轴下方的限高当前状态栏。不会把来源状态时长、GCD或技能复唱当成本人Buff倒计时。</summary>
internal sealed class BuffWindow(Configuration config,BuffState state,ITextureProvider textures,Func<uint,BuffInfo?> metadata)
{
    public void Draw(Vector2 anchor,float width,float timelineHeight,double now,bool combat,bool timelineFrozen)
    {
        var columns=Math.Clamp((int)((width-16)/72),1,24);
        var slots=columns*2;
        var buffs=state.Current.OrderBy(b=>b.Remaining(now)??float.MaxValue).ToArray();
        var flags=ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoScrollbar|ImGuiWindowFlags.NoScrollWithMouse|ImGuiWindowFlags.NoSavedSettings;
        if(config.Locked)flags|=ImGuiWindowFlags.NoMouseInputs;
        var height=ImGui.GetTextLineHeightWithSpacing()+62+2*ImGui.GetStyle().WindowPadding.Y+2;
        var viewport=ImGui.GetMainViewport();
        if(anchor.Y+height>viewport.WorkPos.Y+viewport.WorkSize.Y)anchor.Y=Math.Max(viewport.WorkPos.Y,anchor.Y-timelineHeight-height);
        ImGui.SetNextWindowPos(anchor,ImGuiCond.Always);
        ImGui.SetNextWindowSize(new(width,height),ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(config.Locked?config.LockedOpacity:config.UnlockedOpacity);
        if(ImGui.Begin("PvP Timeline##own-buffs",flags))
        {
            ImGui.TextDisabled(timelineFrozen?"自身状态·实时（技能轴冻结） / Live statuses":"自身状态 / Own statuses");
            if(now-state.LastSnapshot>1) { ImGui.SameLine();ImGui.TextDisabled("观测暂未更新 / stale"); }
            var origin=ImGui.GetCursorScreenPos();
            var visible=Math.Min(buffs.Length,buffs.Length>slots?slots-1:slots);
            for(var i=0;i<visible;i++)
            {
                var buff=buffs[i];var info=metadata(buff.Reading.Id);
                var p=origin+new Vector2((i%columns)*72,(i/columns)*31);
                var draw=ImGui.GetWindowDrawList();
                if(info is {Icon:>0})
                {
                    var wrap=textures.GetFromGameIcon(new GameIconLookup(info.Icon)).GetWrapOrDefault();
                    if(wrap!=null)draw.AddImage(wrap.Handle,p,p+new Vector2(24));
                }
                else draw.AddText(p,0xFFCCCCCC,buff.Reading.Id.ToString());
                var remaining=buff.Remaining(now);
                var timer=remaining is >0 ? remaining>=60?$"{remaining/60:F0}m":$"{remaining:F1}s" : remaining==0?"待确认":"—";
                draw.AddText(p+new Vector2(27,4),0xFFEEEEEE,timer);
                if(ImGui.IsMouseHoveringRect(p,p+new Vector2(70,28)))
                {
                    ImGui.BeginTooltip();
                    ImGui.Text($"{info?.Name ?? "状态"} #{buff.Reading.Id}");
                    ImGui.Text($"来源 / source: {buff.SourceName}");
                    ImGui.Text(remaining!=null?$"本人倒计时 / recipient timer: {remaining:F2}s":"无个人倒计时；不代表永久，也不代表已消失。");
                    ImGui.Text($"来源倒计时：未关联 / source timer unknown · 参数 {buff.Reading.Parameter}");
                    ImGui.Text($"当前战斗 / in combat: {combat} · 状态存在已观测，效果分量条件尚未判定。");
                    if(!string.IsNullOrWhiteSpace(info?.Description))
                    { ImGui.PushTextWrapPos(ImGui.GetFontSize()*24);ImGui.TextUnformatted(info.Description);ImGui.PopTextWrapPos(); }
                    ImGui.EndTooltip();
                }
            }
            if(buffs.Length>slots)
            {
                ImGui.SetCursorScreenPos(origin+new Vector2(((slots-1)%columns)*72,((slots-1)/columns)*31));
                if(ImGui.SmallButton($"+{buffs.Length-visible}"))ImGui.OpenPopup("more-own-statuses");
                if(ImGui.IsPopupOpen("more-own-statuses"))ImGui.SetNextWindowSizeConstraints(new(240,0),new(600,320));
                if(ImGui.BeginPopup("more-own-statuses"))
                {
                    foreach(var buff in buffs.Skip(visible))
                        ImGui.Text($"{metadata(buff.Reading.Id)?.Name ?? buff.Reading.Id.ToString()} · {buff.SourceName} · {(buff.Remaining(now) is {} r?$"{r:F1}s":"无个人倒数")}");
                    ImGui.EndPopup();
                }
            }
            if(buffs.Length==0)ImGui.TextDisabled("当前无已观测状态 / No observed statuses");
        }
        ImGui.End();
    }
}
