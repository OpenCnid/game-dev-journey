# AI-controlled world: options for cooperative weapon stacking

October 2, 2026. **The Engine Kingdoms is the owner-selected world direction; detailed mechanics remain proposals, not validated gameplay.** The owner accepted cooperative PvE for 2-4 players, two-player testing first, and isekai into an AI-controlled world, then selected option A. See [foundation revision 3](../foundation.md) and [decision 0002](../decisions/0002-cooperative-pve-ai-world.md). The planning/integration chat offers this comparison to scope [GDJ-6](../planning/tasks/GDJ-6.md); no separate design chat has started.

## Shared design intention

Players arrive from another world as outsiders the governing system does not understand. They begin weak, assemble simultaneous weapon builds, and cooperate against its AI-controlled forces. The world should explain where power comes from, why enemies arrive in huge numbers, and how teammates can create useful combinations. Technical implementation still needs evidence; the fictional AI need not dictate a particular runtime technology.

## A. The Engine Kingdoms — selected direction

**Premise:** players arrive in an apparently magical world maintained by an ancient artificial intelligence. Spell laws, guardian armies and sacred towers are parts of its infrastructure. Outsiders lack its normal restrictions and can operate combinations of powers that inhabitants cannot.

**AI control:** the world's administrator regulates which powers may coexist and deploys guardians to contain unauthorized combinations. Bosses can announce a bounded encounter rule, such as a protected zone or a temporarily forbidden damage type, with a clear visual warning.

**Arsenal and appearance:** floating spell mechanisms, rune barrels and mechanical familiars form a growing weapon rig. Weathered stone, living vegetation and luminous circuit runes give the world a fantasy surface with visible machinery beneath it. Keep team effects visually distinct from hostile warnings.

**Signature hypothesis — break a rule together:** one weapon marks targets with a temporary fault; another weapon or teammate consumes it for a chain or piercing effect. Every build can still fight independently. The cooperative combination should create an identifiable crowd-clearing event, rather than merely double damage.

**Why investigate it:** stacking forbidden powers gives the central mechanic a place in the fiction. Complementary attacks give the team a reason to coordinate without committing to rigid classes. Authored guardians and repeatable rules suit a small prototype.

**Smallest later experiment:** one static arena, one approaching enemy type, two players, one marking attack and one consuming attack. Compare separate attacks against the combination and observe whether both players understand their contribution. Choose the exact trigger and reward during design.

**Risk:** rule-breaking can become an excuse for unlimited mechanics. Keep the first interaction to one explicit status and one effect; dynamic terrain or arbitrary player-written spells are outside that experiment.

## B. The Broken Tutorial

**Premise:** players awaken inside an AI training simulation that was meant to prepare displaced humans for another world. Its instructor interprets their arrival as a corrupted test and escalates trials without allowing graduation.

**AI control:** the instructor selects authored encounter modifiers and announces them as lessons. Arena resets and enemy generation belong naturally to the simulation. A changing rule can happen between waves without requiring generated levels.

**Arsenal and appearance:** abilities and weapons are salvaged lesson modules that players equip simultaneously. Clean training halls fracture into incomplete environments, with simple silhouettes and readable holographic hazards. The tone can mix menace with the instructor's excessive confidence.

**Signature hypothesis — choose the next malfunction:** between waves the squad selects a shared rule with a benefit and cost, such as stronger piercing attacks accompanied by more protected enemies. Different personal builds make different squad rules desirable.

**Smallest later experiment:** one arena and one shared choice between two authored encounter modifiers; two debug loadouts expose who benefits. Observe whether the decision produces useful discussion and understandable consequences.

**Risk:** interesting choices depend on several meaningful builds, and voting can slow action. This direction needs more progression context than the marking/consuming experiment. Avoid broad procedural generation or live generated missions in the first test.

## C. The Living Factory

**Premise:** players are transported to a vast industrial world whose AI treats every organism as a resource in an endless production plan. Their arrival creates an accounting anomaly; escaped machinery becomes their arsenal.

**AI control:** the director dispatches repair swarms, security constructs and quota-enforcing bosses. Capturing or disrupting infrastructure changes a small set of authored combat conditions.

**Arsenal and appearance:** salvaged drills, launchers, cutters and repair drones stack around each player. Vast machine shapes and bold industrial colors make hordes and power routes readable; organic refuges provide contrast.

**Signature hypothesis — redirect the squad's power:** captured energy can overcharge the team arsenal or sustain a protective device. Holding an exposed power point creates positioning and support decisions around individual weapon builds.

**Smallest later experiment:** one static power point with two modes, one crowd and two complementary loadouts. Observe whether moving together and choosing a mode improve survival in a way both players can describe.

**Risk:** resource management or defending a point may displace the mobile weapon-stacking fantasy. Prototype the movement consequences before expanding infrastructure, crafting or factory simulation.

## Recommendation and next design outcome

Develop **The Engine Kingdoms**, which the owner selected after the planning recommendation. Its premise directly explains simultaneous powers and suggests a small cooperative interaction to test. The proposed marking/consuming effect still needs encounter design and play evidence. The Broken Tutorial and The Living Factory remain unselected alternatives, favoring shared build decisions and positioning/objectives respectively.

GDJ-6 should now write one concrete 60-second two-player encounter in the selected direction: arrival/fantasy, initial weapons, player actions, enemy response, the cooperative moment and ending. Resolve only the progression/upgrade timing and downed/restart rules needed for that encounter. Save additional ideas as options rather than prototype obligations.

The next technical investigation should evaluate one authoritative shared combat state, responsive local player presentation, compact crowd updates and client-side visual reconstruction. A player-hosted test is an option, not an accepted hosting decision. No network library, bandwidth budget, latency target or replication method has been selected or measured. Do not assume that 20,000 locally rendered enemies can be synchronized economically, or that identical seeds make the existing simulation deterministic across machines.
