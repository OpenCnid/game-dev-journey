# Weapon control and combat readability

October 3, 2026 design checkpoint. [Decision 0009](../decisions/0009-weapon-control-and-accessible-combat.md) records owner direction; [decision 0008](../decisions/0008-bullet-heaven-solo-play-and-core-experience.md) records 1-4 player bullet-heaven survival, pleasant level-ups and the performant core. These are design requirements and future research, not implemented options or validated accessibility.

## Owner direction and reference observations

Provide configurable auto-shoot, different weapon/item targeting behavior, through-wall enemy visibility and future weapon-position experiments. Readability is highly valued and should shape the core experience, including players with visual or motor access needs.

The two owner-supplied Vampire Hunters screenshots show primary/secondary/full-auto preferences and global/primary/secondary/passive weapon-position controls. They also show merge-weapons, outline, enemy-detection, health-bar and hit-feedback options. The outline field displays X-Ray Blue. Record these as interface observations; exact values, defaults, feature semantics and layout are not selected for our game. No screenshot binaries are copied into the repository.

## Proposed control boundaries

| Question | What it controls | Owner direction and unresolved details |
| --- | --- | --- |
| Firing activation | When an eligible weapon attempts to fire | Configurable auto-shoot accepted; defaults, toggle/hold/manual inputs and primary/secondary grouping open. |
| Aim or target selection | Where an attack is directed and which actor is selected | Crosshair and selected/guided targets accepted; range, obstacle validity, priority ties and HP interpretation open. |
| Guidance | How an attack tracks or travels toward its target | Guided attacks are a possibility; retargeting and particular projectile behaviors open. |
| Effect activation | When passives or active secondary effects operate | Automatic activation is a possible later extension; costs, restrictions and manual override open. |
| Weapon presentation | Where the arsenal appears on screen | Future positioning experiment accepted; visual layout does not silently decide targeting or damage. |
| Threat presentation | Which enemy information the player can perceive | Through-wall visibility accepted; style, filtering, range and prioritization open. |

These are logical distinctions, not an approved component schema. A crosshair-directed gun can fire automatically. A guided healing effect can select an ally without becoming a damage weapon. Target priority does not decide whether a wall blocks a projectile. Automatic firing preserves the encounter's normal timing and the thinking-space ban on attacks; it does not select upgrades or resume on the player's behalf.

Boss/high-HP/low-HP/healing-ally targets are examples supplied by the owner. Clarify whether HP means absolute or percentage health, which actors qualify, when a target is reconsidered and what happens when none is valid. No player-selectable global priority menu, healing roster or friendly-fire rule is selected here. Avoid assuming every weapon checks every enemy every rendered frame; later feasibility should measure a bounded targeting approach under representative load.

## Perception and readable growth

The proposed human perception gift can explain x-ray AI detection without an introductory lore requirement. Alan Turing effect and Turing Sense remain fictional naming proposals. Do not present them as real sensory science or use this idea to redefine all world inhabitants/enemies as machines.

The player should perceive an earned level-up, understand the choice and notice its effect in combat. More weapons, stronger effects and more outlines should preserve the ability to locate threats, aim, identify allies and understand damage. Exact weapon-growth categories and reward presentation remain exploratory.

Proposed investigations include configurable weapon offsets/visibility, outline style/contrast/thickness, prioritizing nearby threats, readable crosshairs and health bars, HUD/text scaling, reducing obstructive effects, and controlling motion/shake/flash. Distinct shape and audio cues can complement color. Preserve gameplay outcomes when presentation changes; through-wall perception is separate from firing through walls.

## Owner clarification: effect-dependent growth and late-run chaos

The owner selected a steady rifle with predictable, frequent shots as the first archetype to explore; its detailed design and upgrade visuals remain open. Design the baseline gun before committing a concrete upgrade pool. Raw damage, area damage and other chosen capabilities should determine what feels useful against enemies. Bullet changes, trails and after-effects are possibilities; no rule requires every numerical increase to add more particles or change the bullet model.

The owner expects the screen to remain understandable as the arsenal grows and sees possible late-run chaos as part of the fun. Research spectacle together with threat recognition, aiming, ally identification, comfort and performance. Configurable presentation should preserve combat outcomes and useful cues. Candidate visual treatments and their actual enjoyment/accessibility remain unvalidated.

The [first-gun discussion](engine-kingdoms-first-encounter.md#first-gun-and-readable-growth--discussion-checkpoint) records the selected steady-rifle archetype to explore, a proposed baseline and illustrative direct-damage, piercing and area-impact growth. It does not select a final gun, weapon roster or visual-effect system.

## Future tasks and research seeds

- [GDJ-11: Experiment with weapon positioning and arsenal visibility](https://linear.app/cnid/issue/GDJ-11/experiment-with-weapon-positioning-and-arsenal-visibility): future Backlog at Medium. Compare a few arrangements against the same build/encounter, observe weapon/threat obstruction and visible power growth, measure frame costs, then recommend one bounded implementation.
- [GDJ-12: Research combat readability and accessibility](https://linear.app/cnid/issue/GDJ-12/research-combat-readability-and-accessibility): future Backlog at High. Conduct deep research and consent-based human evaluation, including relevant low-vision, color-vision, dexterity and reading needs; produce cited findings and prioritized implementation scopes. No date or immediate implementation is scheduled.

Primary starting guidance: [Microsoft text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101), [contrast](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102), [additional sensory channels](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103), [input](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107) and [time limits](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/116). Microsoft recommends redundant sensory presentation for critical cues and configurable input approaches. These references inform investigations; they neither prove our game accessible nor replace human evidence. This checkpoint seeds the future research rather than claiming it is complete.

Observe recognition, errors, comprehension, comfort and player feedback in equivalent solo/cooperative encounters, then inspect outline/effect costs on representative hardware. Include heavy teammate effects and dense occluded enemies. The screen is essential for many players; useful audio/haptic/narrated information and input alternatives can broaden access further. Exact settings and supported access needs remain to validate.
