﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using DG.Tweening;


public class InGameManager : MonoBehaviour
{
    public static InGameManager instance;

    [SerializeField]
    private PlayerScript[] allPlayersObject;

    [SerializeField]
    private Transform[] allPlayerPos;


    [SerializeField]
    private GameObject cardAnimationPrefab,betAnimationPrefab;
    [SerializeField]
    private Transform animationLayer;

    [SerializeField]
    private Text potText;

    [SerializeField]
    private GameObject winningPrefab,chipscoine;

    [SerializeField]
    public Image[] communityCards;



    private PlayerScript[] onlinePlayersScript = null;
    private PlayerScript myPlayerObject = null,currentPlayer = null;
    private int MATCH_ROUND = 0, LAST_BET_AMOUNT = 0;
    private CardData[] openCards = null;
    private string lastPlayerAction = "";
    private List<GameObject> winnersObject = new List<GameObject>();
    private int communityCardsAniamtionShowedUpToRound = 0;
    private int currentRoundTotalBets = 0;
    private float potAmount = 0;

    private bool isRematchRequestSent = false,isTopUpDone = false;
    private float availableBalance = 0;

    public GameObject WinnAnimationpos;


    private void Awake()
    {
        instance = this;
    }


    private void Start()
    {
        for (int i = 0; i < communityCards.Length; i++)
        {
            communityCards[i].gameObject.SetActive(false);
        }

        UpdatePot("");

        onlinePlayersScript = new PlayerScript[0];

        for (int i = 0; i < allPlayersObject.Length; i++)
        {
            allPlayersObject[i].TogglePlayerUI(false);
            allPlayersObject[i].ResetAllData();
        }

        AdjustAllPlayersOnTable(GlobalGameManager.instance.GetRoomData().players);
    }


    private void Init(List<MatchMakingPlayerData> matchMakingPlayerData)
    {
        isRematchRequestSent = false;
        matchMakingPlayerData = ReArrangePlayersList(matchMakingPlayerData);
        onlinePlayersScript = new PlayerScript[matchMakingPlayerData.Count];
        PlayerScript playerScriptWhosTurn = null;

        for (int i = 0; i < allPlayersObject.Length; i++)
        {
            allPlayersObject[i].ResetAllData();

            if (i < matchMakingPlayerData.Count)
            {
                allPlayersObject[i].TogglePlayerUI(true);

                onlinePlayersScript[i] = allPlayersObject[i];
                onlinePlayersScript[i].Init(matchMakingPlayerData[i]);

                if (matchMakingPlayerData[i].isTurn)
                {
                    playerScriptWhosTurn = onlinePlayersScript[i];
                }
            }
            else
            {
                allPlayersObject[i].TogglePlayerUI(false);
            }   
        }

        if (playerScriptWhosTurn != null)
        {
            StartCoroutine(WaitAndShowCardAnimation(onlinePlayersScript, playerScriptWhosTurn));
        }
        else
        {
#if ERROR_LOG
            Debug.LogError("Null Reference exception found playerId whos turn is not found");
#endif
        }
    }

    private IEnumerator WaitAndShowCardAnimation(PlayerScript[] players, PlayerScript playerScriptWhosTurn)
    {
        if (!GlobalGameManager.IsJoiningPreviousGame)
        {
            List<GameObject> animatedCards = new List<GameObject>();
            for (int i = 0; i < players.Length; i++)
            {
                Image[] playerCards = players[i].GetCardsImage();

                for (int j = 0; j < playerCards.Length; j++)
                {
                    GameObject gm = Instantiate(cardAnimationPrefab, animationLayer) as GameObject;
                    gm.transform.DOMove(playerCards[j].transform.position, GameConstants.CARD_ANIMATION_DURATION);
                    gm.transform.DOScale(playerCards[j].transform.localScale, GameConstants.CARD_ANIMATION_DURATION);
                    gm.transform.DORotateQuaternion(playerCards[j].transform.rotation, GameConstants.CARD_ANIMATION_DURATION);
                    animatedCards.Add(gm);
                    SoundManager.instance.PlaySound(SoundType.CardMove);
                    yield return new WaitForSeconds(0.1f);
                }

                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION);

            for (int i = 0; i < animatedCards.Count; i++)
            {
                Destroy(animatedCards[i]);
            }

            animatedCards.Clear();
        }


        for (int i = 0; i < players.Length; i++)
        {
            players[i].ToggleCards(true, players[i].IsMe());
        }

        SocketController.instance.SetSocketState(SocketState.Game_Running);
        SwitchTurn(playerScriptWhosTurn,false);
    }



