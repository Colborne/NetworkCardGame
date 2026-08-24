using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveDeck
{
    public static void Save(List<int> cardCounts)
    {
        using FileStream fs = new FileStream(Application.persistentDataPath + "/Deck.dat", FileMode.Create);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, cardCounts);
    }

    public static List<int> Load()
    {
        string path = Application.persistentDataPath + "/Deck.dat";
        if(File.Exists(path))
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return formatter.Deserialize(stream) as List<int>;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Unable to load the saved deck. A new deck will be used instead. {exception.Message}");
                return null;
            }
        }
        else
            return null;
    }
}
