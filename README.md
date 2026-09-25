# Atrapa Estrellas — Unity + Firebase (Serverless)

**Autor:** Sergio Nicolás Fonseca Niño
**Curso:** Sistemas Interactivos Distribuidos

Aplicación Unity (Android) con arquitectura **serverless**: todo el backend lo gestiona **Firebase**
(Authentication + Realtime Database). No hay servidor propio.

## Funcionalidades

| Requisito | Implementación |
|---|---|
| Proyecto Firebase para Unity-Android | `Assets/google-services.json`, package `com.nicofonseca.sid2` |
| Registro, login y recuperación de contraseña por correo | `ButtonRegister`, `ButtonLogin`, `ButtonResetPassword`, `ButtonLogout`, `AuthStateHandler` |
| Nombre de usuario y datos adicionales en el registro | Se guardan en `users/{uid}`: `username`, `edad`, `pais` |
| Guardar puntajes de un juego funcional | Juego **Atrapa Estrellas** (`CatchGameManager`); `ScoreService` guarda el mejor puntaje con una transacción y el historial con `Push` |
| Tabla de puntajes más altos | `Leaderboard`: `OrderByChild("score").LimitToLast(10)` con `ValueChanged` (**tiempo real**) |
| Nombre completo visible | Pie de página en todas las pantallas (`AuthorFooter`, `ProjectInfo`) |

## El juego

Atrapa los cuadros **verdes** (+10) y **dorados** (+50) con la barra azul y esquiva las bolas **rojas**.
Si dejas caer un verde o atrapas una roja pierdes una vida (3 vidas). La velocidad aumenta con el tiempo.
Controles: mouse, dedo (touch) o flechas / A-D.

## Estructura de la base de datos

```
users/
  {uid}/
    username: "sergio"
    edad: 21
    pais: "Colombia"
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
  UI/      UIManager, NavigationButton, StatusMessage, ProfileLabels, Leaderboard, AuthorFooter
  Game/    CatchGameManager, PlayerCatcher, FallingItem, GameButton, SpriteFactory
  Editor/  FirebaseSceneBuilder (construye la interfaz en la escena)
```
