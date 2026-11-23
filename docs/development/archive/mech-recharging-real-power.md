# Mech rechargers that draw real power

**Status (20 Nov 2026):** built in D'thorga Patches 0.28.0 as a setting, off by default, with its sums in D'thorga Framework 0.198.0. Not yet tested in game. A mech's information card shows its recharger draw (the owner's follow-up request, the same day). Feeding power back to the grid is not built.

The owner's request (20 Nov 2026): "a patch I wish to introduce, that's disabled by default, and I wish to make mechanoids take *real* power when they are recharging."

## Read this first

- **Off by default.** Mod settings, D'thorga Patches, the **Mechanoids** page. It takes effect at once, both ways.
- **Needs Biotech.** The part loads only while Biotech is active.
- **A large mech can ask more than a small grid has.** The game then switches things off at random, as it does in any shortage: the recharger, or a freezer.
- **Compact gravship rechargers lose their low-power advantage** while this is on (the owner's choice: the draw rises).
- **Safe to add and remove.** It saves nothing; turning it off puts every recharger back to the game's figure.

## What the game does (read from its code and the owner's patched Defs, 20 Nov 2026)

- A recharger's draw is one figure, its Def's `basePowerConsumption`, whoever stands on it. `Building_MechCharger.Tick` never looks at the mech's size.
- Every mech gains 50 units of energy a day on a recharger (`Building_MechCharger.ChargePerDay`); most hold 100, so a full charge takes two days.
- A working mech uses 10 units a day (`Need_MechEnergy.BaseFallPerDayActive`), so it spends about one day in six charging.

| Recharger | Listed | A full charge |
| --- | --- | --- |
| Mech recharger (light) | 200 W | 400 Wd |
| Large mech recharger (medium to ultra-heavy) | 400 W | 800 Wd |
| Automatroid Recharger (The Dead Man's Switch, any size) | 200 W | 400 Wd |
| Floor rechargers (Biotech for Gravship) | 100 / 200 W | 200 / 400 Wd |
| Compact rechargers (Vanilla Gravship Expanded) | 20 / 40 W | 40 / 80 Wd |

Averaged over its working life, a Diabolus costs about 67 W: a little over two standing lamps.

## The four questions

- **What it lets the player do:** size a mech bay as a real load; run small mechs cheaply; choose where each control group stops charging (the game's own recharge range) knowing the last fifth is slow.
- **What it stops:** heavy mechs living on a trickle; a war queen filled from one-seventh of a battery on a compact recharger.
- **How it changes play:** staggered charging, battery banks sized for the bay, a heavy recharger on a circuit that can carry it, a vent or cooler for a busy bay.
- **What outside notices:** the power grid (a large mech plugging in is a load the whole colony feels, and a shortage switches other things off); the room (waste heat); the recharger's inspect text; the mech's information card. Wastepacks are unchanged per unit of energy.

## The model

A mech's battery has a size in watt-days. The recharger draws what that battery takes in, plus what the charger loses.

| Step | Rule | Figure (authored) |
| --- | --- | --- |
| Battery size | body size x watt-days per point, x the size preset, x (the mech's maximum energy / 100) | 480 Wd per point |
| Rate | the game's own 50 units a day over the mech's maximum | half a standard battery a day |
| Taper | full rate to 80% charge, then a straight line down to a quarter rate at full | 80%, 25% |
| Wall power | power into the battery / efficiency | 84% |
| Heat | wall power less battery power, into the room at the game's heater's rate (21 heat a second for 175 W), until the room passes a limit | 80 C |
| Standby | no mech: the Def's own idle figure, else a share of its listed power | 5% |

The figures are chosen so the common light mechs (body size 0.7) draw the 200 W the game's recharger lists: 0.7 x 480 = 336 Wd; half a day is 168 W; 168 / 0.84 = 200 W.

| Mech | Body size | Battery | Draw at full rate | Of that, heat |
| --- | --- | --- | --- | --- |
| Cleansweeper | 0.3 | 144 Wd | 86 W | 14 W |
| Most light mechs | 0.7 | 336 Wd | 200 W | 32 W |
| Tunneler | 3.5 | 1,680 Wd | 1,000 W | 160 W |
| War queen, Diabolus | 4 | 1,920 Wd | 1,143 W | 183 W |

- **Time.** Empty to 80% takes 1.6 days as now; the last fifth takes about 0.74 days in place of 0.4, so a full charge takes about 2.3 days. A mech whose maximum energy another mod raises draws the same power for longer.
- **Waste** follows the energy put in: the taper slows the wastepack count with the charge.
- **Energy is conserved:** what the grid gives is what the battery gains plus the heat.

The sums are game-free in D'thorga Framework's `BatteryChargeRule` (tests in `tests/PhobosFramework.Rules.Tests/BatteryChargeRuleTests.cs`); the figures are `PhobosMechChargingRulesDef`, one Def another mod may patch; `MechCharging` decides, and the two Harmony patches on `Building_MechCharger` (its tick and its inspect text) only apply and show.

## Working alongside power-saving mods (owner, 20 Nov 2026)

The owner asked that this "work alongside mods that allow for power-saving features like when a pawn leaves a room, or where there's idle charge".

- **We write the recharger's stored figure through the game's own `CompPowerTrader.PowerOutput`**, as the game's heater, cooler and gene machines write theirs, and compare against the stored figure, never the reported one.
- **LightsOut** (in the owner's list; source read in the repository [NathanIkola/LightsOut](https://github.com/NathanIkola/LightsOut) at commit 59e32b4, pushed 17 Oct 2026) changes what a power comp *reports*: its `DisableBasePowerDrawOnGet` scales the getter's result by its standby or active rate, and its `PatchMechCharger` holds a recharger on standby until `StartCharging` and after `StopCharging`. So it works on top of our figure: on standby it reports its standby share of our standby draw (almost nothing at its default of 0%); while a mech charges it reports our figure times its active rate (100% by default). Our inference from its source; not yet seen in game.
- **No other active mod patches the recharger** (`query-patches.py who "Building_MechCharger.*"`, 20 Nov 2026). A mod that itself wrote a recharger's `PowerOutput` every tick would fight ours; none is known.
- The game puts its listed figure back when a recharger is spawned, rewired or a research project finishes (`SetUpPowerVars`). We set ours again at the next tick with a mech, or the next slow look without one.

## Owner choices and agent defaults

| Choice | Whose |
| --- | --- |
| Off by default | Owner |
| The draw rises with the mech; charge time is kept (not "the charge slows") | Owner, 20 Nov 2026 |
| Taper near full; waste heat in the room | Owner, 20 Nov 2026 |
| Works alongside power-saving mods | Owner, 20 Nov 2026 |
| Body size as the measure of a mech (in every mech Def, so other mods' mechs are covered) | Agent default |
| 480 Wd per point, 84%, the 80% knee and quarter rate, 5% standby, the 80 C heat limit | Agent defaults, in the rules Def |
| Size presets x1, x3 and x10, x1 the default | Agent default |
| In D'thorga Patches, as a Biotech part | Agent default (a patch of the game's own behaviour) |

## The information card

The owner asked for it on 20 Nov 2026, after the first build. A stat, **recharger draw** (`PhobosMechRechargerDraw`, in the game's
Mechanoid category beside wastepacks per recharge, laid out like the game's own max power output), shows the full-rate draw for the
mech. Its explanation gives the battery's size, the power into it, the heat, the draw when full and the days from empty to full. It
reads a living mech's own body size and maximum energy (another mod may raise either), else its kind's, so it shows on a gestator
recipe's card too, and links to the two Biotech rechargers. Its worker hides it while the setting is off. The figures come from
`MechCharging`, the same place as the recharger's.

## Limits and what is not built

- **A shortage switches things off at random.** That is the game's own rule for any shortage; we add no warning beyond the recharger's "Waiting for power" line.
- **Not built: mechs feeding the grid back** (a dormant mech as an emergency battery; offered to the owner on 20 Nov 2026 and left for later).
- The battery chemistry is not modelled. The taper is a straight line standing in for a real charger's constant-voltage stage; no real cell's curve was looked up.
- Test row 287 in the [in-game test pass](../in-game-test-pass.md).
