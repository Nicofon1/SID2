using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

// Escucha el estado de autenticación de Firebase y muestra el panel que corresponde.
public class AuthStateHandler : MonoBehaviour
{
    private bool _stateDirty;
    private bool _subscribed;
    private string _currentUserId;
    private bool _hasHandledFirstState;

    void Start()
    {
        StatusMessage.Show("Conectando con Firebase...");
        FirebaseService.WhenReady(() =>
        {
            FirebaseService.Auth.StateChanged += AuthStateChanged;
            _subscribed = true;
            _stateDirty = true;
        });
    }

    void OnDestroy()
    {
        if (_subscribed) FirebaseService.Auth.StateChanged -= AuthStateChanged;
    }

    private void AuthStateChanged(object sender, System.EventArgs e)
    {
        // El evento puede llegar fuera del hilo principal; la UI se actualiza en Update.
        _stateDirty = true;
    }

    void Update()
    {
        if (!_stateDirty) return;
        _stateDirty = false;

        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        string userId = user != null ? user.UserId : null;

        // StateChanged también se dispara al refrescar el token; solo reaccionamos si cambió el usuario.
        if (_hasHandledFirstState && userId == _currentUserId) return;
        _hasHandledFirstState = true;
        _currentUserId = userId;

        if (user != null)
        {
            Debug.Log("User is signed in: " + user.Email);
            EnsureUsername(user);
            UIManager.Instance.Show(AppScreen.Home);
        }
        else
        {
            if (CatchGameManager.Instance != null) CatchGameManager.Instance.StopGame();
            UIManager.Instance.Show(AppScreen.Login);
        }
    }

    // Cuentas creadas antes (o cuyo registro no alcanzó a guardar el nombre) no tienen
    // users/{uid}/username y en la tabla salían como "Anónimo". Se completa al iniciar sesión.
    private static void EnsureUsername(FirebaseUser user)
    {
        var usernameReference = FirebaseService.Users.Child(user.UserId).Child("username");
        usernameReference.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("No se pudo leer el nombre de usuario: " + task.Exception);
                return;
            }
            if (!string.IsNullOrEmpty(task.Result.Value as string)) return;

            string username = string.IsNullOrEmpty(user.DisplayName)
                ? ButtonRegister.UsernameFromEmail(user.Email ?? "")
                : user.DisplayName;
            if (string.IsNullOrEmpty(username)) return;
            usernameReference.SetValueAsync(username);
        });
    }
}
