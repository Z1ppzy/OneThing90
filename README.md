# OneThing90

OneThing90 is a focused Windows desktop app for the 90/90/1 habit: spend 90 minutes a day, for 90 days, on one important thing.

## What it does

- Tracks one primary goal for a 90-day path.
- Runs a 90-minute focus timer and a 15-minute rescue timer.
- Logs completed and partial sessions locally.
- Shows a 90-day progress grid with completed, missed, partial, today, and future days.
- Sends evening nudges when today's full session is still missing.
- Supports snooze actions, tray minimization, and launch-on-login.
- Stores data locally in `%LocalAppData%\OneThing90`.

## Requirements

- Windows 10/11
- .NET 9 SDK for development

## Run

```powershell
dotnet run
```

## Build

```powershell
dotnet build
```

## Design goals

- Native Windows app, no embedded browser runtime.
- Fast startup and near-zero idle CPU.
- Local-first data model.
- Clear daily workflow: start, focus, log, repeat.
