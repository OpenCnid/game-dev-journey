# Hexwake resource and runtime notices

## Game source and procedural work

The game's C# implementation, procedural arena/enemy/weapon geometry, loot
glyphs and generated effects come from this project's prototype work. Source
availability does not by itself assign an open-source license.

## Poly Haven resources

The player retains earlier streamed textures, a sky panorama and rock resources
from [Poly Haven](https://polyhaven.com/), offered under
[CC0](https://polyhaven.com/license)
([CC0 1.0 deed](https://creativecommons.org/publicdomain/zero/1.0/)).
Credits are retained even where CC0 does not require attribution.

| Resource | Author(s) | License |
| --- | --- | --- |
| [Dark Rock](https://polyhaven.com/a/dark_rock) | Amal Kumar | CC0-1.0 |
| [Rust Coarse 01](https://polyhaven.com/a/rust_coarse_01) | Dimitrios Savva, Rico Cilliers | CC0-1.0 |
| [Sandstone Cracks](https://polyhaven.com/a/sandstone_cracks) | Rob Tuytel | CC0-1.0 |
| [Brown Leather](https://polyhaven.com/a/brown_leather) | Rob Tuytel | CC0-1.0 |
| [Burned Ground 01](https://polyhaven.com/a/burned_ground_01) | Rob Tuytel | CC0-1.0 |

The retained earlier resource bundle also contains:

- [Kloofendal 48d Partly Cloudy Pure Sky](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky)
- [Rock Face 01](https://polyhaven.com/a/rock_face_01)
- [Forest Floor](https://polyhaven.com/a/forest_floor)
- [Rusty Metal 02](https://polyhaven.com/a/rusty_metal_02)
- [Bark Brown 02](https://polyhaven.com/a/bark_brown_02)
- [Anti Slip Concrete](https://polyhaven.com/a/anti_slip_concrete)

All are recorded as CC0 in the inherited resource provenance. Some retained
resources are not used by the current arena. Sandstone was recolored as fantasy
ivory and leather as fantasy hide; neither is a biological scan.

## Unity and Mono

The Windows player includes Unity **6000.6.3f1** runtime components and the Mono
runtime distributed with that player. Unity software remains subject to
[Unity's terms](https://unity.com/legal/terms-of-service) and
[third-party notices](https://unity.com/legal/third-party-notices); it is not
relicensed as project source or CC0.

The Mono upstream license/notice collection is included as `MONO-LICENSE.txt`,
from <https://github.com/mono/mono/blob/main/LICENSE>. Its introduction identifies
MIT and BSD runtime/class-library terms and distinguishes build-time components.
This collection is a notice reference, not a claim that every listed Mono
component is bundled here.

NUnit is used only for developer verification and is not needed or shipped in
the playable ZIP. Test source requires an independently installed Unity Test
Framework/NUnit dependency.
