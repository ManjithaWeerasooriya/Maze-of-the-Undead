# Maze of the Undead

A first-person survival horror maze game built with Unity 6. Navigate through a labyrinthine environment, evade intelligent zombie enemies, and find your way to the exit while using doors and barricades to slow your pursuers.

## Game Description

You are trapped inside a sprawling maze. Zombies hunt you using smart AI pathfinding, dynamically rerouting around obstacles as you interact with the environment. Your tools are the doors and barricades scattered throughout the maze — block paths, slow enemies, and survive long enough to escape.

**Core mechanics:**
- First-person movement with walk, sprint, crouch, and jump
- Interactive doors that open with the `E` key and physically block zombie paths
- Barricades that obstruct doors and force AI path recalculation
- Zombie AI driven by NavMesh pathfinding that updates dynamically as the environment changes

## Development Tools

| Tool | Version |
|------|---------|
| Unity | 6000.4.4f1 |
| Render Pipeline | Universal Render Pipeline (URP) 17.4.0 |
| Unity AI Navigation | 2.0.12 (NavMesh) |
| Input System | 1.19.0 |
| Cinemachine | 3.1.6 |
| Timeline | 1.8.12 |
| Visual Scripting | 1.9.11 |
| Git LFS | 3.7.1 |
| Version Control | Git + GitHub |

**IDE:** Visual Studio / JetBrains Rider

## Getting Started

1. Clone the repository:
   ```bash
   git clone git@github.com:ManjithaWeerasooriya/Maze-of-the-Undead.git
   ```

2. Install Git LFS and pull assets:
   ```bash
   git lfs install
   git lfs pull
   ```

3. Open the project in Unity 6000.4.4f1 or later.

4. Open `Assets/scenes/Level_01.unity` and press Play.

**Controls:**

| Action | Key |
|--------|-----|
| Move | WASD |
| Sprint | Left Shift |
| Crouch | Left Ctrl |
| Jump | Space |
| Interact (doors) | E |

## Project Structure

```
Assets/
  Animations/         Zombie walk animation and controller
  LoafbrrAssets/      Backrooms-style environment meshes and textures
  Models/             Custom door and barricade models
  Prefabs/            Environment, Player, and Zombie prefabs
  scenes/             Level_01, test and integration scenes
  Scripts/            C# game logic (AI, player, doors, barricades)
  StarterAssets/      First-person controller base
  ThirdParty/         Zombie character model and textures
```

## Git LFS

This project stores large binary assets (textures, models, audio, video, archives) via Git Large File Storage. The `.gitattributes` file at the project root defines which file types are tracked by LFS.

### Issue encountered

After the initial environment assets were committed, `git status` started showing 32 PNG texture files as perpetually modified — even after a fresh pull with no actual changes made. The root cause was that those PNGs had been committed as raw binary blobs directly into Git history at a time when LFS was not active on the contributor's machine. The `.gitattributes` file had `*.png lfs` configured, so the LFS clean filter would produce a 133-byte pointer file from each working-tree PNG — which never matched the multi-megabyte blob stored in `HEAD`, causing Git to report every affected file as modified on every status check.

### Resolution

The history was rewritten using `git lfs migrate import` to retroactively convert those binary blobs into LFS pointers across all commits and branches:

```bash
echo "y" | git lfs migrate import --include="*.png,*.zip,*.fbx" --everything
```

This rewrites commit SHAs, so all collaborators were required to re-clone the repository rather than pull. After the migration, `git lfs fsck` returned clean and `git status` showed no spurious modifications.

### For new contributors

Every developer must run the following once after cloning:

```bash
git lfs install
git lfs pull
```

If you commit large binary files without LFS active, those blobs will inflate the repository and cause the same status noise described above. Ensure LFS is installed and `git lfs install` has been run before your first commit.
