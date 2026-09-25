# Atrapa Estrellas — Unity + Firebase (Serverless)

**Autor:** Sergio Nicolás Fonseca Niño
**Curso:** Sistemas Interactivos Distribuidos

Aplicación Unity (Android) con arquitectura **serverless**: todo el backend lo gestiona **Firebase**
(Authentication + Realtime Database). No hay servidor propio.

## Funcionalidades

| Requisito | Implementación |
|---|---|
| Proyecto Firebase para Unity-Android | `Assets/google-services.json`, package `com.nicofonseca.sid2` |
| Registro, login y recuperación de contraseña por correo | Una sola pantalla con correo y contraseña: `ButtonRegister`, `ButtonLogin`, `ButtonResetPassword`, `ButtonLogout`, `AuthStateHandler` |
| Nombre de usuario | Se toma de la parte del correo antes de la `@` y se guarda en `users/{uid}/username` |
| Guardar puntajes de un juego funcional | Juego **Atrapa Estrellas** (`CatchGameManager`); `ScoreService` guarda el mejor puntaje con una transacción y el historial con `Push` |
| Tabla de puntajes más altos | `Leaderboard`: `OrderByChild("score").LimitToLast(10)` con `ValueChanged` (**tiempo real**) |
| Nombre completo visible | Pie de página en todas las pantallas (`AuthorFooter`, `ProjectInfo`) |

## El juego

Atrapa los cuadros **verdes** (+10) y **verde claro** (+50) con la barra y esquiva los círculos **rosa neón**.
Si dejas caer un cuadro o atrapas un círculo pierdes una vida (3 vidas). La velocidad aumenta con el tiempo.
Controles: mouse, dedo (touch) o flechas / A-D.

## Estructura de la base de datos

```
users/
  {uid}/
    username: "sergio"
    score: 340          ← mejor puntaje (se usa para el leaderboard)
    creado: 1695651234000
scores/
  {uid}/
    {pushId}/ { score: 340, fecha: 1695651234000 }   ← historial de partidas
```

## Reglas de la Realtime Database

```json
{
  "rules": {
    "users": {
      ".read": "auth != null",
      ".indexOn": ["score"],
      "$uid": {
        ".write": "auth != null && auth.uid === $uid"
      }
    },
    "scores": {
      "$uid": {
        ".read": "auth != null && auth.uid === $uid",
        ".write": "auth != null && auth.uid === $uid"
      }
    }
  }
}
```

## Estética

Interfaz estilo terminal: verde sobre negro, fuente monoespaciada **VT323** (licencia OFL, `Assets/Fonts`),
opciones de menú que se invierten al pasar el mouse y líneas de barrido CRT. Los colores están en `TerminalTheme`.

## Cómo abrir y ejecutar

1. Abrir el proyecto con **Unity 6000.5.8f1** con la plataforma **Android** activa.
2. Abrir `Assets/Scenes/SampleScene.unity`.
3. Si la escena aún no tiene la interfaz de Firebase: menú **SID2 → Construir escena Firebase** y guardar (Ctrl+S).
4. Dar **Play**.

La URL de la base de datos está en `Assets/Scripts/Core/FirebaseService.cs` (`DatabaseUrl`).

## Scripts

```
Assets/Scripts/
  Core/    FirebaseService (inicialización), FirebaseErrors, ScoreService, ProjectInfo
  Auth/    ButtonRegister, ButtonLogin, ButtonLogout, ButtonResetPassword, AuthStateHandler
  UI/      UIManager, NavigationButton, StatusMessage, ProfileLabels, Leaderboard, AuthorFooter,
           TerminalTheme (paleta), TerminalButton, CrtOverlay
  Game/    CatchGameManager, PlayerCatcher, FallingItem, GameButton, SpriteFactory
  Editor/  FirebaseSceneBuilder (construye la interfaz en la escena)
```
