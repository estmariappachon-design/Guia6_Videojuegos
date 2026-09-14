using System;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmartphoneSocketController : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private TMP_InputField ipField;
    [SerializeField] private TMP_InputField portField;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float sendInterval = 0.05f;

    [Header("Controls")]
    [SerializeField] private Slider moveXSlider;
    [SerializeField] private Slider moveZSlider;
    [SerializeField] private Slider yawSlider;

    private TcpClient client;
    private NetworkStream stream;
    private float nextSendTime;
    private bool grabRequested;
    private bool releaseRequested;

    public void Connect()
    {
        try
        {
            string ip = ipField.text.Trim();
            int port = int.Parse(portField.text.Trim());
            client = new TcpClient();
            client.Connect(ip, port);
            stream = client.GetStream();
            SetStatus($"Conectado a {ip}:{port}");
        }
        catch (Exception ex)
        {
            SetStatus($"Error de conexión: {ex.Message}");
        }
    }

    private void Update()
    {
        if (stream == null || Time.time < nextSendTime) return;

        SendCurrentInput();
        nextSendTime = Time.time + sendInterval;
    }

    private void SendCurrentInput()
    {
        if (stream == null) return;

        try
        {
            RemoteControlMessage message = new RemoteControlMessage
            {
                x = moveXSlider != null ? moveXSlider.value : 0f,
                z = moveZSlider != null ? moveZSlider.value : 0f,
                yaw = yawSlider != null ? yawSlider.value : 0f,
                grab = grabRequested,
                release = releaseRequested
            };

            string json = JsonUtility.ToJson(message) + "\n";
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);

            grabRequested = false;
            releaseRequested = false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error enviando datos: {ex.Message}");
            Disconnect();
        }
    }

    // --- NUEVO MÉTODO PARA EL BOTÓN DE FRENAR ---
    public void ResetSliders()
    {
        if (moveXSlider != null) moveXSlider.value = 0f;
        if (moveZSlider != null) moveZSlider.value = 0f;
        if (yawSlider != null) yawSlider.value = 0f;

        // Enviar inmediatamente la orden de detenerse al servidor
        SendCurrentInput();
    }

    public void RequestGrab() => grabRequested = true;
    public void RequestRelease() => releaseRequested = true;

    public void Disconnect()
    {
        stream?.Close();
        client?.Close();
        stream = null;
        client = null;
        SetStatus("Desconectado");
    }

    private void OnApplicationQuit() => Disconnect();

    private void SetStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null) statusText.text = message;
    }
}