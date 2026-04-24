param(
    [string]$Root = (Resolve-Path "$PSScriptRoot\..").Path
)

$ErrorActionPreference = 'Stop'

$downloadDir = Join-Path $Root "ExternalAssets\_downloads"
$extractDir = Join-Path $Root "ExternalAssets\_extracted"
$targetArtDir = Join-Path $Root "Assets\_Game\Art\ThirdParty"
$targetAudioDir = Join-Path $Root "Assets\_Game\Audio\ThirdParty"
$resourcesModelsDir = Join-Path $Root "Assets\Resources\Models"
$resourcesAudioDir = Join-Path $Root "Assets\Resources\Audio"

New-Item -ItemType Directory -Path $downloadDir -Force | Out-Null
New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
New-Item -ItemType Directory -Path $targetArtDir -Force | Out-Null
New-Item -ItemType Directory -Path $targetAudioDir -Force | Out-Null
New-Item -ItemType Directory -Path $resourcesModelsDir -Force | Out-Null
New-Item -ItemType Directory -Path $resourcesAudioDir -Force | Out-Null

# Asset 1: 50 CC0 Sci-Fi SFX (OpenGameArt)
$sfxZip = Join-Path $downloadDir "sci-fi-sfx.zip"
$sfxExtract = Join-Path $extractDir "sci-fi-sfx"
$sfxTarget = Join-Path $targetAudioDir "OpenGameArt_50_CC0_SciFi_SFX"

Write-Host "Downloading CC0 sci-fi SFX..."
Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/sci-fi-sfx.zip" -OutFile $sfxZip

if (Test-Path $sfxExtract) { Remove-Item -Recurse -Force $sfxExtract }
Expand-Archive -Path $sfxZip -DestinationPath $sfxExtract -Force

if (Test-Path $sfxTarget) { Remove-Item -Recurse -Force $sfxTarget }
New-Item -ItemType Directory -Path $sfxTarget -Force | Out-Null

Copy-Item -Path (Join-Path $sfxExtract "*") -Destination $sfxTarget -Recurse -Force
Invoke-WebRequest -Uri "https://opengameart.org/content/50-cc0-sci-fi-sfx" -OutFile (Join-Path $sfxTarget "source_and_license.html")
Copy-Item -Path (Join-Path $sfxTarget "shoot_01.ogg") -Destination (Join-Path $resourcesAudioDir "shoot_01.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "shoot_02.ogg") -Destination (Join-Path $resourcesAudioDir "shoot_02.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "explosion_02.ogg") -Destination (Join-Path $resourcesAudioDir "explosion_02.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "retro_laser_02.ogg") -Destination (Join-Path $resourcesAudioDir "retro_laser_02.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "retro_explosion.ogg") -Destination (Join-Path $resourcesAudioDir "retro_explosion.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "terminal_07.ogg") -Destination (Join-Path $resourcesAudioDir "terminal_07.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "terminal_05.ogg") -Destination (Join-Path $resourcesAudioDir "terminal_05.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "beep_03.ogg") -Destination (Join-Path $resourcesAudioDir "beep_03.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "terminal_03.ogg") -Destination (Join-Path $resourcesAudioDir "terminal_03.ogg") -Force
Copy-Item -Path (Join-Path $sfxTarget "misc_09.ogg") -Destination (Join-Path $resourcesAudioDir "misc_09.ogg") -Force

# Asset 1b: Handgun reload sound effect (OpenGameArt)
$reloadTarget = Join-Path $targetAudioDir "OpenGameArt_HandgunReloadCC0"
New-Item -ItemType Directory -Path $reloadTarget -Force | Out-Null

Write-Host "Downloading dedicated reload SFX..."
Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/reload.wav" -OutFile (Join-Path $reloadTarget "reload.wav")
Invoke-WebRequest -Uri "https://opengameart.org/content/handgun-reload-sound-effect" -OutFile (Join-Path $reloadTarget "source_and_license.html")
Copy-Item -Path (Join-Path $reloadTarget "reload.wav") -Destination (Join-Path $resourcesAudioDir "reload_handgun.wav") -Force

# Asset 2: Orbitron font (OFL)
$fontTarget = Join-Path $Root "Assets\_Game\Art\Orbitron-Regular.ttf"
$fontLicenseTarget = Join-Path $Root "Assets\_Game\Art\Orbitron-OFL.txt"

Write-Host "Downloading Orbitron font..."
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/google/fonts/main/ofl/orbitron/Orbitron%5Bwght%5D.ttf" -OutFile $fontTarget
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/google/fonts/main/ofl/orbitron/OFL.txt" -OutFile $fontLicenseTarget

# Asset 3: generic sci-fi metal texture (OpenGameArt, CC-BY-SA 3.0)
$sciFiTextureDir = Join-Path $targetArtDir "OpenGameArt_SciFiMetal"
New-Item -ItemType Directory -Path $sciFiTextureDir -Force | Out-Null

