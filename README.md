# Controllify

A Spotify interface designed for TVs and game controllers, including Xbox and PlayStation. You can also use it with a keyboard.

**Open Controllify here:** https://valadal.github.io/Controllify/

> Unofficial project. Not affiliated with Spotify AB. Spotify is a trademark of Spotify AB.

## Features

- Browse your playlists, Liked Songs, recently played tracks, and top artists.
- Open playlists and view their tracks.
- Control the app with a game controller or keyboard.
- Navigate using the menu and view cover art in full screen.
- Install it as an app on your computer.

## Before you start

You need:

- **Spotify Premium** to play music through the web player.
- A compatible browser, such as Chrome or Edge.
- Your own **Spotify Client ID**. Creating an app in the Spotify Developer Dashboard is free.

## First-time setup

Each person must connect their own Spotify account and use their own Client ID. You do not need to install anything or run the page on your computer.

### 1. Create a Spotify app

1. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/).
2. Create a new app.
3. If prompted to choose products or APIs, enable **Web API** and **Web Playback SDK**.
4. Open your app's settings.

### 2. Set the Redirect URI

In your Spotify app settings, find **Redirect URIs** and add this exact address:

`https://valadal.github.io/Controllify/`

Save your changes. The address must match exactly, including the final `/`.

### 3. Copy your Client ID

1. In the Spotify dashboard, copy your app's **Client ID**.
2. Do not copy or share your **Client Secret**.
3. Open [Controllify](https://valadal.github.io/Controllify/).
4. Paste your Client ID into the setup screen.
5. Follow the instructions to connect your Spotify account and authorize access.

**That's it!** After the initial setup, open the same page whenever you want to use Controllify. Your Client ID and access tokens are stored in your browser, not in this repository.

## Install Controllify as an app

If you'd rather open it like an app instead of a browser tab:

1. Open [Controllify](https://valadal.github.io/Controllify/) in Chrome or Edge.
2. Open the browser menu.
3. Look for **Install page as app** or **Install this site as an app**.
4. Once installed, launch Controllify from its new icon.

To enter full-screen mode, press `F` or `F11`. You can also press **Start** inside the app.

## Controls

Press **View** or the `H` key inside the app to see the controls on screen.

| Action | Xbox | PlayStation | Keyboard |
|---|---|---|---|
| Move | D-pad / left stick | D-pad / left stick | Arrow keys |
| Play / open | A | ✕ | Enter |
| Back | B | ○ | Backspace |
| Pause / resume | X | □ | Space |
| Previous / next track | LB / RB | L1 / R1 | P / N |
| Open menu | Y | △ | M or Esc |
| Volume | Right stick ↑↓ | Right stick ↑↓ | + / − |
| Seek 10 seconds | Right stick ←→ | Right stick ←→ | , / . |
| View cover art | Menu ☰ | OPTIONS | V |
| Show controls | View | CREATE | H or ? |
| Full screen | — | — | F / F11 |

## Compatibility

- **Windows with Chrome or Edge and an Xbox controller:** full support.
- **PlayStation controllers:** work in desktop browsers.
- **Xbox console browser:** the interface and controller may work, but playback depends on browser compatibility.

## Known limitations

- Spotify only allows the app to list tracks from playlists you own or collaborate on. For some other playlists, the app may play music without displaying the track list.
- The app loads up to the first 100 tracks of a playlist and the first 50 Liked Songs.

## Privacy

Controllify uses **Authorization Code with PKCE**, so it does not need a server or a Client Secret. Your Client ID and access tokens are stored only in your browser's local storage (`localStorage`).

To disconnect your account, open **Settings** in Controllify and sign out.

## License

MIT License. See the [LICENSE](LICENSE) file.
