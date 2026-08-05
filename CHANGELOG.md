# Changelog

All notable changes are tracked here, newest first.

## Unpublished — initial commission delivery

- **Protection oven.** A buildable, fuelable oven throws a circular night-safe
  zone around it. Radius and fuel behavior are plain XML.
- **Forest Spirit.** A nightly, damage-immune entity that seeks out and kills
  colonists left outside the light. Uses normal animal/incident routing, not
  debug or forced state, so the behavior plays out through the game's own AI.
- **Night tribal harassers.** Small packs appear at night with one of four
  rolled themes — arson, bombardment, persistent effigy building, or stealing
  from outdoor stockpiles. They fall back when confronted and fade at dawn.
- **Safe-to-safety behavior.** Colonists seek the lit zone on their own when it
  becomes dangerous outside. Per-pawn Assign control lets the player opt each
  pawn in or out.
- **Night containment.** Free colonists are kept inside the protected radius for
  the night and their prior area assignments are restored at dawn. Survives
  save/load mid-night; a manual reassignment overrides it for that night.
- **Night visitor suppression.** Traders, visitors, and travelers no longer
  arrive on home maps after dark. Raids and everything else are unchanged.
- **RimWorld Together compatibility.** Randomness is derived from persisted
  map-local state so shared-map peers converge on the same night's pack.

Work-in-progress items (deferred scope): full art/audio pass, pipe and emitter
network, custom darkness overhaul, and advanced cinematic spirit behavior.