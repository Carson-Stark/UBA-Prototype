# Unity 2D Multiplayer Prototype – Custom 2D NavMesh Pathfinding & Authoritative Photon Networking

## Project Overview  
Ultimate Battle Arena is a fast-paced top-down multiplayer shooter built with Unity. This open-source repository represents the original prototype for a mobile game project that has since been completely overhauled and rebuilt from the ground up. While the prototype is no longer actively developed, it showcases the initial systems I created for networking, movement, AI, and combat.

This version was developed primarily as a learning exercise and early proof of concept. It contains custom implementations of both networking and pathfinding, which may be useful for other developers exploring similar problems.

> For updates on the current version of the game, check out the **[Devlog Series](https://www.youtube.com/playlist?list=PL-BN8ZnuVHHiFwF99EmGyb0d-vyMc462H)**.

[![Devlog Series](https://img.youtube.com/vi/dtZYFixZeLY/0.jpg)](https://www.youtube.com/watch?v=dtZYFixZeLY)

### Project Timeline (Prototype)
**Date Started:** January 2018

**Date Completed:** December 2018

## Key Features (Prototype)

- **Custom Networking Layer (Photon PUN Integration + Authoritative Master Client):**  
  The networking system extends Photon Unity Networking with custom features to improve multiplayer responsiveness and fairness:  
  - **Client-side prediction:** Immediate local application of player inputs to reduce perceived latency.  
  - **Server reconciliation:** Periodic authoritative state updates from the server; clients rewind and replay unacknowledged inputs to correct discrepancies.  
  - **Lag compensation and rollback mechanics:** Maintains a history of game states (`GameManager.cs`) to allow rewinding and correcting player positions and actions.  
  - **Input serialization and sequencing:** `PlayerInput.cs` captures and serializes player inputs with sequence numbers and timestamps to ensure ordered processing and detect lost packets.  
  - **Movement synchronization:** `PlayerMovement.cs` applies inputs, predicts movement, and interpolates corrections from the server, including rubberbanding to smoothly correct position errors.  
  - **Host migration and vote-based cheating detection:** Detects inconsistencies in the master client’s state and initiates host migration through voting.  

- **AI Bots with Custom Pathfinding:**  
  Implements a full **custom 2D navigation system**, since Unity’s built-in NavMesh is 3D-only.  
  - **NavMesh Generator (`NavMesh.cs`):** Builds a 2D navigation mesh from environment colliders, creating nodes and polygons for navigation.  
  - **A\* Pathfinding Algorithm (`Pathfinding.cs`):** Calculates shortest paths on the custom mesh.  
  - **Funnel Algorithm and Geometric Utilities (`Geometry.cs`):** Smooths raw paths for natural movement, reduces jitter, and provides polygon operations such as simplification, triangulation, inflation, and polygon combination.  

- **Custom 2D Graphics for Desert Map:**  
  The project includes a custom designed desert playable level with unique hand-crafted sprites. The desert map assets are located in the project under the `Assets/desert.unity` scene and associated sprite resources, showcasing custom textures, environmental props, and terrain details tailored to the desert setting.

- **Game Modes:**  
  - Team Deathmatch  
  - Free-for-All  

- **Player Mechanics:**  
  - Twin-stick movement with smooth aiming  
  - Shooting, grenade throwing, diving, and reloading  
  - Destructible building roofs  

- **Matchmaking and Lobby:**  
  - Player name input  
  - Room size selection  
  - Automatic matchmaking  

- **Offline Play:**  
  - AI bots for practice or solo play

## Project Environment Details

- **Unity Version:** 2018.4.36f1
- **Photon Unity Networking (PUN) Version:** 1.90 (April 2018)

## Getting Started

1. Open the project in Unity.  
2. Set up Photon Unity Networking with your Photon Cloud App ID in `PhotonServerSettings.asset`.  
3. Build and run the project.  
4. Use the multiplayer menu to enter your player name and select room size.  
5. Join or create a game room to start playing.

## About the Current Version  

This project is a legacy prototype and no longer actively developed. The game has since been fully overhauled with:

- **Photon Fusion** instead of PUN, supporting authoritative server architecture, built-in lag compensation, and client-side prediction.
- **Unity’s NavMesh system** for 3D pathfinding and AI navigation.
- A **new 3D art style and animation system**, optimized for mobile devices.
- **Expanded movement and combat mechanics**, including dashing, grenades, weapon loadouts, and more.
- Ongoing playtesting and polish for iOS release.

This repository preserves the **original prototype** for educational and archival purposes.

## Detailed Geometric Functions and Algorithms (`Geometry.cs`)

- **simplifyPolygon:** Uses Visvalingam’s algorithm to remove insignificant vertices from polygons based on triangle area significance, reducing complexity while preserving shape.  
- **triangulate:** Implements Ear Clipping method to triangulate polygons with holes, including bridge validation and triangle quality improvement via diagonal swapping.  
- **inflatePolygon:** Adds a buffer around polygons by calculating intersections of parallel offset lines, useful for collision padding.  
- **combinePolygons:** Computes the union of two polygons by finding intersections and merging vertices, handling holes and complex shapes.  
- **polygonPartion:** Removes unnecessary diagonals from triangulations to create convex polygon partitions, identifying essential edges.  
- **sharedPoints:** Finds common vertices between two polygons.  
- **pointInsidePolygon:** Determines if a point lies inside a polygon using ray casting and bounding box checks.  
- **doIntersect:** Checks if two line segments intersect, considering special colinear cases.  
- **onSegment:** Checks if a point lies on a line segment between two endpoints.  
- **orientation:** Determines the orientation (clockwise, counterclockwise, colinear) of three points.  
- **midpoint:** Calculates the average position of multiple points.  

The `Segment` and `Polygon` classes support these operations with properties like length, midpoint, and vertex lists.

## Custom Networking Implementation Details

- **Input Handling (`PlayerInput.cs`):**  
  Captures player inputs from joysticks and buttons, packages them with sequence numbers and timestamps, and serializes them over the network. Handles lost packets and ensures input order.  

- **Movement and Prediction (`PlayerMovement.cs`):**  
  Applies inputs locally for immediate response (client-side prediction). Receives authoritative position and rotation updates from the server and reconciles differences by rewinding and replaying inputs. Uses interpolation and rubberbanding to smooth corrections.  
  Includes collision detection to prevent moving through obstacles and a diving mechanic.  
  Implements vote-based detection of cheating by the master client and triggers host migration if necessary.  

- **Game State Management (`GameManager.cs`):**  
  Maintains a history of game states with player positions and rotations to support rollback and rewinding during reconciliation. Manages player spawning, scoring, and host migration.  

- **Connection and Matchmaking (`NetworkLauncher.cs`):**  
  Handles connection to Photon servers, room joining or creation, and scene loading for multiplayer matches.  

## License

Feel free to fork, modify, and learn from this project. Please credit if using significant portions.

