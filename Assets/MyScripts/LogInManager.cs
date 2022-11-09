using System.Collections;
using UnityEngine;
using Photon.Pun;
using Oculus.Platform;
using System;
using UnityEngine.UI;

public class LogInManager : MonoBehaviourPunCallbacks
{
    public GameObject _spawnPoint;
    [SerializeField] private Text m_screenText;
    [SerializeField] private ulong m_userId;

//Singleton implementation
    private static LogInManager m_instance;
    public static LogInManager Instance => m_instance;

    private void Awake()
    {
        if (m_instance == null)
            m_instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(SetUserIdFromLoggedInUser());
        StartCoroutine(ConnectToPhotonRoomOnceUserIdIsFound());
        StartCoroutine(InstantiateNetworkedAvatarOnceInRoom());
    }

    private IEnumerator SetUserIdFromLoggedInUser()
    {
        if (OvrPlatformInit.status == OvrPlatformInitStatus.NotStarted) OvrPlatformInit.InitializeOvrPlatform();

        while (OvrPlatformInit.status != OvrPlatformInitStatus.Succeeded)
        {
            if (OvrPlatformInit.status == OvrPlatformInitStatus.Failed)
            {
                Debug.LogError("OVR Platform failed to initialise");
                m_screenText.text = "OVR Platform failed to initialise";
                yield break;
            }

            yield return null;
        }

        Users.GetLoggedInUser().OnComplete(message =>
        {
            if (message.IsError)
                Debug.LogError("Getting Logged in user error " + message.GetError());
            else
                m_userId = message.Data.ID;
        });
    }

    private IEnumerator ConnectToPhotonRoomOnceUserIdIsFound()
    {
        while (m_userId == 0)
        {
            Debug.Log("Waiting for User id to be set before connecting to room");
            yield return null;
        }

        ConnectToPhotonRoom();
    }

    private void ConnectToPhotonRoom()
    {
        PhotonNetwork.ConnectUsingSettings();
        m_screenText.text = "Connecting to Server";
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        m_screenText.text = "Connecting to Lobby";
    }

    public override void OnJoinedLobby()
    {
        m_screenText.text = "Creating Room";
        PhotonNetwork.JoinOrCreateRoom("room", null, null);
    }

    public override void OnJoinedRoom()
    {
        var roomName = PhotonNetwork.CurrentRoom.Name;
        m_screenText.text = "Joined room with name " + roomName;
    }

    private IEnumerator InstantiateNetworkedAvatarOnceInRoom()
    {
        while (PhotonNetwork.InRoom == false)
        {
            Debug.Log("Waiting to be in room before intantiating avatar");
            yield return null;
        }

        InstantiateNetworkedAvatar();
    }

    private void InstantiateNetworkedAvatar()
    {
        var userId = Convert.ToInt64(m_userId);
        var objects = new object[1] { userId };
        var myAvatar = PhotonNetwork.Instantiate("NetworkPlayer", _spawnPoint.transform.position, Quaternion.identity,
            0, objects);
    }
}