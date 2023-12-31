using Synology.Api.Client;
using System.Text;
using UnityEngine;

public class Main : MonoBehaviour
{
    public string username = "";
    public string passwd = "";
    public string dsmUrl = "";
    public string filePath = "";
    public string destPath = "";
    // Start is called before the first frame update
    async void Start()
    {
        Debug.Log(Encoding.Default);

        var client = new SynologyClient(dsmUrl);
        // Authenticate
        await client.LoginAsync(username, passwd);
        // Upload file
        var uploadResult = await client.FileStationApi().UploadEndpoint().UploadAsync(filePath, destPath, true);
        
        Debug.Log(uploadResult);

        // End session
        await client.LogoutAsync();
    }
}
