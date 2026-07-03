# URID for C#

Allocation-free bijective base-37 string identifiers packed into 64/128/192/256-bit value types (`Urid64`, `Urid128`, `Urid192`, `Urid256`) plus a stack-only `UridBuilder`.

## Installation

**NuGet**

```
dotnet add package Urid
```

**Unity (UPM, git URL)** — Package Manager → *Add package from git URL*:

```
https://github.com/longbombus/URID-Sharp.git?path=src
```

Requires Unity 2021.3+ (needs `Span<T>` from the .NET Standard 2.1 profile).

## Testing

Tests are written once (NUnit, constraint syntax only) and run by both harnesses:

- **.NET**: `dotnet test` at the repo root.
- **Unity**: add the package to a project, then in `Packages/manifest.json` add
  `"testables": ["com.longbombus.urid"]` and run **Window → General → Test Runner**.
