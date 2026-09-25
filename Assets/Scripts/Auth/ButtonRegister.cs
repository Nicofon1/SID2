using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Crea la cuenta con solo correo y contraseña. El nombre que aparece en la
// tabla de puntajes se toma de la parte del correo antes de la "@".
public class ButtonRegister : MonoBehaviour
{
    [SerializeField]
    private Button _registerButton;
    [SerializeField]
    private TMP_InputField _emailInputField;
    [SerializeField]
    private TMP_InputField _passwordInputField;

    private void Reset()
    {
        _registerButton = GetComponent<Button>();
    }

    void Start()
    {
        _registerButton.onClick.AddListener(OnRegisterButtonClick);
    }

    private void OnRegisterButtonClick()
    {
        string email = _emailInputField.text.Trim();
        string password = _passwordInputField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            StatusMessage.Show("Escribe correo y contraseña.", true);
            return;
        }

        RegisterUser(email, password);
    }

    // Se usa ContinueWithOnMainThread en vez de una corrutina: al crear la cuenta Firebase
    // inicia sesión y AuthStateHandler oculta este panel, lo que detendría la corrutina.
    private void RegisterUser(string email, string password)
    {
        _registerButton.interactable = false;
        StatusMessage.Show("Creando cuenta...");

        FirebaseService.Auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(registerTask =>
        {
            if (registerTask.IsCanceled || registerTask.IsFaulted)
            {
                _registerButton.interactable = true;
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + registerTask.Exception);
                StatusMessage.Show(FirebaseErrors.DescribeAuth(registerTask.Exception), true);
                return;
            }

            FirebaseUser newUser = registerTask.Result.User;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.Email, newUser.UserId);

            string username = UsernameFromEmail(email);
            newUser.UpdateUserProfileAsync(new UserProfile { DisplayName = username });
            SaveUserData(newUser.UserId, username);
        });
    }

    public static string UsernameFromEmail(string email)
    {
        int at = email.IndexOf('@');
        string name = at > 0 ? email.Substring(0, at) : email;
        return name.Length > 16 ? name.Substring(0, 16) : name;
    }

    // users/{uid}: nombre visible y mejor puntaje.
    private void SaveUserData(string userId, string username)
    {
        var userData = new Dictionary<string, object>
        {
            { "username", username },
            { "score", 0 },
            { "creado", ServerValue.Timestamp }
        };

        FirebaseService.Users.Child(userId).UpdateChildrenAsync(userData).ContinueWithOnMainThread(saveTask =>
        {
            _registerButton.interactable = true;

            if (saveTask.IsCanceled || saveTask.IsFaulted)
            {
                Debug.LogError("Error guardando los datos del usuario: " + saveTask.Exception);
                StatusMessage.Show("La cuenta se creó, pero no se pudieron guardar tus datos.", true);
                return;
            }

            _passwordInputField.text = "";
        });
    }
}
