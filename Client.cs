using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Runtime.Serialization;
using System.IO;

public class newClient : MonoBehaviour
{
    private const int MAX_CONNECTION = 100;

    private int port = 1234;

    private int hostId;
    private int webHostId;

    private int reliableChannel;
    private int unreliableChannel;

    private int connectionId;

    private float connectionTime;
    private bool isConnected = false;
    private bool isStarted = false;
    private byte error;

    private string playerName;

    private GameObject move;
    private GameObject Joint;

    private void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
        NetworkServer.Reset();
        hostId = NetworkTransport.AddHost(topo, port, null);
    }

    public void connect()
    {
        connectionId = NetworkTransport.Connect(hostId, "10.123.210.221", 8888, 0, out error);
        Debug.Log("connected");

        connectionTime = Time.time;
        isConnected = true;
    }

    private void Update()
    {

        int recHostId;
        int recConnectionId;
        int recChannelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out recConnectionId, out recChannelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.Nothing:         //1
                break;
            case NetworkEventType.ConnectEvent:    //2
                Debug.Log("player " + recConnectionId + "has connected");
                break;
            case NetworkEventType.DataEvent:       //3
                string msg = UTF8Encoding.UTF8.GetString(recBuffer);
                Stream stream = new MemoryStream(recBuffer);
                char[] delimiterChars = { ':' };
                string[] temp = msg.Split(delimiterChars);
                string[] joint = { temp[0], temp[1], temp[2], temp[3], temp[4], temp[5] };
                char[] Chars = { '|' };
                foreach (string t in joint)
                {
                    string[] pos = t.Split(Chars);
                    move = GameObject.Find(pos[0]);
                    move.transform.position = getVector3(pos[1]);
                    move.transform.eulerAngles = getVector3(pos[2]);
                }
                break;
            case NetworkEventType.DisconnectEvent: //4
                Debug.Log("player " + recConnectionId + "has disconnected");
                break;
        }
    }



    public void SendSocketMessage()
    {
        byte error;
        byte[] buffer = new byte[1024];
        string[] joints = { "C3_ARM1", "C3_ARM2", "C3_ARM3", "C3_ARM4", "C3_ARM5", "C3_ARM6" };
        string msg = null;
        foreach (string j in joints)
        {
            Joint = GameObject.Find(j);
            Vector3 Position = Joint.transform.position;
            Vector3 Rotation = Joint.transform.eulerAngles;
            string name = j;
            SerializableVector3 pos = new SerializableVector3(Position.x, Position.y, Position.z);
            SerializableVector3 rot = new SerializableVector3(Rotation.x, Rotation.y, Rotation.z);
            msg += name + "|" + pos.ToString() + "|" + rot.ToString() + ":";
        }
        Stream stream = new MemoryStream(buffer);
        DataContractSerializer formatter = new DataContractSerializer(msg.GetType());
        formatter.WriteObject(stream, msg);

        int bufferSize = 1024;

        NetworkTransport.Send(hostId, connectionId, reliableChannel, buffer, bufferSize, out error);
    }

    public struct SerializableVector3
    {
        /// <summary>
        /// x component
        /// </summary>
        public float x;

        /// <summary>
        /// y component
        /// </summary>
        public float y;

        /// <summary>
        /// z component
        /// </summary>
        public float z;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rX"></param>
        /// <param name="rY"></param>
        /// <param name="rZ"></param>
        public SerializableVector3(float rX, float rY, float rZ)
        {
            x = rX;
            y = rY;
            z = rZ;
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return System.String.Format("[{0}, {1}, {2}]", x, y, z);
        }

        /// <summary>
        /// Automatic conversion from SerializableVector3 to Vector3
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public static implicit operator Vector3(SerializableVector3 rValue)
        {
            return new Vector3(rValue.x, rValue.y, rValue.z);
        }

        /// <summary>
        /// Automatic conversion from Vector3 to SerializableVector3
        /// </summary>
        /// <param name="rValue"></param>
        /// <returns></returns>
        public static implicit operator SerializableVector3(Vector3 rValue)
        {
            return new SerializableVector3(rValue.x, rValue.y, rValue.z);
        }
    }

    public Vector3 getVector3(string rString)
    {
        string[] temp = rString.Substring(1, rString.Length - 2).Split(',');
        float x = float.Parse(temp[0]);
        float y = float.Parse(temp[1]);
        float z = float.Parse(temp[2]);
        Vector3 rValue = new Vector3(x, y, z);
        return rValue;
    }
}

