using UnityEngine;
using System.IO;
using System;
public class FileDataHandler
{
    private string dataDirPath = "";
    private string dataFileName = "";

    public FileDataHandler(string dataDirPath, string dataFileName)
    {
        this.dataDirPath = dataDirPath;
        this.dataFileName = dataFileName;
    }

    public GameData Load()
    {
        string path = Path.Combine(dataDirPath, dataFileName);
        GameData loadedGameData = null;
        if (File.Exists(path))
        {
            try
            {
                string jsonDataIn = "";
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        jsonDataIn = reader.ReadToEnd();
                    }
                }

                loadedGameData = JsonUtility.FromJson<GameData>(jsonDataIn);
            }
            catch (Exception e)
            {
                Debug.LogError("Error occured when trying to load data from file: " + path + "\n" + e);
            }
        }
        return loadedGameData;
    }

    public void Save(GameData gameData)
    {
        string path = Path.Combine(dataDirPath, dataFileName);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string jsonDataOut = JsonUtility.ToJson(gameData, true);

            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(jsonDataOut);
                }
            }
        }
        catch (Exception e) 
        {
            Debug.LogError("Error occured when trying to save data to file: " + path + "\n" + e);
        }
    }
}
