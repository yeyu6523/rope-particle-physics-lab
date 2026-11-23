# 绳索与粒子实验室

<!-- impeccable:product-schema 1 -->

## Platform

Windows desktop (WinForms, native executable).

## Stack

C# / .NET 10, Windows Forms and System.Drawing. Chosen as a professional default because the brief allows programming environments other than Unity and the available machine has .NET 10 but no Unity editor. The core simulation is separated from rendering.

## Product Purpose

Implement an interactive rope simulation and a particle system suitable for coursework submission. Deliver an uncompressed source project in a GitHub repository and a working Windows exe on the user's desktop.

## Users

Assumption: a Chinese-speaking student demonstrates the program to a course evaluator on Windows.

## Capabilities and Constraints

Confirmed: both rope and particle programs, source project rather than an archive or unitypackage, GitHub link, desktop exe.

Implementation defaults: one application with two simulation pages; rope constraints, drag, cut, collisions and presets; particles with fountain, fireworks, snow and vortex presets; adjustable parameters, pause/reset/single-step; Chinese UI and instructions; standalone win-x64 publishing.

## Evidence on Hand

The workspace initially contained only an empty Git repository. No reference image, original simulation, Unity project or additional grading rubric was supplied. Similarity to an unseen reference cannot be established; implement standard, observable physics demonstrations.

## Product Principles

- The simulations themselves occupy most of the window and run immediately.
- Every visible control changes observable simulation behavior.
- Reproducible build and tests are part of the source project.
- Distribution contains a directly runnable Windows executable.
