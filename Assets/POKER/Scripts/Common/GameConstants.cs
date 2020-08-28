﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConstants : MonoBehaviour
{


    public static int TURN_TIME = 20;


    #region ANIMATIONS
    public const float CARD_ANIMATION_DURATION = 0.5f;
    public const float BET_PLACE_ANIMATION_DURATION = 0.5f;
    public const float LOCAL_BET_ANIMATION_DURATION = 1f;

    #endregion

    #region GAME_CONSTANTS
    public const int NUMBER_OF_CARDS_IN_DECK = 52;
    public static int[] NUMBER_OF_CARDS_PLAYER_GET_IN_MATCH = { 2, 4, 10 };
    public const int NUMBER_OF_COMMUNITY_CARDS = 5;
    public const float BUFFER_TIME = 3f;
    #endregion


    #region WEB
    public const float NETWORK_CHECK_DELAY = 2f;
    public const int API_RETRY_LIMIT = 2;
    public const int API_TIME_OUT_LIMIT = 10;


    public const string BASE_URL = "http://3.17.201.78";//"http://18.191.15.121"; // "http://3.6.137.204";


    //Testing
    //public const string API_URL = BASE_URL + ":3007";
    //public const string SOCKET_URL = BASE_URL + ":3002";

    //Production
    public const string API_URL = BASE_URL + ":3000";// ":3009";
    public const string SOCKET_URL = BASE_URL + ":3333";// ":3008";

    public const string GAME_PORTAL_URL = "http://3.17.201.78";//"http://18.191.15.121";//"http://3.6.137.204";


    public static string[] GAME_URLS =
    {
        API_URL +"/userSignUp",
        API_URL +"/userLogin",
        API_URL +"/createClub",
        API_URL +"/joinClubRequest",
        API_URL +"/getClubListByUserId",
        API_URL +"/getClubMemberListByClubId",
        API_URL +"/changePlayerRoleAndStatus",
        API_URL +"/deleteUserClubJoinedRedquest",
        API_URL +"/getShops",
        API_URL +"/getCardFeatures",
        API_URL +"/getRooms",
        API_URL +"/updateUserDetails",
        API_URL +"/updateFirebaseToken",
        API_URL +"/getFirebaseNotifiction",
        API_URL +"/updateFirebaseNotifiction",
        "http://18.191.15.121:3000/getForum",
        "http://18.191.15.121:3000/postLike",
        "http://18.191.15.121:3000/getComment",
        "http://18.191.15.121:3000/postComment",
        "http://18.191.15.121:3333/getMissions",
         API_URL +"/getCountries",
         API_URL+"/getAvatars",
         API_URL+"/updateUserSettings",
         "http://18.191.15.121:3333/getTopPlayers",
         API_URL+"/updateTableSettings",
         API_URL+"/getTableSettingData",
         API_URL+"/getUserDetails",
         API_URL+"/rewardCoins",
         API_URL+"/getFriendList",
         API_URL+"/getAllFriendRequest",
         API_URL+"/sendFriendRequest",
         API_URL+"/updateRequestStatus",
         "http://18.191.15.121:3000/getSpinWheelItems",
         "http://18.191.15.121:3000/setSpinWheelWinning"
    };
    #endregion

}

[System.Serializable]
public enum RequestType
{
    Registration,
    Login,
    CreateClub,
    SendClubJoinRequest,
    GetClubList,
    GetClubMemberList,
    ChangePlayerRoleInClub,
    DeleteUserJoinRequest,
    GetShopValues,
    GetVIPPrivilege,
    GetLobbyRooms,
    UpdateUserBalance,
    SendNotificationToken,
    GetNotificationMessage,
    UpdateNotificationMessage,
    GetForum,
    PostLike,
    GetComment,
    PostComment,
    GetMissions,
    GetCountryList,
    GetAvatars,
    UpdateUserSettings,
    GetTopPlayers,
    UpdateTableSettings,
    GetTableSettingData,
    GetUserDetails,
    GetRewardCoins, GetFriendList, GetAllFriendRequest, SendFriendRequest, UpdateRequestStatus,
    GetSpinWheelItems,
    SetSpinWheelWinning
}