Write-Host "Downloading sci-fi metal texture..."
Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/Texture_v5_1.png" -OutFile (Join-Path $sciFiTextureDir "Texture_v5_1.png")
Invoke-WebRequest -Uri "https://opengameart.org/content/generic-sci-fi-metal-texture" -OutFile (Join-Path $sciFiTextureDir "source_and_license.html")

# Asset 4: low poly animated guns (OpenGameArt, CC0)
$gunsZip = Join-Path $downloadDir "low-poly-animated-guns.zip"
$gunsExtract = Join-Path $extractDir "low-poly-animated-guns"
$gunsTarget = Join-Path $targetArtDir "OpenGameArt_LowPolyAnimatedGuns"

Write-Host "Downloading low poly gun models..."
Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/FPS%20Pack_0.zip" -OutFile $gunsZip

if (Test-Path $gunsExtract) { Remove-Item -Recurse -Force $gunsExtract }
Expand-Archive -Path $gunsZip -DestinationPath $gunsExtract -Force

if (Test-Path $gunsTarget) { Remove-Item -Recurse -Force $gunsTarget }
New-Item -ItemType Directory -Path $gunsTarget -Force | Out-Null

$gunsRoot = Join-Path $gunsExtract "FPS Pack"
Copy-Item -Path (Join-Path $gunsRoot "*") -Destination $gunsTarget -Recurse -Force
Invoke-WebRequest -Uri "https://opengameart.org/content/low-poly-animated-guns" -OutFile (Join-Path $gunsTarget "source_and_license.html")
Copy-Item -Path (Join-Path $gunsTarget "FBX\Shotgun.fbx") -Destination (Join-Path $resourcesModelsDir "Shotgun.fbx") -Force

# Asset 5: robot pack (OpenGameArt, CC0)
$robotsZip = Join-Path $downloadDir "robot-pack-3d.zip"
$robotsExtract = Join-Path $extractDir "robot-pack-3d"
$robotsTarget = Join-Path $targetArtDir "OpenGameArt_RobotPack3D"

Write-Host "Downloading robot models..."
Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/Robots.zip" -OutFile $robotsZip

if (Test-Path $robotsExtract) { Remove-Item -Recurse -Force $robotsExtract }
Expand-Archive -Path $robotsZip -DestinationPath $robotsExtract -Force

if (Test-Path $robotsTarget) { Remove-Item -Recurse -Force $robotsTarget }
New-Item -ItemType Directory -Path $robotsTarget -Force | Out-Null

Copy-Item -Path (Join-Path $robotsExtract "*") -Destination $robotsTarget -Recurse -Force
Invoke-WebRequest -Uri "https://opengameart.org/content/robot-pack-3d" -OutFile (Join-Path $robotsTarget "source_and_license.html")
Copy-Item -Path (Join-Path $robotsTarget "Model\BaseModel.dae") -Destination (Join-Path $resourcesModelsDir "BaseModel.dae") -Force
Copy-Item -Path (Join-Path $robotsTarget "Model\SmallerModel.dae") -Destination (Join-Path $resourcesModelsDir "SmallerModel.dae") -Force
Copy-Item -Path (Join-Path $robotsTarget "textures\*") -Destination $resourcesModelsDir -Recurse -Force

# Asset 6: animated mech pack (OpenGameArt, CC0)
$mechZip = Join-Path $downloadDir "animated_mech_pack.zip"
$mechExtract = Join-Path $extractDir "animated_mech_pack"
$mechTarget = Join-Path $targetArtDir "OpenGameArt_AnimatedMechPack"

Write-Host "Downloading animated mech models..."
Invoke-WebRequest -Uri "https://opengameart.org/sites/default/files/animated_mech_pack_-_march_2021.zip" -OutFile $mechZip

if (Test-Path $mechExtract) { Remove-Item -Recurse -Force $mechExtract }
Expand-Archive -Path $mechZip -DestinationPath $mechExtract -Force

if (Test-Path $mechTarget) { Remove-Item -Recurse -Force $mechTarget }
New-Item -ItemType Directory -Path $mechTarget -Force | Out-Null

$mechRoot = Join-Path $mechExtract "Animated Mech Pack - March 2021"
Copy-Item -Path (Join-Path $mechRoot "*") -Destination $mechTarget -Recurse -Force
Invoke-WebRequest -Uri "https://opengameart.org/content/animated-mech-pack" -OutFile (Join-Path $mechTarget "source_and_license.html")
Copy-Item -Path (Join-Path $mechTarget "FBX\Mike.fbx") -Destination (Join-Path $resourcesModelsDir "Mike.fbx") -Force
Copy-Item -Path (Join-Path $mechTarget "FBX\Stan.fbx") -Destination (Join-Path $resourcesModelsDir "Stan.fbx") -Force
Copy-Item -Path (Join-Path $mechTarget "Textures\Mike_Texture.png") -Destination (Join-Path $resourcesModelsDir "Mike_Texture.png") -Force
Copy-Item -Path (Join-Path $mechTarget "Textures\Stan_Texture.png") -Destination (Join-Path $resourcesModelsDir "Stan_Texture.png") -Force

Write-Host "Assets fetched successfully."