    public int GetLastBetAmount()
    {
        return LAST_BET_AMOUNT;
    }

    public void UpdateAvailableBalance(float balance)
    {
        availableBalance = balance;
    }
    public void PlayerTimerReset()
    {
        for (int i = 0; i < onlinePlayersScript.Length; i++)
        {
            onlinePlayersScript[i].ResetTurn();
        }


    }

    private void SwitchTurn(PlayerScript playerScript,bool isCheckAvailable)
    {
        SoundManager.instance.PlaySound(SoundType.TurnSwitch);

        for (int i = 0; i < onlinePlayersScript.Length; i++)
        {
            onlinePlayersScript[i].ResetTurn();
        }


        currentPlayer = playerScript;
        if (currentPlayer.IsMe())
        {
            InGameUiManager.instance.ToggleSuggestionButton(false);

            SuggestionActions selectedSuggestionAction = InGameUiManager.instance.GetSelectedSuggestionAction();
            InGameUiManager.instance.ResetSuggetionAction();

            if (selectedSuggestionAction != SuggestionActions.Null)
            {
                switch (selectedSuggestionAction)
                {
                    case SuggestionActions.Call:
                    case SuggestionActions.Call_Any:
                        {
                            int callAmount = GetLastBetAmount() - (int)GetMyPlayerObject().GetPlayerData().totalBet;

                            if (callAmount < GetMyPlayerObject().GetPlayerData().balance)
                            {
                                OnPlayerActionCompleted(PlayerAction.Call, callAmount, "Call");
                            }
                            else
                            {
                                InGameUiManager.instance.ToggleActionButton(true, currentPlayer, isCheckAvailable, LAST_BET_AMOUNT);
                            }
                        }
                        break;

                    case SuggestionActions.Check:
                        {
                            OnPlayerActionCompleted(PlayerAction.Check, 0, "Check");
                        }
                        break;

                    case SuggestionActions.Fold:
                        {
                            OnPlayerActionCompleted(PlayerAction.Fold, 0, "Fold");
                        }
                        break;

                    default:
                        {
                            Debug.LogError("Unhandled suggetion type found = "+selectedSuggestionAction);
                        }
                    break;
                }
            }
            else
            {
                InGameUiManager.instance.ToggleActionButton(true, currentPlayer, isCheckAvailable, LAST_BET_AMOUNT);
            }
        }
        else
        {
            InGameUiManager.instance.ToggleActionButton(false);

            if (!GetMyPlayerObject().GetPlayerData().isFold)
            {
                int callAmount = GetLastBetAmount() - (int)GetMyPlayerObject().GetPlayerData().totalBet;
                InGameUiManager.instance.ToggleSuggestionButton(true, isCheckAvailable, callAmount, GetMyPlayerObject().GetPlayerData().balance);
            }
        }
    }




    private List<MatchMakingPlayerData> ReArrangePlayersList(List<MatchMakingPlayerData> matchMakingPlayerData)
    {
        List<MatchMakingPlayerData> updatedList = new List<MatchMakingPlayerData>();

        for (int i = 0; i < matchMakingPlayerData.Count; i++)
        {
            if (matchMakingPlayerData[i].playerData.userId == PlayerManager.instance.GetPlayerGameData().userId)
            {
                int index = i;
                int counter = 0;

                while (counter < matchMakingPlayerData.Count)
                {
                    updatedList.Add(matchMakingPlayerData[index]);

                    ++index;

                    if (index >= matchMakingPlayerData.Count)
                    {
                        index = 0;
                    }

                    ++counter;
                }

                break;
            }
        }


        return updatedList;
    }




    public void LoadMainMenu()
    {
        InGameUiManager.instance.ShowScreen(InGameScreens.Loading);
        StartCoroutine(WaitAndSendLeaveRequest());
    }


    private IEnumerator WaitAndSendLeaveRequest()
    {
        SocketController.instance.SendLeaveMatchRequest();
        yield return new WaitForSeconds(GameConstants.BUFFER_TIME);
        SocketController.instance.ResetConnection();

        GlobalGameManager.instance.LoadScene(Scenes.MainMenu);
    }

    public PlayerScript GetMyPlayerObject()
    {
        if (myPlayerObject == null)
        {
            myPlayerObject = GetPlayerObject(PlayerManager.instance.GetPlayerGameData().userId);
        }

        return myPlayerObject;
    }


