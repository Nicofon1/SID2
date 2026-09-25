using Firebase.Auth;
using Firebase.Database;
using TMPro;
using UnityEngine;

// Muestra el nombre de usuario y el mejor puntaje, escuchando users/{uid} en tiempo real.
public class ProfileLabels : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _greetingLabel;
    [SerializeField]
    private TMP_Text _bestScoreLabel;

    private DatabaseReference _userReference;

    void OnEnable()
    {
        FirebaseService.WhenReady(Subscribe);
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (!isActiveAndEnabled || _userReference != null) return;

        FirebaseUser user = FirebaseService.Auth.CurrentUser;
        if (user == null) return;

        string name = string.IsNullOrEmpty(user.DisplayName) ? ButtonRegister.UsernameFromEmail(user.Email) : user.DisplayName;
        SetGreeting(name);
        _bestScoreLabel.text = "RÉCORD : ...";

        _userReference = FirebaseService.Users.Child(user.UserId);
        _userReference.ValueChanged += HandleValueChanged;
    }

    private void Unsubscribe()
    {
        if (_userReference == null) return;
        _userReference.ValueChanged -= HandleValueChanged;
        _userReference = null;
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error leyendo el perfil: " + args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        if (snapshot == null || !snapshot.Exists) return;

        string username = snapshot.Child("username").Value as string;
        if (!string.IsNullOrEmpty(username)) SetGreeting(username);

        _bestScoreLabel.text = "RÉCORD : " + FirebaseService.ToLong(snapshot.Child("score").Value);
    }

    private void SetGreeting(string name)
    {
        _greetingLabel.text = "> " + name.ToUpperInvariant();
    }
}
