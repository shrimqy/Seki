# Seki

**Seki** is a custom-built Windows app designed to enhance your workflow by enabling seamless clipboard and notification sharing between your Windows PC and Android device. It's an alternative to existing solutions, tailored for users who want a straightforward and efficient way to keep their devices in sync.

## Features

- **Clipboard Sharing**: Instantly share clipboard content between your Windows PC and Android device.
- **Notification Mirroring**: Receive Android notifications directly on your Windows PC.
- **Planned Features**:
  - File Sharing
  - Notification Actions
  - Device Controls (e.g., screen sharing, remote input)

## Tech Stack

- **Windows**: WinUI 3, C#
- **Android**: [Sekia](https://github.com/shrimqy/Sekia) (Kotlin, Jetpack Compose)

## Installation

### Windows

1. **Download the Seki Installer**: [Seki Installer](#) (Link to the installer or release page).
2. **Install the Certificate**:
   - Manually install the provided certificate (to the personal certification store, Trusted Root Certification Authorities and Trusted People) in order to install the app .
3. **Run the App**:
   - Launch the Seki app and follow any on-screen instructions to connect with your Android device.

### Android

1. **Download the Android App**: [Sekia for Android](https://github.com/shrimqy/Sekia).
2. **Install the APK and Configure Permissions**:
   - Allow restricted access and grant notification permissions when prompted on your Android device.

## How to Use

1. **Set Up**:
   - Make sure both your Windows PC and Android device are connected to the same network.
   - Open Seki on your Windows PC and Sekia on your Android device.
   - Follow the prompts to establish a connection between the two devices.
2. **Clipboard Sharing**:
   - Copy text or other content on one device, and paste it directly on the other.
3. **Notification Mirroring**:
   - View your Android notifications on your Windows PC and stay up to date without switching devices.

## Limitations

- **Manual Certificate Installation**:
  - The current version requires manual certificate installation to install the app, and the app's UI is still under development.