    public PlayerScript GetPlayerObject(string userId)
    {
        if (onlinePlayersScript == null)
        {
            return null;
        }

        for (int i = 0; i < onlinePlayersScript.Length; i++)
        {
            if (onlinePlayersScript[i].GetPlayerData().userId == userId)
            {
                return onlinePlayersScript[i];
            }
        }

        return null;
    }

    public PlayerScript[] GetAllPlayers()
    {
        return onlinePlayersScript;
    }




    private void ShowNewPlayersOnTable(JsonData data,bool isMatchStarted)
    {
        List<PlayerData> playerData = new List<PlayerData>();

        for (int i = 0; i < data[0].Count; i++)
        {
            if (GetPlayerObject(data[0][i]["userId"].ToString()) == null) // player not in our list
            {
                PlayerData playerDataObject = new PlayerData();

                playerDataObject.userId = data[0][i]["userId"].ToString();
                playerDataObject.userName = data[0][i]["userName"].ToString();
                playerDataObject.tableId = data[0][i]["tableId"].ToString();
                playerDataObject.balance = float.Parse(data[0][i]["totalCoins"].ToString());

                if (isMatchStarted)
                {
                    playerDataObject.isFold = data[0][i]["isBlocked"].Equals(true);
                }
                else
                {
                    playerDataObject.isFold = false;
                }

                playerData.Add(playerDataObject);
            }
        }

        
        for (int i = onlinePlayersScript.Length; i < allPlayersObject.Length; i++)
        {
            allPlayersObject[i].TogglePlayerUI(false);
        }


        if (isMatchStarted)
        {
            if (playerData.Count > 0)
            {
                int startIndex = onlinePlayersScript.Length;
                int maxIndex = startIndex + playerData.Count;
                int index = 0;

                for (int i = startIndex; i < maxIndex && i < allPlayersObject.Length; i++)
                {
                    allPlayersObject[i].TogglePlayerUI(true);
                    allPlayersObject[i].ShowDetailsAsNewPlayer(playerData[index]);
                    ++index;
                }
            }
        }
        else
        {
            int index = 1;

            for (int i = 0; i < playerData.Count && i < allPlayersObject.Length; i++)
            {
                if (playerData[i].userId == PlayerManager.instance.GetPlayerGameData().userId)
                {
                    allPlayersObject[0].TogglePlayerUI(true);
                    allPlayersObject[0].ShowDetailsAsNewPlayer(playerData[i]);
                }
                else
                {
                    allPlayersObject[index].TogglePlayerUI(true);
                    allPlayersObject[index].ShowDetailsAsNewPlayer(playerData[i]);
                }

                ++index;
            }
        }


        if (isMatchStarted && onlinePlayersScript != null && onlinePlayersScript.Length > 0)
        {
            List<PlayerScript> leftPlayers = new List<PlayerScript>();

            for (int i = 0; i < onlinePlayersScript.Length; i++)
            {
                bool isMatchFound = false;

                for (int j = 0; j < data[0].Count; j++)
                {
                    if (data[0][j]["userId"].ToString() == onlinePlayersScript[i].GetPlayerData().userId)
                    {
                        isMatchFound = true;
                        j = 100;
                    }
                }

                if (!isMatchFound)
                {
                    leftPlayers.Add(onlinePlayersScript[i]);
                }
            }

            for (int i = 0; i < leftPlayers.Count; i++)
            {
                leftPlayers[i].TogglePlayerUI(false);
            }
        }


        for (int i = 0; i < allPlayersObject.Length; i++)
        {
            allPlayersObject[i].ToggleEmptyObject(false);
        }

        int maxPlayerOnTable = GlobalGameManager.instance.GetRoomData().players;
        
        for (int i = 0; i < maxPlayerOnTable && i < allPlayersObject.Length; i++)
        {
            if (!allPlayersObject[i].IsPlayerObjectActive())
            {
                allPlayersObject[i].ToggleEmptyObject(true);
            }
        }
    }



    private void AdjustAllPlayersOnTable(int totalPlayerCount)
    {
        if (totalPlayerCount <= 4)
        {
            int index = 0;
            for (int i = 0; i < totalPlayerCount; i++)
            {
                allPlayersObject[i].transform.position = allPlayerPos[index].position;
                index += 2;
            }
        }
        else if (totalPlayerCount <= 7)
        {
            int index = 0;

            for (int i = 0; i < totalPlayerCount; i++)
            {
                if (i == 2 || i == 7)
                {
                    ++index;
                }

                allPlayersObject[i].transform.position = allPlayerPos[index].position;
                ++index;
            }
        }
        else
        {
            for (int i = 0; i < totalPlayerCount; i++)
            {
                allPlayersObject[i].gameObject.transform.position = allPlayerPos[i].position;
            }
        }
    }



