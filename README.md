# Night Safety

A RimWorld 1.6 mod where nightfall is a survival problem.

- A buildable, refuelable protection oven that projects a circular safe zone
  while it burns.
- A Forest Spirit that hunts colonists caught outside the light at night and
  withdraws at dawn.
- Night tribal harassers that arrive in small packs with one of four themes:
  arson, bombardment, effigy building, or theft. They flee when confronted.
- Colonists head for the light on their own when it gets dangerous outside;
  each pawn can be toggled in the Assign tab.

## Build

```
dotnet build Source/NightSafety/NightSafety.csproj -c Release
```

Unit tests for the pure decision logic:

```
dotnet test Source/NightSafety.Tests/NightSafety.Tests.csproj
```

## Compatibility

No Harmony patches; everything runs through vanilla comps, think trees, and
incident workers.
