# Spawn System

## Scripts
- SpawnManager.cs — controls all spawning logic
- ZombieHealth.cs — tracks zombie health and fires death event

## How to set up
1. Run Tools > Setup Spawn Scene from the Unity menu
2. Press Play — player and zombies spawn automatically

## Wave system
- Wave 1 spawns 3 zombies
- Each wave adds 2 more zombies
- Next wave starts when all zombies are dead

## Events
- OnWaveStarted(int wave)
- OnWaveCompleted(int wave)
- OnZombieCountChanged(int count)
Subscribe to these from any UI or game manager script.
