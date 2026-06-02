# MindAttic.Helpers

Small, dependency-free helpers shared across the MindAttic ecosystem.

The first helper is **`AbstractArtGenerator`** — a deterministic generative-art engine that turns any string seed into a stable SVG "fingerprint" image. The same seed always produces the same picture, so it's perfect for avatars, project tiles, or persona portraits keyed by a slug. It's a faithful port of the generative art on **mindattic.com**, so everything it draws shares the house style: a deep gradient ground, a scatter of translucent shapes, and one bold accent letter.

## Use it

```csharp
using MindAttic.Helpers;

// A base64 data URI — drop straight into <img src> or CSS background-image:
string uri = AbstractArtGenerator.DataUri("persona-0042");

// Or the raw 300×300 SVG, with an optional letter override:
string svg = AbstractArtGenerator.Svg("persona-0042", initial: 'M');
```

```razor
<img src="@AbstractArtGenerator.DataUri(persona.Id)" alt="@persona.Name" />
```

## How it works

1. **Seed → stream.** The seed string is hashed with FNV-1a (32-bit) and advanced by a Numerical-Recipes LCG — the exact same deterministic stream as the website.
2. **Palette.** One of 16 curated palettes (deep teal / indigo / purple / charcoal grounds, each with a single neon accent).
3. **Composition.** A random-direction two-stop linear gradient, 5–8 translucent shapes (circles, rotated rectangles, triangles) that bleed past the frame, and a single large initial letter in the accent colour.
4. **Output.** A self-contained 300×300 SVG (or its base64 data URI). No network, no files, no fonts to ship.

Deterministic by construction: feed it the same slug anywhere — server, client, build step — and you get pixel-identical art.

## Install

```xml
<PackageReference Include="MindAttic.Helpers" Version="0.1.0" />
```

MIT licensed. Part of the [MindAttic](https://mindattic.com) ecosystem.
