using UnityEngine;

// Her zaman buradan baslanir. Tek isi menuye gecmek; ileride kaydedilmis
// ayar yuklemek gibi tek seferlik hazirliklar da buraya girer.
public class Boot : MonoBehaviour
{
    void Start()
    {
        GameFlow.LoadMenu();
    }
}
