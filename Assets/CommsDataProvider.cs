// using System;
// using System.Threading;
// using UnityEngine;

// using MQTTnet;
// using MQTTnet.Client;
// using MQTTnet.Client.Options;

// // cert pinning / server cert authentication
// using System.Security.Cryptography.X509Certificates;

// public class CommsDataProvider : MonoBehaviour
// {
//     public static CommsDataProvider Instance { get; private set; }

//     public int LatestImu { get; private set; } = -1;
//     public int LatestFlex { get; private set; } = -1;
//     public int LatestProcessAudio { get; private set; } = -1;

//     public bool HasImu { get; private set; }
//     public bool HasFlex { get; private set; }
//     public bool HasProcessAudio { get; private set; }

//     public event Action<int> OnImuUpdated;
//     public event Action<int> OnFlexUpdated;
//     public event Action<int> OnProcessAudioUpdated;

//     private IMqttClient client;

//     // Match your current smoke test
//     private const string Host = "172.20.10.2";
//     private const int Port = 8883;

//     private async void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);

//         Debug.Log("[CommsDataProvider] Awake()");

//         var factory = new MqttFactory();
//         client = factory.CreateMqttClient();

//         client.UseApplicationMessageReceivedHandler(e =>
//         {
//             string topic = e.ApplicationMessage.Topic;
//             byte[] bytes = e.ApplicationMessage.Payload;

//             if (bytes == null)
//             {
//                 Debug.Log($"[CommsDataProvider] RX {topic}: <null payload>");
//                 return;
//             }

//             if (bytes.Length < 7)
//             {
//                 Debug.LogWarning($"[CommsDataProvider] RX {topic}: payload too short, len={bytes.Length}");
//                 return;
//             }

//             int value = bytes[6];

//             // USE THESE VALUES
//             if (topic == "imu")
//             {
//                 LatestImu = value;
//                 HasImu = true;
//                 OnImuUpdated?.Invoke(value);
//             }
//             else if (topic == "flex")
//             {
//                 LatestFlex = value;
//                 HasFlex = true;
//                 OnFlexUpdated?.Invoke(value);
//             }
//             else if (topic == "process_audio")
//             {
//                 LatestProcessAudio = value;
//                 HasProcessAudio = true;
//                 OnProcessAudioUpdated?.Invoke(value);
//             }

//             Debug.Log($"[CommsDataProvider] RX {topic}: value={value} (len={bytes.Length})");
//         });

//         client.UseConnectedHandler(e =>
//         {
//             Debug.Log("[CommsDataProvider] Connected");
//         });

//         client.UseDisconnectedHandler(e =>
//         {
//             Debug.Log("[CommsDataProvider] Disconnected");
//         });

//         var caAsset = Resources.Load<TextAsset>("ca_cert");
//         if (caAsset == null)
//         {
//             Debug.LogError("[CommsDataProvider] Missing ca_cert in Assets/Resources/ca_cert.txt");
//             return;
//         }

//         X509Certificate2 expectedCert;
//         try
//         {
//             expectedCert = LoadX509FromPem(caAsset.text);
//             Debug.Log($"[CommsDataProvider] Loaded pinned cert thumbprint: {expectedCert.Thumbprint}");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError("[CommsDataProvider] Could not parse ca_cert: " + ex);
//             return;
//         }

//         var tlsParams = new MqttClientOptionsBuilderTlsParameters
//         {
//             UseTls = true,
//             AllowUntrustedCertificates = false,
//             IgnoreCertificateChainErrors = false,
//             IgnoreCertificateRevocationErrors = true,
//             CertificateValidationHandler = context =>
//             {
//                 return ValidatePinnedCertificate(context, expectedCert);
//             }
//         };

//         var options = new MqttClientOptionsBuilder()
//             .WithClientId("unity-provider-" + Guid.NewGuid().ToString("N"))
//             .WithTcpServer(Host, Port)
//             .WithTls(tlsParams)
//             .Build();

//         try
//         {
//             Debug.Log($"[CommsDataProvider] Connecting to {Host}:{Port} ...");
//             await client.ConnectAsync(options, CancellationToken.None);

//             var filters = new[]
//             {
//                 new TopicFilterBuilder().WithTopic("imu").Build(),
//                 new TopicFilterBuilder().WithTopic("flex").Build(),
//                 new TopicFilterBuilder().WithTopic("process_audio").Build()
//             };

