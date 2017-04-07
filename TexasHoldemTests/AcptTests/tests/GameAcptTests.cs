﻿
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TexasHoldemTests.AcptTests.Bridges;

namespace TexasHoldemTests.AcptTests.tests
{
    [TestFixture]
    public class GameAcptTests : AcptTest
    {
        private int _userId2;

        //setup: (called from base)
        protected override void SubClassInit()
        {
            //nothing at the moment
        }

        //tear down: (called from case)
        protected override void SubClassDispose()
        {
            if (_userId2 != -1)
            {
                UserBridge.DeleteUser(_userId2);
            }

            _userId2 = -1;
        }

        //general tests:
        [TestCase]
        public void CreateGameTestGood()
        {
            RegisterUser1();

            Assert.True(GameBridge.CreateGameRoom(UserId, RoomId));
            Assert.True(GameBridge.DoesRoomExist(RoomId));
            Assert.Equals(1, GameBridge.GetPlayersInRoom(RoomId).Count);
            Assert.Equals(UserId, GameBridge.GetPlayersInRoom(RoomId).First());
        }
        
        [TestCase]
        public void CreateGameTestBad()
        {
            //user1 is not logged in
            Assert.False(GameBridge.CreateGameRoom(UserId, RoomId));
            Assert.False(GameBridge.DoesRoomExist(RoomId));
        }

        [TestCase]
        public void GameBecomesInactiveGood()
        {
            _userId2 = UserBridge.GetNextFreeUserId();

            RegisterUser1();

            Assert.True(GameBridge.CreateGameRoom(UserId, RoomId));
            Assert.True(UserBridge.AddUserToGameRoomAsPlayer(_userId2, RoomId, 0));
            Assert.True(GameBridge.StartGame(RoomId));
            Assert.True(GameBridge.IsRoomActive(RoomId));

            Assert.True(UserBridge.RemoveUserFromRoom(_userId2, RoomId));
            Assert.False(GameBridge.IsRoomActive(RoomId));
            Assert.False(GameBridge.StartGame(RoomId));
        }

        [TestCase]
        public void ListGamesByRankTestGood()
        {
            //delete all users and games, register user1
            RestartSystem();

            int rank = UserBridge.GetUserRank(UserId);
            int someUser = GetNextUserId();
            UserBridge.SetUserRank(someUser, rank);

            Assert.True(GameBridge.CreateGameRoom(someUser, RoomId));
            Assert.Contains(RoomId, GameBridge.ListAvailableGamesByUserRank(rank));
        }
        
        [TestCase]
        public void ListGamesByRankTestSad()
        {
            //delete all users and games, register user1
            RestartSystem();

            int rank1 = UserBridge.GetUserRank(UserId);
            int someUser = GetNextUserId();
            UserBridge.SetUserRank(someUser, rank1 + 10);

            Assert.True(GameBridge.CreateGameRoom(someUser, RoomId));
            Assert.IsEmpty(GameBridge.ListAvailableGamesByUserRank(rank1));
        }
        
        [TestCase]
        public void ListSpectatableGamesTestGood()
        {
            //delete all users and games, register user1
            RestartSystem();

            int someUser1 = GetNextUserId();
            int someUser2 = GetNextUserId();

            Assert.True(GameBridge.CreateGameRoom(someUser1, RoomId));
            Assert.True(UserBridge.AddUserToGameRoomAsPlayer(someUser2, RoomId, 0));
            Assert.True(GameBridge.StartGame(RoomId));

            Assert.Contains(RoomId, GameBridge.ListSpectateableRooms());
        }
        
        [TestCase]
        public void ListSpectatableGamesTestSad()
        {
            //delete all users and games, register user1
            RestartSystem();

            Assert.IsEmpty(GameBridge.ListSpectateableRooms());
        }

        //game related tests:

        //create a game with 3 players and start it
        private void InitGame()
        {
            //create users:
            RegisterUser1();
            int user2 = CreateGameWithUser(); //user2 is now in game as player
            int user3 = GetNextUserId();

            //add users to game:
            UserBridge.AddUserToGameRoomAsPlayer(UserId, RoomId, 10);
            UserBridge.AddUserToGameRoomAsPlayer(user3, RoomId, 10);

            GameBridge.StartGame(RoomId);

            Assert.True(GameBridge.IsRoomActive(RoomId));
            Assert.AreEqual(user2, GameBridge.GetDealerId(RoomId));
            Assert.AreEqual(UserId, GameBridge.GetSbId(RoomId));
            Assert.AreEqual(user3, GameBridge.GetBbId(RoomId));
        }

        //tests a whole game including all actions, card deals, pot size changes, etc.
        [TestCase]
        public void GameTestGood()
        {
            //create users:
            RegisterUser1();
            int user2 = CreateGameWithUser(); //user2 is now in game as player
            int user3 = GetNextUserId();
            int user4 = GetNextUserId();
            List<int> userList = new List<int> {user2, UserId, user3, user4};

            int money1 = UserBridge.GetUserMoney(UserId);
            int money2 = UserBridge.GetUserMoney(user2);
            int money3 = UserBridge.GetUserMoney(user3);
            int money4 = UserBridge.GetUserMoney(user4);
            List<int> moneyList = new List<int> {money2, money1, money3, money4 };

            int chips1 = UserBridge.GetUserChips(UserId, RoomId);
            int chips2 = UserBridge.GetUserChips(user2, RoomId);
            int chips3 = UserBridge.GetUserChips(user3, RoomId);
            int chips4 = UserBridge.GetUserChips(user4, RoomId);
            List<int> chipsList = new List<int> {chips1, chips2, chips3, chips4};

            int smallBlind = GameBridge.GetSbSize(RoomId);
            int bb = 2 * smallBlind;
            int currMinBet = bb;
            int potSize = 0;

            //add users to game:
            UserBridge.AddUserToGameRoomAsPlayer(UserId, RoomId, 10);
            UserBridge.AddUserToGameRoomAsPlayer(user3, RoomId, 10);
            UserBridge.AddUserToGameRoomAsPlayer(user4, RoomId, 10);

            for (int i = 0; i < userList.Count; i++)
            {
                Assert.AreEqual(moneyList[i] - 10, UserBridge.GetUserMoney(userList[i]));
                moneyList[i] -= 10;
                Assert.AreEqual(chipsList[i] + 10, UserBridge.GetUserChips(userList[i], RoomId));
                chipsList[i] += 10;
            }

            //pot should be empty
            Assert.AreEqual(0, GameBridge.GetPotSize(RoomId));

            GameBridge.StartGame(RoomId);

            potSize += smallBlind + bb;

            Assert.True(GameBridge.IsRoomActive(RoomId));
            Assert.AreEqual(user2, GameBridge.GetDealerId(RoomId));
            Assert.AreEqual(UserId, GameBridge.GetSbId(RoomId));
            Assert.AreEqual(user3, GameBridge.GetBbId(RoomId));
            Assert.AreEqual(52 - 6, GameBridge.GetDeckSize(RoomId));
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            //sb and bb paied:
            Assert.AreEqual(chipsList[1] - smallBlind, UserBridge.GetUserChips(userList[1], RoomId));
            chipsList[1] -= smallBlind;
            potSize += smallBlind;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));
            
