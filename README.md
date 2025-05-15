This is a Private Repo made public only to temporarily share with people who are already knowledgeable about this project.
This project uses PlasticSCM for version control, and
for that reason, there is little to no documentation in this repo.

# NimbusNET: Aerial Threat Interception Simulator

**Algorithm**: TOPSIS
**Platform**: Unity (C#)

## Project Overview

This project simulates a missile defense system focused on solving an Embedding Problem — how to prioritize threats and allocate interceptors efficiently in large-scale attacks.

## How the Algorithm Works

* Each threat is evaluated based on multiple criteria (e.g., Alt, size, impact zone, interception cost).
* Using the TOPSIS algorithm:

  * Calculate ideal and anti-ideal threat vectors.
  * Normalize the decision matrix.
  * Rank threats by proximity to the ideal solution.
* Assign interceptors to threats based on the highest match score and operational constraints.

## Appendix

* Main focus: Logic behind threat prioritization and interceptor allocation.
* Visualization: 3D simulation in Unity.
* UNDER CONSTRUCTION: This project is a work in progress.