//             await client.SubscribeAsync(filters);
//             Debug.Log("[CommsDataProvider] Subscribed to imu, flex, process_audio");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError("[CommsDataProvider] FAILED: " + ex);
//         }
//     }

//     private async void OnDestroy()
//     {
//         if (Instance == this)
//         {
//             Instance = null;
//         }

//         if (client != null && client.IsConnected)
//         {
//             await client.DisconnectAsync();
//         }
//     }

//     private static X509Certificate2 LoadX509FromPem(string pem)
//     {
//         const string header = "-----BEGIN CERTIFICATE-----";
//         const string footer = "-----END CERTIFICATE-----";

//         int start = pem.IndexOf(header, StringComparison.Ordinal);
//         if (start >= 0) start += header.Length;
//         else start = 0;

//         int end = pem.IndexOf(footer, StringComparison.Ordinal);
//         if (end < 0) end = pem.Length;

//         string base64 = pem.Substring(start, end - start)
//             .Replace("\r", "")
//             .Replace("\n", "")
//             .Trim();

//         byte[] der = Convert.FromBase64String(base64);
//         return new X509Certificate2(der);
//     }

//     private static bool ValidatePinnedCertificate(
//         MqttClientCertificateValidationCallbackContext context,
//         X509Certificate2 pinnedCert)
//     {
//         try
//         {
//             var brokerCert = new X509Certificate2(context.Certificate);

//             if (context.Chain != null)
//             {
//                 foreach (var chainElement in context.Chain.ChainElements)
//                 {
//                     if (string.Equals(
//                             chainElement.Certificate.Thumbprint,
//                             pinnedCert.Thumbprint,
//                             StringComparison.OrdinalIgnoreCase))
//                     {
//                         return true;
//                     }
//                 }
//             }

//             if (string.Equals(
//                     brokerCert.Thumbprint,
//                     pinnedCert.Thumbprint,
//                     StringComparison.OrdinalIgnoreCase))
//             {
//                 return true;
//             }

//             Debug.LogError(
//                 $"[CommsDataProvider] Security Alert: Broker cert ({brokerCert.Thumbprint}) " +
//                 $"does not match pinned cert ({pinnedCert.Thumbprint})");
//             return false;
//         }
//         catch (Exception ex)
//         {
//             Debug.LogError("[CommsDataProvider] TLS validation exception: " + ex);
//             return false;
//         }
//     }
// }

using System;
using System.Threading;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System.Security.Cryptography.X509Certificates;

public class CommsDataProvider : MonoBehaviour
{
    public static CommsDataProvider Instance { get; private set; }

    public int LatestImu { get; private set; } = -1;
    public int LatestFlex { get; private set; } = -1;
    public int LatestProcessAudio { get; private set; } = -1;

    public bool HasImu { get; private set; }
    public bool HasFlex { get; private set; }
    public bool HasProcessAudio { get; private set; }

    public event Action<int> OnImuUpdated;
    public event Action<int> OnFlexUpdated;
    public event Action<int> OnProcessAudioUpdated;

    private IMqttClient client;

    private const string Host = "10.144.89.48";
    private const int Port = 8883;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[CommsDataProvider] Instance set. gameObject={gameObject.name}, id={GetInstanceID()}");

        var factory = new MqttFactory();
        client = factory.CreateMqttClient();

        client.UseApplicationMessageReceivedHandler(e =>
        {
            string topic = e.ApplicationMessage.Topic;
            byte[] bytes = e.ApplicationMessage.Payload;

            if (bytes == null)
            {
                Debug.Log($"[CommsDataProvider] RX {topic}: <null payload>");
                return;
            }

            if (bytes.Length < 7)
            {
                Debug.LogWarning($"[CommsDataProvider] RX {topic}: payload too short, len={bytes.Length}");
                return;
            }

            int value = bytes[6];

            if (topic == "imu")
            {
                LatestImu = value;
                HasImu = true;
                OnImuUpdated?.Invoke(value);
            }
            else if (topic == "flex")
            {
                LatestFlex = value;
                HasFlex = true;
                OnFlexUpdated?.Invoke(value);
            }
            else if (topic == "process_audio")
            {
                LatestProcessAudio = value;
                HasProcessAudio = true;
                OnProcessAudioUpdated?.Invoke(value);
            }

            Debug.Log($"[CommsDataProvider] RX {topic}: value={value} (len={bytes.Length})");
        });

