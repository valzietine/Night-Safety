# Night Safety (placeholder name)

A mod where nightfall is a survival problem.

When night falls, colonists left outside are in real danger. This mod gives the
colony a reliable way to keep its people safe after dark:

- A buildable, refuelable protection oven that projects a circular safe zone.
- A Forest Spirit that hunts colonists who stray outside the light at night.
- Night tribal harassers that besiege the settlement and its stores.
- Safe-to-safety behavior so pawns head for the light on their own.
- Optional night containment: free colonists are kept within the lit radius
  and their own work assignments are restored at dawn.
- Best-effort compatibility with RimWorld Together.

## Install

Subscribe or drop the folder into `RimWorld/Mods`, matching the workshop folder
name. Requires RimWorld 1.6.

## Build

```
dotnet build Source/NightSafety/NightSafety.csproj -c Release
```

Defs and pawn columns are plain XML; no Harmony patches are used at runtime.

## Compatibility

Night arrivals are suppressed while it's dark, and randomness is kept
deterministic from map-local state so shared-map RimWorld Together sessions stay
consistent. No other mod is required.

## License

Distribution rights for this work are held by the commissioning client. See
`LICENSE`.
