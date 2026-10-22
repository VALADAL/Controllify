# Spotify Console UI

Interfaz de Spotify estilo consola, pensada para TV y control (Xbox, PlayStation y cualquier gamepad con mapeo estándar). Un solo archivo HTML, sin dependencias ni backend. · *Console-style Spotify interface for TV and gamepads. Single HTML file, no backend.* (UI in ES/EN)

> Proyecto no oficial. No está afiliado a Spotify AB. Spotify es marca de Spotify AB.

## Requisitos
- **Spotify Premium** (lo exige el Web Playback SDK).
- Navegador con soporte de DRM (Widevine): Chrome, Edge o Firefox en Windows, macOS o Linux.
- Una app creada en el [Spotify Developer Dashboard](https://developer.spotify.com/dashboard). Revisa ahí los requisitos vigentes para apps en modo desarrollo.

## Uso
1. Sirve la carpeta con cualquier servidor local, por ejemplo:
   ```bash
   python -m http.server 8888
   ```
2. Abre **`http://127.0.0.1:8888/`** (no `localhost`: Spotify no lo acepta como Redirect URI; `file://` tampoco funciona).
3. En el dashboard de Spotify, agrega ese mismo valor (`http://127.0.0.1:8888/`) en **Redirect URIs**.
4. Pega tu **Client ID** en la pantalla de configuración y conecta tu cuenta.

### Usarlo desde GitHub Pages (sin servidor local)
1. Sube el proyecto a un repositorio y activa **Settings → Pages** (rama `main`, carpeta raíz).
2. Tu dirección será `https://TU-USUARIO.github.io/NOMBRE-REPO/`. Esa misma dirección, con la barra final, es el Redirect URI que debes registrar en tu app de Spotify.
3. Cada persona usa **su propio Client ID**: se pega en la pantalla de configuración y se guarda solo en su navegador. El repositorio nunca contiene credenciales.

## Instalar como app (pantalla completa, sin barras del navegador)
1. Abre la página en Chrome o Edge (con el servidor local o desde HTTPS).
2. Menú ⋮ → **Transmitir, guardar y compartir → Instalar página como app** (en Edge: ⋯ → **Aplicaciones → Instalar este sitio como una aplicación**).
3. Ábrela desde el ícono nuevo del escritorio o del menú Inicio. Se abre en su propia ventana.
4. Al presionar **Entrar** la app pide pantalla completa sola. Si no lo hace, usa `F11` o la tecla `F`.

Para que arranque siempre en pantalla completa, edita las propiedades del acceso directo de la app y agrega al final del destino: `--start-fullscreen`.

Si cambias de puerto o de dirección, el navegador la trata como otra app: instálala de nuevo y actualiza el Redirect URI.

## Controles

| Acción | Xbox | PlayStation | Teclado |
|---|---|---|---|
| Moverse | Cruceta / stick izq. | Cruceta / stick izq. | Flechas |
| Reproducir | A | ✕ | Enter |
| Atrás | B | ○ | Backspace |
| Pausa / reanudar | X | □ | Espacio |
| Pista anterior / siguiente | LB / RB | L1 / R1 | P / N |
| Pantalla completa | — | — | F / F11 |
| Ver canciones de una playlist | A | ✕ | Enter |
| Menú de burbujas (Explorar / Playlist actual / Ajustes) | Y, luego ←→ y A | △, luego ←→ y ✕ | M o Esc |
| Carátula en pantalla completa | Menú ☰ | OPTIONS | V |
| Volumen | Stick der. ↑↓ | Stick der. ↑↓ | + / − |
| Adelantar / retroceder 10 s | Stick der. ←→ | Stick der. ←→ | . / , |

Las etiquetas de la barra inferior cambian solas según el control detectado. Con el navegador, presiona un botón del control una vez para que lo detecte.

## Compatibilidad
- **Windows + Chrome/Edge + control Xbox (USB, Bluetooth o adaptador):** soporte completo.
- **PlayStation (DualSense/DualShock):** funciona en un navegador de PC/Mac conectado al control. La PS5 no ofrece un navegador web propio.
- **Navegador de consola Xbox (Edge):** la interfaz y el control deberían funcionar, pero la reproducción depende de que el DRM esté disponible; no está verificado.

## Privacidad
Usa Authorization Code con PKCE: no hay secretos ni servidor. Tu Client ID y tokens se guardan solo en el `localStorage` de tu navegador. Puedes cerrar sesión desde Ajustes.

## Licencia
MIT, ver [LICENSE](LICENSE).