            Assert.AreEqual(chipsList[2] - currMinBet, UserBridge.GetUserChips(userList[2], RoomId));
            chipsList[2] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[3], GameBridge.GetCurrPlayer(RoomId)); //user1 is sb, user2 is dealr, user3 is bb => user4 starts
            Assert.True(GameBridge.Call(userList[3], RoomId, currMinBet)); //user4 calls equal to bb
            Assert.AreEqual(chipsList[3] - currMinBet, UserBridge.GetUserChips(userList[3], RoomId));
            chipsList[3] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[0], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Raise(userList[0], RoomId, currMinBet * 2)); //user2 raises
            currMinBet *= 2;
            Assert.AreEqual(chipsList[0] - currMinBet, UserBridge.GetUserChips(userList[1], RoomId));
            chipsList[1] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[1], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Call(userList[1], RoomId, currMinBet)); //user1 calls
            Assert.AreEqual(chipsList[1] - currMinBet, UserBridge.GetUserChips(userList[1], RoomId));
            chipsList[1] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[2], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Fold(userList[2], RoomId)); //user3 folds
            Assert.AreEqual(chipsList[2], UserBridge.GetUserChips(userList[2], RoomId));
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[3], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Call(userList[3], RoomId, currMinBet)); //user4 calls
            Assert.AreEqual(chipsList[3] - currMinBet, UserBridge.GetUserChips(userList[3], RoomId));
            chipsList[3] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            //deal flop:
            Assert.True(GameBridge.DealFlop(RoomId));
            Assert.AreEqual(43, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[1], GameBridge.GetCurrPlayer(RoomId)); //user1 is left of dealer
            Assert.True(GameBridge.Check(userList[1], RoomId));
            Assert.AreEqual(chipsList[1], UserBridge.GetUserChips(userList[1], RoomId));
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[3], GameBridge.GetCurrPlayer(RoomId)); //user3 folded so user4 is next
            Assert.True(GameBridge.Call(userList[3], RoomId, currMinBet)); //user4 calls
            Assert.AreEqual(chipsList[3] - currMinBet, UserBridge.GetUserChips(userList[3], RoomId));
            chipsList[3] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[0], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Call(userList[0], RoomId, currMinBet)); //user2 calls
            Assert.AreEqual(chipsList[0] - currMinBet, UserBridge.GetUserChips(userList[0], RoomId));
            chipsList[0] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            //deal turn:
            Assert.True(GameBridge.DealSingleCardToTable(RoomId));
            Assert.AreEqual(42, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[1], GameBridge.GetCurrPlayer(RoomId)); //user1 is left of dealer
            Assert.True(GameBridge.Check(userList[1], RoomId));
            Assert.AreEqual(chipsList[1], UserBridge.GetUserChips(userList[1], RoomId));
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[3], GameBridge.GetCurrPlayer(RoomId)); //user3 folded so user4 is next
            Assert.True(GameBridge.Call(userList[3], RoomId, currMinBet)); //user4 calls
            Assert.AreEqual(chipsList[3] - currMinBet, UserBridge.GetUserChips(userList[3], RoomId));
            chipsList[3] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[0], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Call(userList[0], RoomId, currMinBet)); //user2 calls
            Assert.AreEqual(chipsList[0] - currMinBet, UserBridge.GetUserChips(userList[0], RoomId));
            chipsList[0] -= currMinBet;
            potSize += currMinBet;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            //deal River:
            Assert.True(GameBridge.DealSingleCardToTable(RoomId));
            Assert.AreEqual(41, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[1], GameBridge.GetCurrPlayer(RoomId)); //user1 is left of dealer
            Assert.True(GameBridge.Raise(userList[1], RoomId, bb));
            Assert.AreEqual(chipsList[1] - bb, UserBridge.GetUserChips(userList[1], RoomId));
            chipsList[0] -= bb;
            potSize += bb;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));

            Assert.AreEqual(userList[3], GameBridge.GetCurrPlayer(RoomId)); //user3 folded so user4 is next
            Assert.True(GameBridge.Fold(userList[3], RoomId)); //user4 folds
            Assert.AreEqual(chipsList[3], UserBridge.GetUserChips(userList[3], RoomId));

            Assert.AreEqual(userList[0], GameBridge.GetCurrPlayer(RoomId));
            Assert.True(GameBridge.Call(userList[0], RoomId, currMinBet)); //user2 calls
            Assert.AreEqual(chipsList[0] - bb, UserBridge.GetUserChips(userList[0], RoomId));
            chipsList[0] -= bb;
            potSize += bb;
            Assert.AreEqual(potSize, GameBridge.GetPotSize(RoomId));
        }

    }
}
