using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Uma UI de depura��o simples para iniciar sess�es de Netcode.
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    private void OnGUI()
    {
        // Define uma �rea fixa na tela para os bot�es
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        // Se nenhuma sess�o de rede estiver ativa (nem cliente nem servidor)
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // Mostra os bot�es para iniciar uma sess�o
            StartButtons();
        }
        else
        {
            // Se uma sess�o estiver ativa, mostra o status e o bot�o de desligar
            StatusLabels();
            ShutdownButton();
        }

        GUILayout.EndArea();
    }

    /// <summary>
    /// Desenha os bot�es para iniciar como Host, Server ou Client.
    /// </summary>
    private void StartButtons()
    {
        if (GUILayout.Button("Host (Server + Client)"))
        {
            NetworkManager.Singleton.StartHost();
        }

        if (GUILayout.Button("Server"))
        {
            NetworkManager.Singleton.StartServer();
        }



        if (GUILayout.Button("Client"))
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    /// <summary>
    /// Mostra o modo de rede atual.
    /// </summary>
    private void StatusLabels()
    {
        string mode = "";
        if (NetworkManager.Singleton.IsHost)
        {
            mode = "Host";
        }
        else if (NetworkManager.Singleton.IsServer)
        {
            mode = "Server";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            mode = "Client";
        }

        GUILayout.Label($"Modo: {mode}");
    }

    /// <summary>
    /// Desenha o bot�o para encerrar a sess�o de rede.
    /// </summary>
    private void ShutdownButton()
    {
        if (GUILayout.Button("Shutdown"))
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}