    private IEnumerator WaitAndShowBetAnimation(PlayerScript playerScript, string betAmount)
    {
        GameObject gm = Instantiate(betAnimationPrefab,animationLayer) as GameObject;
        gm.GetComponent<Text>().text = betAmount;
        gm.transform.position = playerScript.transform.position;
        Vector3 initialScale = gm.transform.localScale;
        gm.transform.localScale = Vector3.zero;

        gm.transform.DOMove(playerScript.GetLocaPot().transform.position,GameConstants.BET_PLACE_ANIMATION_DURATION).SetEase(Ease.OutBack);
        gm.transform.DOScale(initialScale,GameConstants.BET_PLACE_ANIMATION_DURATION).SetEase(Ease.OutBack);
        SoundManager.instance.PlaySound(SoundType.Bet);
        yield return new WaitForSeconds(GameConstants.BET_PLACE_ANIMATION_DURATION);
        Destroy(gm);
    }

    private IEnumerator WaitAndShowWinnersAnimation(PlayerScript playerScript, string betAmount,GameObject amount)
    {
        yield return new WaitForSeconds(.6f);
        GameObject gm = Instantiate(chipscoine,WinnAnimationpos.transform) as GameObject;
    //    gm.GetComponent<Text>().text = betAmount;
        gm.transform.position = WinnAnimationpos.transform.position;
/*        Vector3 initialScale = gm.transform.localScale;
        gm.transform.localScale = Vector3.zero;*/

        gm.transform.DOMove(playerScript.transform.position, .5f).SetEase(Ease.Linear);
       // gm.transform.DOScale(initialScale, GameConstants.BET_PLACE_ANIMATION_DURATION).SetEase(Ease.OutBack);
        SoundManager.instance.PlaySound(SoundType.Bet);
        yield return new WaitForSeconds(.6f);
        Destroy(gm);
        amount.transform.DOScale(Vector3.one, GameConstants.BET_PLACE_ANIMATION_DURATION).SetEase(Ease.OutBack);
    }
    public float GetPotAmount()
    {
        return potAmount;
    }

    private void UpdatePot(string textToShow)
    {
        potText.text = textToShow;
    }

    public int GetMatchRound()
    {
        return MATCH_ROUND;
    }

    public void UpdateLastPlayerAction(string dataToAssign)
    {
        lastPlayerAction = dataToAssign;
    }

    public string GetLastPlayerAction()
    {
        return lastPlayerAction;
    }



    private void ShowCommunityCardsAnimation()
    {
        if (MATCH_ROUND <= communityCardsAniamtionShowedUpToRound || openCards == null)
        {
            return;
        }

        StartCoroutine(WaitAndShowCommunityCardsAnimation());
    }

    private IEnumerator WaitAndShowCommunityCardsAnimation()
    {
        communityCardsAniamtionShowedUpToRound = MATCH_ROUND;
        bool isBetFound = false;
        for (int i = 0; i < onlinePlayersScript.Length; i++)
        {
            Text text = onlinePlayersScript[i].GetLocaPot();

            if (text.gameObject.activeInHierarchy && !string.IsNullOrEmpty(text.text))
            {
                isBetFound = true;
                GameObject gm = Instantiate(betAnimationPrefab, animationLayer) as GameObject;

                gm.GetComponent<Text>().text = text.text;
                gm.transform.DOMove(potText.transform.position, GameConstants.LOCAL_BET_ANIMATION_DURATION).SetEase(Ease.OutBack);
                Destroy(gm,GameConstants.LOCAL_BET_ANIMATION_DURATION + 0.1f);
            }

            onlinePlayersScript[i].UpdateRoundNo(GetMatchRound());
        }

        if (isBetFound)
        {
            SoundManager.instance.PlaySound(SoundType.ChipsCollect);
        }

        UpdatePot("" + (int)potAmount);

        switch (MATCH_ROUND)
        {
            case 1:
                {
                    SoundManager.instance.PlaySound(SoundType.CardMove);

                    for (int i = 0; i < 3; i++)
                    {
                        GameObject gm = Instantiate(cardAnimationPrefab,animationLayer) as GameObject;
                        gm.transform.localScale = communityCards[0].transform.localScale;
                        gm.GetComponent<Image>().sprite = openCards[i].cardsSprite;
                        gm.transform.Rotate(0,-90,0);
                        gm.transform.position = communityCards[0].transform.position;

                        gm.transform.DORotate(new Vector3(0,90,0), GameConstants.CARD_ANIMATION_DURATION,RotateMode.LocalAxisAdd);
                        gm.transform.DOMove(communityCards[i].transform.position,GameConstants.CARD_ANIMATION_DURATION);
                        //gm.transform.DOScale(communityCards[i].transform.localScale, GameConstants.CARD_ANIMATION_DURATION).SetEase(Ease.OutBack);

                        yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION * 0.3f);

                        Destroy(gm, GameConstants.CARD_ANIMATION_DURATION * 3);
                    }

                    yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION);

                    for (int i = 0; i < 3; i++)
                    {
                        communityCards[i].gameObject.SetActive(true);
                        communityCards[i].sprite = openCards[i].cardsSprite;
                    }
                }
            break;

