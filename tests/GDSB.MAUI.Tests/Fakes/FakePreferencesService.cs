using GDSB.MAUI.Services;

namespace GDSB.MAUI.Tests.Fakes
{
    // Substitui o Preferences.Default real por um dicionário em memória - é o que torna
    // UnlockViewModel/BiometricOptInCoordinator testáveis sem o runtime do MAUI.
    internal sealed class FakePreferencesService : IPreferencesService
    {
        private readonly Dictionary<string, object> _values = new();

        public string? GetString(string key, string? defaultValue) =>
            _values.TryGetValue(key, out var value) ? (string)value : defaultValue;

        public bool GetBool(string key, bool defaultValue) =>
            _values.TryGetValue(key, out var value) ? (bool)value : defaultValue;

        public void SetString(string key, string value) => _values[key] = value;

        public void SetBool(string key, bool value) => _values[key] = value;

        public void Remove(string key) => _values.Remove(key);
    }
}
