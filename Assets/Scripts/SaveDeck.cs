using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveDeck
{
    public static void Save(List<int> cardCounts)
    {
        FileStream fs = new FileStream(Application.persistentDataPath + "/Deck.dat", FileMode.Create);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, cardCounts);
        fs.Close();
    }

    public static List<int> Load()
    {
        string path = Application.persistentDataPath + "/Deck.dat";
        if(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            return (List<int>)formatter.Deserialize(stream);
        }
        else
            return null;
    }
}
