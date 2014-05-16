using UnityEngine;
using System.Collections;

using System.IO;

public class UnityGlobals : MonoBehaviour
{
    /// <summary>
    /// Enum to say which folder is going to be used.
    /// </summary>
    public enum WorkingMode : byte
    {
        None = 0,
        Server,
        Client
    }

    public string ServerIOFolder = "ServerConfiguration";
    public string ClientIOFolder = "ClientConfiguration";

    public WorkingMode workingMode = WorkingMode.None;

    /// <summary>
    /// Path to the working directory used to work.
    /// </summary>
    public static string WorkingDirectory;

    void Awake()
    {
        DontDestroyOnLoad(this);
        switch (this.workingMode)
        {
            case WorkingMode.Server:
                UnityGlobals.WorkingDirectory = string.Format("{0}{1}{2}{3}", Path.GetDirectoryName( Application.dataPath ), Path.DirectorySeparatorChar, ServerIOFolder, Path.DirectorySeparatorChar);
                break;
            case WorkingMode.Client:
                UnityGlobals.WorkingDirectory = string.Format("{0}{1}{2}{3}", Path.GetDirectoryName( Application.dataPath ), Path.DirectorySeparatorChar, ClientIOFolder, Path.DirectorySeparatorChar);
                break;
            case WorkingMode.None:
                Debug.LogWarning("Not working mode assigned");
                break;
        }
        if (!Directory.Exists(UnityGlobals.WorkingDirectory))
        {
            Debug.Log(string.Format("Directory does not exists [\"{0}\"], creating directory.", UnityGlobals.WorkingDirectory));
            try
            {
                Directory.CreateDirectory(UnityGlobals.WorkingDirectory);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