        client.UseConnectedHandler(async e =>
        {
            Debug.Log("[CommsDataProvider] Connected");

            try
            {
                var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("imu")
                    .WithTopicFilter("flex")
                    .WithTopicFilter("process_audio")
                    .Build();

                await client.SubscribeAsync(subscribeOptions);
                Debug.Log("[CommsDataProvider] Subscribed to imu, flex, process_audio");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CommsDataProvider] Subscribe failed after connect: " + ex);
            }
        });

        client.UseDisconnectedHandler(e =>
        {
            Debug.LogError(
                "[CommsDataProvider] Disconnected. " +
                $"Reason={e.Reason}, Exception={e.Exception}"
            );
        });

        var caAsset = Resources.Load<TextAsset>("ca_cert");
        if (caAsset == null)
        {
            Debug.LogError("[CommsDataProvider] Missing ca_cert in Assets/Resources/ca_cert.txt");
            return;
        }

        X509Certificate2 expectedCert;
        try
        {
            expectedCert = LoadX509FromPem(caAsset.text);
            Debug.Log($"[CommsDataProvider] Loaded pinned cert thumbprint: {expectedCert.Thumbprint}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[CommsDataProvider] Could not parse ca_cert: " + ex);
            return;
        }

        var tlsParams = new MqttClientOptionsBuilderTlsParameters
        {
            UseTls = true,
            AllowUntrustedCertificates = false,
            IgnoreCertificateChainErrors = false,
            IgnoreCertificateRevocationErrors = true,
            CertificateValidationHandler = context =>
            {
                Debug.Log("[CommsDataProvider] TLS validation callback called.");
                return ValidatePinnedCertificate(context, expectedCert);
            }
        };

        var options = new MqttClientOptionsBuilder()
            .WithClientId("unity-provider-" + Guid.NewGuid().ToString("N"))
            .WithTcpServer(Host, Port)
            .WithTls(tlsParams)
            .Build();

        try
        {
            Debug.Log($"[CommsDataProvider] Connecting to {Host}:{Port} ...");
            await client.ConnectAsync(options, CancellationToken.None);

            Debug.Log($"[CommsDataProvider] ConnectAsync finished. IsConnected={client.IsConnected}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[CommsDataProvider] Connect FAILED: " + ex);
        }
    }

    private async void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (client != null && client.IsConnected)
            await client.DisconnectAsync();
    }

    private static X509Certificate2 LoadX509FromPem(string pem)
    {
        const string header = "-----BEGIN CERTIFICATE-----";
        const string footer = "-----END CERTIFICATE-----";

        int start = pem.IndexOf(header, StringComparison.Ordinal);
        if (start >= 0) start += header.Length;
        else start = 0;

        int end = pem.IndexOf(footer, StringComparison.Ordinal);
        if (end < 0) end = pem.Length;

        string base64 = pem.Substring(start, end - start)
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        byte[] der = Convert.FromBase64String(base64);
        return new X509Certificate2(der);
    }

    private static bool ValidatePinnedCertificate(
        MqttClientCertificateValidationCallbackContext context,
        X509Certificate2 pinnedCert)
    {
        try
        {
            var brokerCert = new X509Certificate2(context.Certificate);

            Debug.Log($"[CommsDataProvider] Broker cert thumbprint: {brokerCert.Thumbprint}");

            if (context.Chain != null)
            {
                foreach (var chainElement in context.Chain.ChainElements)
                {
                    Debug.Log($"[CommsDataProvider] Chain cert: {chainElement.Certificate.Thumbprint}");

                    if (string.Equals(
                        chainElement.Certificate.Thumbprint,
                        pinnedCert.Thumbprint,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("[CommsDataProvider] TLS pin match found in chain.");
                        return true;
                    }
                }
            }

            if (string.Equals(
                brokerCert.Thumbprint,
                pinnedCert.Thumbprint,
                StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[CommsDataProvider] TLS pin matched broker cert directly.");
                return true;
            }

            Debug.LogError(
                $"[CommsDataProvider] Security Alert: Broker cert ({brokerCert.Thumbprint}) " +
                $"does not match pinned cert ({pinnedCert.Thumbprint})");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError("[CommsDataProvider] TLS validation exception: " + ex);
            return false;
        }
    }
}