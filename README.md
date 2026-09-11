# BarClip

BarClip is a cross-platform (iOS/mobile) application that automatically detects and trims the usable portion of weightlifting training videos, eliminating the dead time at the start and end of each clip.

## The Problem

As a competitive weightlifter, I record a lot of training footage to critique technique and share on social media. Every clip comes with unusable dead time at the start and end, from setting up the phone to walking away after the lift, and manually trimming each video after training gets old fast. Left untrimmed, footage also eats up phone storage unnecessarily.

BarClip solves this by automatically identifying when a lift starts and ends, trimming the video down to just the working portion for a single clip or an entire training session at once.

## How It Works

1. A custom-trained **YOLOv8** computer vision model detects weightlifting plates at every second of the video.
2. A custom multi-plate identity tracking system (`PlateIdentity`) locks onto and tracks each detected plate across frames — using a lock-in phase for the first several frames, then rank-based assignment afterward to maintain consistent tracking.
3. The tracked plate positions are translated into vertical motion data, which is used to algorithmically identify the start and end of each lift.
4. The video is automatically trimmed to the identified lifting window either as a single clip, or batch-processed and merged across an entire session.

The YOLOv8 model is converted to **ONNX** and run via ONNX Runtime, enabling fully on-device inference in C#/.NET without any server round-trip for video processing.

## Architecture

**Client (iOS / .NET MAUI)**
- .NET MAUI, C#, XAML — cross-platform UI
- MVVM architecture using CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`)
- On-device ML inference via ONNX Runtime (custom YOLOv8 model)
- Native video handling via AVFoundation (capture, export, composition)
- Local persistence via SQLite

**Backend (Azure)**
- RESTful API built with ASP.NET Core (`BarClipApi`)
- Entity Framework Core with SQL Server for relational data (users, video metadata)
- Azure Blob Storage for optional cloud video hosting
- Microsoft Entra External ID (MSAL) for authentication and user identity isolation
- Azure Functions for isolated, event-driven background tasks

**Solution Structure**
- `BarClip.Maui` — the mobile client application
- `BarClip.Core` — shared business logic and detection/tracking algorithms
- `BarClip.Data` — data access layer (EF Core)
- `BarClipApi` — the cloud backend API

**CI/CD**
- GitHub Actions pipeline builds and deploys the iOS app to TestFlight — including on-device testing workflows without requiring local Mac hardware
- Sentry integrated for crash reporting and diagnostics

## Tech Stack

`C#` `.NET MAUI` `ASP.NET Core` `Entity Framework Core` `SQL Server` `YOLOv8` `ONNX Runtime` `Azure Functions` `Azure Blob Storage` `Microsoft Entra External ID` `GitHub Actions` `Sentry`

## Demo



## Status

BarClip is an actively developed personal project that I use to process my own training footage. Core detection, tracking, and trimming are functional; further work is ongoing on storage optimization, UI polish, and expanded batch-processing features.