            case 2:
                {
                    for (int i = 0; i < 3; i++)
                    {
                        communityCards[i].gameObject.SetActive(true);
                        communityCards[i].sprite = openCards[i].cardsSprite;
                    }

                    SoundManager.instance.PlaySound(SoundType.CardMove);

                    for (int i = 3; i < 4; i++)
                    {
                        GameObject gm = Instantiate(cardAnimationPrefab, animationLayer) as GameObject;


                        gm.transform.localScale = communityCards[i].transform.localScale;
                        gm.GetComponent<Image>().sprite = openCards[i].cardsSprite;
                        gm.transform.Rotate(0, -90, 0);
                        gm.transform.position = communityCards[i].transform.position;

                        gm.transform.DORotate(new Vector3(0, 90, 0), GameConstants.CARD_ANIMATION_DURATION, RotateMode.LocalAxisAdd);
                        gm.transform.DOMove(communityCards[i].transform.position, GameConstants.CARD_ANIMATION_DURATION);

                        yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION * 0.3f);

                        Destroy(gm, GameConstants.CARD_ANIMATION_DURATION * 1);
                    }

                    yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION);

                    for (int i = 0; i < 4; i++)
                    {
                        communityCards[i].gameObject.SetActive(true);
                        communityCards[i].sprite = openCards[i].cardsSprite;
                    }
                }
            break;

            case 3:
                {
                    for (int i = 0; i < 4; i++)
                    {
                        communityCards[i].gameObject.SetActive(true);
                        communityCards[i].sprite = openCards[i].cardsSprite;
                    }

                    SoundManager.instance.PlaySound(SoundType.CardMove);

                    for (int i = 4; i < 5; i++)
                    {
                        GameObject gm = Instantiate(cardAnimationPrefab, animationLayer) as GameObject;

                        gm.transform.localScale = communityCards[i].transform.localScale;
                        gm.GetComponent<Image>().sprite = openCards[i].cardsSprite;
                        gm.transform.Rotate(0, -90, 0);
                        gm.transform.position = communityCards[i].transform.position;

                        gm.transform.DORotate(new Vector3(0, 90, 0), GameConstants.CARD_ANIMATION_DURATION, RotateMode.LocalAxisAdd);
                        gm.transform.DOMove(communityCards[i].transform.position, GameConstants.CARD_ANIMATION_DURATION);

                        yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION * 0.3f);
                        
                        Destroy(gm, GameConstants.CARD_ANIMATION_DURATION * 1);
                    }

                    yield return new WaitForSeconds(GameConstants.CARD_ANIMATION_DURATION);

                    for (int i = 0; i < communityCards.Length; i++)
                    {
                        communityCards[i].gameObject.SetActive(true);
                        communityCards[i].sprite = openCards[i].cardsSprite;
                    }
                }
            break;

            default:
                {
                    for (int i = 0; i < communityCards.Length; i++)
                    {
                        communityCards[i].gameObject.SetActive(true);
                        communityCards[i].sprite = openCards[i].cardsSprite;
                    }
                }
            break;
        }

        yield return new WaitForSeconds(0.1f);
    }



    public void OnPlayerActionCompleted(PlayerAction actionType,int betAmount,string playerAction)
    {
        GetMyPlayerObject().ResetTurn();

        InGameUiManager.instance.ToggleActionButton(false);

        if (actionType == PlayerAction.Fold)
        {
            SoundManager.instance.PlaySound(SoundType.Fold);
            SocketController.instance.SendFoldRequest(GetMyPlayerObject().GetLocalBetAmount());
        }
        else
        {
            if (actionType == PlayerAction.Check)
            {
                SoundManager.instance.PlaySound(SoundType.Check);
            }

            GetMyPlayerObject().AddIntoLocalBetAmount(betAmount, GetMatchRound());
            SocketController.instance.SendBetData(betAmount, GetMyPlayerObject().GetLocalBetAmount(), playerAction, GetMatchRound());
        }
    }



    public void ToggleTopUpDone(bool isDone)
    {
        isTopUpDone = isDone;
    }


    #region SocketCallBacks

    public void OnResultResponseFound(string serverResponse)
    {
        if (winnersObject.Count > 0)
        {
            return;
        }

        MATCH_ROUND = 10; // ToShow all cards
        ShowCommunityCardsAnimation();
        InGameUiManager.instance.ToggleActionButton(false);
        InGameUiManager.instance.ToggleSuggestionButton(false);

        JsonData data = JsonMapper.ToObject(serverResponse);
        
        if (data[0].Count > 0)
        {
            for (int i = 0; i < data[0][0].Count; i++)
            {
                if (data[0][0][i]["isWin"].Equals(true))
                {
                    PlayerScript winnerPlayer = GetPlayerObject(data[0][0][i]["userId"].ToString());

                    if (winnerPlayer != null)
                    {
                        GameObject gm = Instantiate(winningPrefab, animationLayer) as GameObject;
                        gm.transform.Find("WinBy").GetComponent<Text>().text = data[0][0][i]["name"].ToString();
                        gm.transform.Find("winAmount").GetComponent<Text>().text="+"+data[0][0][i]["winAmount"].ToString(); 
                        gm.transform.position = winnerPlayer.gameObject.transform.position;
                        gm.transform.SetParent(winnerPlayer.gameObject.transform.GetChild(0).transform);
                        gm.transform.SetSiblingIndex(0);
                        Vector3 inititalScale = gm.transform.localScale;
                        gm.transform.localScale = Vector3.zero;
                        StartCoroutine(WaitAndShowWinnersAnimation(winnerPlayer,  data[0][0][i]["winAmount"].ToString(), gm));
                       // gm.transform.DOScale(inititalScale, GameConstants.BET_PLACE_ANIMATION_DURATION).SetEase(Ease.OutBack);
                        winnersObject.Add(gm);
                    }
                }
            }
        }
        for (int i = 0; i < onlinePlayersScript.Length; i++)
        {
            onlinePlayersScript[i].ToggleCards(true,true);
        }
    }


    public void OnNextMatchCountDownFound(string serverResponse)
    {
        JsonData data = JsonMapper.ToObject(serverResponse);
        int remainingTime = (int)float.Parse(data[0].ToString());

        if (remainingTime > 1)
        {
           // InGameUiManager.instance.ShowTableMessage("Next Round Will Start In : " + remainingTime);
           // InGameUiManager.instance.LoadingImage.SetActive(true);
            if (!isRematchRequestSent)
            {
                if (remainingTime > GameConstants.BUFFER_TIME)
                {

                    if (isTopUpDone || availableBalance >= GlobalGameManager.instance.GetRoomData().minBuyIn)
                    {
                        ToggleTopUpDone(false);
                        SocketController.instance.SendReMatchRequest("Yes", "0");
                    }
                    else
                    {
                        int balanceToAdd = (int)GlobalGameManager.instance.GetRoomData().minBuyIn - (int)availableBalance;
                        float userMainBalance = PlayerManager.instance.GetPlayerGameData().coins;

                        if (userMainBalance >= balanceToAdd)
                        {
                            SocketController.instance.SendReMatchRequest("Yes", "" + balanceToAdd);

                            userMainBalance -= balanceToAdd;
                            PlayerGameDetails playerData = PlayerManager.instance.GetPlayerGameData();
                            playerData.coins = userMainBalance;
                            PlayerManager.instance.SetPlayerGameData(playerData);
                        }
                        else
                        {
                            if (availableBalance > GlobalGameManager.instance.GetRoomData().smallBlind)
                            {
                                SocketController.instance.SendReMatchRequest("Yes", "0");
                            }
                            else
                            {
                                InGameUiManager.instance.ShowMessage("You don't have enough coins to play, please purchase some coins to continue");
                                // TODO call sit out
                                // TODO show coin purchase screen
                            }
                        }
                    }
                }
                else
                {
                    SocketController.instance.SendReMatchRequest("No", "0");
                }
            }
        }
        else
        {
           // InGameUiManager.instance.LoadingImage.SetActive(false);
            InGameUiManager.instance.ShowTableMessage("");
        }

        ResetMatchData();
    }




    public void OnTurnCountDownFound(string serverResponse)
    {
        if (SocketController.instance.GetSocketState() == SocketState.Game_Running)
        {
            JsonData data = JsonMapper.ToObject(serverResponse);

            if (currentPlayer != null)
            {
                int remainingTime = (int)float.Parse(data[0].ToString());

                if (currentPlayer.IsMe())
                {
                    
                    int endTime = (int)(GameConstants.TURN_TIME * 0.25f);
                    Debug.Log("%%%%%%%%%%%%%%%%%%%%%%   end time " + endTime);

                    if (remainingTime == endTime)
                    {
                        SoundManager.instance.PlaySound(SoundType.TurnEnd);
                    }
                }
                Debug.Log("^^^^^^^^^^^^^^^^^^^   end time " + remainingTime);
                
                currentPlayer.ShowRemainingTime(remainingTime);
                
            }
            else
            {
                Debug.LogError("Null reference exception found current player object is null");
            }
        }
    }


    public void OnBetDataFound(string serverResponse)
    {
        JsonData data = JsonMapper.ToObject(serverResponse);
        LAST_BET_AMOUNT = (int)float.Parse(data[0]["lastBet"].ToString());
        string userId = data[0]["userId"].ToString();
        potAmount = float.Parse(data[0]["pot"].ToString());


        if (SocketController.instance.GetSocketState() == SocketState.Game_Running)
        {
            int betAmount = (int)float.Parse(data[0]["bet"].ToString());

            if (betAmount > 0 && userId != PlayerManager.instance.GetPlayerGameData().userId)
            {
                PlayerScript playerObject = GetPlayerObject(userId);

                if (playerObject != null)
                {
                    StartCoroutine(WaitAndShowBetAnimation(playerObject,""+ betAmount));
                }
                else
                {
#if ERROR_LOG
                    Debug.LogError("Null Reference exception found playerScript is null in BetDatFound Method = " + userId);
#endif
                }
            }
        }
        
    }


    public void OnRoundDataFound(string serverResponse)
    {
        JsonData data = JsonMapper.ToObject(serverResponse);
        MATCH_ROUND = (int)float.Parse(data[0]["currentSubRounds"].ToString());
        ShowCommunityCardsAnimation();
    }


    public void OnOpenCardsDataFound(string serverResponse)
    {
        JsonData data = JsonMapper.ToObject(serverResponse);
        openCards = new CardData[data[0].Count];

        for (int i = 0; i < data[0].Count; i++)
        {
            openCards[i] = CardsManager.instance.GetCardData(data[0][i].ToString());
        }
    }



    public void OnGameStartTimeFound(string serverResponse)
    {
        JsonData data = JsonMapper.ToObject(serverResponse);

        int remainingTime = (int)float.Parse(data[0].ToString());

     /*   if (remainingTime < 30)
        {*/
            if (remainingTime <= 1)
            {
                InGameUiManager.instance.ShowTableMessage("");
         //   InGameUiManager.instance.LoadingImage.SetActive (false);
            }
            else
            {
//InGameUiManager.instance.LoadingImage.SetActive(true);
        //    InGameUiManager.instance.ShowTableMessage("Match will start in " + remainingTime + " sec");
            }
      /*  }
        else
        {
            InGameUiManager.instance.ShowTableMessage("Waiting for opponent");
        }*/
    }

    public void OnPlayerObjectFound(string serverResponse)
    {
        if (serverResponse.Length < 20)
        {
            Debug.LogError("Invalid playerObject response found = " + serverResponse);
            return;
        }

        JsonData data = JsonMapper.ToObject(serverResponse);

        if (data[0].Count > 0)
        {
            //AdjustAllPlayersOnTable(data[0].Count);
            bool isMatchStarted = data[0][0]["isStart"].Equals(true);
            ShowNewPlayersOnTable(data, isMatchStarted);

            if (SocketController.instance.GetSocketState() == SocketState.WaitingForOpponent)
            {
                SocketController.instance.SetTableId(data[0][0]["tableId"].ToString());

                if (isMatchStarted) // Match is started
                {
                    List<MatchMakingPlayerData> matchMakingPlayerData = new List<MatchMakingPlayerData>();

                    SocketController.instance.SetTableId(data[0][0]["tableId"].ToString());
                    for (int i = 0; i < data[0].Count; i++)
                    {
                        MatchMakingPlayerData playerData = new MatchMakingPlayerData();

                        playerData.playerData = new PlayerData();
                        playerData.playerData.userId = data[0][i]["userId"].ToString();

                        playerData.playerData.userName = data[0][i]["userName"].ToString();
                        playerData.playerData.tableId = data[0][i]["tableId"].ToString();
                        playerData.playerData.isFold = data[0][i]["isBlocked"].Equals(true);

                        playerData.playerData.totalBet = float.Parse(data[0][i]["totalBet"].ToString());
                        playerData.playerData.balance = float.Parse(data[0][i]["totalCoins"].ToString());

                        playerData.playerType = data[0][i]["playerType"].ToString();

                        playerData.isTurn = data[0][i]["isTurn"].Equals(true);
                        playerData.playerData.isDealer = data[0][i]["isDealer"].Equals(true);
                        playerData.playerData.isSmallBlind = data[0][i]["smallBlind"].Equals(true);
                        playerData.playerData.isBigBlind = data[0][i]["bigBlind"].Equals(true);
                        Debug.LogError("************************************************************");

                        if (playerData.isTurn)
                        {
                            playerData.isCheckAvailable = data[0][i]["isCheck"].Equals(true);
                        }

                        playerData.playerData.cards = new CardData[data[0][i]["cards"].Count];

                        for (int j = 0; j < playerData.playerData.cards.Length; j++)
                        {
                            if (playerData == null)
                            {
#if ERROR_LOG
                                Debug.LogError("matchmaking object is null");
#endif
                            }

                            if (playerData.playerData.cards == null)
                            {
#if ERROR_LOG
                                Debug.LogError("cards is null");
#endif
                            }

                            playerData.playerData.cards[j] = CardsManager.instance.GetCardData(data[0][i]["cards"][j].ToString());
                        }

                        matchMakingPlayerData.Add(playerData);
                    }

                    Init(matchMakingPlayerData);
                }
            }
            else if (SocketController.instance.GetSocketState() == SocketState.Game_Running)
            {
                PlayerScript playerWhosTurn = null;
                bool isCheckAvailable = false;

                for (int i = 0; i < data[0].Count; i++)
                {
                    PlayerScript playerObject = GetPlayerObject(data[0][i]["userId"].ToString());

                    if (playerObject != null)
                    {
                        PlayerData playerData = new PlayerData();
                        Debug.LogError("************************************************************");
                        playerData.isFold = data[0][i]["isBlocked"].Equals(true);
                        playerData.totalBet = float.Parse(data[0][i]["totalBet"].ToString());
                        playerData.balance = float.Parse(data[0][i]["totalCoins"].ToString());

                        if (data[0][i]["isTurn"].Equals(true))
                        {
                            playerWhosTurn = playerObject;
                            isCheckAvailable = data[0][i]["isCheck"].Equals(true);
                        }

                        if (data[0][i]["userData"] != null && data[0][i]["userData"].ToString().Length > 0)
                        {
                            string playerAction = data[0][i]["userData"]["playerAction"].ToString();
                            int betAmount = (int)float.Parse(data[0][i]["userData"]["betData"].ToString());
                            int roundNo = (int)float.Parse(data[0][i]["userData"]["roundNo"].ToString());
                            playerObject.UpdateDetails(playerData, playerAction, betAmount, roundNo);
                        }
                        else
                        {
                            playerObject.UpdateDetails(playerData,"",0,-1);
                        }
                    }
                }

                if (playerWhosTurn != null)
                {
                    SwitchTurn(playerWhosTurn, isCheckAvailable);
                }
                else
                {
#if ERROR_LOG
                    Debug.LogError("Null reference exception found playerWhosTurn is not found");
#endif
                }
            }
        }        
    }

    #endregion



    private void ResetMatchData()
    {
        UpdatePot("");
        isRematchRequestSent = true;

        SocketController.instance.SetSocketState(SocketState.WaitingForOpponent);

        for (int i = 0; i < communityCards.Length; i++)
        {
            communityCards[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < winnersObject.Count; i++)
        {
            Destroy(winnersObject[i]);
        }

        winnersObject.Clear();
        communityCardsAniamtionShowedUpToRound = 0;
        currentRoundTotalBets = 0;
        potAmount = 0;
        lastPlayerAction = "";
        openCards = null;
        LAST_BET_AMOUNT = 0;

        for (int i = 0; i < allPlayersObject.Length; i++)
        {
            //allPlayersObject[i].ResetAllData();
            allPlayersObject[i].ToggleCards(false);
        }

        myPlayerObject = null;

        onlinePlayersScript = null;
        onlinePlayersScript = new PlayerScript[0];
    }
}

public class MatchMakingPlayerData
{
    public PlayerData playerData;
    public bool isTurn;
    public bool isCheckAvailable;
    public string playerType;
}