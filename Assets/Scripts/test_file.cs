using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using Unity.VisualScripting;

[Serializable]
public class my_class{
    public int x;
    public int y;

    public List <int> list;
}

class test_file : MonoBehaviour{
    void Start()
    {
        my_class cls = new my_class {x = 5, y = 10, list = new List <int> () {4, 3, 2, 1}};
        Debug.Log($"cls = {cls.x}, {cls.y}, {cls.list}");

        RoadGraph rg = FindObjectOfType<RoadGraph>();
        // graphNodes node_copy = new graphNodes {Nodes = rg.Nodes};

        // BinaryFormatter formatter = new BinaryFormatter();
        // using (FileStream stream = new FileStream("myObject.dat", FileMode.Create))
        // {
        //     formatter.Serialize(stream, node_copy);
        // }

        // using (FileStream stream = new FileStream("myObject.dat", FileMode.Open)){
        //     node_copy = (graphNodes)formatter.Deserialize(stream);
        // }
        // // Debug.Log(Directory.GetCurrentDirectory());
        // Debug.Log($"{node_copy.Nodes.Count}, {node_copy.Nodes[0].Position} {node_copy.Nodes[0].Outgoing}");
        // // Load the object
        // my_class loadedObject;
        // using (FileStream stream = new FileStream("myObject.dat", FileMode.Open))
        // {
        //     loadedObject = (my_class)formatter.Deserialize(stream);
        // }

        // Debug.Log($"Loaded object: {loadedObject.x}, {loadedObject.y}, {loadedObject.list}");

    }
}
