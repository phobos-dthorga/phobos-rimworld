# A setup wizard on first load: the idea (9 Nov 2026)

**Status: recorded for later. Nothing is designed or built.** The owner asked for this to be kept until the right point in development;
an agent may raise it when it judges that point has come.

## The idea (owner, 9 Nov 2026)

When the time is right, offer the player a wizard the first time they load a game with our mods, to customise how they wish to play.
**Above all, it covers how our mods affect third-party mods.**

## Why it would earn its place (agent's reading)

Our settings are spread across several mods' windows, and some have to be decided before a colony starts. Some of the largest choices
change other authors' content, and a player meets them only by reading a README or opening the settings:

| Choice today | What it does to other mods' content |
| --- | --- |
| Which vehicles get systems (D'thorga Framework) | Appends parts to every Vehicle Framework vehicle from any mod, unless narrowed |
| Destroyed parts need replacing, and what they cost | Changes repairs on every player's vehicle from any mod |
| Drivetrain, fuel use, hover limits | Change how other authors' vehicles move and fly |
| Keep the game's log going past its limit | Replaces the game's anti-spam limit for every mod |

This table is an example from 9 Nov 2026, not an inventory. Build the real list when the wizard is designed.

## When to raise it (agent's view)

- **Not before** the settings have settled, because a wizard written early has to be rewritten with every new option.
- **The natural moment** is while preparing the first mod for its public beta gate, or sooner if the owner or testers find the settings
  hard to get through.

## To settle when it is designed

- Once per install, or once per colony? A first load of an existing colony may need to behave differently from a new one.
- One wizard across all our mods, hosted by D'thorga Framework, or one per mod?
- A player who skips it gets today's defaults, and it can be opened again from the settings.
- Choices that need a restart have to say so, and be separated from the ones that apply at once.
- It must answer the project's rule for new behaviour: what it lets the player do, what it stops them doing, and how it changes the way
  they play.
- **Player text:** a wizard naming other mods is like the settings window, where that is allowed, rather than text met in play. Confirm that
  reading against the "in-world text only" rule then. UI presents and services decide, so it only drives the existing settings.
