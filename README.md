<p align="center">
  <img alt="Files hero image" src="./.github/readme-images/ReadmeHero.png" />
</p>

# Seki

**Seki** is a custom-built Windows app designed to enhance your workflow by enabling seamless clipboard and notification sharing between your Windows PC and Android device. It's an alternative to existing solutions, tailored for users who want a straightforward and efficient way to keep their devices in sync.
## Features

- **Clipboard Sharing**: Seamlessly share clipboard content between your Android device and Windows PC.
- **Media Control**: Control media playback and volume of your PC from android. 
- **File Sharing**: Share files between your devices easily.
- **Notification**: Allows toasting the notifications from your android in desktop.
- **Remote Control**: Control your phone from pc.

## Limitations

- **Clipboard Sharing on Android 13+**:
    - Due to Android's restrictions, clipboard sharing is only possible via the share sheet after Android 13.
- **Remote Control**:
   - Keyboard input works optimally on android 13+

## Installation

Since the app has been temporarily removed from the Windows Store, please use the [drive](https://drive.google.com/drive/u/4/folders/1DcjlZteJkj067erkfgd9pWoFBM9Icb_G) to download and install it.


<p align="left">
  <!-- Store Badge -->
  <a style="text-decoration:none" href="https://apps.microsoft.com/detail/9PJV6D1JPG0H?launch=true&mode=full" target="_blank" rel="noopener noreferrer">
    <picture>
      <source media="(prefers-color-scheme: light)" srcset=".github/./readme-images/StoreBadge-dark.png" width="220" />
      <img src=".github/./readme-images/StoreBadge-light.png" width="220" />
    </picture>
  </a>
</p>

## How to Use

1. **Download and Install the Android App**: [Sekia](https://github.com/shrimqy/Sekia/releases/)

1. **Setting Up**:
    - Permissions: Allow the app to post notifications, location access, and notification access (Allow restricted access from App Info only after trying to grant notification access).
    - Ensure both your Android device and Windows PC are connected to the same network.
    - Launch the app on your Windows PC.
    - Try to add a new device from the device card or device tab (Pull to refresh if it doesn't show any devices).

2. **Clipboard Sharing**:
    - Copy content on the desktop and it will automatically sync with your android (That is if you have enabled it from the settings).
    - To share clipboard from android you will have to manually sent it through the share sheet that shows after you perform a copy action).
3. **File Transfer**:
    - Use the share sheet from android/windows and select the app to share any files between the devices. 
4. **Remote Control**:
   - Click the button next to the connection status button which will give a prompt to cast on your phone, choose the entire screen.


## Screenshots

<p align="center">
  <img alt="Files hero image" src="./.github/readme-images/Screenshot.png" />
</p>

## Roadmap

- Notification Actions.
- Exploring Device Files.

## Tech Stack

- **Android**: [Sekia](https://github.com/shrimqy/Sekia) Kotlin, Jetpack Compose
- **Desktop**: WinUI 3, C#
- **Network**: [Mdns](https://github.com/meamod/MeaMod.DNS), [Websockets](https://github.com/chronoxor/NetCoreServer). 

