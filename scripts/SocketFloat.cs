using UnityEngine;
using System.Net.Sockets;
using System;

public class SocketFloat : MonoBehaviour
{
    public string ip = "127.0.0.1"; // assign IP address to connect socket
    public int port = 60000; // assign port 
    private Socket client;

    [SerializeField]
    private float[] Data_to_send, Data_received;


    //for Sending data form other scripts
    public float[] SendData(float[] Data_to_send)
    {
        this.Data_to_send = Data_to_send;
        this.Data_received = SendAndReceiveData(Data_to_send);
        return this.Data_received;
    }

    private float[] SendAndReceiveData(float[] Data_to_send)
    {
        //initialize socket
        float[] FloatArrayReceived;
        //set up client socket
        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //attempt to connect to port
        client.Connect(ip, port);

        // output error if cannot connect
        if (!client.Connected)
        {
            Debug.LogError("Could not connect.");
            return null;
        }

        //convert floats to bytes, send to port
        var ArrayOfBytesToSend = new byte[Data_to_send.Length * 4];
        Buffer.BlockCopy(Data_to_send, 0, ArrayOfBytesToSend, 0, ArrayOfBytesToSend.Length);
        client.Send(ArrayOfBytesToSend);

        //allocate and receive bytes
        byte[] bytes = new byte[4000];
        int idxUsedBytes = client.Receive(bytes);

        //convert bytes to floats
        FloatArrayReceived = new float[idxUsedBytes / 4];
        Buffer.BlockCopy(bytes, 0, FloatArrayReceived, 0, idxUsedBytes);

        //close connection and return the array of floats recieved,
        client.Close();
        return FloatArrayReceived;
    }


}
