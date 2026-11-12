# PvP Timeline

**English** | [简体中文](README.zh-CN.md)

A personal, real-time action timeline for PvP in FINAL FANTASY XIV.

See action use, casting, the global cooldown, animation lock, your own animations, and effect feedback on one timeline. Review what you just did: when you used an action, when its effects were received, and which abilities you weaved in between.

![PvP Timeline in-game preview](assets/pvp-timeline-preview.gif)

> This is a test release. Game updates may affect observation and display, and feedback may still be missing for certain action stages. The timeline shows local observations, not an exact reconstruction of the server's internal hit detection. It does not automate actions.

## How it works

![PvP Timeline local observation and message flow](assets/how-pvp-timeline-works.png)

The diagram separates the messages exchanged by the game client and server from the timing information observed locally by the plugin. PvP Timeline records an accepted action request, locally visible cast and cooldown state, animation lock, the player's own animation playback, received effect messages, and the player's current status snapshots. The colored tracks use the same meanings as the default timeline display.

The plugin does not send an additional combat packet and cannot inspect the server's internal decision process. A physical button press that never becomes a game action request is not observable. Effect-feedback timestamps show when the local client received the message, so they include network, server, and client processing delay.

## What does it do?

- **Separate action lanes:** larger icons in the lower lane show spells, weaponskills, and certain special actions. Smaller icons above show abilities, using at most two rows to keep the overlay compact.
- **Distinct timing tracks:** the global cooldown, cast time, animation lock, and animation playback are recorded separately rather than treated as the same interval.
- **Effect feedback:** a pink diamond marks received damage, healing, status application, or other supported feedback. Critical and direct hit results add a soft glow to the original action icon.
- **Delayed effects:** supported automatic detonations and follow-up stages are linked to the original use, or shown as translucent icons for actual triggered effects without inventing another button press.
- **Your own statuses:** compact status history and timing below the action lanes, including selected buffs and observed debuffs.
- **Customizable appearance:** click-through locking, horizontal or vertical layout, reverse scrolling, colors, background opacity, icon sizes, and overall scale.
- **Freeze and review:** pause the display to inspect recent actions, or clear the current records to start another test.

Observation is restricted to supported areas within **the Wolves' Den Pier, Crystalline Conflict, Frontline, and Rival Wings**. It is not enabled in ordinary PvE areas. New maps require updates to the support list.

## Getting started

### Installation

You need a game environment capable of running **Dalamud API 15** plugins. Use a release compatible with your current game and Dalamud versions.

1. Install and launch XIVLauncher, and make sure Dalamud is enabled.
2. Open **Dalamud Settings → Experimental**.
3. Add the following custom plugin repository URL:

   ```text
   https://raw.githubusercontent.com/chefiffer/dalamud-plugins/main/repo.json
   ```

4. Open the plugin installer, search for **PVPTimeline**, and install it.
5. Enter the following command in game chat to open settings:

   ```text
   /pvptimeline
   ```

6. Enter the Wolves' Den Pier or a supported PvP map and use actions to observe the timeline.

### First-time setup

Start with **Combat only** turned off and test against a striking dummy at the Wolves' Den Pier:

1. Unlock the overlay on the **Display** tab, then move and resize it.
2. On the **Layout** tab, adjust GCD and ability icon sizes, bar positions, and thicknesses.
3. Once you are happy with the layout, enable **Locked** to make the timeline click-through and avoid accidental interaction.

A locked timeline cannot be clicked directly. Use `/pvptimeline` to reopen settings. When unlocked, you can also right-click the timeline to open settings.

## Reading the timeline

### Action icons and timing

`0s` is the current observation time; negative values show the past. During live display, the grid and time labels stay fixed while actions and bars move toward the history side.

The lower lane with larger icons does **not** mean every action there uses the global cooldown: some Limit Breaks and special actions also appear in that lane. A blue GCD bar is drawn only when a shared cooldown is actually observed.

| Default appearance | Meaning |
| --- | --- |
| Blue bar | Observed global cooldown (GCD) |
| Green bar | Locally observed cast interval |
| Short bright-yellow bar | Animation lock, recorded separately from the GCD |
| Purple bar | Observed playback interval of your own animation |
| Solid pink diamond | Direct feedback such as damage, healing, or status application; for certain verified movement actions, it may also indicate a server acknowledgment |
| Hollow pink diamond | Feedback such as visual effects or gauge changes; this alone does not prove damage or healing occurred |
| Translucent action icon | An unconfirmed cast, or an actual triggered effect without a corresponding manual-use record; it does not mean you pressed the action again |
| Icon glow | Critical hit, direct hit, or critical direct hit flags from actual effect messages; colors are customizable |
| Thin gray bar (off by default) | The action's independent cooldown, not the GCD |

All colors can be changed. A multi-target action may produce different results on different targets. Its icon combines the observed hit flags for that use; a critical direct hit glow does not mean every target received one.

Recuperate and Shadowbringer also have **short independent blue recast bars** in the ability lane. These remain independent cooldowns; the blue bars do not turn them into GCD actions.

### Why is a purple bar or diamond sometimes missing?

Purple bars use actual observations of your own animations. Missing observations, uncertain associations, pauses, or replacement by another action can cause a bar to be absent, interrupted, or cut short. A replacement marker is shown only when another action is confirmed to have overwritten the animation; an ordinary ending does not add an orange diamond.

Pink diamonds likewise require actual effect feedback. The plugin does not guess a marker from an action's description when feedback is missing, cannot be reliably associated, or belongs to an unsupported stage.

**A missing bar or marker is not proof that the action failed.** The plugin does not substitute the GCD or animation lock duration for animation playback time.

## Reading the status rows

