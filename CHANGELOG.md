# Changelog

All notable changes are tracked here, newest first.

## Unreleased

- Stemroot: impassable walls of anomalous trees. They block light, hold up a
  roof, and knit themselves back together an hour after being cut into. Patches
  generate on colony and settlement maps and shift slowly over time, closing old
  paths and opening new ones, but they never expand into a lit oven's radius or
  the two cells around it.
- Stemroot cells bleed, bloom, fruit, and flush depending on damage, light, and
  warmth. Bleeding leaves white sap on the floor, blooming ripens into fruit on
  its own, and the inspect panel shows the state, the countdown, and what can be
  taken off it.
- Stemroot is cut for wood or harvested with the plant cutting skill. Cutting
  leaves the worker with the forest affliction and a stacking Unsettled memory,
  worse for psychically sensitive people; harvesting costs less and leaves the
  wall standing. Stemroot destroyed by anything other than a colonist doing the
  work leaves no memory behind.
- Odd fruit and odd fungus, harvested from fruiting and flushing stemroot. Odd
  fruit counts as meat and vegetables at once and slows hunger for a while after
  eating. Bleeding stemroot gives chemfuel instead.

## Initial release

- Protection oven: a buildable, fuelable oven throws a circular night-safe
  zone around it. Radius and fuel behavior are plain XML.
- Forest Spirit: a nightly, damage-immune entity that seeks out and kills
  colonists left outside the light. Uses normal animal/incident routing, not
  debug or forced state, so the behavior plays out through the game's own AI.
- Night tribal harassers: small packs appear at night with one of four
  rolled themes: arson, bombardment, persistent effigy building, or stealing
  from outdoor stockpiles. They fall back when confronted and fade at dawn.
- Safe-to-safety behavior: colonists seek the lit zone on their own when it
  becomes dangerous outside. Per-pawn Assign control lets the player opt each
  pawn in or out.
- Night containment: free colonists are kept inside the protected radius for
  the night and their prior area assignments are restored at dawn. Survives
  save/load mid-night; a manual reassignment overrides it for that night.
- Night visitor suppression: traders, visitors, and travelers no longer
  arrive on home maps after dark. Raids and everything else are unchanged.
- RimWorld Together compatibility: randomness is derived from persisted
  map-local state so shared-map peers converge on the same night's pack.

Still to come: a full art and audio pass, the pipe and emitter network, a
custom darkness overhaul, and more cinematic spirit behavior.