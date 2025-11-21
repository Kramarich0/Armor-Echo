# Diff Details

Date : 2025-11-14 05:30:45

Directory c:\\Users\\karen\\Armor Echo\\Assets\\Scripts

Total : 83 files,  400 codes, -59 comments, 93 blanks, all 434 lines

[Summary](results.md) / [Details](details.md) / [Diff Summary](diff.md) / Diff Details

## Files
| filename | language | code | comment | blank | total |
| :--- | :--- | ---: | ---: | ---: | ---: |
| [Assets/Scripts/AI/HealthAiDisplay.cs](/Assets/Scripts/AI/HealthAiDisplay.cs) | C# | -51 | 0 | -16 | -67 |
| [Assets/Scripts/AI/HealthMarker.cs](/Assets/Scripts/AI/HealthMarker.cs) | C# | -83 | 0 | -15 | -98 |
| [Assets/Scripts/AI/TankAi.cs](/Assets/Scripts/AI/TankAi.cs) | C# | -427 | -18 | -65 | -510 |
| [Assets/Scripts/Base/Bullet.cs](/Assets/Scripts/Base/Bullet.cs) | C# | -51 | 0 | -12 | -63 |
| [Assets/Scripts/Base/CapturePoint.cs](/Assets/Scripts/Base/CapturePoint.cs) | C# | -227 | -26 | -39 | -292 |
| [Assets/Scripts/Base/IDamageable.cs](/Assets/Scripts/Base/IDamageable.cs) | C# | -4 | 0 | 0 | -4 |
| [Assets/Scripts/Base/TankHealth.cs](/Assets/Scripts/Base/TankHealth.cs) | C# | -70 | 0 | -18 | -88 |
| [Assets/Scripts/Base/TeamComponent.cs](/Assets/Scripts/Base/TeamComponent.cs) | C# | -7 | 0 | -2 | -9 |
| [Assets/Scripts/Base/TeamMarker.cs](/Assets/Scripts/Base/TeamMarker.cs) | C# | -90 | 0 | -18 | -108 |
| [Assets/Scripts/Core/Base/Bullet.cs](/Assets/Scripts/Core/Base/Bullet.cs) | C# | 102 | 1 | 23 | 126 |
| [Assets/Scripts/Core/Base/BulletPool.cs](/Assets/Scripts/Core/Base/BulletPool.cs) | C# | 41 | 0 | 12 | 53 |
| [Assets/Scripts/Core/Base/Interfaces.cs](/Assets/Scripts/Core/Base/Interfaces.cs) | C# | 13 | 0 | 3 | 16 |
| [Assets/Scripts/Core/Base/TankCollisionDamage.cs](/Assets/Scripts/Core/Base/TankCollisionDamage.cs) | C# | 67 | 0 | 24 | 91 |
| [Assets/Scripts/Core/Base/TankHealth.cs](/Assets/Scripts/Core/Base/TankHealth.cs) | C# | 77 | 0 | 19 | 96 |
| [Assets/Scripts/Core/Base/TeamComponent.cs](/Assets/Scripts/Core/Base/TeamComponent.cs) | C# | 7 | 0 | 2 | 9 |
| [Assets/Scripts/Core/Base/TeamMarker.cs](/Assets/Scripts/Core/Base/TeamMarker.cs) | C# | 90 | 0 | 18 | 108 |
| [Assets/Scripts/Core/Enums/CapturePointEnum.cs](/Assets/Scripts/Core/Enums/CapturePointEnum.cs) | C# | 6 | 0 | 1 | 7 |
| [Assets/Scripts/Core/Enums/TeamEnum.cs](/Assets/Scripts/Core/Enums/TeamEnum.cs) | C# | 6 | 0 | 1 | 7 |
| [Assets/Scripts/Enum/CapturePointEnum.cs](/Assets/Scripts/Enum/CapturePointEnum.cs) | C# | -6 | 0 | -1 | -7 |
| [Assets/Scripts/Enum/TeamEnum.cs](/Assets/Scripts/Enum/TeamEnum.cs) | C# | -6 | 0 | -1 | -7 |
| [Assets/Scripts/GamePlay/AI/HealthAiDisplay.cs](/Assets/Scripts/GamePlay/AI/HealthAiDisplay.cs) | C# | 51 | 0 | 16 | 67 |
| [Assets/Scripts/GamePlay/AI/HealthMarker.cs](/Assets/Scripts/GamePlay/AI/HealthMarker.cs) | C# | 83 | 0 | 15 | 98 |
| [Assets/Scripts/GamePlay/AI/TankAi.cs](/Assets/Scripts/GamePlay/AI/TankAi.cs) | C# | 516 | 1 | 84 | 601 |
| [Assets/Scripts/GamePlay/CaptureSystem/CapturePoint.cs](/Assets/Scripts/GamePlay/CaptureSystem/CapturePoint.cs) | C# | 227 | 0 | 65 | 292 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TankMovement.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TankMovement.cs) | C# | 195 | 0 | 52 | 247 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TankShoot.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TankShoot.cs) | C# | 120 | 0 | 29 | 149 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TankSniperView.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TankSniperView.cs) | C# | 156 | 0 | 41 | 197 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TankTrack.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TankTrack.cs) | C# | 18 | 0 | 6 | 24 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TankWheelSetup.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TankWheelSetup.cs) | C# | 42 | 0 | 22 | 64 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TrackController.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TrackController.cs) | C# | 49 | 0 | 14 | 63 |
| [Assets/Scripts/GamePlay/Vehicles/Tank/TurretAiming.cs](/Assets/Scripts/GamePlay/Vehicles/Tank/TurretAiming.cs) | C# | 149 | 0 | 37 | 186 |
| [Assets/Scripts/Managers/CursorManager.cs](/Assets/Scripts/Managers/CursorManager.cs) | C# | -32 | 0 | -8 | -40 |
| [Assets/Scripts/Managers/GameManager.cs](/Assets/Scripts/Managers/GameManager.cs) | C# | -213 | 0 | -44 | -257 |
| [Assets/Scripts/Managers/GameUIManager.cs](/Assets/Scripts/Managers/GameUIManager.cs) | C# | -259 | 0 | -63 | -322 |
| [Assets/Scripts/Managers/LevelInitializer.cs](/Assets/Scripts/Managers/LevelInitializer.cs) | C# | -8 | 0 | -1 | -9 |
| [Assets/Scripts/Managers/LevelSelectManager.cs](/Assets/Scripts/Managers/LevelSelectManager.cs) | C# | -80 | 0 | -22 | -102 |
| [Assets/Scripts/Managers/MenuManager.cs](/Assets/Scripts/Managers/MenuManager.cs) | C# | -13 | 0 | -3 | -16 |
| [Assets/Scripts/Managers/MusicManager.cs](/Assets/Scripts/Managers/MusicManager.cs) | C# | -53 | 0 | -10 | -63 |
| [Assets/Scripts/Network/GameNetUiManager.cs](/Assets/Scripts/Network/GameNetUiManager.cs) | C# | -20 | 0 | -4 | -24 |
| [Assets/Scripts/Network/MenuNetworkUI.cs](/Assets/Scripts/Network/MenuNetworkUI.cs) | C# | -18 | 0 | -7 | -25 |
| [Assets/Scripts/Network/NetworkOwnerInput.cs](/Assets/Scripts/Network/NetworkOwnerInput.cs) | C# | -18 | 0 | -2 | -20 |
| [Assets/Scripts/Network/PersistentNetworkManager.cs](/Assets/Scripts/Network/PersistentNetworkManager.cs) | C# | -32 | 0 | -7 | -39 |
| [Assets/Scripts/Network/PlayerSetup.cs](/Assets/Scripts/Network/PlayerSetup.cs) | C# | 0 | -40 | -6 | -46 |
| [Assets/Scripts/Networking/GameNetUiManager.cs](/Assets/Scripts/Networking/GameNetUiManager.cs) | C# | 20 | 0 | 4 | 24 |
| [Assets/Scripts/Networking/MenuNetworkUI.cs](/Assets/Scripts/Networking/MenuNetworkUI.cs) | C# | 18 | 0 | 7 | 25 |
| [Assets/Scripts/Networking/NetworkOwnerInput.cs](/Assets/Scripts/Networking/NetworkOwnerInput.cs) | C# | 18 | 0 | 2 | 20 |
| [Assets/Scripts/Networking/PersistentNetworkManager.cs](/Assets/Scripts/Networking/PersistentNetworkManager.cs) | C# | 32 | 0 | 7 | 39 |
| [Assets/Scripts/Networking/PlayerSetup.cs](/Assets/Scripts/Networking/PlayerSetup.cs) | C# | 0 | 40 | 6 | 46 |
| [Assets/Scripts/Player/CrossHairAim.cs](/Assets/Scripts/Player/CrossHairAim.cs) | C# | -44 | 0 | -8 | -52 |
| [Assets/Scripts/Player/GunElevation.cs](/Assets/Scripts/Player/GunElevation.cs) | C# | -29 | 0 | -17 | -46 |
| [Assets/Scripts/Player/TankMovement.cs](/Assets/Scripts/Player/TankMovement.cs) | C# | -150 | -3 | -49 | -202 |
| [Assets/Scripts/Player/TankShoot.cs](/Assets/Scripts/Player/TankShoot.cs) | C# | -115 | 0 | -32 | -147 |
| [Assets/Scripts/Player/TankSniperView.cs](/Assets/Scripts/Player/TankSniperView.cs) | C# | -137 | -14 | -39 | -190 |
| [Assets/Scripts/Player/TrackController.cs](/Assets/Scripts/Player/TrackController.cs) | C# | -65 | 0 | -24 | -89 |
| [Assets/Scripts/Player/TurretAiming.cs](/Assets/Scripts/Player/TurretAiming.cs) | C# | -91 | 0 | -32 | -123 |
| [Assets/Scripts/Player/TurretMovement.cs](/Assets/Scripts/Player/TurretMovement.cs) | C# | -34 | 0 | -18 | -52 |
| [Assets/Scripts/Systems/AudioManager.cs](/Assets/Scripts/Systems/AudioManager.cs) | C# | 23 | 0 | 4 | 27 |
| [Assets/Scripts/Systems/CursorManager.cs](/Assets/Scripts/Systems/CursorManager.cs) | C# | 32 | 0 | 8 | 40 |
| [Assets/Scripts/Systems/GameManager.cs](/Assets/Scripts/Systems/GameManager.cs) | C# | 225 | 0 | 47 | 272 |
| [Assets/Scripts/Systems/GameUIManager.cs](/Assets/Scripts/Systems/GameUIManager.cs) | C# | 259 | 0 | 63 | 322 |
| [Assets/Scripts/Systems/LevelInitializer.cs](/Assets/Scripts/Systems/LevelInitializer.cs) | C# | 8 | 0 | 1 | 9 |
| [Assets/Scripts/Systems/LevelSelectManager.cs](/Assets/Scripts/Systems/LevelSelectManager.cs) | C# | 80 | 0 | 22 | 102 |
| [Assets/Scripts/Systems/MenuManager.cs](/Assets/Scripts/Systems/MenuManager.cs) | C# | 13 | 0 | 3 | 16 |
| [Assets/Scripts/Systems/MusicManager.cs](/Assets/Scripts/Systems/MusicManager.cs) | C# | 53 | 0 | 10 | 63 |
| [Assets/Scripts/UI/CameraSwitchers.cs](/Assets/Scripts/UI/CameraSwitchers.cs) | C# | -22 | 0 | -6 | -28 |
| [Assets/Scripts/UI/Displays/FpsDisplay.cs](/Assets/Scripts/UI/Displays/FpsDisplay.cs) | C# | 25 | 0 | 7 | 32 |
| [Assets/Scripts/UI/Displays/HealthDisplay.cs](/Assets/Scripts/UI/Displays/HealthDisplay.cs) | C# | 41 | 0 | 9 | 50 |
| [Assets/Scripts/UI/Displays/KillLogDisplay.cs](/Assets/Scripts/UI/Displays/KillLogDisplay.cs) | C# | 107 | 0 | 20 | 127 |
| [Assets/Scripts/UI/Displays/ReloadDisplay.cs](/Assets/Scripts/UI/Displays/ReloadDisplay.cs) | C# | 42 | 0 | 5 | 47 |
| [Assets/Scripts/UI/Displays/SpeedDisplay.cs](/Assets/Scripts/UI/Displays/SpeedDisplay.cs) | C# | 20 | 0 | 4 | 24 |
| [Assets/Scripts/UI/Displays/StarsDisplay.cs](/Assets/Scripts/UI/Displays/StarsDisplay.cs) | C# | 55 | 0 | 14 | 69 |
| [Assets/Scripts/UI/Displays/TankCountDisplay.cs](/Assets/Scripts/UI/Displays/TankCountDisplay.cs) | C# | 46 | 1 | 12 | 59 |
| [Assets/Scripts/UI/Displays/TicketsDisplay.cs](/Assets/Scripts/UI/Displays/TicketsDisplay.cs) | C# | 74 | 0 | 16 | 90 |
| [Assets/Scripts/UI/FpsDisplay.cs](/Assets/Scripts/UI/FpsDisplay.cs) | C# | -25 | 0 | -7 | -32 |
| [Assets/Scripts/UI/HUD/CameraSwitchers.cs](/Assets/Scripts/UI/HUD/CameraSwitchers.cs) | C# | 22 | 0 | 6 | 28 |
| [Assets/Scripts/UI/HUD/CrossHairAim.cs](/Assets/Scripts/UI/HUD/CrossHairAim.cs) | C# | 44 | 0 | 8 | 52 |
| [Assets/Scripts/UI/HealthDisplay.cs](/Assets/Scripts/UI/HealthDisplay.cs) | C# | -41 | 0 | -9 | -50 |
| [Assets/Scripts/UI/KillLogDisplay.cs](/Assets/Scripts/UI/KillLogDisplay.cs) | C# | -107 | 0 | -20 | -127 |
| [Assets/Scripts/UI/ReloadDisplay.cs](/Assets/Scripts/UI/ReloadDisplay.cs) | C# | -42 | 0 | -5 | -47 |
| [Assets/Scripts/UI/SpeedDisplay.cs](/Assets/Scripts/UI/SpeedDisplay.cs) | C# | -27 | 0 | -4 | -31 |
| [Assets/Scripts/UI/StarsDisplay.cs](/Assets/Scripts/UI/StarsDisplay.cs) | C# | -55 | 0 | -14 | -69 |
| [Assets/Scripts/UI/TankCountDisplay.cs](/Assets/Scripts/UI/TankCountDisplay.cs) | C# | -46 | -1 | -12 | -59 |
| [Assets/Scripts/UI/TicketsDisplay.cs](/Assets/Scripts/UI/TicketsDisplay.cs) | C# | -74 | 0 | -16 | -90 |

[Summary](results.md) / [Details](details.md) / [Diff Summary](diff.md) / Diff Details