The status rows primarily show **statuses actually present on your character**. They may come from you, an ally, or an enemy. An ally using an action is not, by itself, treated as proof that you received its effect.

Buffs use compact start and end icons connected by bars:

- When a status appears, its start icon moves with time. A duplicate is not placed at the left edge before the original reaches it.
- Once the start icon reaches the edge, it joins the queued icons. If remaining time is known, a countdown of at most two digits appears below it, without a unit or a zero.
- Queued icons are ordered by end time, with earlier-ending statuses farther to the right so they are easier to notice.
- **An icon with a small red underline marks an endpoint.** If an expected expiry time is known, this icon may appear in the future area before the status ends. The underline does not mean the status has already ended.
- After the status ends, its endpoint continues moving into the past. The segment disappears when the endpoint meets its start icon.

Statuses from different sources can be recorded separately. Refreshes, stacking, and removals follow actual changes in your status list; the plugin does not invent stacks based on how many times an action was used.

Not every status is shown. The plugin filters reviewed job buffs and excludes certain role-action selection indicators and action-ready or follow-up states. This filtering is still being refined; it is not a complete status database for every job. If you would like another buff displayed, please open an issue or contact the developer.

**The plugin displays only statuses and countdowns it can confirm.** If your buff has no readable remaining time, no countdown is shown. Conditions such as needing to be in combat for part of an effect to work are not currently evaluated separately.

## Common settings

| Location | Options |
| --- | --- |
| Top of settings | Language, overall scale, freeze/resume live display, and clear records |
| Display | Enable, click-through locking, combat-only display, cutscene hiding, status rows, native animation observation, orientation, and backgrounds |
| Layout | Both icon sizes, ability height and row spacing, separate GCD/ability bar positions and thicknesses, and cast/animation positions |
| Tracks | Visibility and colors for timing tracks, short independent recasts, abilities, and auto-attacks |
| Time | Pixels per second, history range, future space, grid, and subdivisions per second |
| Feedback | Critical/direct hit glow colors, animation replacement marker color, and triggered-effect icon opacity |

Enter values directly, or hover over a numeric field and use the mouse wheel to increase or decrease it. Wheel adjustment ends the field's current text edit. Overall scale supports **0.2–2.0×**, including the timeline text.

The interface supports Simplified Chinese, Traditional Chinese, English, German, French, and Japanese. On first use, it follows Dalamud's UI language; you can also choose a language manually. Action and status names use the available multilingual name resources. New actions or missing translations may fall back to English, the client language, or an ID. Player names are not translated. Please open an issue or contact the developer if you find a translation problem.

To revise interface translations, edit `ui-localization.txt` beside the DLL and reload the plugin. This file controls UI text, not action or status names.

## FAQ

### The timeline is not visible in game

Check that you are in a supported PvP area and that the plugin is enabled. Also check **Combat only** and **Hide in cutscene**. If it still does not appear, open **Diagnostics** below the settings tabs to check observation status. Game updates may require compatibility fixes.

### Why does repeatedly pressing an action not show every press?

The timeline shows observable action use, not physical mouse clicks. Clicks that never produce a game action request cannot be observed, and unsuccessful repeated presses do not fill the timeline with red bars or icons.

### Why does automatically triggered damage use a translucent icon?

The plugin received an effect caused by you, but there may be no corresponding manual action-use record. An automatic detonation can deal real damage without another button press at that moment. Reliably associated stages update the original action; other actual triggered effects may remain as translucent icons.

### Is the feedback timestamp the server's hit-detection timestamp?

No. It is when your client received the message and includes network, server, and client processing effects. Observed animation playback is not the server's hit snapshot either. A cast ending also does not establish the earliest moment you could have moved.

### Does it upload data or save complete matches?

The timeline does not automatically upload combat data or continuously save complete match logs. It keeps bounded recent records in memory. Changing areas or clearing records resets them.

Diagnostics are copied to your local clipboard only when you click the copy button. They may contain entity IDs, action/status IDs, and timing information. Review them before sharing.

## Limitations and precautions

- Each action use has a feedback-association window of at most **14 seconds**. Overdue feedback does not occupy a pending slot forever. Feedback without a source sequence, or with multiple possible owners, is not forcibly assigned.
- Action history is limited to 256 entries and 120 seconds. The visible history range is configured separately. Current and historical statuses also have count limits to prevent unbounded growth.
- Animation observation currently focuses on your own character. It does not fully reconstruct target-side animations, projectiles, or ground effects.
- Native animation observation uses internal game interfaces. Game updates may break it or affect stability. You can disable this observation in settings, but doing so does not uninstall every observation hook used by the plugin.
- This is an unofficial third-party plugin. Understand the game and plugin platform's rules before use. The project does not guarantee account safety or crash-free operation.

## Reporting an issue

Please include:

1. Plugin version, game region/version, and selected language.
2. Job and action name, including whether the effect was triggered manually or automatically.
3. Expected behavior, actual behavior, and steps to reproduce it.
4. A screenshot or short GIF. For association errors, include effect diagnostics copied from settings if possible. For loading failures, include relevant Dalamud logs with private information removed.

Screenshots help explain display problems. Diagnostics help distinguish missing observations from association or rendering errors. Do not share account tokens, passwords, or unrelated private logs.

## Acknowledgments

The following project informed the timeline presentation, locking interaction, and customization approach:

- [ActionTimelineEx](https://github.com/Ted-Jin-Lab/ActionTimelineEx)
- [ActionTimelineReplacement](https://github.com/Loskh/ActionTimelineReplacement)

PvP Timeline is not an official version of that project. Game icons and related assets belong to their respective rights holders.

## License

PvP Timeline is licensed under the GNU Affero General Public License v3.0. See [LICENSE](LICENSE). The retained MIT notice for the upstream fork is included in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).
